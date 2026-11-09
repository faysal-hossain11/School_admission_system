using System.Data;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using SchoolAdmission.Api.Data;
using SchoolAdmission.Api.Models;

namespace SchoolAdmission.Api.Services;

public sealed class AdmissionHoldService(
    AdmissionDbContext dbContext,
    TimeProvider timeProvider,
    AdmissionHoldOptions options) : IAdmissionHoldService
{
    private const string DuplicateApplicationConstraint =
        "IX_AdmissionHolds_AdmissionClassId_StudentRegistrationId";

    public async Task<AdmissionHoldResult<AdmissionHoldData>> CreateAsync(
        string classId,
        string studentId,
        CancellationToken cancellationToken)
    {
        await using var transaction = await dbContext.Database.BeginTransactionAsync(
            IsolationLevel.ReadCommitted,
            cancellationToken);

        var admissionClass = await LockClassAsync(classId, cancellationToken);
        if (admissionClass is null)
        {
            return AdmissionHoldResult<AdmissionHoldData>.Failure(AdmissionHoldError.ClassNotFound);
        }

        var now = timeProvider.GetUtcNow();
        await ExpireClassHoldsAsync(classId, now, cancellationToken);

        var hasExistingApplication = await dbContext.AdmissionHolds.AnyAsync(
            hold => hold.AdmissionClassId == classId &&
                    hold.StudentRegistrationId == studentId &&
                    (hold.Status == AdmissionHoldStatus.HELD ||
                     hold.Status == AdmissionHoldStatus.CONFIRMED),
            cancellationToken);
        if (hasExistingApplication)
        {
            return AdmissionHoldResult<AdmissionHoldData>.Failure(
                AdmissionHoldError.DuplicateApplication);
        }

        var heldSeats = await dbContext.AdmissionHolds.CountAsync(
            hold => hold.AdmissionClassId == classId &&
                    hold.Status == AdmissionHoldStatus.HELD &&
                    hold.ExpiresAt > now,
            cancellationToken);
        var confirmedSeats = await dbContext.AdmissionHolds.CountAsync(
            hold => hold.AdmissionClassId == classId &&
                    hold.Status == AdmissionHoldStatus.CONFIRMED,
            cancellationToken);

        if (heldSeats + confirmedSeats >= admissionClass.TotalSeats)
        {
            return AdmissionHoldResult<AdmissionHoldData>.Failure(
                AdmissionHoldError.NoAvailableSeats);
        }

        var hold = new AdmissionHold
        {
            AdmissionClassId = classId,
            StudentRegistrationId = studentId,
            Status = AdmissionHoldStatus.HELD,
            CreatedAt = now,
            ExpiresAt = now.Add(options.Duration)
        };

        dbContext.AdmissionHolds.Add(hold);
        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception) when (IsDuplicateApplication(exception))
        {
            return AdmissionHoldResult<AdmissionHoldData>.Failure(
                AdmissionHoldError.DuplicateApplication);
        }

        await transaction.CommitAsync(cancellationToken);

        return AdmissionHoldResult<AdmissionHoldData>.Success(
            new AdmissionHoldData(
                hold.Id,
                hold.AdmissionClassId,
                hold.StudentRegistrationId,
                hold.Status,
                hold.ExpiresAt));
    }

    public async Task<AdmissionHoldData?> GetAsync(
        string holdId,
        CancellationToken cancellationToken)
    {
        var now = timeProvider.GetUtcNow();

        return await dbContext.AdmissionHolds
            .AsNoTracking()
            .Where(hold => hold.Id == holdId)
            .Select(hold => new AdmissionHoldData(
                hold.Id,
                hold.AdmissionClassId,
                hold.StudentRegistrationId,
                hold.Status == AdmissionHoldStatus.HELD && hold.ExpiresAt <= now
                    ? AdmissionHoldStatus.EXPIRED
                    : hold.Status,
                hold.ExpiresAt))
            .SingleOrDefaultAsync(cancellationToken);
    }

    public async Task<AdmissionHoldResult<ConfirmAdmissionHoldData>> ConfirmAsync(
        string holdId,
        string studentId,
        CancellationToken cancellationToken)
    {
        await using var transaction = await dbContext.Database.BeginTransactionAsync(
            IsolationLevel.ReadCommitted,
            cancellationToken);

        var classId = await GetClassIdForHoldAsync(holdId, cancellationToken);
        if (classId is null)
        {
            return AdmissionHoldResult<ConfirmAdmissionHoldData>.Failure(
                AdmissionHoldError.HoldNotFound);
        }

        if (await LockClassAsync(classId, cancellationToken) is null)
        {
            return AdmissionHoldResult<ConfirmAdmissionHoldData>.Failure(
                AdmissionHoldError.HoldNotFound);
        }

        var now = timeProvider.GetUtcNow();
        await ExpireClassHoldsAsync(classId, now, cancellationToken);

        var hold = await dbContext.AdmissionHolds.SingleOrDefaultAsync(
            item => item.Id == holdId,
            cancellationToken);
        if (hold is null)
        {
            return AdmissionHoldResult<ConfirmAdmissionHoldData>.Failure(
                AdmissionHoldError.HoldNotFound);
        }

        if (hold.StudentRegistrationId != studentId)
        {
            return AdmissionHoldResult<ConfirmAdmissionHoldData>.Failure(
                AdmissionHoldError.Forbidden);
        }

        if (hold.Status == AdmissionHoldStatus.CONFIRMED)
        {
            await transaction.CommitAsync(cancellationToken);
            return AdmissionHoldResult<ConfirmAdmissionHoldData>.Success(
                new ConfirmAdmissionHoldData(hold.Id, hold.Status));
        }

        if (hold.Status == AdmissionHoldStatus.EXPIRED || hold.ExpiresAt <= now)
        {
            return AdmissionHoldResult<ConfirmAdmissionHoldData>.Failure(
                AdmissionHoldError.Expired);
        }

        if (hold.Status != AdmissionHoldStatus.HELD)
        {
            return AdmissionHoldResult<ConfirmAdmissionHoldData>.Failure(
                AdmissionHoldError.InvalidState);
        }

        hold.Status = AdmissionHoldStatus.CONFIRMED;
        await dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return AdmissionHoldResult<ConfirmAdmissionHoldData>.Success(
            new ConfirmAdmissionHoldData(hold.Id, hold.Status));
    }

    public async Task<AdmissionHoldResult<AdmissionHoldCancellationCompleted>> CancelAsync(
        string holdId,
        string studentId,
        CancellationToken cancellationToken)
    {
        await using var transaction = await dbContext.Database.BeginTransactionAsync(
            IsolationLevel.ReadCommitted,
            cancellationToken);

        var classId = await GetClassIdForHoldAsync(holdId, cancellationToken);
        if (classId is null)
        {
            return AdmissionHoldResult<AdmissionHoldCancellationCompleted>.Failure(
                AdmissionHoldError.HoldNotFound);
        }

        if (await LockClassAsync(classId, cancellationToken) is null)
        {
            return AdmissionHoldResult<AdmissionHoldCancellationCompleted>.Failure(
                AdmissionHoldError.HoldNotFound);
        }

        var now = timeProvider.GetUtcNow();
        await ExpireClassHoldsAsync(classId, now, cancellationToken);

        var hold = await dbContext.AdmissionHolds.SingleOrDefaultAsync(
            item => item.Id == holdId,
            cancellationToken);
        if (hold is null)
        {
            return AdmissionHoldResult<AdmissionHoldCancellationCompleted>.Failure(
                AdmissionHoldError.HoldNotFound);
        }

        if (hold.StudentRegistrationId != studentId)
        {
            return AdmissionHoldResult<AdmissionHoldCancellationCompleted>.Failure(
                AdmissionHoldError.Forbidden);
        }

        if (hold.Status == AdmissionHoldStatus.EXPIRED || hold.ExpiresAt <= now)
        {
            return AdmissionHoldResult<AdmissionHoldCancellationCompleted>.Failure(
                AdmissionHoldError.Expired);
        }

        if (hold.Status != AdmissionHoldStatus.HELD)
        {
            return AdmissionHoldResult<AdmissionHoldCancellationCompleted>.Failure(
                AdmissionHoldError.InvalidState);
        }

        hold.Status = AdmissionHoldStatus.CANCELLED;
        await dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return AdmissionHoldResult<AdmissionHoldCancellationCompleted>.Success(
            new AdmissionHoldCancellationCompleted());
    }

    public async Task<IReadOnlyList<StudentAdmissionHoldData>> GetForStudentAsync(
        string studentId,
        CancellationToken cancellationToken)
    {
        var now = timeProvider.GetUtcNow();

        return await dbContext.AdmissionHolds
            .AsNoTracking()
            .Where(hold => hold.StudentRegistrationId == studentId)
            .OrderByDescending(hold => hold.CreatedAt)
            .Select(hold => new StudentAdmissionHoldData(
                hold.Id,
                hold.AdmissionClassId,
                hold.AdmissionClass.Name,
                hold.Status == AdmissionHoldStatus.HELD && hold.ExpiresAt <= now
                    ? AdmissionHoldStatus.EXPIRED
                    : hold.Status,
                hold.ExpiresAt))
            .ToListAsync(cancellationToken);
    }

    public async Task ExpireDueHoldsAsync(CancellationToken cancellationToken)
    {
        var classIds = await dbContext.AdmissionHolds
            .AsNoTracking()
            .Where(hold =>
                hold.Status == AdmissionHoldStatus.HELD &&
                hold.ExpiresAt <= timeProvider.GetUtcNow())
            .Select(hold => hold.AdmissionClassId)
            .Distinct()
            .ToListAsync(cancellationToken);

        foreach (var classId in classIds)
        {
            await using var transaction = await dbContext.Database.BeginTransactionAsync(
                IsolationLevel.ReadCommitted,
                cancellationToken);

            if (await LockClassAsync(classId, cancellationToken) is not null)
            {
                await ExpireClassHoldsAsync(
                    classId,
                    timeProvider.GetUtcNow(),
                    cancellationToken);
            }

            await transaction.CommitAsync(cancellationToken);
        }
    }

    private Task<AdmissionClass?> LockClassAsync(
        string classId,
        CancellationToken cancellationToken) =>
        dbContext.AdmissionClasses
            .FromSqlInterpolated(
                $"SELECT * FROM \"AdmissionClasses\" WHERE \"Id\" = {classId} FOR UPDATE")
            .AsNoTracking()
            .SingleOrDefaultAsync(cancellationToken);

    private Task<string?> GetClassIdForHoldAsync(
        string holdId,
        CancellationToken cancellationToken) =>
        dbContext.AdmissionHolds
            .AsNoTracking()
            .Where(hold => hold.Id == holdId)
            .Select(hold => hold.AdmissionClassId)
            .SingleOrDefaultAsync(cancellationToken);

    private Task<int> ExpireClassHoldsAsync(
        string classId,
        DateTimeOffset now,
        CancellationToken cancellationToken) =>
        dbContext.AdmissionHolds
            .Where(hold =>
                hold.AdmissionClassId == classId &&
                hold.Status == AdmissionHoldStatus.HELD &&
                hold.ExpiresAt <= now)
            .ExecuteUpdateAsync(
                setters => setters.SetProperty(
                    hold => hold.Status,
                    AdmissionHoldStatus.EXPIRED),
                cancellationToken);

    private static bool IsDuplicateApplication(DbUpdateException exception) =>
        exception.InnerException is PostgresException
        {
            SqlState: PostgresErrorCodes.UniqueViolation,
            ConstraintName: DuplicateApplicationConstraint
        };
}

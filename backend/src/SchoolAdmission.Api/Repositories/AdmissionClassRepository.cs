using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using SchoolAdmission.Api.Data;
using SchoolAdmission.Api.Models;

namespace SchoolAdmission.Api.Repositories;

public sealed class AdmissionClassRepository(AdmissionDbContext dbContext) : IAdmissionClassRepository
{
    public async Task<AdmissionClass> AddAsync(
        AdmissionClass admissionClass,
        CancellationToken cancellationToken)
    {
        dbContext.AdmissionClasses.Add(admissionClass);
        await dbContext.SaveChangesAsync(cancellationToken);

        return admissionClass;
    }

    public async Task<IReadOnlyList<AdmissionClassSummary>> GetSummariesAsync(
        CancellationToken cancellationToken)
    {
        var now = DateTimeOffset.UtcNow;

        return await dbContext.AdmissionClasses
            .AsNoTracking()
            .OrderBy(admissionClass => admissionClass.Name)
            .ThenBy(admissionClass => admissionClass.Id)
            .Select(SummaryProjection(now))
            .ToListAsync(cancellationToken);
    }

    public Task<AdmissionClassSummary?> GetSummaryAsync(
        string classId,
        CancellationToken cancellationToken)
    {
        var now = DateTimeOffset.UtcNow;

        return dbContext.AdmissionClasses
            .AsNoTracking()
            .Where(admissionClass => admissionClass.Id == classId)
            .Select(SummaryProjection(now))
            .SingleOrDefaultAsync(cancellationToken);
    }

    private static Expression<Func<AdmissionClass, AdmissionClassSummary>> SummaryProjection(
        DateTimeOffset now) =>
        admissionClass => new AdmissionClassSummary(
            admissionClass.Id,
            admissionClass.Name,
            admissionClass.TotalSeats,
            admissionClass.Holds.Count(hold =>
                hold.Status == AdmissionHoldStatus.HELD && hold.ExpiresAt > now),
            admissionClass.Holds.Count(hold =>
                hold.Status == AdmissionHoldStatus.CONFIRMED),
            admissionClass.TotalSeats
                - admissionClass.Holds.Count(hold =>
                    hold.Status == AdmissionHoldStatus.HELD && hold.ExpiresAt > now)
                - admissionClass.Holds.Count(hold =>
                    hold.Status == AdmissionHoldStatus.CONFIRMED));
}

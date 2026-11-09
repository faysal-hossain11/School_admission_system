using SchoolAdmission.Api.Models;

namespace SchoolAdmission.Api.Services;

public interface IAdmissionHoldService
{
    Task<AdmissionHoldResult<AdmissionHoldData>> CreateAsync(
        string classId,
        string studentId,
        CancellationToken cancellationToken);

    Task<AdmissionHoldData?> GetAsync(string holdId, CancellationToken cancellationToken);

    Task<AdmissionHoldResult<ConfirmAdmissionHoldData>> ConfirmAsync(
        string holdId,
        string studentId,
        CancellationToken cancellationToken);

    Task<AdmissionHoldResult<AdmissionHoldCancellationCompleted>> CancelAsync(
        string holdId,
        string studentId,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<StudentAdmissionHoldData>> GetForStudentAsync(
        string studentId,
        CancellationToken cancellationToken);

    Task ExpireDueHoldsAsync(CancellationToken cancellationToken);
}

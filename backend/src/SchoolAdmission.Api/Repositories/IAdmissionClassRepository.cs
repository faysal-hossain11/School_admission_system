using SchoolAdmission.Api.Models;

namespace SchoolAdmission.Api.Repositories;

public interface IAdmissionClassRepository
{
    Task<AdmissionClass> AddAsync(AdmissionClass admissionClass, CancellationToken cancellationToken);

    Task<IReadOnlyList<AdmissionClassSummary>> GetSummariesAsync(CancellationToken cancellationToken);

    Task<AdmissionClassSummary?> GetSummaryAsync(string classId, CancellationToken cancellationToken);
}

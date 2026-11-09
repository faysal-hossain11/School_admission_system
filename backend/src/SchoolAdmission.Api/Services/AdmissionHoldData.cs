using SchoolAdmission.Api.Models;

namespace SchoolAdmission.Api.Services;

public sealed record AdmissionHoldData(
    string HoldId,
    string ClassId,
    string StudentId,
    AdmissionHoldStatus Status,
    DateTimeOffset ExpiresAt);

public sealed record StudentAdmissionHoldData(
    string HoldId,
    string ClassId,
    string ClassName,
    AdmissionHoldStatus Status,
    DateTimeOffset ExpiresAt);

public sealed record ConfirmAdmissionHoldData(
    string HoldId,
    AdmissionHoldStatus Status);

public sealed record AdmissionHoldCancellationCompleted
{
}

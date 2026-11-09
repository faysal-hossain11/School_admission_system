namespace SchoolAdmission.Api.Contracts.Holds;

public sealed record AdmissionHoldResponse(
    string HoldId,
    string ClassId,
    string StudentId,
    string Status,
    DateTimeOffset ExpiresAt);

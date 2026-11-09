namespace SchoolAdmission.Api.Contracts.Holds;

public sealed record AdmissionHoldStatusResponse(
    string HoldId,
    string ClassId,
    string Status,
    DateTimeOffset ExpiresAt);

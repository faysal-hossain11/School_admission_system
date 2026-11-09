namespace SchoolAdmission.Api.Contracts.Holds;

public sealed record StudentAdmissionHoldResponse(
    string HoldId,
    string ClassId,
    string ClassName,
    string Status,
    DateTimeOffset ExpiresAt);

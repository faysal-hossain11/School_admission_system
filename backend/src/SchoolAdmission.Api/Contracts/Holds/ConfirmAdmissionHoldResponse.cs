namespace SchoolAdmission.Api.Contracts.Holds;

public sealed record ConfirmAdmissionHoldResponse(
    string HoldId,
    string Status);

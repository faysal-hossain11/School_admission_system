namespace SchoolAdmission.Api.Contracts.Classes;

public sealed record AdmissionClassSummaryResponse(
    string Id,
    string Name,
    int TotalSeats,
    int HeldSeats,
    int ConfirmedSeats,
    int AvailableSeats);

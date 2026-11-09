namespace SchoolAdmission.Api.Repositories;

public sealed record AdmissionClassSummary(
    string Id,
    string Name,
    int TotalSeats,
    int HeldSeats,
    int ConfirmedSeats,
    int AvailableSeats);

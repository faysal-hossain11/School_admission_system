namespace SchoolAdmission.Api.Contracts.Classes;

public sealed record AdmissionClassResponse(
    string Id,
    string Name,
    int TotalSeats);

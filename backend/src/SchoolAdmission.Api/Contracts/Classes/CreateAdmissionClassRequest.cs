using System.ComponentModel.DataAnnotations;

namespace SchoolAdmission.Api.Contracts.Classes;

public sealed class CreateAdmissionClassRequest
{
    [Required]
    public string Name { get; init; } = null!;

    [Range(1, int.MaxValue)]
    public int TotalSeats { get; init; }
}

namespace SchoolAdmission.Api.Models;

public sealed class AdmissionClass
{
    public string Id { get; private set; } = $"c_{Guid.NewGuid():N}";

    public string Name { get; set; } = null!;

    public int TotalSeats { get; set; }

    public ICollection<AdmissionHold> Holds { get; } = new List<AdmissionHold>();
}

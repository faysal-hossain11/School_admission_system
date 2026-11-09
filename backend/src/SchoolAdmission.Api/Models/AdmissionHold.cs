namespace SchoolAdmission.Api.Models;

public sealed class AdmissionHold
{
    public string Id { get; private set; } = $"h_{Guid.NewGuid():N}";

    public string AdmissionClassId { get; set; } = null!;

    public string StudentRegistrationId { get; set; } = null!;

    public AdmissionHoldStatus Status { get; set; } = AdmissionHoldStatus.HELD;

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset ExpiresAt { get; set; }

    public AdmissionClass AdmissionClass { get; set; } = null!;
}

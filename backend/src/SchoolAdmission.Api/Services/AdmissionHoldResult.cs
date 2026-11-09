namespace SchoolAdmission.Api.Services;

public enum AdmissionHoldError
{
    None,
    ClassNotFound,
    HoldNotFound,
    NoAvailableSeats,
    DuplicateApplication,
    Forbidden,
    Expired,
    InvalidState
}

public sealed record AdmissionHoldResult<T>(T? Value, AdmissionHoldError Error)
    where T : class
{
    public bool Succeeded => Error == AdmissionHoldError.None;

    public static AdmissionHoldResult<T> Success(T value) =>
        new(value, AdmissionHoldError.None);

    public static AdmissionHoldResult<T> Failure(AdmissionHoldError error) =>
        new(null, error);
}

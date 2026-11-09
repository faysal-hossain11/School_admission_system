using Microsoft.AspNetCore.Mvc;
using SchoolAdmission.Api.Contracts.Holds;
using SchoolAdmission.Api.Services;

namespace SchoolAdmission.Api.Controllers;

[ApiController]
[Route("classes/{classId}/holds")]
public sealed class ClassHoldsController(IAdmissionHoldService holdService) : ControllerBase
{
    [HttpPost]
    [ProducesResponseType<AdmissionHoldResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<AdmissionHoldResponse>> Create(
        string classId,
        [FromHeader(Name = "X-Student-Id")] string? studentId,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(studentId))
        {
            return Problem(
                statusCode: StatusCodes.Status400BadRequest,
                title: "X-Student-Id header is required.");
        }

        var result = await holdService.CreateAsync(classId, studentId, cancellationToken);
        if (!result.Succeeded)
        {
            return ToProblem(result.Error);
        }

        var hold = result.Value!;
        var response = new AdmissionHoldResponse(
            hold.HoldId,
            hold.ClassId,
            hold.StudentId,
            hold.Status.ToString(),
            hold.ExpiresAt);

        return CreatedAtAction(
            nameof(HoldsController.Get),
            "Holds",
            new { holdId = hold.HoldId },
            response);
    }

    private ObjectResult ToProblem(AdmissionHoldError error)
    {
        var (statusCode, title) = error switch
        {
            AdmissionHoldError.ClassNotFound or AdmissionHoldError.HoldNotFound =>
                (StatusCodes.Status404NotFound, "The requested admission record was not found."),
            AdmissionHoldError.NoAvailableSeats =>
                (StatusCodes.Status409Conflict, "No seats are available in this class."),
            AdmissionHoldError.DuplicateApplication =>
                (StatusCodes.Status409Conflict, "This student already has an active or confirmed application for this class."),
            AdmissionHoldError.Forbidden =>
                (StatusCodes.Status403Forbidden, "This application belongs to another student."),
            AdmissionHoldError.Expired =>
                (StatusCodes.Status409Conflict, "This application has expired."),
            AdmissionHoldError.InvalidState =>
                (StatusCodes.Status409Conflict, "This application cannot be changed in its current state."),
            _ => throw new ArgumentOutOfRangeException(nameof(error), error, "Unknown admission error.")
        };

        return Problem(statusCode: statusCode, title: title);
    }
}

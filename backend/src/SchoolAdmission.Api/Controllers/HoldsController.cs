using Microsoft.AspNetCore.Mvc;
using SchoolAdmission.Api.Contracts.Holds;
using SchoolAdmission.Api.Models;
using SchoolAdmission.Api.Services;

namespace SchoolAdmission.Api.Controllers;

[ApiController]
[Route("holds")]
public sealed class HoldsController(IAdmissionHoldService holdService) : ControllerBase
{
    [HttpGet("{holdId}")]
    [ProducesResponseType<AdmissionHoldStatusResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AdmissionHoldStatusResponse>> Get(
        string holdId,
        CancellationToken cancellationToken)
    {
        var hold = await holdService.GetAsync(holdId, cancellationToken);
        return hold is null
            ? NotFound()
            : Ok(new AdmissionHoldStatusResponse(
                hold.HoldId,
                hold.ClassId,
                hold.Status.ToString(),
                hold.ExpiresAt));
    }

    [HttpPost("{holdId}/confirm")]
    [ProducesResponseType<ConfirmAdmissionHoldResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ConfirmAdmissionHoldResponse>> Confirm(
        string holdId,
        [FromHeader(Name = "X-Student-Id")] string? studentId,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(studentId))
        {
            return Problem(
                statusCode: StatusCodes.Status400BadRequest,
                title: "X-Student-Id header is required.");
        }

        var result = await holdService.ConfirmAsync(holdId, studentId, cancellationToken);
        if (!result.Succeeded)
        {
            return ToProblem(result.Error);
        }

        return Ok(new ConfirmAdmissionHoldResponse(
            result.Value!.HoldId,
            result.Value.Status.ToString()));
    }

    [HttpDelete("{holdId}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Cancel(
        string holdId,
        [FromHeader(Name = "X-Student-Id")] string? studentId,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(studentId))
        {
            return Problem(
                statusCode: StatusCodes.Status400BadRequest,
                title: "X-Student-Id header is required.");
        }

        var result = await holdService.CancelAsync(holdId, studentId, cancellationToken);
        return result.Succeeded ? NoContent() : ToProblem(result.Error);
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

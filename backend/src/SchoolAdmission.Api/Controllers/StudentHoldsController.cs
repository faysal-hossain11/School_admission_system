using Microsoft.AspNetCore.Mvc;
using SchoolAdmission.Api.Contracts.Holds;
using SchoolAdmission.Api.Services;

namespace SchoolAdmission.Api.Controllers;

[ApiController]
[Route("me/holds")]
public sealed class StudentHoldsController(IAdmissionHoldService holdService) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<IReadOnlyList<StudentAdmissionHoldResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<IReadOnlyList<StudentAdmissionHoldResponse>>> Get(
        [FromHeader(Name = "X-Student-Id")] string? studentId,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(studentId))
        {
            return Problem(
                statusCode: StatusCodes.Status400BadRequest,
                title: "X-Student-Id header is required.");
        }

        var holds = await holdService.GetForStudentAsync(studentId, cancellationToken);
        return Ok(holds.Select(hold => new StudentAdmissionHoldResponse(
            hold.HoldId,
            hold.ClassId,
            hold.ClassName,
            hold.Status.ToString(),
            hold.ExpiresAt)).ToArray());
    }
}

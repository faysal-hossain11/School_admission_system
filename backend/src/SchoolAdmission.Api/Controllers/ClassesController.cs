using Microsoft.AspNetCore.Mvc;
using SchoolAdmission.Api.Contracts.Classes;
using SchoolAdmission.Api.Models;
using SchoolAdmission.Api.Repositories;

namespace SchoolAdmission.Api.Controllers;

[ApiController]
[Route("classes")]
public sealed class ClassesController(IAdmissionClassRepository repository) : ControllerBase
{
    [HttpPost]
    [ProducesResponseType<AdmissionClassResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<AdmissionClassResponse>> Create(
        CreateAdmissionClassRequest request,
        CancellationToken cancellationToken)
    {
        var admissionClass = await repository.AddAsync(
            new AdmissionClass
            {
                Name = request.Name,
                TotalSeats = request.TotalSeats
            },
            cancellationToken);

        var response = new AdmissionClassResponse(
            admissionClass.Id,
            admissionClass.Name,
            admissionClass.TotalSeats);

        return CreatedAtAction(
            nameof(GetById),
            new { classId = admissionClass.Id },
            response);
    }

    [HttpGet]
    [ProducesResponseType<IReadOnlyList<AdmissionClassSummaryResponse>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<AdmissionClassSummaryResponse>>> GetAll(
        CancellationToken cancellationToken)
    {
        var summaries = await repository.GetSummariesAsync(cancellationToken);
        return Ok(summaries.Select(ToResponse).ToArray());
    }

    [HttpGet("{classId}")]
    [ProducesResponseType<AdmissionClassSummaryResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AdmissionClassSummaryResponse>> GetById(
        string classId,
        CancellationToken cancellationToken)
    {
        var summary = await repository.GetSummaryAsync(classId, cancellationToken);
        return summary is null ? NotFound() : Ok(ToResponse(summary));
    }

    private static AdmissionClassSummaryResponse ToResponse(AdmissionClassSummary summary) =>
        new(
            summary.Id,
            summary.Name,
            summary.TotalSeats,
            summary.HeldSeats,
            summary.ConfirmedSeats,
            summary.AvailableSeats);
}

using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Net;
using Utano.Module.ClinicalNotes.Domain.Interfaces;
using Utano.Module.Core.Services;

namespace Utano.Module.ClinicalNotes.Features.Visits.TriageVisit;

[ApiController]
[Route("api/visits")]
[Authorize]
public class TriageVisitEndpoint(ISender sender) : ControllerBase
{
    [HttpPut("{id:guid}/triage")]
    [ProducesResponseType((int)HttpStatusCode.NoContent)]
    [ProducesResponseType((int)HttpStatusCode.NotFound)]
    [EndpointSummary("Record triage vitals and advance status to Triaged")]
    [Tags("Clinical Notes Module")]
    public async Task<IActionResult> Triage(Guid id, [FromBody] TriageVisitBody body, CancellationToken ct)
    {
        var ok = await sender.Send(new TriageVisitCommand(id,
            body.BloodPressureSystolic, body.BloodPressureDiastolic,
            body.WeightKg, body.HeightCm, body.TemperatureCelsius,
            body.PulseRate, body.OxygenSaturation, body.ChiefComplaint), ct);
        return ok ? NoContent() : NotFound();
    }
}

public record TriageVisitBody(
    int? BloodPressureSystolic,
    int? BloodPressureDiastolic,
    decimal? WeightKg,
    decimal? HeightCm,
    decimal? TemperatureCelsius,
    int? PulseRate,
    decimal? OxygenSaturation,
    string? ChiefComplaint);

public record TriageVisitCommand(
    Guid Id,
    int? BloodPressureSystolic, int? BloodPressureDiastolic,
    decimal? WeightKg, decimal? HeightCm,
    decimal? TemperatureCelsius, int? PulseRate, decimal? OxygenSaturation,
    string? ChiefComplaint) : IRequest<bool>;

public class TriageVisitHandler(
    IVisitReadRepository readRepository,
    IVisitWriteRepository writeRepository,
    IAuditService auditService)
    : IRequestHandler<TriageVisitCommand, bool>
{
    public async Task<bool> Handle(TriageVisitCommand cmd, CancellationToken ct)
    {
        var visit = await readRepository.GetByIdAsync(cmd.Id, ct);
        if (visit is null) return false;

        visit.Triage(
            cmd.BloodPressureSystolic, cmd.BloodPressureDiastolic,
            cmd.WeightKg, cmd.HeightCm, cmd.TemperatureCelsius,
            cmd.PulseRate, cmd.OxygenSaturation, cmd.ChiefComplaint);

        await writeRepository.UpdateAsync(visit, ct);
        await auditService.LogAsync("Visit", visit.Id.ToString(), "Triaged",
            $"Patient: {visit.PatientName}", ct);
        return true;
    }
}
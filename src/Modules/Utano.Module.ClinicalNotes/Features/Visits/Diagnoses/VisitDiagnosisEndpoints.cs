using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Net;
using Utano.Module.ClinicalNotes.DatabaseMappings;
using Utano.Module.ClinicalNotes.Domain.Entities;
using Utano.Module.Core.Services;

namespace Utano.Module.ClinicalNotes.Features.Visits.Diagnoses;

[ApiController]
[Authorize]
[Route("api/visits/{visitId:guid}/diagnoses")]
public class VisitDiagnosisEndpoints(ISender sender) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(List<DiagnosisRow>), (int)HttpStatusCode.OK)]
    [EndpointSummary("List ICD-10 diagnoses for a visit")]
    [Tags("Clinical Notes Module")]
    public async Task<IActionResult> GetAll(Guid visitId, CancellationToken ct)
        => Ok(await sender.Send(new GetDiagnosesQuery(visitId), ct));

    [HttpPost]
    [ProducesResponseType(typeof(DiagnosisRow), (int)HttpStatusCode.Created)]
    [EndpointSummary("Add an ICD-10 diagnosis to the visit")]
    [Tags("Clinical Notes Module")]
    public async Task<IActionResult> Add(Guid visitId, [FromBody] AddDiagnosisBody body, CancellationToken ct)
    {
        var result = await sender.Send(new AddDiagnosisCommand(visitId, body.IcdCode, body.Description, body.IsPrimary), ct);
        return CreatedAtAction(nameof(GetAll), new { visitId }, result);
    }

    [HttpPatch("{diagnosisId:guid}/set-primary")]
    [ProducesResponseType((int)HttpStatusCode.NoContent)]
    [ProducesResponseType((int)HttpStatusCode.NotFound)]
    [EndpointSummary("Set a diagnosis as the primary ICD-10 code for the visit")]
    [Tags("Clinical Notes Module")]
    public async Task<IActionResult> SetPrimary(Guid visitId, Guid diagnosisId, CancellationToken ct)
    {
        var ok = await sender.Send(new SetPrimaryDiagnosisCommand(visitId, diagnosisId), ct);
        return ok ? NoContent() : NotFound();
    }

    [HttpDelete("{diagnosisId:guid}")]
    [ProducesResponseType((int)HttpStatusCode.NoContent)]
    [ProducesResponseType((int)HttpStatusCode.NotFound)]
    [EndpointSummary("Remove a diagnosis from the visit")]
    [Tags("Clinical Notes Module")]
    public async Task<IActionResult> Remove(Guid visitId, Guid diagnosisId, CancellationToken ct)
    {
        var ok = await sender.Send(new RemoveDiagnosisCommand(visitId, diagnosisId), ct);
        return ok ? NoContent() : NotFound();
    }
}

public record AddDiagnosisBody(string IcdCode, string Description, bool IsPrimary = false);
public record DiagnosisRow(Guid Id, string IcdCode, string Description, bool IsPrimary, DateTimeOffset CreatedAt);

// ─── Get ────────────────────────────────────────────────────────────────────

public record GetDiagnosesQuery(Guid VisitId) : IRequest<List<DiagnosisRow>>;

public class GetDiagnosesHandler(ClinicalNotesDbContext db)
    : IRequestHandler<GetDiagnosesQuery, List<DiagnosisRow>>
{
    public async Task<List<DiagnosisRow>> Handle(GetDiagnosesQuery q, CancellationToken ct)
        => await db.VisitDiagnoses
            .Where(d => d.VisitId == q.VisitId)
            .OrderByDescending(d => d.IsPrimary).ThenBy(d => d.CreatedAt)
            .Select(d => new DiagnosisRow(d.Id, d.IcdCode, d.Description, d.IsPrimary, d.CreatedAt))
            .ToListAsync(ct);
}

// ─── Add ────────────────────────────────────────────────────────────────────

public record AddDiagnosisCommand(Guid VisitId, string IcdCode, string Description, bool IsPrimary)
    : IRequest<DiagnosisRow>;

public class AddDiagnosisHandler(ClinicalNotesDbContext db, ICurrentUserService currentUser)
    : IRequestHandler<AddDiagnosisCommand, DiagnosisRow>
{
    public async Task<DiagnosisRow> Handle(AddDiagnosisCommand cmd, CancellationToken ct)
    {
        // Ensure only one primary per visit
        if (cmd.IsPrimary)
            await db.VisitDiagnoses
                .Where(d => d.VisitId == cmd.VisitId && d.IsPrimary)
                .ExecuteUpdateAsync(s => s.SetProperty(d => d.IsPrimary, false), ct);

        var diagnosis = VisitDiagnosis.Create(
            currentUser.PracticeId, cmd.VisitId,
            cmd.IcdCode, cmd.Description, cmd.IsPrimary);

        db.VisitDiagnoses.Add(diagnosis);
        await db.SaveChangesAsync(ct);

        return new DiagnosisRow(diagnosis.Id, diagnosis.IcdCode, diagnosis.Description, diagnosis.IsPrimary, diagnosis.CreatedAt);
    }
}

// ─── Set Primary ─────────────────────────────────────────────────────────────

public record SetPrimaryDiagnosisCommand(Guid VisitId, Guid DiagnosisId) : IRequest<bool>;

public class SetPrimaryDiagnosisHandler(ClinicalNotesDbContext db)
    : IRequestHandler<SetPrimaryDiagnosisCommand, bool>
{
    public async Task<bool> Handle(SetPrimaryDiagnosisCommand cmd, CancellationToken ct)
    {
        var diagnosis = await db.VisitDiagnoses
            .FirstOrDefaultAsync(d => d.Id == cmd.DiagnosisId && d.VisitId == cmd.VisitId, ct);
        if (diagnosis is null) return false;

        await db.VisitDiagnoses
            .Where(d => d.VisitId == cmd.VisitId && d.IsPrimary)
            .ExecuteUpdateAsync(s => s.SetProperty(d => d.IsPrimary, false), ct);

        diagnosis.SetPrimary(true);
        await db.SaveChangesAsync(ct);
        return true;
    }
}

// ─── Remove ─────────────────────────────────────────────────────────────────

public record RemoveDiagnosisCommand(Guid VisitId, Guid DiagnosisId) : IRequest<bool>;

public class RemoveDiagnosisHandler(ClinicalNotesDbContext db)
    : IRequestHandler<RemoveDiagnosisCommand, bool>
{
    public async Task<bool> Handle(RemoveDiagnosisCommand cmd, CancellationToken ct)
    {
        var diagnosis = await db.VisitDiagnoses
            .FirstOrDefaultAsync(d => d.Id == cmd.DiagnosisId && d.VisitId == cmd.VisitId, ct);
        if (diagnosis is null) return false;

        db.VisitDiagnoses.Remove(diagnosis);
        await db.SaveChangesAsync(ct);
        return true;
    }
}

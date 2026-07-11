using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Net;
using Utano.Module.ClinicalNotes.DatabaseMappings;
using Utano.Module.ClinicalNotes.Domain.Entities;
using Utano.Module.Core.Exceptions;
using Utano.Module.Core.Services;

namespace Utano.Module.ClinicalNotes.Features.Visits.Procedures;

[ApiController]
[Authorize]
[Route("api/visits/{visitId:guid}/procedures")]
public class VisitProcedureEndpoints(ISender sender) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(List<ProcedureRow>), (int)HttpStatusCode.OK)]
    [EndpointSummary("List procedures documented by the doctor for a visit")]
    [Tags("Clinical Notes Module")]
    public async Task<IActionResult> GetAll(Guid visitId, CancellationToken ct)
        => Ok(await sender.Send(new GetProceduresQuery(visitId), ct));

    [HttpPost]
    [ProducesResponseType(typeof(ProcedureRow), (int)HttpStatusCode.Created)]
    [EndpointSummary("Doctor documents a procedure performed during the visit")]
    [Tags("Clinical Notes Module")]
    public async Task<IActionResult> Add(Guid visitId, [FromBody] AddProcedureBody body, CancellationToken ct)
    {
        var result = await sender.Send(new AddProcedureCommand(visitId, body.ServiceItemId, body.Name, body.NhrplCode, body.Notes), ct);
        return CreatedAtAction(nameof(GetAll), new { visitId }, result);
    }

    [HttpDelete("{procedureId:guid}")]
    [ProducesResponseType((int)HttpStatusCode.NoContent)]
    [ProducesResponseType((int)HttpStatusCode.NotFound)]
    [EndpointSummary("Remove a procedure (only if not yet posted to invoice)")]
    [Tags("Clinical Notes Module")]
    public async Task<IActionResult> Remove(Guid visitId, Guid procedureId, CancellationToken ct)
    {
        var ok = await sender.Send(new RemoveProcedureCommand(visitId, procedureId), ct);
        return ok ? NoContent() : NotFound();
    }

    [HttpPut("{procedureId:guid}/post-to-invoice")]
    [ProducesResponseType((int)HttpStatusCode.NoContent)]
    [ProducesResponseType((int)HttpStatusCode.NotFound)]
    [EndpointSummary("Billing staff posts a documented procedure as an invoice line item")]
    [Tags("Clinical Notes Module")]
    public async Task<IActionResult> PostToInvoice(Guid visitId, Guid procedureId, CancellationToken ct)
    {
        var ok = await sender.Send(new PostProcedureToInvoiceCommand(visitId, procedureId), ct);
        return ok ? NoContent() : NotFound();
    }
}

public record AddProcedureBody(Guid ServiceItemId, string Name, string? NhrplCode, string? Notes);
public record ProcedureRow(Guid Id, Guid ServiceItemId, string Name, string? NhrplCode, string? Notes, bool PostedToInvoice, DateTimeOffset CreatedAt);

// ─── Get ────────────────────────────────────────────────────────────────────

public record GetProceduresQuery(Guid VisitId) : IRequest<List<ProcedureRow>>;

public class GetProceduresHandler(ClinicalNotesDbContext db)
    : IRequestHandler<GetProceduresQuery, List<ProcedureRow>>
{
    public async Task<List<ProcedureRow>> Handle(GetProceduresQuery q, CancellationToken ct)
        => await db.VisitProcedures
            .Where(p => p.VisitId == q.VisitId)
            .OrderBy(p => p.CreatedAt)
            .Select(p => new ProcedureRow(p.Id, p.ServiceItemId, p.Name, p.NhrplCode, p.Notes, p.PostedToInvoice, p.CreatedAt))
            .ToListAsync(ct);
}

// ─── Add ────────────────────────────────────────────────────────────────────

public record AddProcedureCommand(Guid VisitId, Guid ServiceItemId, string Name, string? NhrplCode, string? Notes)
    : IRequest<ProcedureRow>;

public class AddProcedureHandler(ClinicalNotesDbContext db, ICurrentUserService currentUser)
    : IRequestHandler<AddProcedureCommand, ProcedureRow>
{
    public async Task<ProcedureRow> Handle(AddProcedureCommand cmd, CancellationToken ct)
    {
        var procedure = VisitProcedure.Create(
            currentUser.PracticeId, cmd.VisitId, cmd.ServiceItemId,
            cmd.Name, cmd.NhrplCode, cmd.Notes);

        db.VisitProcedures.Add(procedure);
        await db.SaveChangesAsync(ct);

        return new ProcedureRow(procedure.Id, procedure.ServiceItemId, procedure.Name,
            procedure.NhrplCode, procedure.Notes, procedure.PostedToInvoice, procedure.CreatedAt);
    }
}

// ─── Remove ─────────────────────────────────────────────────────────────────

public record RemoveProcedureCommand(Guid VisitId, Guid ProcedureId) : IRequest<bool>;

public class RemoveProcedureHandler(ClinicalNotesDbContext db)
    : IRequestHandler<RemoveProcedureCommand, bool>
{
    public async Task<bool> Handle(RemoveProcedureCommand cmd, CancellationToken ct)
    {
        var procedure = await db.VisitProcedures
            .FirstOrDefaultAsync(p => p.Id == cmd.ProcedureId && p.VisitId == cmd.VisitId, ct);
        if (procedure is null) return false;
        if (procedure.PostedToInvoice)
            throw new UtanoDomainException("Cannot remove a procedure that has already been posted to the invoice.");

        db.VisitProcedures.Remove(procedure);
        await db.SaveChangesAsync(ct);
        return true;
    }
}

// ─── Post to Invoice ─────────────────────────────────────────────────────────

public record PostProcedureToInvoiceCommand(Guid VisitId, Guid ProcedureId) : IRequest<bool>;

public class PostProcedureToInvoiceHandler(
    ClinicalNotesDbContext db,
    ICurrentUserService currentUser,
    IBillingService billingService)
    : IRequestHandler<PostProcedureToInvoiceCommand, bool>
{
    public async Task<bool> Handle(PostProcedureToInvoiceCommand cmd, CancellationToken ct)
    {
        var procedure = await db.VisitProcedures
            .FirstOrDefaultAsync(p => p.Id == cmd.ProcedureId && p.VisitId == cmd.VisitId, ct);
        if (procedure is null) return false;
        if (procedure.PostedToInvoice) return true;

        var visit = await db.Visits.FirstOrDefaultAsync(v => v.Id == cmd.VisitId, ct);
        if (visit is null) return false;

        await billingService.AddServiceLineItemAsync(
            currentUser.PracticeId,
            visit.Id,
            visit.PatientId,
            visit.PatientName,
            visit.DoctorId,
            visit.DoctorName,
            procedure.Name,
            0, // price is looked up by billing service via ServiceItemId — set to 0 here as placeholder
            "Procedure",
            procedure.ServiceItemId,
            ct);

        procedure.MarkPostedToInvoice();
        await db.SaveChangesAsync(ct);
        return true;
    }
}

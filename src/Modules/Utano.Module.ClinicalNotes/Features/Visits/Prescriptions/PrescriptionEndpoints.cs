using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Net;
using Utano.Module.ClinicalNotes.DatabaseMappings;
using Utano.Module.ClinicalNotes.Domain.Entities;
using Utano.Module.ClinicalNotes.Domain.Enums;
using Utano.Module.Core.Exceptions;
using Utano.Module.Core.Services;

namespace Utano.Module.ClinicalNotes.Features.Visits.Prescriptions;

[ApiController]
[Authorize]
[Route("api/visits/{visitId:guid}/prescriptions")]
public class PrescriptionEndpoints(ISender sender) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(List<PrescriptionRow>), (int)HttpStatusCode.OK)]
    [EndpointSummary("List prescriptions for a visit")]
    [Tags("ClinicalNotes Module")]
    public async Task<IActionResult> GetAll(Guid visitId, CancellationToken ct)
    {
        var result = await sender.Send(new GetPrescriptionsQuery(visitId), ct);
        return Ok(result);
    }

    [HttpPost]
    [ProducesResponseType(typeof(PrescriptionRow), (int)HttpStatusCode.Created)]
    [ProducesResponseType((int)HttpStatusCode.NotFound)]
    [EndpointSummary("Add a prescription to a visit — doctor selects from inventory, dispensary fulfils later")]
    [Tags("ClinicalNotes Module")]
    public async Task<IActionResult> Add(Guid visitId, [FromBody] AddPrescriptionBody body, CancellationToken ct)
    {
        var result = await sender.Send(new AddPrescriptionCommand(visitId, body.StockItemId, body.Quantity, body.DosageInstructions), ct);
        if (result is null) return NotFound();
        return CreatedAtAction(nameof(GetAll), new { visitId }, result);
    }

    [HttpPut("{prescriptionId:guid}/dispense")]
    [ProducesResponseType(typeof(DispenseOutcome), (int)HttpStatusCode.OK)]
    [ProducesResponseType((int)HttpStatusCode.NotFound)]
    [EndpointSummary("Dispense a prescription. Pass QuantityOverride to partially fulfil; omit to auto-compute from stock.")]
    [Tags("ClinicalNotes Module")]
    public async Task<IActionResult> Dispense(Guid visitId, Guid prescriptionId, [FromBody] DispenseBody? body, CancellationToken ct)
    {
        var result = await sender.Send(new DispensePrescriptionCommand(visitId, prescriptionId, body?.QuantityOverride), ct);
        if (result is null) return NotFound();
        return Ok(result);
    }

    [HttpDelete("{prescriptionId:guid}")]
    [ProducesResponseType((int)HttpStatusCode.NoContent)]
    [ProducesResponseType((int)HttpStatusCode.NotFound)]
    [EndpointSummary("Remove a pending prescription")]
    [Tags("ClinicalNotes Module")]
    public async Task<IActionResult> Remove(Guid visitId, Guid prescriptionId, CancellationToken ct)
    {
        var ok = await sender.Send(new RemovePrescriptionCommand(visitId, prescriptionId), ct);
        return ok ? NoContent() : NotFound();
    }
}

public record DispenseBody(decimal? QuantityOverride = null);

public record AddPrescriptionBody(
    Guid StockItemId,
    decimal Quantity,
    string? DosageInstructions = null);

public record PrescriptionRow(
    Guid Id,
    string Description,
    decimal Quantity,
    decimal? QuantityDispensed,
    string? DosageInstructions,
    string Status,
    Guid StockItemId,
    string StockItemName,
    DateTimeOffset CreatedAt);

public record DispenseOutcome(
    string Status,
    decimal Quantity,
    decimal? QuantityDispensed);

// ─── Get ────────────────────────────────────────────────────────────────────

public record GetPrescriptionsQuery(Guid VisitId) : IRequest<List<PrescriptionRow>>;

public class GetPrescriptionsHandler(ClinicalNotesDbContext db)
    : IRequestHandler<GetPrescriptionsQuery, List<PrescriptionRow>>
{
    public async Task<List<PrescriptionRow>> Handle(GetPrescriptionsQuery q, CancellationToken ct)
    {
        return await db.Prescriptions
            .Where(p => p.VisitId == q.VisitId)
            .OrderBy(p => p.CreatedAt)
            .Select(p => new PrescriptionRow(
                p.Id, p.Description, p.Quantity, p.QuantityDispensed,
                p.DosageInstructions, p.Status.ToString(),
                p.StockItemId, p.StockItemName, p.CreatedAt))
            .ToListAsync(ct);
    }
}

// ─── Add ────────────────────────────────────────────────────────────────────

public record AddPrescriptionCommand(
    Guid VisitId,
    Guid StockItemId,
    decimal Quantity,
    string? DosageInstructions) : IRequest<PrescriptionRow?>;

public class AddPrescriptionValidator : AbstractValidator<AddPrescriptionCommand>
{
    public AddPrescriptionValidator()
    {
        RuleFor(x => x.Quantity).GreaterThan(0);
    }
}

public class AddPrescriptionHandler(
    ClinicalNotesDbContext db,
    ICurrentUserService currentUser,
    IInventoryService inventoryService)
    : IRequestHandler<AddPrescriptionCommand, PrescriptionRow?>
{
    public async Task<PrescriptionRow?> Handle(AddPrescriptionCommand cmd, CancellationToken ct)
    {
        var visit = await db.Visits.FirstOrDefaultAsync(v => v.Id == cmd.VisitId, ct);
        if (visit is null) return null;

        var stockItem = await inventoryService.GetStockItemAsync(currentUser.PracticeId, cmd.StockItemId, ct);
        if (stockItem is null)
            throw new UtanoDomainException("Stock item not found.");

        var prescription = Prescription.Create(
            currentUser.PracticeId,
            visit.Id,
            visit.PatientId,
            visit.PatientName,
            cmd.StockItemId,
            stockItem.Name,
            cmd.Quantity,
            cmd.DosageInstructions);

        db.Prescriptions.Add(prescription);
        await db.SaveChangesAsync(ct);

        return new PrescriptionRow(
            prescription.Id, prescription.Description, prescription.Quantity,
            prescription.QuantityDispensed, prescription.DosageInstructions,
            prescription.Status.ToString(), prescription.StockItemId,
            prescription.StockItemName, prescription.CreatedAt);
    }
}

// ─── Dispense ───────────────────────────────────────────────────────────────

public record DispensePrescriptionCommand(Guid VisitId, Guid PrescriptionId, decimal? QuantityOverride = null) : IRequest<DispenseOutcome?>;

public class DispensePrescriptionHandler(
    ClinicalNotesDbContext db,
    ICurrentUserService currentUser,
    IInventoryService inventoryService,
    IBillingService billingService)
    : IRequestHandler<DispensePrescriptionCommand, DispenseOutcome?>
{
    public async Task<DispenseOutcome?> Handle(DispensePrescriptionCommand cmd, CancellationToken ct)
    {
        var prescription = await db.Prescriptions
            .FirstOrDefaultAsync(p => p.Id == cmd.PrescriptionId && p.VisitId == cmd.VisitId, ct);
        if (prescription is null) return null;

        if (prescription.Status != PrescriptionStatus.Pending)
            throw new UtanoDomainException("Prescription has already been processed.");

        var stockItem = await inventoryService.GetStockItemAsync(currentUser.PracticeId, prescription.StockItemId, ct);
        if (stockItem is null)
            throw new UtanoDomainException("Stock item not found.");

        var available = stockItem.QuantityOnHand;
        // If caller specifies a quantity (partial fulfil), cap it by available stock.
        // Otherwise auto-compute: dispense as much as possible.
        var requested = cmd.QuantityOverride ?? prescription.Quantity;
        var toDispense = Math.Min(requested, available);

        if (toDispense > 0)
        {
            await inventoryService.DispenseAsync(
                currentUser.PracticeId,
                prescription.StockItemId,
                toDispense,
                $"Prescription for {prescription.PatientName}",
                prescription.Id,
                ct);

            await billingService.AddPrescriptionLineItemAsync(
                currentUser.PracticeId,
                prescription.VisitId,
                prescription.PatientId,
                prescription.PatientName,
                prescription.Description,
                toDispense,
                stockItem.SellingPrice,
                prescription.StockItemId,
                ct);

            prescription.Dispense(toDispense);
        }
        else
        {
            prescription.MarkExternal();
        }

        await db.SaveChangesAsync(ct);

        return new DispenseOutcome(
            prescription.Status.ToString(),
            prescription.Quantity,
            prescription.QuantityDispensed);
    }
}

// ─── Remove ─────────────────────────────────────────────────────────────────

public record RemovePrescriptionCommand(Guid VisitId, Guid PrescriptionId) : IRequest<bool>;

public class RemovePrescriptionHandler(ClinicalNotesDbContext db)
    : IRequestHandler<RemovePrescriptionCommand, bool>
{
    public async Task<bool> Handle(RemovePrescriptionCommand cmd, CancellationToken ct)
    {
        var prescription = await db.Prescriptions
            .FirstOrDefaultAsync(p => p.Id == cmd.PrescriptionId && p.VisitId == cmd.VisitId, ct);
        if (prescription is null) return false;

        if (prescription.Status != PrescriptionStatus.Pending)
            throw new UtanoDomainException("Cannot remove a prescription that has already been processed.");

        db.Prescriptions.Remove(prescription);
        await db.SaveChangesAsync(ct);
        return true;
    }
}

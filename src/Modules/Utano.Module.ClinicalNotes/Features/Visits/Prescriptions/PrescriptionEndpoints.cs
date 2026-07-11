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
    [EndpointSummary("Add a prescription to a visit")]
    [Tags("ClinicalNotes Module")]
    public async Task<IActionResult> Add(Guid visitId, [FromBody] AddPrescriptionBody body, CancellationToken ct)
    {
        var result = await sender.Send(new AddPrescriptionCommand(visitId, body.Description,
            body.Quantity, body.DosageInstructions, body.DispensingType, body.StockItemId,
            body.StockItemName, body.UnitPrice), ct);
        if (result is null) return NotFound();
        return CreatedAtAction(nameof(GetAll), new { visitId }, result);
    }

    [HttpPut("{prescriptionId:guid}/dispense")]
    [ProducesResponseType((int)HttpStatusCode.NoContent)]
    [ProducesResponseType((int)HttpStatusCode.NotFound)]
    [EndpointSummary("Dispense a prescription (reduces stock and adds billing line item if BillAndDispense)")]
    [Tags("ClinicalNotes Module")]
    public async Task<IActionResult> Dispense(Guid visitId, Guid prescriptionId, CancellationToken ct)
    {
        var ok = await sender.Send(new DispensePrescriptionCommand(visitId, prescriptionId), ct);
        return ok ? NoContent() : NotFound();
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

public record AddPrescriptionBody(
    string Description,
    decimal Quantity,
    string DispensingType,
    Guid? StockItemId = null,
    string? StockItemName = null,
    string? DosageInstructions = null,
    decimal? UnitPrice = null);

public record PrescriptionRow(
    Guid Id,
    string Description,
    decimal Quantity,
    string? DosageInstructions,
    string DispensingType,
    string Status,
    Guid? StockItemId,
    string? StockItemName,
    decimal? UnitPrice,
    DateTimeOffset CreatedAt);

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
                p.Id, p.Description, p.Quantity, p.DosageInstructions,
                p.DispensingType.ToString(), p.Status.ToString(),
                p.StockItemId, p.StockItemName, null, p.CreatedAt))
            .ToListAsync(ct);
    }
}

// ─── Add ────────────────────────────────────────────────────────────────────

public record AddPrescriptionCommand(
    Guid VisitId,
    string Description,
    decimal Quantity,
    string? DosageInstructions,
    string DispensingType,
    Guid? StockItemId,
    string? StockItemName,
    decimal? UnitPrice) : IRequest<PrescriptionRow?>;

public class AddPrescriptionValidator : AbstractValidator<AddPrescriptionCommand>
{
    public AddPrescriptionValidator()
    {
        RuleFor(x => x.Description).NotEmpty().MaximumLength(500);
        RuleFor(x => x.Quantity).GreaterThan(0);
        RuleFor(x => x.DispensingType)
            .Must(t => Enum.TryParse<DispensingType>(t, true, out _))
            .WithMessage($"DispensingType must be one of: {string.Join(", ", Enum.GetNames<DispensingType>())}");
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

        var dispensingType = Enum.Parse<DispensingType>(cmd.DispensingType, ignoreCase: true);

        string? stockItemName = cmd.StockItemName;
        decimal? unitPrice = cmd.UnitPrice;

        if (dispensingType == DispensingType.BillAndDispense && cmd.StockItemId.HasValue)
        {
            var info = await inventoryService.GetStockItemAsync(currentUser.PracticeId, cmd.StockItemId.Value, ct);
            if (info is not null)
            {
                stockItemName ??= info.Name;
                unitPrice ??= info.SellingPrice;
            }
        }

        var prescription = Prescription.Create(
            currentUser.PracticeId,
            visit.Id,
            visit.PatientId,
            visit.PatientName,
            cmd.Description,
            cmd.Quantity,
            dispensingType,
            cmd.StockItemId,
            stockItemName,
            cmd.DosageInstructions);

        db.Prescriptions.Add(prescription);
        await db.SaveChangesAsync(ct);

        return new PrescriptionRow(
            prescription.Id, prescription.Description, prescription.Quantity,
            prescription.DosageInstructions, prescription.DispensingType.ToString(),
            prescription.Status.ToString(), prescription.StockItemId,
            prescription.StockItemName, unitPrice, prescription.CreatedAt);
    }
}

// ─── Dispense ───────────────────────────────────────────────────────────────

public record DispensePrescriptionCommand(Guid VisitId, Guid PrescriptionId) : IRequest<bool>;

public class DispensePrescriptionHandler(
    ClinicalNotesDbContext db,
    ICurrentUserService currentUser,
    IInventoryService inventoryService,
    IBillingService billingService)
    : IRequestHandler<DispensePrescriptionCommand, bool>
{
    public async Task<bool> Handle(DispensePrescriptionCommand cmd, CancellationToken ct)
    {
        var prescription = await db.Prescriptions
            .FirstOrDefaultAsync(p => p.Id == cmd.PrescriptionId && p.VisitId == cmd.VisitId, ct);
        if (prescription is null) return false;

        if (prescription.Status == PrescriptionStatus.Dispensed)
            throw new UtanoDomainException("Prescription has already been dispensed.");

        if (prescription.DispensingType == DispensingType.BillAndDispense)
        {
            if (!prescription.StockItemId.HasValue)
                throw new UtanoDomainException("Cannot dispense: no stock item linked.");

            var info = await inventoryService.GetStockItemAsync(currentUser.PracticeId, prescription.StockItemId.Value, ct);
            if (info is null) throw new UtanoDomainException("Stock item not found.");
            if (info.QuantityOnHand < prescription.Quantity)
                throw new UtanoDomainException($"Insufficient stock. Available: {info.QuantityOnHand} {info.Unit}.");

            await inventoryService.DispenseAsync(
                currentUser.PracticeId,
                prescription.StockItemId.Value,
                prescription.Quantity,
                $"Prescription for {prescription.PatientName}",
                prescription.Id,
                ct);

            await billingService.AddPrescriptionLineItemAsync(
                currentUser.PracticeId,
                prescription.VisitId,
                prescription.PatientId,
                prescription.PatientName,
                prescription.Description,
                prescription.Quantity,
                info.SellingPrice,
                prescription.StockItemId,
                ct);
        }

        prescription.MarkDispensed();
        await db.SaveChangesAsync(ct);
        return true;
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

        if (prescription.Status == PrescriptionStatus.Dispensed)
            throw new UtanoDomainException("Cannot remove a dispensed prescription.");

        db.Prescriptions.Remove(prescription);
        await db.SaveChangesAsync(ct);
        return true;
    }
}
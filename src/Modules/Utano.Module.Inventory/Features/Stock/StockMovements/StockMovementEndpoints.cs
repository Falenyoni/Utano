using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Net;
using Utano.Module.Inventory.DatabaseMappings;

namespace Utano.Module.Inventory.Features.Stock.StockMovements;

[ApiController]
[Route("api/inventory/stock/{id:guid}")]
[Authorize]
public class StockMovementEndpoints(ISender sender) : ControllerBase
{
    [HttpPost("receive")]
    [ProducesResponseType((int)HttpStatusCode.NoContent)]
    [ProducesResponseType((int)HttpStatusCode.NotFound)]
    [EndpointSummary("Receive stock (increase quantity)")]
    [Tags("Inventory Module")]
    public async Task<IActionResult> Receive(Guid id, [FromBody] ReceiveStockBody body, CancellationToken ct)
    {
        var ok = await sender.Send(new ReceiveStockCommand(id, body.Quantity, body.UnitCost, body.Notes), ct);
        return ok ? NoContent() : NotFound();
    }

    [HttpPost("adjust")]
    [ProducesResponseType((int)HttpStatusCode.NoContent)]
    [ProducesResponseType((int)HttpStatusCode.NotFound)]
    [EndpointSummary("Adjust stock quantity (positive or negative)")]
    [Tags("Inventory Module")]
    public async Task<IActionResult> Adjust(Guid id, [FromBody] AdjustStockBody body, CancellationToken ct)
    {
        var ok = await sender.Send(new AdjustStockCommand(id, body.Quantity, body.Notes), ct);
        return ok ? NoContent() : NotFound();
    }

    [HttpPost("dispense")]
    [ProducesResponseType((int)HttpStatusCode.NoContent)]
    [ProducesResponseType((int)HttpStatusCode.NotFound)]
    [ProducesResponseType((int)HttpStatusCode.BadRequest)]
    [EndpointSummary("Dispense stock (linked to invoice/billing)")]
    [Tags("Inventory Module")]
    public async Task<IActionResult> Dispense(Guid id, [FromBody] DispenseStockBody body, CancellationToken ct)
    {
        var ok = await sender.Send(new DispenseStockCommand(id, body.Quantity, body.Notes, body.InvoiceId), ct);
        return ok ? NoContent() : NotFound();
    }

    [HttpPut("deactivate")]
    [ProducesResponseType((int)HttpStatusCode.NoContent)]
    [ProducesResponseType((int)HttpStatusCode.NotFound)]
    [EndpointSummary("Deactivate a stock item")]
    [Tags("Inventory Module")]
    public async Task<IActionResult> Deactivate(Guid id, CancellationToken ct)
    {
        var ok = await sender.Send(new DeactivateStockItemCommand(id), ct);
        return ok ? NoContent() : NotFound();
    }
}

// --- Receive ---
public record ReceiveStockBody(decimal Quantity, decimal? UnitCost, string? Notes);
public record ReceiveStockCommand(Guid Id, decimal Quantity, decimal? UnitCost, string? Notes) : IRequest<bool>;

public class ReceiveStockValidator : AbstractValidator<ReceiveStockCommand>
{
    public ReceiveStockValidator() => RuleFor(x => x.Quantity).GreaterThan(0);
}

public class ReceiveStockHandler(InventoryDbContext db) : IRequestHandler<ReceiveStockCommand, bool>
{
    public async Task<bool> Handle(ReceiveStockCommand cmd, CancellationToken ct)
    {
        var item = await db.StockItems.FirstOrDefaultAsync(s => s.Id == cmd.Id, ct);
        if (item is null) return false;
        var txn = item.Receive(cmd.Quantity, cmd.UnitCost, cmd.Notes);
        db.StockTransactions.Add(txn);
        await db.SaveChangesAsync(ct);
        return true;
    }
}

// --- Adjust ---
public record AdjustStockBody(decimal Quantity, string? Notes);
public record AdjustStockCommand(Guid Id, decimal Quantity, string? Notes) : IRequest<bool>;

public class AdjustStockHandler(InventoryDbContext db) : IRequestHandler<AdjustStockCommand, bool>
{
    public async Task<bool> Handle(AdjustStockCommand cmd, CancellationToken ct)
    {
        var item = await db.StockItems.FirstOrDefaultAsync(s => s.Id == cmd.Id, ct);
        if (item is null) return false;
        var txn = item.Adjust(cmd.Quantity, cmd.Notes);
        db.StockTransactions.Add(txn);
        await db.SaveChangesAsync(ct);
        return true;
    }
}

// --- Dispense ---
public record DispenseStockBody(decimal Quantity, string? Notes, Guid? InvoiceId);
public record DispenseStockCommand(Guid Id, decimal Quantity, string? Notes, Guid? InvoiceId) : IRequest<bool>;

public class DispenseStockValidator : AbstractValidator<DispenseStockCommand>
{
    public DispenseStockValidator() => RuleFor(x => x.Quantity).GreaterThan(0);
}

public class DispenseStockHandler(InventoryDbContext db) : IRequestHandler<DispenseStockCommand, bool>
{
    public async Task<bool> Handle(DispenseStockCommand cmd, CancellationToken ct)
    {
        var item = await db.StockItems.FirstOrDefaultAsync(s => s.Id == cmd.Id, ct);
        if (item is null) return false;
        var txn = item.Dispense(cmd.Quantity, cmd.Notes, cmd.InvoiceId);
        db.StockTransactions.Add(txn);
        await db.SaveChangesAsync(ct);
        return true;
    }
}

// --- Deactivate ---
public record DeactivateStockItemCommand(Guid Id) : IRequest<bool>;

public class DeactivateStockItemHandler(InventoryDbContext db) : IRequestHandler<DeactivateStockItemCommand, bool>
{
    public async Task<bool> Handle(DeactivateStockItemCommand cmd, CancellationToken ct)
    {
        var item = await db.StockItems.FirstOrDefaultAsync(s => s.Id == cmd.Id, ct);
        if (item is null) return false;
        item.Deactivate();
        await db.SaveChangesAsync(ct);
        return true;
    }
}
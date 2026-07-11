using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Net;
using Utano.Module.Inventory.DatabaseMappings;

namespace Utano.Module.Inventory.Features.Stock.GetStockItem;

[ApiController]
[Route("api/inventory/stock")]
[Authorize]
public class GetStockItemEndpoint(ISender sender) : ControllerBase
{
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(StockItemDetail), (int)HttpStatusCode.OK)]
    [ProducesResponseType((int)HttpStatusCode.NotFound)]
    [EndpointSummary("Get stock item with transaction history")]
    [Tags("Inventory Module")]
    public async Task<IActionResult> Get(Guid id, CancellationToken ct)
    {
        var result = await sender.Send(new GetStockItemQuery(id), ct);
        return result is null ? NotFound() : Ok(result);
    }
}

public record GetStockItemQuery(Guid Id) : IRequest<StockItemDetail?>;

public record StockItemDetail(
    Guid Id, string Name, string? Sku, string? Description,
    string Category, string Unit,
    decimal SellingPrice, decimal CostPrice,
    decimal QuantityOnHand, decimal ReorderLevel,
    bool IsLowStock, bool IsActive,
    DateTimeOffset CreatedAt, DateTimeOffset UpdatedAt,
    IReadOnlyList<TransactionRow> RecentTransactions);

public record TransactionRow(
    Guid Id, string Type, decimal Quantity,
    decimal QuantityBefore, decimal QuantityAfter,
    decimal UnitCost, string? Notes,
    string? ReferenceType, DateTimeOffset CreatedAt);

public class GetStockItemHandler(InventoryDbContext db) : IRequestHandler<GetStockItemQuery, StockItemDetail?>
{
    public async Task<StockItemDetail?> Handle(GetStockItemQuery q, CancellationToken ct)
    {
        var item = await db.StockItems.AsNoTracking().FirstOrDefaultAsync(s => s.Id == q.Id, ct);
        if (item is null) return null;

        var txns = await db.StockTransactions
            .AsNoTracking()
            .Where(t => t.StockItemId == q.Id)
            .OrderByDescending(t => t.CreatedAt)
            .Take(50)
            .Select(t => new TransactionRow(t.Id, t.Type.ToString(), t.Quantity,
                t.QuantityBefore, t.QuantityAfter, t.UnitCost, t.Notes, t.ReferenceType, t.CreatedAt))
            .ToListAsync(ct);

        return new StockItemDetail(item.Id, item.Name, item.Sku, item.Description,
            item.Category.ToString(), item.Unit, item.SellingPrice, item.CostPrice,
            item.QuantityOnHand, item.ReorderLevel, item.IsLowStock, item.IsActive,
            item.CreatedAt, item.UpdatedAt, txns);
    }
}
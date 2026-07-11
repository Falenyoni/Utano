using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Net;
using Utano.Module.Core.Models;
using Utano.Module.Inventory.DatabaseMappings;
using Utano.Module.Inventory.Domain.Enums;

namespace Utano.Module.Inventory.Features.Stock.GetStockItems;

[ApiController]
[Route("api/inventory/stock")]
[Authorize]
public class GetStockItemsEndpoint(ISender sender) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<StockItemSummary>), (int)HttpStatusCode.OK)]
    [EndpointSummary("List stock items")]
    [Tags("Inventory Module")]
    public async Task<IActionResult> Get(
        [FromQuery] string? search,
        [FromQuery] string? category,
        [FromQuery] bool? lowStockOnly,
        [FromQuery] bool? activeOnly,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var result = await sender.Send(
            new GetStockItemsQuery(search, category, lowStockOnly, activeOnly ?? true, page, pageSize),
            cancellationToken);
        return Ok(result);
    }
}

public record GetStockItemsQuery(
    string? Search,
    string? Category,
    bool? LowStockOnly,
    bool ActiveOnly,
    int Page,
    int PageSize) : IRequest<PagedResult<StockItemSummary>>;

public record StockItemSummary(
    Guid Id,
    string Name,
    string? Sku,
    string Category,
    string Unit,
    decimal SellingPrice,
    decimal CostPrice,
    decimal QuantityOnHand,
    decimal ReorderLevel,
    bool IsLowStock,
    bool IsActive);

public class GetStockItemsHandler(InventoryDbContext db) : IRequestHandler<GetStockItemsQuery, PagedResult<StockItemSummary>>
{
    public async Task<PagedResult<StockItemSummary>> Handle(GetStockItemsQuery q, CancellationToken ct)
    {
        var query = db.StockItems.AsNoTracking();

        if (q.ActiveOnly)
            query = query.Where(s => s.IsActive);

        if (!string.IsNullOrWhiteSpace(q.Search))
        {
            var lower = $"%{q.Search.ToLower()}%";
            var matchingIds = await db.StockItems
                .FromSql($"""SELECT * FROM "StockItems" WHERE LOWER("Name") LIKE {lower} OR LOWER("Sku") LIKE {lower}""")
                .Select(s => s.Id)
                .ToListAsync(ct);
            query = query.Where(s => matchingIds.Contains(s.Id));
        }

        if (Enum.TryParse<StockCategory>(q.Category, true, out var cat))
            query = query.Where(s => s.Category == cat);

        if (q.LowStockOnly == true)
            query = query.Where(s => s.QuantityOnHand <= s.ReorderLevel && s.ReorderLevel > 0);

        var total = await query.CountAsync(ct);
        var items = await query
            .OrderBy(s => s.Name)
            .Skip((q.Page - 1) * q.PageSize)
            .Take(q.PageSize)
            .Select(s => new StockItemSummary(s.Id, s.Name, s.Sku, s.Category.ToString(), s.Unit,
                s.SellingPrice, s.CostPrice, s.QuantityOnHand, s.ReorderLevel,
                s.QuantityOnHand <= s.ReorderLevel && s.ReorderLevel > 0, s.IsActive))
            .ToListAsync(ct);

        return new PagedResult<StockItemSummary>
        {
            Data = items,
            TotalCount = total,
            Page = q.Page,
            PageSize = q.PageSize
        };
    }
}
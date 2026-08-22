using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Net;
using Utano.Module.Inventory.DatabaseMappings;

namespace Utano.Module.Inventory.Features.StockTakes;

[ApiController]
[Route("api/inventory/stock-takes")]
[Authorize]
public class GetStockTakesEndpoint(ISender sender) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<StockTakeSummaryDto>), (int)HttpStatusCode.OK)]
    [EndpointSummary("List past and in-progress stock take sessions")]
    [Tags("Inventory Module")]
    public async Task<IActionResult> Get(CancellationToken ct)
        => Ok(await sender.Send(new GetStockTakesQuery(), ct));
}

public record GetStockTakesQuery : IRequest<IReadOnlyList<StockTakeSummaryDto>>;

public record StockTakeSummaryDto(
    Guid Id, string? Category, string Status, string StartedByName, string? CompletedByName,
    DateTimeOffset StartedAt, DateTimeOffset? CompletedAt, decimal? TotalVarianceValue, int LineCount);

public class GetStockTakesHandler(InventoryDbContext db) : IRequestHandler<GetStockTakesQuery, IReadOnlyList<StockTakeSummaryDto>>
{
    public async Task<IReadOnlyList<StockTakeSummaryDto>> Handle(GetStockTakesQuery query, CancellationToken ct)
    {
        // Materialize first, then map - calling .ToString() on the converted enum columns inside
        // the SQL-translated Select isn't reliably supported by EF Core.
        var stockTakes = await db.StockTakes
            .Include(t => t.Lines)
            .OrderByDescending(t => t.StartedAt)
            .ToListAsync(ct);

        return stockTakes.Select(t => new StockTakeSummaryDto(
            t.Id, t.Category?.ToString(), t.Status.ToString(),
            t.StartedByName, t.CompletedByName, t.StartedAt, t.CompletedAt, t.TotalVarianceValue,
            t.Lines.Count)).ToList();
    }
}

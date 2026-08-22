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
public class GetStockTakeByIdEndpoint(ISender sender) : ControllerBase
{
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(StockTakeResponse), (int)HttpStatusCode.OK)]
    [ProducesResponseType((int)HttpStatusCode.NotFound)]
    [EndpointSummary("Get a stock take session with its lines")]
    [Tags("Inventory Module")]
    public async Task<IActionResult> Get(Guid id, CancellationToken ct)
    {
        var result = await sender.Send(new GetStockTakeByIdQuery(id), ct);
        return result is null ? NotFound() : Ok(result);
    }
}

public record GetStockTakeByIdQuery(Guid Id) : IRequest<StockTakeResponse?>;

public class GetStockTakeByIdHandler(InventoryDbContext db) : IRequestHandler<GetStockTakeByIdQuery, StockTakeResponse?>
{
    public async Task<StockTakeResponse?> Handle(GetStockTakeByIdQuery query, CancellationToken ct)
    {
        var stockTake = await db.StockTakes
            .Include(t => t.Lines)
            .FirstOrDefaultAsync(t => t.Id == query.Id, ct);

        return stockTake is null ? null : StartStockTakeHandler.Map(stockTake);
    }
}

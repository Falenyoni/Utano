using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Net;
using Utano.Module.Core.Services;
using Utano.Module.Inventory.DatabaseMappings;

namespace Utano.Module.Inventory.Features.StockTakes;

[ApiController]
[Route("api/inventory/stock-takes/{id:guid}/finalize")]
[Authorize]
public class FinalizeStockTakeEndpoint(ISender sender) : ControllerBase
{
    [HttpPost]
    [ProducesResponseType(typeof(StockTakeResponse), (int)HttpStatusCode.OK)]
    [ProducesResponseType((int)HttpStatusCode.NotFound)]
    [ProducesResponseType((int)HttpStatusCode.BadRequest)]
    [EndpointSummary("Finalize a stock take - applies a stock adjustment for every counted item with a variance")]
    [Tags("Inventory Module")]
    public async Task<IActionResult> Post(Guid id, CancellationToken ct)
    {
        var result = await sender.Send(new FinalizeStockTakeCommand(id), ct);
        return result is null ? NotFound() : Ok(result);
    }
}

public record FinalizeStockTakeCommand(Guid StockTakeId) : IRequest<StockTakeResponse?>;

public class FinalizeStockTakeHandler(
    InventoryDbContext db,
    ICurrentUserService currentUser,
    IAuditService auditService,
    ILogger<FinalizeStockTakeHandler> logger)
    : IRequestHandler<FinalizeStockTakeCommand, StockTakeResponse?>
{
    public async Task<StockTakeResponse?> Handle(FinalizeStockTakeCommand cmd, CancellationToken ct)
    {
        var stockTake = await db.StockTakes
            .Include(t => t.Lines)
            .FirstOrDefaultAsync(t => t.Id == cmd.StockTakeId, ct);
        if (stockTake is null) return null;

        var linesNeedingAdjustment = stockTake.Finalize(currentUser.FullName);

        var itemIds = linesNeedingAdjustment.Select(l => l.StockItemId).ToList();
        var items = await db.StockItems
            .Where(s => itemIds.Contains(s.Id))
            .ToDictionaryAsync(s => s.Id, ct);

        foreach (var line in linesNeedingAdjustment)
        {
            if (!items.TryGetValue(line.StockItemId, out var item)) continue; // item deleted/deactivated mid-session
            item.Adjust(line.Variance!.Value, $"Stock take reconciliation ({stockTake.StartedAt:d MMM yyyy})");
        }

        await db.SaveChangesAsync(ct);

        try
        {
            await auditService.LogAsync("StockTake", stockTake.Id.ToString(), "Finalized",
                $"{stockTake.Category?.ToString() ?? "All categories"} · {linesNeedingAdjustment.Count} adjustment(s) · Net variance: {stockTake.TotalVarianceValue:C}", ct);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to audit-log stock take finalization for {StockTakeId}", stockTake.Id);
        }

        return StartStockTakeHandler.Map(stockTake);
    }
}

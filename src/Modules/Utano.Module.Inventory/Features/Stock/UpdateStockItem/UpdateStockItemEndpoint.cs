using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Net;
using Utano.Module.Core.Services;
using Utano.Module.Inventory.DatabaseMappings;
using Utano.Module.Inventory.Domain.Enums;

namespace Utano.Module.Inventory.Features.Stock.UpdateStockItem;

[ApiController]
[Route("api/inventory/stock")]
[Authorize]
public class UpdateStockItemEndpoint(ISender sender) : ControllerBase
{
    [HttpPut("{id:guid}")]
    [ProducesResponseType((int)HttpStatusCode.NoContent)]
    [ProducesResponseType((int)HttpStatusCode.NotFound)]
    [EndpointSummary("Update stock item details")]
    [Tags("Inventory Module")]
    public async Task<IActionResult> Put(Guid id, [FromBody] UpdateStockItemBody body, CancellationToken ct)
    {
        var result = await sender.Send(new UpdateStockItemCommand(id, body.Name, body.Sku, body.Description,
            body.Category, body.Unit, body.SellingPrice, body.CostPrice, body.ReorderLevel), ct);
        return result ? NoContent() : NotFound();
    }
}

public record UpdateStockItemBody(
    string Name, string? Sku, string? Description,
    string Category, string Unit,
    decimal SellingPrice, decimal CostPrice, decimal ReorderLevel);

public record UpdateStockItemCommand(
    Guid Id, string Name, string? Sku, string? Description,
    string Category, string Unit,
    decimal SellingPrice, decimal CostPrice, decimal ReorderLevel) : IRequest<bool>;

public class UpdateStockItemValidator : AbstractValidator<UpdateStockItemCommand>
{
    public UpdateStockItemValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Category)
            .NotEmpty()
            .Must(c => Enum.TryParse<StockCategory>(c, true, out _))
            .WithMessage($"Category must be one of: {string.Join(", ", Enum.GetNames<StockCategory>())}");
        RuleFor(x => x.Unit).NotEmpty().MaximumLength(50);
        RuleFor(x => x.SellingPrice).GreaterThanOrEqualTo(0);
        RuleFor(x => x.CostPrice).GreaterThanOrEqualTo(0);
        RuleFor(x => x.ReorderLevel).GreaterThanOrEqualTo(0);
    }
}

public class UpdateStockItemHandler(
    InventoryDbContext db,
    IAuditService auditService,
    ILogger<UpdateStockItemHandler> logger)
    : IRequestHandler<UpdateStockItemCommand, bool>
{
    public async Task<bool> Handle(UpdateStockItemCommand cmd, CancellationToken ct)
    {
        var item = await db.StockItems.FirstOrDefaultAsync(s => s.Id == cmd.Id, ct);
        if (item is null) return false;
        var category = Enum.Parse<StockCategory>(cmd.Category, ignoreCase: true);

        var oldSellingPrice = item.SellingPrice;
        var oldCostPrice = item.CostPrice;

        item.UpdateDetails(cmd.Name, cmd.Sku, cmd.Description, category, cmd.Unit,
            cmd.SellingPrice, cmd.CostPrice, cmd.ReorderLevel);
        await db.SaveChangesAsync(ct);

        try
        {
            var priceChanged = oldSellingPrice != cmd.SellingPrice || oldCostPrice != cmd.CostPrice;
            var priceNote = priceChanged
                ? $" · Price: Selling {oldSellingPrice:C}→{cmd.SellingPrice:C}, Cost {oldCostPrice:C}→{cmd.CostPrice:C}"
                : "";
            await auditService.LogAsync("StockItem", item.Id.ToString(), "Updated",
                $"Item: {item.Name} · Category: {category}{priceNote}", ct);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to audit-log stock item update for {StockItemId}", item.Id);
        }

        return true;
    }
}
using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Net;
using Utano.Module.Core.Exceptions;
using Utano.Module.Core.Services;
using Utano.Module.Inventory.DatabaseMappings;
using Utano.Module.Inventory.Domain.Enums;

namespace Utano.Module.Inventory.Features.Stock.BulkRepriceStockItems;

[ApiController]
[Route("api/inventory/stock/bulk-reprice")]
[Authorize]
public class BulkRepriceStockItemsEndpoint(ISender sender) : ControllerBase
{
    [HttpPost]
    [ProducesResponseType(typeof(BulkRepriceResponse), (int)HttpStatusCode.OK)]
    [ProducesResponseType((int)HttpStatusCode.BadRequest)]
    [EndpointSummary("Apply a percentage or fixed-amount price adjustment across a stock category")]
    [Tags("Inventory Module")]
    public async Task<IActionResult> Post([FromBody] BulkRepriceStockItemsCommand cmd, CancellationToken ct)
        => Ok(await sender.Send(cmd, ct));
}

public enum RepriceTarget { SellingPrice, CostPrice, Both }
public enum RepriceAdjustmentType { Percent, Fixed }

public record BulkRepriceStockItemsCommand(
    string Category,
    string Target,
    string AdjustmentType,
    decimal Value) : IRequest<BulkRepriceResponse>;

public record BulkRepriceResponse(int ItemsUpdated);

public class BulkRepriceStockItemsValidator : AbstractValidator<BulkRepriceStockItemsCommand>
{
    public BulkRepriceStockItemsValidator()
    {
        RuleFor(x => x.Category)
            .NotEmpty()
            .Must(c => Enum.TryParse<StockCategory>(c, true, out _))
            .WithMessage($"Category must be one of: {string.Join(", ", Enum.GetNames<StockCategory>())}");
        RuleFor(x => x.Target)
            .NotEmpty()
            .Must(t => Enum.TryParse<RepriceTarget>(t, true, out _))
            .WithMessage($"Target must be one of: {string.Join(", ", Enum.GetNames<RepriceTarget>())}");
        RuleFor(x => x.AdjustmentType)
            .NotEmpty()
            .Must(a => Enum.TryParse<RepriceAdjustmentType>(a, true, out _))
            .WithMessage($"AdjustmentType must be one of: {string.Join(", ", Enum.GetNames<RepriceAdjustmentType>())}");
        RuleFor(x => x.Value).NotEqual(0).WithMessage("Adjustment value cannot be zero.");
    }
}

public class BulkRepriceStockItemsHandler(
    InventoryDbContext db,
    IAuditService auditService,
    ILogger<BulkRepriceStockItemsHandler> logger)
    : IRequestHandler<BulkRepriceStockItemsCommand, BulkRepriceResponse>
{
    public async Task<BulkRepriceResponse> Handle(BulkRepriceStockItemsCommand cmd, CancellationToken ct)
    {
        var category = Enum.Parse<StockCategory>(cmd.Category, ignoreCase: true);
        var target = Enum.Parse<RepriceTarget>(cmd.Target, ignoreCase: true);
        var adjustmentType = Enum.Parse<RepriceAdjustmentType>(cmd.AdjustmentType, ignoreCase: true);

        var items = await db.StockItems
            .Where(s => s.Category == category && s.IsActive)
            .ToListAsync(ct);

        if (items.Count == 0) return new BulkRepriceResponse(0);

        // Compute every new price up front and validate before touching anything, so a single
        // item that would go negative aborts the whole batch instead of leaving it half-applied.
        var updates = items.Select(item => (
            Item: item,
            NewSellingPrice: Apply(item.SellingPrice, target is RepriceTarget.SellingPrice or RepriceTarget.Both, adjustmentType, cmd.Value),
            NewCostPrice: Apply(item.CostPrice, target is RepriceTarget.CostPrice or RepriceTarget.Both, adjustmentType, cmd.Value)
        )).ToList();

        if (updates.Any(u => u.NewSellingPrice < 0 || u.NewCostPrice < 0))
            throw new UtanoDomainException("This adjustment would result in a negative price for at least one item.");

        foreach (var (item, newSellingPrice, newCostPrice) in updates)
            item.AdjustPricing(newSellingPrice, newCostPrice);

        await db.SaveChangesAsync(ct);

        try
        {
            var direction = cmd.Value >= 0 ? "increased" : "decreased";
            var amountDesc = adjustmentType == RepriceAdjustmentType.Percent ? $"{Math.Abs(cmd.Value)}%" : $"{Math.Abs(cmd.Value):C}";
            await auditService.LogAsync("StockItem", category.ToString(), "BulkRepriced",
                $"{items.Count} {category} item(s) · {target} {direction} by {amountDesc}", ct);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to audit-log bulk reprice for category {Category}", category);
        }

        return new BulkRepriceResponse(items.Count);
    }

    private static decimal Apply(decimal currentPrice, bool applies, RepriceAdjustmentType adjustmentType, decimal value)
    {
        if (!applies) return currentPrice;
        var adjusted = adjustmentType == RepriceAdjustmentType.Percent
            ? currentPrice * (1 + value / 100m)
            : currentPrice + value;
        return Math.Round(adjusted, 2);
    }
}

using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Net;
using Utano.Module.Core.Services;
using Utano.Module.Inventory.DatabaseMappings;
using Utano.Module.Inventory.Domain.Entities;
using Utano.Module.Inventory.Domain.Enums;

namespace Utano.Module.Inventory.Features.Stock.AddStockItem;

[ApiController]
[Route("api/inventory/stock")]
[Authorize]
public class AddStockItemEndpoint(ISender sender) : ControllerBase
{
    [HttpPost]
    [ProducesResponseType(typeof(AddStockItemResponse), (int)HttpStatusCode.Created)]
    [ProducesResponseType((int)HttpStatusCode.BadRequest)]
    [EndpointSummary("Add a new stock item")]
    [Tags("Inventory Module")]
    public async Task<IActionResult> Post([FromBody] AddStockItemCommand cmd, CancellationToken ct)
    {
        var result = await sender.Send(cmd, ct);
        return CreatedAtAction(nameof(Post), new { id = result.Id }, result);
    }
}

public record AddStockItemCommand(
    string Name,
    string? Sku,
    string? Description,
    string Category,
    string Unit,
    decimal SellingPrice,
    decimal CostPrice,
    decimal ReorderLevel) : IRequest<AddStockItemResponse>;

public record AddStockItemResponse(Guid Id, string Name);

public class AddStockItemValidator : AbstractValidator<AddStockItemCommand>
{
    public AddStockItemValidator()
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

public class AddStockItemHandler(InventoryDbContext db, ICurrentUserService currentUser)
    : IRequestHandler<AddStockItemCommand, AddStockItemResponse>
{
    public async Task<AddStockItemResponse> Handle(AddStockItemCommand cmd, CancellationToken ct)
    {
        var category = Enum.Parse<StockCategory>(cmd.Category, ignoreCase: true);
        var item = StockItem.Create(currentUser.PracticeId, cmd.Name, category, cmd.Unit,
            cmd.SellingPrice, cmd.CostPrice, cmd.ReorderLevel, cmd.Sku, cmd.Description);

        db.StockItems.Add(item);
        await db.SaveChangesAsync(ct);
        return new AddStockItemResponse(item.Id, item.Name);
    }
}
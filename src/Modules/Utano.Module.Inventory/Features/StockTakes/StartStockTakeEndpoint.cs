using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Net;
using Utano.Module.Core.Services;
using Utano.Module.Inventory.DatabaseMappings;
using Utano.Module.Inventory.Domain.Entities;
using Utano.Module.Inventory.Domain.Enums;

namespace Utano.Module.Inventory.Features.StockTakes;

[ApiController]
[Route("api/inventory/stock-takes")]
[Authorize]
public class StartStockTakeEndpoint(ISender sender) : ControllerBase
{
    [HttpPost]
    [ProducesResponseType(typeof(StockTakeResponse), (int)HttpStatusCode.Created)]
    [ProducesResponseType((int)HttpStatusCode.BadRequest)]
    [EndpointSummary("Start a new stock take session (whole inventory, or one category)")]
    [Tags("Inventory Module")]
    public async Task<IActionResult> Post([FromBody] StartStockTakeCommand cmd, CancellationToken ct)
    {
        var result = await sender.Send(cmd, ct);
        return CreatedAtAction(nameof(Post), new { id = result.Id }, result);
    }
}

// Category null/omitted means the whole active inventory.
public record StartStockTakeCommand(string? Category, string? Notes) : IRequest<StockTakeResponse>;

public record StockTakeLineDto(
    Guid StockItemId, string StockItemName, decimal ExpectedQuantity,
    decimal? CountedQuantity, decimal? Variance, decimal UnitCostSnapshot);

public record StockTakeResponse(
    Guid Id, string? Category, string Status, string StartedByName, string? CompletedByName,
    string? Notes, DateTimeOffset StartedAt, DateTimeOffset? CompletedAt, decimal? TotalVarianceValue,
    IReadOnlyList<StockTakeLineDto> Lines);

public class StartStockTakeValidator : AbstractValidator<StartStockTakeCommand>
{
    public StartStockTakeValidator()
    {
        RuleFor(x => x.Category)
            .Must(c => string.IsNullOrWhiteSpace(c) || Enum.TryParse<StockCategory>(c, true, out _))
            .WithMessage($"Category must be one of: {string.Join(", ", Enum.GetNames<StockCategory>())}");
    }
}

public class StartStockTakeHandler(
    InventoryDbContext db, ICurrentUserService currentUser, IValidator<StartStockTakeCommand> validator)
    : IRequestHandler<StartStockTakeCommand, StockTakeResponse>
{
    public async Task<StockTakeResponse> Handle(StartStockTakeCommand cmd, CancellationToken ct)
    {
        var validation = await validator.ValidateAsync(cmd, ct);
        if (!validation.IsValid)
            throw new Utano.Module.Core.Exceptions.UtanoDomainException(validation.Errors[0].ErrorMessage);

        StockCategory? category = string.IsNullOrWhiteSpace(cmd.Category)
            ? null
            : Enum.Parse<StockCategory>(cmd.Category, ignoreCase: true);

        var itemsQuery = db.StockItems.Where(s => s.IsActive);
        if (category.HasValue)
            itemsQuery = itemsQuery.Where(s => s.Category == category.Value);

        var items = await itemsQuery.ToListAsync(ct);

        var stockTake = StockTake.Start(currentUser.PracticeId, category, currentUser.FullName, cmd.Notes, items);

        db.StockTakes.Add(stockTake);
        await db.SaveChangesAsync(ct);

        return Map(stockTake);
    }

    internal static StockTakeResponse Map(StockTake t) => new(
        t.Id, t.Category?.ToString(), t.Status.ToString(), t.StartedByName, t.CompletedByName,
        t.Notes, t.StartedAt, t.CompletedAt, t.TotalVarianceValue,
        t.Lines.Select(l => new StockTakeLineDto(
            l.StockItemId, l.StockItemName, l.ExpectedQuantity, l.CountedQuantity, l.Variance, l.UnitCostSnapshot)).ToList());
}

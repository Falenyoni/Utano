using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Net;
using Utano.Module.Core.Exceptions;
using Utano.Module.Inventory.DatabaseMappings;

namespace Utano.Module.Inventory.Features.StockTakes;

[ApiController]
[Route("api/inventory/stock-takes/{id:guid}/lines/{stockItemId:guid}")]
[Authorize]
public class RecordStockTakeCountEndpoint(ISender sender) : ControllerBase
{
    [HttpPut]
    [ProducesResponseType((int)HttpStatusCode.NoContent)]
    [ProducesResponseType((int)HttpStatusCode.NotFound)]
    [ProducesResponseType((int)HttpStatusCode.BadRequest)]
    [EndpointSummary("Record the physical count for one item in an in-progress stock take")]
    [Tags("Inventory Module")]
    public async Task<IActionResult> Put(Guid id, Guid stockItemId, [FromBody] RecordStockTakeCountBody body, CancellationToken ct)
    {
        var ok = await sender.Send(new RecordStockTakeCountCommand(id, stockItemId, body.CountedQuantity), ct);
        return ok ? NoContent() : NotFound();
    }
}

public record RecordStockTakeCountBody(decimal CountedQuantity);
public record RecordStockTakeCountCommand(Guid StockTakeId, Guid StockItemId, decimal CountedQuantity) : IRequest<bool>;

public class RecordStockTakeCountValidator : AbstractValidator<RecordStockTakeCountCommand>
{
    public RecordStockTakeCountValidator()
    {
        RuleFor(x => x.CountedQuantity).GreaterThanOrEqualTo(0);
    }
}

public class RecordStockTakeCountHandler(
    InventoryDbContext db, IValidator<RecordStockTakeCountCommand> validator)
    : IRequestHandler<RecordStockTakeCountCommand, bool>
{
    public async Task<bool> Handle(RecordStockTakeCountCommand cmd, CancellationToken ct)
    {
        var validation = await validator.ValidateAsync(cmd, ct);
        if (!validation.IsValid)
            throw new UtanoDomainException(validation.Errors[0].ErrorMessage);

        var stockTake = await db.StockTakes
            .Include(t => t.Lines)
            .FirstOrDefaultAsync(t => t.Id == cmd.StockTakeId, ct);
        if (stockTake is null) return false;

        stockTake.RecordCount(cmd.StockItemId, cmd.CountedQuantity);
        await db.SaveChangesAsync(ct);

        return true;
    }
}

using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Net;
using Utano.Module.Billing.DatabaseMappings;
using Utano.Module.Billing.Domain.Entities;
using Utano.Module.Billing.Domain.Enums;
using Utano.Module.Core.Exceptions;

namespace Utano.Module.Billing.Features.Invoices.ManageLineItems;

[ApiController]
[Route("api/billing/invoices/{invoiceId:guid}/line-items")]
[Authorize]
public class LineItemEndpoints(ISender sender) : ControllerBase
{
    [HttpPost]
    [ProducesResponseType(typeof(AddLineItemResponse), (int)HttpStatusCode.Created)]
    [ProducesResponseType((int)HttpStatusCode.NotFound)]
    [EndpointSummary("Add a line item to a Draft invoice")]
    [Tags("Billing Module")]
    public async Task<IActionResult> Add(Guid invoiceId, [FromBody] AddLineItemBody body, CancellationToken ct)
    {
        var result = await sender.Send(new AddLineItemCommand(invoiceId, body.Type, body.Description,
            body.Quantity, body.UnitPrice, body.DiscountPercent, body.StockItemId), ct);
        if (result is null) return NotFound();
        return CreatedAtAction(nameof(Add), result);
    }

    [HttpDelete("{lineItemId:guid}")]
    [ProducesResponseType((int)HttpStatusCode.NoContent)]
    [ProducesResponseType((int)HttpStatusCode.NotFound)]
    [EndpointSummary("Remove a line item from a Draft invoice")]
    [Tags("Billing Module")]
    public async Task<IActionResult> Remove(Guid invoiceId, Guid lineItemId, CancellationToken ct)
    {
        var ok = await sender.Send(new RemoveLineItemCommand(invoiceId, lineItemId), ct);
        return ok ? NoContent() : NotFound();
    }
}

public record AddLineItemBody(string Type, string Description, decimal Quantity,
    decimal UnitPrice, decimal DiscountPercent = 0, Guid? StockItemId = null);

public record AddLineItemCommand(Guid InvoiceId, string Type, string Description, decimal Quantity,
    decimal UnitPrice, decimal DiscountPercent, Guid? StockItemId) : IRequest<AddLineItemResponse?>;

public record AddLineItemResponse(Guid LineItemId, decimal Amount, decimal InvoiceTotal);

public class AddLineItemValidator : AbstractValidator<AddLineItemCommand>
{
    public AddLineItemValidator()
    {
        RuleFor(x => x.Type)
            .Must(t => Enum.TryParse<LineItemType>(t, true, out _))
            .WithMessage($"Type must be one of: {string.Join(", ", Enum.GetNames<LineItemType>())}");
        RuleFor(x => x.Description).NotEmpty().MaximumLength(500);
        RuleFor(x => x.Quantity).GreaterThan(0);
        RuleFor(x => x.UnitPrice).GreaterThanOrEqualTo(0);
        RuleFor(x => x.DiscountPercent).InclusiveBetween(0, 100);
    }
}

public class AddLineItemHandler(BillingDbContext db) : IRequestHandler<AddLineItemCommand, AddLineItemResponse?>
{
    public async Task<AddLineItemResponse?> Handle(AddLineItemCommand cmd, CancellationToken ct)
    {
        var invoice = await db.Invoices.FirstOrDefaultAsync(i => i.Id == cmd.InvoiceId, ct);
        if (invoice is null) return null;
        if (invoice.Status != InvoiceStatus.Draft)
            throw new UtanoDomainException("Line items can only be added to Draft invoices.");

        var type = Enum.Parse<LineItemType>(cmd.Type, ignoreCase: true);
        // Create and persist line item directly — avoids EF Core navigation tracking issues
        // with Invoice._lineItems being a private readonly field.
        var item = InvoiceLineItem.Create(invoice.Id, type, cmd.Description, cmd.Quantity,
            cmd.UnitPrice, cmd.DiscountPercent, cmd.StockItemId);
        db.InvoiceLineItems.Add(item);
        await db.SaveChangesAsync(ct);

        // Recompute totals from DB and update invoice
        var newSubTotal = await db.InvoiceLineItems
            .Where(l => l.InvoiceId == invoice.Id)
            .SumAsync(l => l.Amount, ct);

        await db.Invoices.Where(i => i.Id == invoice.Id)
            .ExecuteUpdateAsync(s => s
                .SetProperty(i => i.SubTotal, newSubTotal)
                .SetProperty(i => i.TotalAmount, newSubTotal)
                .SetProperty(i => i.UpdatedAt, DateTimeOffset.UtcNow), ct);

        return new AddLineItemResponse(item.Id, item.Amount, newSubTotal);
    }
}

public record RemoveLineItemCommand(Guid InvoiceId, Guid LineItemId) : IRequest<bool>;

public class RemoveLineItemHandler(BillingDbContext db) : IRequestHandler<RemoveLineItemCommand, bool>
{
    public async Task<bool> Handle(RemoveLineItemCommand cmd, CancellationToken ct)
    {
        var invoice = await db.Invoices.FirstOrDefaultAsync(i => i.Id == cmd.InvoiceId, ct);
        if (invoice is null) return false;
        if (invoice.Status != InvoiceStatus.Draft)
            throw new UtanoDomainException("Line items can only be removed from Draft invoices.");

        var item = await db.InvoiceLineItems
            .FirstOrDefaultAsync(l => l.Id == cmd.LineItemId && l.InvoiceId == cmd.InvoiceId, ct);
        if (item is null) return false;

        db.InvoiceLineItems.Remove(item);
        await db.SaveChangesAsync(ct);

        var newSubTotal = await db.InvoiceLineItems
            .Where(l => l.InvoiceId == invoice.Id)
            .SumAsync(l => l.Amount, ct);

        await db.Invoices.Where(i => i.Id == invoice.Id)
            .ExecuteUpdateAsync(s => s
                .SetProperty(i => i.SubTotal, newSubTotal)
                .SetProperty(i => i.TotalAmount, newSubTotal)
                .SetProperty(i => i.UpdatedAt, DateTimeOffset.UtcNow), ct);

        return true;
    }
}
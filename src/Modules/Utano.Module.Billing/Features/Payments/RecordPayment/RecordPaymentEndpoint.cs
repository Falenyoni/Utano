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
using Utano.Module.Core.Services;

namespace Utano.Module.Billing.Features.Payments.RecordPayment;

[ApiController]
[Route("api/billing/invoices/{invoiceId:guid}/payments")]
[Authorize]
public class RecordPaymentEndpoint(ISender sender) : ControllerBase
{
    [HttpPost]
    [ProducesResponseType(typeof(RecordPaymentResponse), (int)HttpStatusCode.Created)]
    [ProducesResponseType((int)HttpStatusCode.NotFound)]
    [EndpointSummary("Record a payment against an invoice")]
    [Tags("Billing Module")]
    public async Task<IActionResult> Post(Guid invoiceId, [FromBody] RecordPaymentBody body, CancellationToken ct)
    {
        var result = await sender.Send(new RecordPaymentCommand(invoiceId, body.Amount, body.Method,
            body.Reference, body.Notes, body.InstallmentId), ct);
        if (result is null) return NotFound();
        return CreatedAtAction(nameof(Post), result);
    }
}

public record RecordPaymentBody(
    decimal Amount, string Method, string? Reference,
    string? Notes, Guid? InstallmentId);

public record RecordPaymentCommand(
    Guid InvoiceId, decimal Amount, string Method,
    string? Reference, string? Notes, Guid? InstallmentId) : IRequest<RecordPaymentResponse?>;

public record RecordPaymentResponse(Guid PaymentId, decimal BalanceDue, string InvoiceStatus);

public class RecordPaymentValidator : AbstractValidator<RecordPaymentCommand>
{
    public RecordPaymentValidator()
    {
        RuleFor(x => x.Amount).GreaterThan(0);
        RuleFor(x => x.Method)
            .Must(m => Enum.TryParse<PaymentMethod>(m, true, out _))
            .WithMessage($"Method must be one of: {string.Join(", ", Enum.GetNames<PaymentMethod>())}");
    }
}

public class RecordPaymentHandler(BillingDbContext db, ICurrentUserService currentUser, IFiscalDevice fiscalDevice)
    : IRequestHandler<RecordPaymentCommand, RecordPaymentResponse?>
{
    public async Task<RecordPaymentResponse?> Handle(RecordPaymentCommand cmd, CancellationToken ct)
    {
        var invoice = await db.Invoices.Include(i => i.LineItems)
            .FirstOrDefaultAsync(i => i.Id == cmd.InvoiceId, ct);
        if (invoice is null) return null;

        // Issue fiscal receipt via ZIMRA (no-op if not configured)
        string? fiscalNumber = null;
        if (Enum.TryParse<PaymentMethod>(cmd.Method, true, out var method) && method == PaymentMethod.Cash)
        {
            var fiscal = await fiscalDevice.IssueReceiptAsync(new FiscalReceiptRequest(
                invoice.InvoiceNumber, cmd.Amount, invoice.Currency, invoice.PatientName,
                DateOnly.FromDateTime(DateTime.UtcNow),
                invoice.LineItems.Select(l => (l.Description, l.Amount)).ToList()), ct);
            fiscalNumber = fiscal.FiscalReceiptNumber;
        }

        invoice.ApplyPayment(cmd.Amount);

        var payment = Payment.Create(currentUser.PracticeId, cmd.InvoiceId, cmd.Amount,
            method, cmd.Reference, cmd.Notes, cmd.InstallmentId, fiscalNumber);

        // Apply to payment plan installment if linked
        if (cmd.InstallmentId.HasValue)
        {
            var plan = await db.PaymentPlans.Include(p => p.Installments)
                .FirstOrDefaultAsync(p => p.InvoiceId == cmd.InvoiceId, ct);
            plan?.MarkInstallmentPaid(cmd.InstallmentId.Value, cmd.Amount);
        }

        db.Payments.Add(payment);
        await db.SaveChangesAsync(ct);
        return new RecordPaymentResponse(payment.Id, invoice.BalanceDue, invoice.Status.ToString());
    }
}
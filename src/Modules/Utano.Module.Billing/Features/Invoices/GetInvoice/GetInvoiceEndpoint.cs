using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Net;
using Utano.Module.Billing.DatabaseMappings;

namespace Utano.Module.Billing.Features.Invoices.GetInvoice;

[ApiController]
[Route("api/billing/invoices")]
[Authorize]
public class GetInvoiceEndpoint(ISender sender) : ControllerBase
{
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(InvoiceDetail), (int)HttpStatusCode.OK)]
    [ProducesResponseType((int)HttpStatusCode.NotFound)]
    [EndpointSummary("Get full invoice detail")]
    [Tags("Billing Module")]
    public async Task<IActionResult> Get(Guid id, CancellationToken ct)
    {
        var result = await sender.Send(new GetInvoiceQuery(id), ct);
        return result is null ? NotFound() : Ok(result);
    }
}

public record GetInvoiceQuery(Guid Id) : IRequest<InvoiceDetail?>;

public record LineItemRow(Guid Id, string Type, string Description, decimal Quantity,
    decimal UnitPrice, decimal DiscountPercent, decimal Amount, Guid? StockItemId);

public record PaymentRow(Guid Id, DateOnly PaymentDate, decimal Amount, string Method,
    string? Reference, string? Notes, string? FiscalReceiptNumber, DateTimeOffset CreatedAt);

public record PaymentPlanRow(Guid Id, decimal TotalAmount, decimal AmountPaid,
    int InstallmentCount, string Frequency, string Status, DateOnly StartDate,
    IReadOnlyList<InstallmentRow> Installments);

public record InstallmentRow(Guid Id, int Number, DateOnly DueDate, decimal Amount,
    decimal PaidAmount, string Status);

public record InvoiceDetail(
    Guid Id, string InvoiceNumber, Guid PatientId, string PatientName,
    Guid? DoctorId, string? DoctorName, Guid? VisitId,
    string Status, DateOnly InvoiceDate, DateOnly DueDate, string Currency,
    decimal SubTotal, decimal DiscountAmount, decimal TaxAmount,
    decimal TotalAmount, decimal AmountPaid, decimal BalanceDue,
    Guid? MedicalAidId, string? MedicalAidName, decimal MedAidClaimAmount, string MedAidClaimStatus,
    string? Notes, DateTimeOffset CreatedAt, DateTimeOffset UpdatedAt,
    IReadOnlyList<LineItemRow> LineItems,
    IReadOnlyList<PaymentRow> Payments,
    PaymentPlanRow? PaymentPlan);

public class GetInvoiceHandler(BillingDbContext db) : IRequestHandler<GetInvoiceQuery, InvoiceDetail?>
{
    public async Task<InvoiceDetail?> Handle(GetInvoiceQuery q, CancellationToken ct)
    {
        var inv = await db.Invoices
            .AsNoTracking()
            .Include(i => i.LineItems)
            .FirstOrDefaultAsync(i => i.Id == q.Id, ct);
        if (inv is null) return null;

        var payments = await db.Payments
            .AsNoTracking().Where(p => p.InvoiceId == q.Id)
            .OrderByDescending(p => p.CreatedAt)
            .Select(p => new PaymentRow(p.Id, p.PaymentDate, p.Amount, p.Method.ToString(),
                p.Reference, p.Notes, p.FiscalReceiptNumber, p.CreatedAt))
            .ToListAsync(ct);

        var plan = await db.PaymentPlans
            .AsNoTracking()
            .Include(pp => pp.Installments)
            .FirstOrDefaultAsync(pp => pp.InvoiceId == q.Id, ct);

        PaymentPlanRow? planRow = null;
        if (plan is not null)
        {
            var installments = plan.Installments
                .OrderBy(i => i.InstallmentNumber)
                .Select(i => new InstallmentRow(i.Id, i.InstallmentNumber, i.DueDate,
                    i.Amount, i.PaidAmount, i.Status.ToString()))
                .ToList();
            planRow = new PaymentPlanRow(plan.Id, plan.TotalAmount, plan.AmountPaid,
                plan.InstallmentCount, plan.Frequency.ToString(), plan.Status.ToString(),
                plan.StartDate, installments);
        }

        var lineItems = inv.LineItems
            .Select(l => new LineItemRow(l.Id, l.Type.ToString(), l.Description,
                l.Quantity, l.UnitPrice, l.DiscountPercent, l.Amount, l.StockItemId))
            .ToList();

        return new InvoiceDetail(inv.Id, inv.InvoiceNumber, inv.PatientId, inv.PatientName,
            inv.DoctorId, inv.DoctorName, inv.VisitId, inv.Status.ToString(),
            inv.InvoiceDate, inv.DueDate, inv.Currency,
            inv.SubTotal, inv.DiscountAmount, inv.TaxAmount,
            inv.TotalAmount, inv.AmountPaid, inv.BalanceDue,
            inv.MedicalAidId, inv.MedicalAidName, inv.MedAidClaimAmount, inv.MedAidClaimStatus.ToString(),
            inv.Notes, inv.CreatedAt, inv.UpdatedAt, lineItems, payments, planRow);
    }
}
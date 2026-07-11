using Microsoft.EntityFrameworkCore;
using Utano.Module.Billing.DatabaseMappings;
using Utano.Module.Billing.Domain.Entities;
using Utano.Module.Billing.Domain.Enums;
using Utano.Module.Core.Services;

namespace Utano.Module.Billing.Infrastructure;

public class BillingService(BillingDbContext db) : IBillingService
{
    public async Task TryAddAppointmentConsultationFeeAsync(
        Guid practiceId, Guid visitId, Guid patientId, string patientName,
        Guid? doctorId, string? doctorName, string appointmentTypeKey, CancellationToken ct = default)
    {
        var item = await db.ServiceItems
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(s =>
                (s.PracticeId == null || s.PracticeId == practiceId) &&
                s.AppointmentTypeKey == appointmentTypeKey &&
                s.IsActive, ct);

        if (item is null) return;

        await AddServiceLineItemInternalAsync(practiceId, visitId, patientId, patientName,
            doctorId, doctorName, item.Name, item.DefaultPrice, LineItemType.Consultation, item.Id, ct);
    }

    public async Task AddServiceLineItemAsync(
        Guid practiceId, Guid visitId, Guid patientId, string patientName,
        Guid? doctorId, string? doctorName, string description, decimal unitPrice,
        string lineItemType, Guid serviceItemId, CancellationToken ct = default)
    {
        var type = Enum.TryParse<LineItemType>(lineItemType, out var parsed)
            ? parsed
            : LineItemType.Other;

        // If no price supplied, pull default from the service catalog
        if (unitPrice <= 0)
        {
            var catalogItem = await db.ServiceItems
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(s => s.Id == serviceItemId, ct);
            if (catalogItem is not null)
            {
                unitPrice = catalogItem.DefaultPrice;
                if (string.IsNullOrWhiteSpace(description))
                    description = catalogItem.Name;
            }
        }

        await AddServiceLineItemInternalAsync(practiceId, visitId, patientId, patientName,
            doctorId, doctorName, description, unitPrice, type, serviceItemId, ct);
    }

    private async Task AddServiceLineItemInternalAsync(
        Guid practiceId, Guid visitId, Guid patientId, string patientName,
        Guid? doctorId, string? doctorName, string description, decimal unitPrice,
        LineItemType type, Guid serviceItemId, CancellationToken ct)
    {
        await CreateDraftInvoiceForVisitAsync(practiceId, visitId, patientId, patientName, doctorId, doctorName, ct);

        var invoice = await db.Invoices
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(i => i.VisitId == visitId && i.PracticeId == practiceId, ct);
        if (invoice is null) return;

        // Avoid duplicate service line items for the same item on the same invoice
        var alreadyAdded = await db.InvoiceLineItems
            .AnyAsync(l => l.InvoiceId == invoice.Id && l.ServiceItemId == serviceItemId, ct);
        if (alreadyAdded) return;

        var lineItem = InvoiceLineItem.Create(invoice.Id, type, description, 1, unitPrice, 0, null, serviceItemId);
        db.InvoiceLineItems.Add(lineItem);
        await db.SaveChangesAsync(ct);

        await RecalculateTotalsAsync(invoice.Id, ct);
    }
    public async Task CreateDraftInvoiceForVisitAsync(
        Guid practiceId, Guid visitId, Guid patientId, string patientName,
        Guid? doctorId, string? doctorName, CancellationToken cancellationToken = default)
    {
        var alreadyExists = await db.Invoices
            .IgnoreQueryFilters()
            .AnyAsync(i => i.VisitId == visitId, cancellationToken);
        if (alreadyExists) return;

        var prefix = $"INV-{DateTime.UtcNow:yyyyMM}-";
        var count = await db.Invoices
            .IgnoreQueryFilters()
            .Where(i => i.PracticeId == practiceId && i.InvoiceNumber.StartsWith(prefix))
            .CountAsync(cancellationToken);
        var invoiceNumber = $"{prefix}{(count + 1):D4}";

        var invoice = Invoice.Create(practiceId, invoiceNumber, patientId, patientName,
            doctorId, doctorName, visitId);

        db.Invoices.Add(invoice);
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task AddPrescriptionLineItemAsync(
        Guid practiceId, Guid visitId, Guid patientId, string patientName,
        string description, decimal quantity, decimal unitPrice, Guid? stockItemId,
        CancellationToken ct = default)
    {
        await CreateDraftInvoiceForVisitAsync(practiceId, visitId, patientId, patientName, null, null, ct);

        var invoice = await db.Invoices
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(i => i.VisitId == visitId && i.PracticeId == practiceId, ct);
        if (invoice is null) return;

        var item = InvoiceLineItem.Create(invoice.Id, LineItemType.Medication, description,
            quantity, unitPrice, 0, stockItemId);
        db.InvoiceLineItems.Add(item);
        await db.SaveChangesAsync(ct);
        await RecalculateTotalsAsync(invoice.Id, ct);
    }

    private async Task RecalculateTotalsAsync(Guid invoiceId, CancellationToken ct)
    {
        var newSubTotal = await db.InvoiceLineItems
            .Where(l => l.InvoiceId == invoiceId)
            .SumAsync(l => l.Amount, ct);

        await db.Invoices
            .Where(i => i.Id == invoiceId)
            .ExecuteUpdateAsync(s => s
                .SetProperty(i => i.SubTotal, newSubTotal)
                .SetProperty(i => i.TotalAmount, newSubTotal)
                .SetProperty(i => i.UpdatedAt, DateTimeOffset.UtcNow), ct);
    }
}

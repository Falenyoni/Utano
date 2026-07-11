using Microsoft.EntityFrameworkCore;
using Utano.Module.Billing.DatabaseMappings;
using Utano.Module.Billing.Domain.Entities;
using Utano.Module.Billing.Domain.Enums;
using Utano.Module.Core.Services;

namespace Utano.Module.Billing.Infrastructure;

public class BillingService(BillingDbContext db) : IBillingService
{
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

        var newSubTotal = await db.InvoiceLineItems
            .Where(l => l.InvoiceId == invoice.Id)
            .SumAsync(l => l.Amount, ct);

        await db.Invoices
            .Where(i => i.Id == invoice.Id)
            .ExecuteUpdateAsync(s => s
                .SetProperty(i => i.SubTotal, newSubTotal)
                .SetProperty(i => i.TotalAmount, newSubTotal)
                .SetProperty(i => i.UpdatedAt, DateTimeOffset.UtcNow), ct);
    }
}

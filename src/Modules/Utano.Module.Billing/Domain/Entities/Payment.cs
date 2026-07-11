using Utano.Module.Billing.Domain.Enums;

namespace Utano.Module.Billing.Domain.Entities;

public class Payment
{
    private Payment() { }

    public Guid Id { get; private set; }
    public Guid PracticeId { get; private set; }
    public Guid InvoiceId { get; private set; }
    public DateOnly PaymentDate { get; private set; }
    public decimal Amount { get; private set; }
    public PaymentMethod Method { get; private set; }
    public string? Reference { get; private set; }
    public string? Notes { get; private set; }
    public string? FiscalReceiptNumber { get; private set; }
    public Guid? PaymentPlanInstallmentId { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }

    public static Payment Create(Guid practiceId, Guid invoiceId, decimal amount,
        PaymentMethod method, string? reference, string? notes,
        Guid? installmentId = null, string? fiscalReceiptNumber = null)
    {
        return new Payment
        {
            Id = Guid.NewGuid(),
            PracticeId = practiceId,
            InvoiceId = invoiceId,
            PaymentDate = DateOnly.FromDateTime(DateTime.UtcNow),
            Amount = amount,
            Method = method,
            Reference = reference,
            Notes = notes,
            FiscalReceiptNumber = fiscalReceiptNumber,
            PaymentPlanInstallmentId = installmentId,
            CreatedAt = DateTimeOffset.UtcNow
        };
    }
}

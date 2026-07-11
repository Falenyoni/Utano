using Utano.Module.Billing.Domain.Enums;
using Utano.Module.Core.Domain.Aggregate;
using Utano.Module.Core.Exceptions;

namespace Utano.Module.Billing.Domain.Entities;

public class Invoice : AggregateRoot
{
    private readonly List<InvoiceLineItem> _lineItems = [];
    private Invoice() { }

    public string InvoiceNumber { get; private set; } = null!;
    public Guid PatientId { get; private set; }
    public string PatientName { get; private set; } = null!;
    public Guid? DoctorId { get; private set; }
    public string? DoctorName { get; private set; }
    public Guid? VisitId { get; private set; }
    public InvoiceStatus Status { get; private set; }
    public DateOnly InvoiceDate { get; private set; }
    public DateOnly DueDate { get; private set; }
    public string Currency { get; private set; } = "USD";

    public decimal SubTotal { get; private set; }
    public decimal DiscountAmount { get; private set; }
    public decimal TaxAmount { get; private set; }
    public decimal TotalAmount { get; private set; }
    public decimal AmountPaid { get; private set; }
    public decimal BalanceDue => TotalAmount - AmountPaid;

    public Guid? MedicalAidId { get; private set; }
    public string? MedicalAidName { get; private set; }
    public decimal MedAidClaimAmount { get; private set; }
    public MedAidClaimStatus MedAidClaimStatus { get; private set; }

    public string? Notes { get; private set; }

    public IReadOnlyList<InvoiceLineItem> LineItems => _lineItems.AsReadOnly();

    public static Invoice Create(
        Guid practiceId,
        string invoiceNumber,
        Guid patientId,
        string patientName,
        Guid? doctorId,
        string? doctorName,
        Guid? visitId,
        string currency = "USD",
        Guid? medicalAidId = null,
        string? medicalAidName = null,
        string? notes = null)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        return new Invoice
        {
            Id = Guid.NewGuid(),
            PracticeId = practiceId,
            InvoiceNumber = invoiceNumber,
            PatientId = patientId,
            PatientName = patientName,
            DoctorId = doctorId,
            DoctorName = doctorName,
            VisitId = visitId,
            Status = InvoiceStatus.Draft,
            InvoiceDate = today,
            DueDate = today.AddDays(30),
            Currency = currency,
            MedicalAidId = medicalAidId,
            MedicalAidName = medicalAidName,
            MedAidClaimStatus = medicalAidId.HasValue ? MedAidClaimStatus.Pending : MedAidClaimStatus.None,
            Notes = notes,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };
    }

    public InvoiceLineItem AddLineItem(LineItemType type, string description, decimal quantity,
        decimal unitPrice, decimal discountPercent = 0, Guid? stockItemId = null)
    {
        if (Status != InvoiceStatus.Draft)
            throw new UtanoDomainException("Line items can only be added to Draft invoices.");
        var item = InvoiceLineItem.Create(Id, type, description, quantity, unitPrice, discountPercent, stockItemId);
        _lineItems.Add(item);
        RecalculateTotals();
        UpdatedAt = DateTimeOffset.UtcNow;
        return item;
    }

    public void RemoveLineItem(Guid lineItemId)
    {
        if (Status != InvoiceStatus.Draft)
            throw new UtanoDomainException("Line items can only be removed from Draft invoices.");
        var item = _lineItems.FirstOrDefault(l => l.Id == lineItemId)
            ?? throw new UtanoDomainException("Line item not found.");
        _lineItems.Remove(item);
        RecalculateTotals();
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void Issue()
    {
        if (Status != InvoiceStatus.Draft)
            throw new UtanoDomainException("Only Draft invoices can be issued.");
        Status = InvoiceStatus.Issued;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void ApplyPayment(decimal amount)
    {
        if (amount <= 0) throw new UtanoDomainException("Payment amount must be positive.");
        if (amount > BalanceDue) throw new UtanoDomainException("Payment exceeds balance due.");
        AmountPaid += amount;
        Status = AmountPaid >= TotalAmount ? InvoiceStatus.Paid : InvoiceStatus.PartiallyPaid;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void Void()
    {
        if (Status == InvoiceStatus.Paid)
            throw new UtanoDomainException("Fully paid invoices cannot be voided.");
        Status = InvoiceStatus.Void;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void SetMedAidClaim(decimal claimAmount, MedAidClaimStatus status)
    {
        MedAidClaimAmount = claimAmount;
        MedAidClaimStatus = status;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    private void RecalculateTotals()
    {
        SubTotal = _lineItems.Sum(l => l.Amount);
        TotalAmount = SubTotal - DiscountAmount + TaxAmount;
    }
}

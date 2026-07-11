using Utano.Module.Billing.Domain.Enums;
using Utano.Module.Core.Exceptions;

namespace Utano.Module.Billing.Domain.Entities;

public class PaymentPlan
{
    private readonly List<PaymentPlanInstallment> _installments = [];
    private PaymentPlan() { }

    public Guid Id { get; private set; }
    public Guid PracticeId { get; private set; }
    public Guid InvoiceId { get; private set; }
    public decimal TotalAmount { get; private set; }
    public decimal AmountPaid { get; private set; }
    public int InstallmentCount { get; private set; }
    public PaymentPlanFrequency Frequency { get; private set; }
    public DateOnly StartDate { get; private set; }
    public PaymentPlanStatus Status { get; private set; }
    public string? Notes { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }

    public IReadOnlyList<PaymentPlanInstallment> Installments => _installments.AsReadOnly();

    public static PaymentPlan Create(Guid practiceId, Guid invoiceId, decimal totalAmount,
        int installmentCount, PaymentPlanFrequency frequency, DateOnly startDate, string? notes)
    {
        if (installmentCount < 2) throw new UtanoDomainException("A payment plan requires at least 2 installments.");
        if (totalAmount <= 0) throw new UtanoDomainException("Total amount must be positive.");

        var plan = new PaymentPlan
        {
            Id = Guid.NewGuid(),
            PracticeId = practiceId,
            InvoiceId = invoiceId,
            TotalAmount = totalAmount,
            InstallmentCount = installmentCount,
            Frequency = frequency,
            StartDate = startDate,
            Status = PaymentPlanStatus.Active,
            Notes = notes,
            CreatedAt = DateTimeOffset.UtcNow
        };

        var perInstallment = Math.Floor(totalAmount / installmentCount * 100) / 100m;
        var remainder = totalAmount - perInstallment * installmentCount;

        for (var i = 0; i < installmentCount; i++)
        {
            var daysOffset = frequency switch
            {
                PaymentPlanFrequency.Weekly => i * 7,
                PaymentPlanFrequency.Biweekly => i * 14,
                PaymentPlanFrequency.Monthly => i * 30,
                _ => i * 30
            };
            var amount = i == installmentCount - 1 ? perInstallment + remainder : perInstallment;
            plan._installments.Add(PaymentPlanInstallment.Create(plan.Id, i + 1,
                startDate.AddDays(daysOffset), Math.Round(amount, 2)));
        }

        return plan;
    }

    public void MarkInstallmentPaid(Guid installmentId, decimal amount)
    {
        var inst = _installments.FirstOrDefault(i => i.Id == installmentId)
            ?? throw new UtanoDomainException("Installment not found.");
        inst.RecordPayment(amount);
        AmountPaid += amount;
        if (AmountPaid >= TotalAmount)
            Status = PaymentPlanStatus.Completed;
    }
}

public class PaymentPlanInstallment
{
    private PaymentPlanInstallment() { }

    public Guid Id { get; private set; }
    public Guid PaymentPlanId { get; private set; }
    public int InstallmentNumber { get; private set; }
    public DateOnly DueDate { get; private set; }
    public decimal Amount { get; private set; }
    public decimal PaidAmount { get; private set; }
    public PaymentInstallmentStatus Status { get; private set; }

    internal static PaymentPlanInstallment Create(Guid planId, int number, DateOnly dueDate, decimal amount)
    {
        return new PaymentPlanInstallment
        {
            Id = Guid.NewGuid(),
            PaymentPlanId = planId,
            InstallmentNumber = number,
            DueDate = dueDate,
            Amount = amount,
            Status = PaymentInstallmentStatus.Pending
        };
    }

    internal void RecordPayment(decimal amount)
    {
        PaidAmount += amount;
        Status = PaidAmount >= Amount ? PaymentInstallmentStatus.Paid : PaymentInstallmentStatus.PartiallyPaid;
    }
}

public enum PaymentInstallmentStatus { Pending, PartiallyPaid, Paid, Overdue }

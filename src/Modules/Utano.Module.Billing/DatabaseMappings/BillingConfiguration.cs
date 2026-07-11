using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Utano.Module.Billing.Domain.Entities;

namespace Utano.Module.Billing.DatabaseMappings;

public class InvoiceConfiguration : IEntityTypeConfiguration<Invoice>
{
    public void Configure(EntityTypeBuilder<Invoice> b)
    {
        b.ToTable("Invoices");
        b.HasKey(i => i.Id);
        b.Property(i => i.InvoiceNumber).HasMaxLength(30).IsRequired();
        b.Property(i => i.PatientName).HasMaxLength(200).IsRequired();
        b.Property(i => i.DoctorName).HasMaxLength(200);
        b.Property(i => i.Status).HasConversion<string>().HasMaxLength(30).IsRequired();
        b.Property(i => i.Currency).HasMaxLength(10).IsRequired();
        b.Property(i => i.SubTotal).HasPrecision(18, 4);
        b.Property(i => i.DiscountAmount).HasPrecision(18, 4);
        b.Property(i => i.TaxAmount).HasPrecision(18, 4);
        b.Property(i => i.TotalAmount).HasPrecision(18, 4);
        b.Property(i => i.AmountPaid).HasPrecision(18, 4);
        b.Property(i => i.MedicalAidName).HasMaxLength(200);
        b.Property(i => i.MedAidClaimAmount).HasPrecision(18, 4);
        b.Property(i => i.MedAidClaimStatus).HasConversion<string>().HasMaxLength(20);
        b.Property(i => i.Notes).HasMaxLength(1000);
        b.Ignore(i => i.BalanceDue);

        b.HasMany(i => i.LineItems).WithOne().HasForeignKey(l => l.InvoiceId).OnDelete(DeleteBehavior.Cascade);

        b.HasIndex(i => i.PracticeId);
        b.HasIndex(i => new { i.PracticeId, i.PatientId });
        b.HasIndex(i => new { i.PracticeId, i.InvoiceNumber }).IsUnique();
    }
}

public class InvoiceLineItemConfiguration : IEntityTypeConfiguration<InvoiceLineItem>
{
    public void Configure(EntityTypeBuilder<InvoiceLineItem> b)
    {
        b.ToTable("InvoiceLineItems");
        b.HasKey(l => l.Id);
        b.Property(l => l.Type).HasConversion<string>().HasMaxLength(30).IsRequired();
        b.Property(l => l.Description).HasMaxLength(500).IsRequired();
        b.Property(l => l.Quantity).HasPrecision(18, 4);
        b.Property(l => l.UnitPrice).HasPrecision(18, 4);
        b.Property(l => l.DiscountPercent).HasPrecision(5, 2);
        b.Property(l => l.Amount).HasPrecision(18, 4);
    }
}

public class PaymentConfiguration : IEntityTypeConfiguration<Payment>
{
    public void Configure(EntityTypeBuilder<Payment> b)
    {
        b.ToTable("Payments");
        b.HasKey(p => p.Id);
        b.Property(p => p.Amount).HasPrecision(18, 4).IsRequired();
        b.Property(p => p.Method).HasConversion<string>().HasMaxLength(30).IsRequired();
        b.Property(p => p.Reference).HasMaxLength(100);
        b.Property(p => p.Notes).HasMaxLength(500);
        b.Property(p => p.FiscalReceiptNumber).HasMaxLength(100);
        b.HasIndex(p => p.InvoiceId);
    }
}

public class PaymentPlanConfiguration : IEntityTypeConfiguration<PaymentPlan>
{
    public void Configure(EntityTypeBuilder<PaymentPlan> b)
    {
        b.ToTable("PaymentPlans");
        b.HasKey(pp => pp.Id);
        b.Property(pp => pp.TotalAmount).HasPrecision(18, 4);
        b.Property(pp => pp.AmountPaid).HasPrecision(18, 4);
        b.Property(pp => pp.Frequency).HasConversion<string>().HasMaxLength(20);
        b.Property(pp => pp.Status).HasConversion<string>().HasMaxLength(20);
        b.Property(pp => pp.Notes).HasMaxLength(500);

        b.HasMany(pp => pp.Installments).WithOne().HasForeignKey(i => i.PaymentPlanId).OnDelete(DeleteBehavior.Cascade);
        b.HasIndex(pp => pp.InvoiceId).IsUnique();
    }
}

public class PaymentPlanInstallmentConfiguration : IEntityTypeConfiguration<PaymentPlanInstallment>
{
    public void Configure(EntityTypeBuilder<PaymentPlanInstallment> b)
    {
        b.ToTable("PaymentPlanInstallments");
        b.HasKey(i => i.Id);
        b.Property(i => i.Amount).HasPrecision(18, 4);
        b.Property(i => i.PaidAmount).HasPrecision(18, 4);
        b.Property(i => i.Status).HasConversion<string>().HasMaxLength(20);
    }
}

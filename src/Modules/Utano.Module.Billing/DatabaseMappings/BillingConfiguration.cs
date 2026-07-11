using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Utano.Module.Billing.Domain.Entities;
using Utano.Module.Billing.Domain.Enums;

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
        b.Property(l => l.ServiceItemId);
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

public class ServiceItemConfiguration : IEntityTypeConfiguration<ServiceItem>
{
    // Stable seed date — do not change (migration snapshot depends on it)
    private static readonly DateTimeOffset _seed = new(2024, 1, 1, 0, 0, 0, TimeSpan.Zero);

    public void Configure(EntityTypeBuilder<ServiceItem> b)
    {
        b.ToTable("ServiceItems");
        b.HasKey(s => s.Id);
        b.Property(s => s.Name).HasMaxLength(200).IsRequired();
        b.Property(s => s.Category).HasConversion<string>().HasMaxLength(30).IsRequired();
        b.Property(s => s.NhrplCode).HasMaxLength(20);
        b.Property(s => s.DefaultIcdCode).HasMaxLength(20);
        b.Property(s => s.DefaultPrice).HasPrecision(18, 4);
        b.Property(s => s.AppointmentTypeKey).HasMaxLength(50);

        b.HasIndex(s => s.PracticeId);
        b.HasIndex(s => new { s.PracticeId, s.NhrplCode });

        b.HasData(
            // ── Consultations ───────────────────────────────────────────
            new
            {
                Id = Guid.Parse("10000000-0000-0000-0000-000000000001"),
                PracticeId = (Guid?)null,
                Name = "General Consultation",
                Category = ServiceItemCategory.Consultation,
                NhrplCode = "0190",
                DefaultIcdCode = (string?)null,
                DefaultPrice = 450m,
                AppointmentTypeKey = "Consultation",
                IsActive = true,
                CreatedAt = _seed,
                UpdatedAt = _seed
            },
            new
            {
                Id = Guid.Parse("10000000-0000-0000-0000-000000000002"),
                PracticeId = (Guid?)null,
                Name = "Follow-up Consultation",
                Category = ServiceItemCategory.Consultation,
                NhrplCode = "0191",
                DefaultIcdCode = (string?)null,
                DefaultPrice = 350m,
                AppointmentTypeKey = "FollowUp",
                IsActive = true,
                CreatedAt = _seed,
                UpdatedAt = _seed
            },
            new
            {
                Id = Guid.Parse("10000000-0000-0000-0000-000000000003"),
                PracticeId = (Guid?)null,
                Name = "Walk-in Consultation",
                Category = ServiceItemCategory.Consultation,
                NhrplCode = "0190",
                DefaultIcdCode = (string?)null,
                DefaultPrice = 450m,
                AppointmentTypeKey = "WalkIn",
                IsActive = true,
                CreatedAt = _seed,
                UpdatedAt = _seed
            },
            new
            {
                Id = Guid.Parse("10000000-0000-0000-0000-000000000004"),
                PracticeId = (Guid?)null,
                Name = "After Hours Consultation",
                Category = ServiceItemCategory.Consultation,
                NhrplCode = "0188",
                DefaultIcdCode = (string?)null,
                DefaultPrice = 550m,
                AppointmentTypeKey = (string?)null,
                IsActive = true,
                CreatedAt = _seed,
                UpdatedAt = _seed
            },
            // ── Procedures ──────────────────────────────────────────────
            new
            {
                Id = Guid.Parse("10000000-0000-0000-0000-000000000005"),
                PracticeId = (Guid?)null,
                Name = "Minor Wound Dressing",
                Category = ServiceItemCategory.Procedure,
                NhrplCode = "0177",
                DefaultIcdCode = (string?)null,
                DefaultPrice = 180m,
                AppointmentTypeKey = (string?)null,
                IsActive = true,
                CreatedAt = _seed,
                UpdatedAt = _seed
            },
            new
            {
                Id = Guid.Parse("10000000-0000-0000-0000-000000000006"),
                PracticeId = (Guid?)null,
                Name = "Sutures (Simple)",
                Category = ServiceItemCategory.Procedure,
                NhrplCode = "0178",
                DefaultIcdCode = (string?)null,
                DefaultPrice = 280m,
                AppointmentTypeKey = (string?)null,
                IsActive = true,
                CreatedAt = _seed,
                UpdatedAt = _seed
            },
            new
            {
                Id = Guid.Parse("10000000-0000-0000-0000-000000000007"),
                PracticeId = (Guid?)null,
                Name = "Intramuscular Injection",
                Category = ServiceItemCategory.Procedure,
                NhrplCode = "0159",
                DefaultIcdCode = (string?)null,
                DefaultPrice = 120m,
                AppointmentTypeKey = (string?)null,
                IsActive = true,
                CreatedAt = _seed,
                UpdatedAt = _seed
            },
            new
            {
                Id = Guid.Parse("10000000-0000-0000-0000-000000000008"),
                PracticeId = (Guid?)null,
                Name = "ECG (12-lead)",
                Category = ServiceItemCategory.Procedure,
                NhrplCode = "3242",
                DefaultIcdCode = (string?)null,
                DefaultPrice = 350m,
                AppointmentTypeKey = (string?)null,
                IsActive = true,
                CreatedAt = _seed,
                UpdatedAt = _seed
            },
            new
            {
                Id = Guid.Parse("10000000-0000-0000-0000-000000000009"),
                PracticeId = (Guid?)null,
                Name = "Nebulisation",
                Category = ServiceItemCategory.Procedure,
                NhrplCode = "0167",
                DefaultIcdCode = (string?)null,
                DefaultPrice = 200m,
                AppointmentTypeKey = (string?)null,
                IsActive = true,
                CreatedAt = _seed,
                UpdatedAt = _seed
            }
        );
    }
}

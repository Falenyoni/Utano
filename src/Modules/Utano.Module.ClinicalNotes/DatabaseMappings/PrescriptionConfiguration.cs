using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Utano.Module.ClinicalNotes.Domain.Entities;

namespace Utano.Module.ClinicalNotes.DatabaseMappings;

public class PrescriptionConfiguration : IEntityTypeConfiguration<Prescription>
{
    public void Configure(EntityTypeBuilder<Prescription> builder)
    {
        builder.ToTable("Prescriptions", "clinical");
        builder.HasKey(p => p.Id);

        builder.Property(p => p.PatientName).IsRequired().HasMaxLength(200);
        builder.Property(p => p.StockItemName).IsRequired().HasMaxLength(200);
        builder.Property(p => p.Description).IsRequired().HasMaxLength(500);
        builder.Property(p => p.DosageInstructions).HasMaxLength(1000);
        builder.Property(p => p.Quantity).HasPrecision(10, 3);
        builder.Property(p => p.QuantityDispensed).HasPrecision(10, 3);

        builder.Property(p => p.Status)
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.HasIndex(p => new { p.PracticeId, p.VisitId });
        builder.HasIndex(p => new { p.PracticeId, p.PatientId });
    }
}

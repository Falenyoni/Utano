using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Utano.Module.ClinicalNotes.Domain.Entities;
using Utano.Module.ClinicalNotes.Domain.Enums;

namespace Utano.Module.ClinicalNotes.DatabaseMappings;

public class VisitConfiguration : IEntityTypeConfiguration<Visit>
{
    public void Configure(EntityTypeBuilder<Visit> builder)
    {
        builder.ToTable("Visits", "clinical");
        builder.HasKey(v => v.Id);

        builder.Property(v => v.PatientName).IsRequired().HasMaxLength(200);
        builder.Property(v => v.DoctorName).IsRequired().HasMaxLength(200);

        builder.Property(v => v.Department).HasMaxLength(100);
        builder.Property(v => v.ChiefComplaint).HasMaxLength(500);
        builder.Property(v => v.Symptoms).HasMaxLength(2000);
        builder.Property(v => v.Diagnosis).HasMaxLength(2000);
        builder.Property(v => v.Treatment).HasMaxLength(2000);
        builder.Property(v => v.Prescription).HasMaxLength(2000);
        builder.Property(v => v.Notes).HasMaxLength(2000);

        builder.Property(v => v.Status)
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.Property(v => v.WeightKg).HasPrecision(5, 2);
        builder.Property(v => v.HeightCm).HasPrecision(5, 2);
        builder.Property(v => v.TemperatureCelsius).HasPrecision(4, 1);
        builder.Property(v => v.OxygenSaturation).HasPrecision(4, 1);

        builder.Property(v => v.AppointmentId);

        builder.HasIndex(v => new { v.PracticeId, v.VisitDate });
        builder.HasIndex(v => new { v.PracticeId, v.PatientId });
        builder.HasIndex(v => v.AppointmentId).IsUnique().HasFilter("\"AppointmentId\" IS NOT NULL");
    }
}

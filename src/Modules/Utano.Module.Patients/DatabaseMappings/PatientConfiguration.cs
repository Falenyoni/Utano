using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Utano.Module.Patients.Domain.Entities;
using Utano.Module.Patients.Domain.ValueObjects;

namespace Utano.Module.Patients.DatabaseMappings;

public class PatientConfiguration : IEntityTypeConfiguration<Patient>
{
    public void Configure(EntityTypeBuilder<Patient> builder)
    {
        builder.ToTable("Patients");

        builder.HasKey(p => p.Id);

        builder.Property(p => p.PracticeId).IsRequired();

        builder.OwnsOne(p => p.FullName, fn =>
        {
            fn.Property(f => f.FirstName).HasColumnName("FirstName").HasMaxLength(100).IsRequired();
            fn.Property(f => f.LastName).HasColumnName("LastName").HasMaxLength(100).IsRequired();
            fn.Property(f => f.MiddleName).HasColumnName("MiddleName").HasMaxLength(100);
        });

        builder.Property(p => p.NationalId)
            .HasConversion(v => v.Value, v => NationalId.Create(v))
            .HasColumnName("NationalId")
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(p => p.DateOfBirth).IsRequired();

        builder.Property(p => p.Gender)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(p => p.Status)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(p => p.Notes).HasMaxLength(2000);
        builder.Property(p => p.BloodGroup).HasConversion<string>().HasMaxLength(20);
        builder.Property(p => p.Allergies).HasMaxLength(1000);
        builder.Property(p => p.ChronicConditions).HasMaxLength(1000);
        builder.Property(p => p.MedicalAidId);
        builder.Property(p => p.MedicalAidNumber).HasMaxLength(50);

        builder.Property(p => p.CreatedAt).IsRequired();
        builder.Property(p => p.UpdatedAt).IsRequired();

        builder.HasMany(p => p.Contacts)
            .WithOne()
            .HasForeignKey(c => c.PatientId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(p => p.Addresses)
            .WithOne()
            .HasForeignKey(a => a.PatientId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(p => p.PracticeId);
        builder.HasIndex(p => new { p.PracticeId, p.Status });
        builder.HasIndex(p => new { p.PracticeId, p.NationalId }).IsUnique();
    }
}

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Utano.Module.Patients.Domain.Entities;

namespace Utano.Module.Patients.DatabaseMappings;

public class PatientAddressConfiguration : IEntityTypeConfiguration<PatientAddress>
{
    public void Configure(EntityTypeBuilder<PatientAddress> builder)
    {
        builder.ToTable("PatientAddresses");

        builder.HasKey(a => a.Id);

        builder.Property(a => a.PatientId).IsRequired();

        builder.Property(a => a.Type)
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(a => a.Street)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(a => a.Suburb)
            .HasMaxLength(100);

        builder.Property(a => a.City)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(a => a.Country)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(a => a.IsPrimary).IsRequired();

        builder.HasIndex(a => a.PatientId);
    }
}

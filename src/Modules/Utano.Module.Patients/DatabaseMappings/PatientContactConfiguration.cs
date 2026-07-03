using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Utano.Module.Patients.Domain.Entities;

namespace Utano.Module.Patients.DatabaseMappings;

public class PatientContactConfiguration : IEntityTypeConfiguration<PatientContact>
{
    public void Configure(EntityTypeBuilder<PatientContact> builder)
    {
        builder.ToTable("PatientContacts");

        builder.HasKey(c => c.Id);

        builder.Property(c => c.PatientId).IsRequired();

        builder.Property(c => c.Type)
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(c => c.PhoneNumber)
            .HasMaxLength(30)
            .IsRequired();

        builder.Property(c => c.Email)
            .HasMaxLength(255);

        builder.Property(c => c.IsPrimary).IsRequired();

        builder.HasIndex(c => c.PatientId);
    }
}

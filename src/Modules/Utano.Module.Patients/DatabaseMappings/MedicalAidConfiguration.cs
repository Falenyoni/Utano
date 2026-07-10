using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Utano.Module.Patients.Domain.Entities;

namespace Utano.Module.Patients.DatabaseMappings;

public class MedicalAidConfiguration : IEntityTypeConfiguration<MedicalAid>
{
    public void Configure(EntityTypeBuilder<MedicalAid> builder)
    {
        builder.ToTable("MedicalAids");

        builder.HasKey(m => m.Id);
        builder.Property(m => m.Name).IsRequired().HasMaxLength(150);
        builder.Property(m => m.Code).IsRequired().HasMaxLength(20);
        builder.Property(m => m.IsActive).IsRequired();
        builder.Property(m => m.CreatedAt).IsRequired();
        builder.Property(m => m.UpdatedAt).IsRequired();

        builder.HasIndex(m => new { m.PracticeId, m.Code }).IsUnique();
    }
}

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Utano.Module.ClinicalNotes.Domain.Entities;

namespace Utano.Module.ClinicalNotes.DatabaseMappings;

public class VisitDiagnosisConfiguration : IEntityTypeConfiguration<VisitDiagnosis>
{
    public void Configure(EntityTypeBuilder<VisitDiagnosis> b)
    {
        b.ToTable("VisitDiagnoses");
        b.HasKey(d => d.Id);
        b.Property(d => d.IcdCode).HasMaxLength(20).IsRequired();
        b.Property(d => d.Description).HasMaxLength(500).IsRequired();

        b.HasIndex(d => d.VisitId);
        b.HasIndex(d => d.PracticeId);
    }
}

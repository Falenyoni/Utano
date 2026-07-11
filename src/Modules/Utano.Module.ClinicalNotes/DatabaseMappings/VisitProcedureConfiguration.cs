using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Utano.Module.ClinicalNotes.Domain.Entities;

namespace Utano.Module.ClinicalNotes.DatabaseMappings;

public class VisitProcedureConfiguration : IEntityTypeConfiguration<VisitProcedure>
{
    public void Configure(EntityTypeBuilder<VisitProcedure> b)
    {
        b.ToTable("VisitProcedures");
        b.HasKey(p => p.Id);
        b.Property(p => p.Name).HasMaxLength(200).IsRequired();
        b.Property(p => p.NhrplCode).HasMaxLength(20);
        b.Property(p => p.Notes).HasMaxLength(2000);

        b.HasIndex(p => p.VisitId);
        b.HasIndex(p => p.PracticeId);
    }
}

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Utano.Module.ClinicalNotes.Domain.Entities;

namespace Utano.Module.ClinicalNotes.DatabaseMappings;

public class AuditLogConfiguration : IEntityTypeConfiguration<AuditLog>
{
    public void Configure(EntityTypeBuilder<AuditLog> builder)
    {
        builder.ToTable("AuditLogs", "clinical");
        builder.HasKey(a => a.Id);

        builder.Property(a => a.UserId).IsRequired();
        builder.Property(a => a.UserName).IsRequired().HasMaxLength(200);
        builder.Property(a => a.EntityType).IsRequired().HasMaxLength(100);
        builder.Property(a => a.EntityId).IsRequired().HasMaxLength(200);
        builder.Property(a => a.Action).IsRequired().HasMaxLength(100);
        builder.Property(a => a.Description).HasMaxLength(1000);

        builder.HasIndex(a => new { a.PracticeId, a.Timestamp });
        builder.HasIndex(a => new { a.PracticeId, a.EntityType, a.EntityId });
    }
}

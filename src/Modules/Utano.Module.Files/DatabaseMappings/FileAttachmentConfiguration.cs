using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Utano.Module.Files.Domain.Entities;

namespace Utano.Module.Files.DatabaseMappings;

public class FileAttachmentConfiguration : IEntityTypeConfiguration<FileAttachment>
{
    public void Configure(EntityTypeBuilder<FileAttachment> builder)
    {
        builder.ToTable("FileAttachments");

        builder.HasKey(f => f.Id);

        builder.Property(f => f.PracticeId).IsRequired();
        builder.Property(f => f.PatientId).IsRequired();
        builder.Property(f => f.ConsultationId);

        builder.Property(f => f.FileName).HasMaxLength(500).IsRequired();
        builder.Property(f => f.ObjectKey).HasMaxLength(1000).IsRequired();
        builder.Property(f => f.ContentType).HasMaxLength(200).IsRequired();
        builder.Property(f => f.SizeBytes).IsRequired();

        builder.Property(f => f.AttachmentType)
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(f => f.Description).HasMaxLength(1000);
        builder.Property(f => f.IsDeleted).IsRequired().HasDefaultValue(false);

        builder.Property(f => f.CreatedAt).IsRequired();
        builder.Property(f => f.UpdatedAt).IsRequired();

        builder.HasIndex(f => f.PracticeId);
        builder.HasIndex(f => new { f.PracticeId, f.PatientId });
        builder.HasIndex(f => new { f.PracticeId, f.PatientId, f.AttachmentType });
        builder.HasIndex(f => f.ConsultationId).HasFilter("\"ConsultationId\" IS NOT NULL");
    }
}

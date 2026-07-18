using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Utano.Module.Notifications.Domain.Entities;

namespace Utano.Module.Notifications.DatabaseMappings;

public class NotificationConfiguration : IEntityTypeConfiguration<Notification>
{
    public void Configure(EntityTypeBuilder<Notification> builder)
    {
        builder.HasKey(n => n.Id);
        builder.Property(n => n.SenderName).HasMaxLength(200).IsRequired();
        builder.Property(n => n.Title).HasMaxLength(300).IsRequired();
        builder.Property(n => n.Message).HasMaxLength(2000).IsRequired();
        builder.Property(n => n.Type).HasMaxLength(50).IsRequired();

        builder.HasIndex(n => new { n.PracticeId, n.RecipientUserId, n.IsRead });
        builder.HasIndex(n => n.CreatedAt);
    }
}

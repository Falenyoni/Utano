using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Utano.Module.Notifications.Domain.Entities;

namespace Utano.Module.Notifications.DatabaseMappings;

public class NotificationPreferenceConfiguration : IEntityTypeConfiguration<NotificationPreference>
{
    public void Configure(EntityTypeBuilder<NotificationPreference> builder)
    {
        builder.HasKey(p => p.Id);
        builder.HasIndex(p => new { p.PracticeId, p.UserId }).IsUnique();
    }
}

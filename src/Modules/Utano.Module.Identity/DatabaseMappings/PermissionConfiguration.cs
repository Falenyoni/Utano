using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Utano.Module.Identity.Domain.Entities;

namespace Utano.Module.Identity.DatabaseMappings;

public class PermissionConfiguration : IEntityTypeConfiguration<Permission>
{
    public void Configure(EntityTypeBuilder<Permission> builder)
    {
        builder.ToTable("Permissions");

        builder.HasKey(p => p.Key);
        builder.Property(p => p.Key).HasMaxLength(100).IsRequired();
        builder.Property(p => p.Description).HasMaxLength(500);
    }
}

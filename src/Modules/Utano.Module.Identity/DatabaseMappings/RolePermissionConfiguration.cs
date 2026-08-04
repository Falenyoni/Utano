using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Utano.Module.Identity.Domain.Entities;

namespace Utano.Module.Identity.DatabaseMappings;

public class RolePermissionConfiguration : IEntityTypeConfiguration<RolePermission>
{
    public void Configure(EntityTypeBuilder<RolePermission> builder)
    {
        builder.ToTable("RolePermissions");

        builder.HasKey(rp => new { rp.RoleId, rp.PermissionKey });

        builder.Property(rp => rp.PermissionKey).HasMaxLength(100).IsRequired();

        // Real referential integrity - a typo'd or orphaned permission key is now rejected at
        // write time instead of silently inserting and never matching anything. Restrict (not
        // cascade) on delete: a Permission row should never normally be deleted while roles still
        // reference it.
        builder.HasOne<Permission>()
            .WithMany()
            .HasForeignKey(rp => rp.PermissionKey)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

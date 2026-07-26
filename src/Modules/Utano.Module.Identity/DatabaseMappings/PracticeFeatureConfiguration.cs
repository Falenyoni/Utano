using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Utano.Module.Identity.Domain.Entities;

namespace Utano.Module.Identity.DatabaseMappings;

public class PracticeFeatureConfiguration : IEntityTypeConfiguration<PracticeFeature>
{
    public void Configure(EntityTypeBuilder<PracticeFeature> builder)
    {
        builder.ToTable("PracticeFeatures");

        builder.HasKey(pf => pf.Id);

        builder.Property(pf => pf.PracticeId).IsRequired();
        builder.Property(pf => pf.FeatureKey).HasMaxLength(100).IsRequired();
        builder.Property(pf => pf.IsEnabled).IsRequired();
        builder.Property(pf => pf.EnabledAt).IsRequired();

        builder.HasIndex(pf => new { pf.PracticeId, pf.FeatureKey }).IsUnique();
    }
}

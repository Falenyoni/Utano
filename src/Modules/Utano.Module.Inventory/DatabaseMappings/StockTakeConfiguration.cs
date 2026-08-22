using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Utano.Module.Inventory.Domain.Entities;

namespace Utano.Module.Inventory.DatabaseMappings;

public class StockTakeConfiguration : IEntityTypeConfiguration<StockTake>
{
    public void Configure(EntityTypeBuilder<StockTake> builder)
    {
        builder.ToTable("StockTakes");
        builder.HasKey(t => t.Id);
        builder.Property(t => t.PracticeId).IsRequired();
        builder.Property(t => t.Category).HasConversion<string>().HasMaxLength(50);
        builder.Property(t => t.Status).HasConversion<string>().HasMaxLength(50).IsRequired();
        builder.Property(t => t.StartedByName).HasMaxLength(200).IsRequired();
        builder.Property(t => t.CompletedByName).HasMaxLength(200);
        builder.Property(t => t.Notes).HasMaxLength(1000);
        builder.Property(t => t.TotalVarianceValue).HasPrecision(18, 4);
        builder.Property(t => t.StartedAt).IsRequired();
        builder.Property(t => t.CreatedAt).IsRequired();
        builder.Property(t => t.UpdatedAt).IsRequired();

        builder.HasMany(t => t.Lines)
            .WithOne()
            .HasForeignKey(l => l.StockTakeId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(t => new { t.PracticeId, t.StartedAt });
    }
}

public class StockTakeLineConfiguration : IEntityTypeConfiguration<StockTakeLine>
{
    public void Configure(EntityTypeBuilder<StockTakeLine> builder)
    {
        builder.ToTable("StockTakeLines");
        builder.HasKey(l => l.Id);
        builder.Property(l => l.StockTakeId).IsRequired();
        builder.Property(l => l.StockItemId).IsRequired();
        builder.Property(l => l.StockItemName).HasMaxLength(200).IsRequired();
        builder.Property(l => l.ExpectedQuantity).HasPrecision(18, 4).IsRequired();
        builder.Property(l => l.UnitCostSnapshot).HasPrecision(18, 4).IsRequired();
        builder.Property(l => l.CountedQuantity).HasPrecision(18, 4);
        builder.Property(l => l.Variance).HasPrecision(18, 4);

        builder.HasIndex(l => l.StockTakeId);
    }
}

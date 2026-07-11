using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Utano.Module.Inventory.Domain.Entities;

namespace Utano.Module.Inventory.DatabaseMappings;

public class StockItemConfiguration : IEntityTypeConfiguration<StockItem>
{
    public void Configure(EntityTypeBuilder<StockItem> builder)
    {
        builder.ToTable("StockItems");
        builder.HasKey(s => s.Id);
        builder.Property(s => s.PracticeId).IsRequired();
        builder.Property(s => s.Name).HasMaxLength(200).IsRequired();
        builder.Property(s => s.Sku).HasMaxLength(100);
        builder.Property(s => s.Description).HasMaxLength(1000);
        builder.Property(s => s.Category).HasConversion<string>().HasMaxLength(50).IsRequired();
        builder.Property(s => s.Unit).HasMaxLength(50).IsRequired();
        builder.Property(s => s.SellingPrice).HasPrecision(18, 4).IsRequired();
        builder.Property(s => s.CostPrice).HasPrecision(18, 4).IsRequired();
        builder.Property(s => s.QuantityOnHand).HasPrecision(18, 4).IsRequired();
        builder.Property(s => s.ReorderLevel).HasPrecision(18, 4).IsRequired();
        builder.Property(s => s.IsActive).IsRequired();
        builder.Property(s => s.CreatedAt).IsRequired();
        builder.Property(s => s.UpdatedAt).IsRequired();

        builder.HasIndex(s => s.PracticeId);
        builder.HasIndex(s => new { s.PracticeId, s.IsActive });
    }
}

public class StockTransactionConfiguration : IEntityTypeConfiguration<StockTransaction>
{
    public void Configure(EntityTypeBuilder<StockTransaction> builder)
    {
        builder.ToTable("StockTransactions");
        builder.HasKey(t => t.Id);
        builder.Property(t => t.PracticeId).IsRequired();
        builder.Property(t => t.StockItemId).IsRequired();
        builder.Property(t => t.StockItemName).HasMaxLength(200).IsRequired();
        builder.Property(t => t.Type).HasConversion<string>().HasMaxLength(50).IsRequired();
        builder.Property(t => t.Quantity).HasPrecision(18, 4).IsRequired();
        builder.Property(t => t.QuantityBefore).HasPrecision(18, 4).IsRequired();
        builder.Property(t => t.QuantityAfter).HasPrecision(18, 4).IsRequired();
        builder.Property(t => t.UnitCost).HasPrecision(18, 4).IsRequired();
        builder.Property(t => t.Notes).HasMaxLength(500);
        builder.Property(t => t.ReferenceType).HasMaxLength(50);
        builder.Property(t => t.CreatedAt).IsRequired();

        builder.HasIndex(t => t.StockItemId);
        builder.HasIndex(t => new { t.PracticeId, t.CreatedAt });
    }
}

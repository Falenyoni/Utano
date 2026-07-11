using Utano.Module.Core.Domain.Aggregate;
using Utano.Module.Core.Exceptions;
using Utano.Module.Inventory.Domain.Enums;

namespace Utano.Module.Inventory.Domain.Entities;

public class StockItem : AggregateRoot
{
    private StockItem() { }

    public string Name { get; private set; } = null!;
    public string? Sku { get; private set; }
    public string? Description { get; private set; }
    public StockCategory Category { get; private set; }
    public string Unit { get; private set; } = null!;
    public decimal SellingPrice { get; private set; }
    public decimal CostPrice { get; private set; }
    public decimal QuantityOnHand { get; private set; }
    public decimal ReorderLevel { get; private set; }
    public bool IsActive { get; private set; }

    public bool IsLowStock => QuantityOnHand <= ReorderLevel && ReorderLevel > 0;

    public static StockItem Create(
        Guid practiceId,
        string name,
        StockCategory category,
        string unit,
        decimal sellingPrice,
        decimal costPrice,
        decimal reorderLevel,
        string? sku = null,
        string? description = null)
    {
        if (string.IsNullOrWhiteSpace(name)) throw new UtanoDomainException("Stock item name is required.");
        if (string.IsNullOrWhiteSpace(unit)) throw new UtanoDomainException("Unit is required.");
        if (sellingPrice < 0) throw new UtanoDomainException("Selling price cannot be negative.");
        if (costPrice < 0) throw new UtanoDomainException("Cost price cannot be negative.");

        return new StockItem
        {
            Id = Guid.NewGuid(),
            PracticeId = practiceId,
            Name = name.Trim(),
            Sku = sku?.Trim(),
            Description = description?.Trim(),
            Category = category,
            Unit = unit.Trim(),
            SellingPrice = sellingPrice,
            CostPrice = costPrice,
            QuantityOnHand = 0,
            ReorderLevel = reorderLevel,
            IsActive = true,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };
    }

    public void UpdateDetails(string name, string? sku, string? description, StockCategory category,
        string unit, decimal sellingPrice, decimal costPrice, decimal reorderLevel)
    {
        if (string.IsNullOrWhiteSpace(name)) throw new UtanoDomainException("Stock item name is required.");
        Name = name.Trim();
        Sku = sku?.Trim();
        Description = description?.Trim();
        Category = category;
        Unit = unit.Trim();
        SellingPrice = sellingPrice;
        CostPrice = costPrice;
        ReorderLevel = reorderLevel;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public StockTransaction Receive(decimal quantity, decimal? unitCost, string? notes, Guid? referenceId = null)
    {
        if (quantity <= 0) throw new UtanoDomainException("Receive quantity must be positive.");
        var before = QuantityOnHand;
        QuantityOnHand += quantity;
        UpdatedAt = DateTimeOffset.UtcNow;
        return StockTransaction.Create(PracticeId, Id, Name, TransactionType.Received,
            quantity, before, QuantityOnHand, unitCost ?? CostPrice, notes, "Manual", referenceId);
    }

    public StockTransaction Dispense(decimal quantity, string? notes, Guid? referenceId = null, string referenceType = "Invoice")
    {
        if (quantity <= 0) throw new UtanoDomainException("Dispense quantity must be positive.");
        if (quantity > QuantityOnHand) throw new UtanoDomainException($"Insufficient stock. Available: {QuantityOnHand} {Unit}.");
        var before = QuantityOnHand;
        QuantityOnHand -= quantity;
        UpdatedAt = DateTimeOffset.UtcNow;
        return StockTransaction.Create(PracticeId, Id, Name, TransactionType.Dispensed,
            quantity, before, QuantityOnHand, SellingPrice, notes, referenceType, referenceId);
    }

    public StockTransaction Adjust(decimal quantity, string? notes)
    {
        var before = QuantityOnHand;
        QuantityOnHand += quantity;
        if (QuantityOnHand < 0) throw new UtanoDomainException("Adjustment would result in negative stock.");
        UpdatedAt = DateTimeOffset.UtcNow;
        return StockTransaction.Create(PracticeId, Id, Name, TransactionType.Adjusted,
            quantity, before, QuantityOnHand, CostPrice, notes, "Manual", null);
    }

    public void Deactivate()
    {
        IsActive = false;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void Activate()
    {
        IsActive = true;
        UpdatedAt = DateTimeOffset.UtcNow;
    }
}

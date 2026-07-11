using Utano.Module.Inventory.Domain.Enums;

namespace Utano.Module.Inventory.Domain.Entities;

public class StockTransaction
{
    private StockTransaction() { }

    public Guid Id { get; private set; }
    public Guid PracticeId { get; private set; }
    public Guid StockItemId { get; private set; }
    public string StockItemName { get; private set; } = null!;
    public TransactionType Type { get; private set; }
    public decimal Quantity { get; private set; }
    public decimal QuantityBefore { get; private set; }
    public decimal QuantityAfter { get; private set; }
    public decimal UnitCost { get; private set; }
    public string? Notes { get; private set; }
    public string? ReferenceType { get; private set; }
    public Guid? ReferenceId { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }

    internal static StockTransaction Create(
        Guid practiceId, Guid stockItemId, string stockItemName,
        TransactionType type, decimal quantity, decimal before, decimal after,
        decimal unitCost, string? notes, string? referenceType, Guid? referenceId)
    {
        return new StockTransaction
        {
            Id = Guid.NewGuid(),
            PracticeId = practiceId,
            StockItemId = stockItemId,
            StockItemName = stockItemName,
            Type = type,
            Quantity = quantity,
            QuantityBefore = before,
            QuantityAfter = after,
            UnitCost = unitCost,
            Notes = notes,
            ReferenceType = referenceType,
            ReferenceId = referenceId,
            CreatedAt = DateTimeOffset.UtcNow
        };
    }
}

using Utano.Module.Core.Exceptions;

namespace Utano.Module.Inventory.Domain.Entities;

public class StockTakeLine
{
    private StockTakeLine() { }

    public Guid Id { get; private set; }
    public Guid StockTakeId { get; private set; }
    public Guid StockItemId { get; private set; }
    public string StockItemName { get; private set; } = null!;

    // Snapshots taken when the session starts, not live-read from StockItem later - a stock take
    // is a record of what the system believed at a point in time, so it must stay stable even if
    // the item's price or quantity changes while counting is in progress.
    public decimal ExpectedQuantity { get; private set; }
    public decimal UnitCostSnapshot { get; private set; }

    public decimal? CountedQuantity { get; private set; }
    public decimal? Variance { get; private set; }

    internal static StockTakeLine Create(Guid stockTakeId, Guid stockItemId, string stockItemName,
        decimal expectedQuantity, decimal unitCostSnapshot)
    {
        return new StockTakeLine
        {
            Id = Guid.NewGuid(),
            StockTakeId = stockTakeId,
            StockItemId = stockItemId,
            StockItemName = stockItemName,
            ExpectedQuantity = expectedQuantity,
            UnitCostSnapshot = unitCostSnapshot,
        };
    }

    internal void RecordCount(decimal countedQuantity)
    {
        if (countedQuantity < 0) throw new UtanoDomainException("Counted quantity cannot be negative.");
        CountedQuantity = countedQuantity;
        Variance = countedQuantity - ExpectedQuantity;
    }
}

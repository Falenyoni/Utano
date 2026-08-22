using Utano.Module.Core.Domain.Aggregate;
using Utano.Module.Core.Exceptions;
using Utano.Module.Inventory.Domain.Enums;

namespace Utano.Module.Inventory.Domain.Entities;

public class StockTake : AggregateRoot
{
    private StockTake() { }

    private readonly List<StockTakeLine> _lines = [];
    public IReadOnlyCollection<StockTakeLine> Lines => _lines.AsReadOnly();

    public StockCategory? Category { get; private set; } // null = whole inventory
    public StockTakeStatus Status { get; private set; }
    public string StartedByName { get; private set; } = null!;
    public string? CompletedByName { get; private set; }
    public string? Notes { get; private set; }
    public DateTimeOffset StartedAt { get; private set; }
    public DateTimeOffset? CompletedAt { get; private set; }

    // Stored at completion rather than computed on read, since Lines' Variance values are frozen
    // at that point and this is what the history list needs to display cheaply.
    public decimal? TotalVarianceValue { get; private set; }

    public static StockTake Start(
        Guid practiceId, StockCategory? category, string startedByName, string? notes,
        IEnumerable<StockItem> itemsInScope)
    {
        if (string.IsNullOrWhiteSpace(startedByName))
            throw new UtanoDomainException("StartedByName is required.");

        var take = new StockTake
        {
            Id = Guid.NewGuid(),
            PracticeId = practiceId,
            Category = category,
            Status = StockTakeStatus.InProgress,
            StartedByName = startedByName,
            Notes = notes,
            StartedAt = DateTimeOffset.UtcNow,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow,
        };

        foreach (var item in itemsInScope)
            take._lines.Add(StockTakeLine.Create(take.Id, item.Id, item.Name, item.QuantityOnHand, item.CostPrice));

        if (take._lines.Count == 0)
            throw new UtanoDomainException("No active items match this stock take's scope.");

        return take;
    }

    public void RecordCount(Guid stockItemId, decimal countedQuantity)
    {
        if (Status != StockTakeStatus.InProgress)
            throw new UtanoDomainException("This stock take has already been finalized.");

        var line = _lines.FirstOrDefault(l => l.StockItemId == stockItemId)
            ?? throw new UtanoDomainException("This item is not part of this stock take.");

        line.RecordCount(countedQuantity);
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    // Returns the lines that actually need a StockTransaction applied (counted + non-zero
    // variance) - the caller (handler) owns loading and adjusting each StockItem, since that's a
    // cross-aggregate write this entity shouldn't reach into directly. Uncounted lines are simply
    // left with no adjustment, per the "partial counts are fine" scope decision.
    public IReadOnlyList<StockTakeLine> Finalize(string completedByName)
    {
        if (Status != StockTakeStatus.InProgress)
            throw new UtanoDomainException("This stock take has already been finalized.");

        Status = StockTakeStatus.Completed;
        CompletedByName = completedByName;
        CompletedAt = DateTimeOffset.UtcNow;
        UpdatedAt = DateTimeOffset.UtcNow;

        var linesNeedingAdjustment = _lines.Where(l => l.Variance is not (null or 0)).ToList();
        TotalVarianceValue = linesNeedingAdjustment.Sum(l => l.Variance!.Value * l.UnitCostSnapshot);

        return linesNeedingAdjustment;
    }
}

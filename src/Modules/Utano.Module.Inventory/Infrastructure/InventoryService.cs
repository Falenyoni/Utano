using Microsoft.EntityFrameworkCore;
using Utano.Module.Core.Services;
using Utano.Module.Inventory.DatabaseMappings;

namespace Utano.Module.Inventory.Infrastructure;

public class InventoryService(InventoryDbContext db) : IInventoryService
{
    public async Task<StockItemInfo?> GetStockItemAsync(Guid practiceId, Guid stockItemId, CancellationToken ct = default)
    {
        return await db.StockItems
            .IgnoreQueryFilters()
            .Where(s => s.Id == stockItemId && s.PracticeId == practiceId && s.IsActive)
            .Select(s => new StockItemInfo(s.Name, s.SellingPrice, s.QuantityOnHand, s.Unit))
            .FirstOrDefaultAsync(ct);
    }

    public async Task DispenseAsync(Guid practiceId, Guid stockItemId, decimal quantity, string? notes, Guid? referenceId, CancellationToken ct = default)
    {
        var item = await db.StockItems
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(s => s.Id == stockItemId && s.PracticeId == practiceId, ct);
        if (item is null) throw new InvalidOperationException($"Stock item {stockItemId} not found.");

        var transaction = item.Dispense(quantity, notes, referenceId, "Prescription");
        db.StockTransactions.Add(transaction);
        await db.SaveChangesAsync(ct);
    }
}

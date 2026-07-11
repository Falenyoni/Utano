namespace Utano.Module.Core.Services;

public record StockItemInfo(string Name, decimal SellingPrice, decimal QuantityOnHand, string Unit);

public interface IInventoryService
{
    Task<StockItemInfo?> GetStockItemAsync(Guid practiceId, Guid stockItemId, CancellationToken ct = default);
    Task DispenseAsync(Guid practiceId, Guid stockItemId, decimal quantity, string? notes, Guid? referenceId, CancellationToken ct = default);
}

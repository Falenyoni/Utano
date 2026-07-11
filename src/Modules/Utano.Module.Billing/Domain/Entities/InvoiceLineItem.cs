using Utano.Module.Billing.Domain.Enums;

namespace Utano.Module.Billing.Domain.Entities;

public class InvoiceLineItem
{
    private InvoiceLineItem() { }

    public Guid Id { get; private set; }
    public Guid InvoiceId { get; private set; }
    public LineItemType Type { get; private set; }
    public string Description { get; private set; } = null!;
    public decimal Quantity { get; private set; }
    public decimal UnitPrice { get; private set; }
    public decimal DiscountPercent { get; private set; }
    public decimal Amount { get; private set; }
    public Guid? StockItemId { get; private set; }

    internal static InvoiceLineItem Create(Guid invoiceId, LineItemType type, string description,
        decimal quantity, decimal unitPrice, decimal discountPercent, Guid? stockItemId)
    {
        var amount = quantity * unitPrice * (1 - discountPercent / 100m);
        return new InvoiceLineItem
        {
            Id = Guid.NewGuid(),
            InvoiceId = invoiceId,
            Type = type,
            Description = description,
            Quantity = quantity,
            UnitPrice = unitPrice,
            DiscountPercent = discountPercent,
            Amount = Math.Round(amount, 2),
            StockItemId = stockItemId
        };
    }
}

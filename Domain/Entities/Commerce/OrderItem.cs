namespace Ecommerce.Domain;

public class OrderItem : BaseEntity
{
    public Guid OrderId { get; private set; }
    public Guid ProductVariantId { get; private set; }
    public string SKUSnapshot { get; private set; } = default!;
    public string ProductNameSnapshot { get; private set; } = default!;
    public int Quantity { get; private set; }
    public decimal UnitPrice { get; private set; }
    public decimal TotalPrice { get; private set; }

    protected OrderItem() { }

    public static OrderItem Create(Guid variantId, string sku, string productName,
    int quantity, decimal unitPrice) => new()
    {
        ProductVariantId = variantId,
        SKUSnapshot = sku,
        ProductNameSnapshot = productName,
        Quantity = quantity,
        UnitPrice = unitPrice,
        TotalPrice = unitPrice * quantity
    };
}

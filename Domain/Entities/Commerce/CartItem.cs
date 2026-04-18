namespace Ecommerce.Domain;

public class CartItem : BaseEntity
{
    public Guid CartId { get; private set; }
    public Guid ProductVariantId { get; private set; }
    public int Quantity { get; private set; }
    public decimal UnitPrice { get; private set; }

    public decimal LineTotal => UnitPrice * Quantity;

    protected CartItem() { }

    internal static CartItem Create(Guid cartId, Guid variantId, int quantity, decimal unitPrice) => new()
    {
        CartId = cartId,
        ProductVariantId = variantId,
        Quantity = quantity,
        UnitPrice = unitPrice
    };

    internal void UpdateQuantity(int quantity) => Quantity = quantity;
}

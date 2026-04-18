namespace Ecommerce.Domain;

public class StockMovement : BaseEntity
{
    public Guid ProductVariantId { get; private set; }
    public Guid? OrderItemId { get; private set; }
    public int Quantity { get; private set; }
    public int QuantityBefore { get; private set; }
    public int QuantityAfter { get; private set; }
    public StockMovementType MovementType { get; private set; }
    public string Reason { get; private set; } = default!;

    protected StockMovement() { }

    internal static StockMovement Create(Guid variantId, int qty, int before, int after,
    StockMovementType type, string reason, Guid? orderItemId) => new()
    {
        ProductVariantId = variantId,
        Quantity = qty,
        QuantityBefore = before,
        QuantityAfter = after,
        MovementType = type,
        Reason = reason,
        OrderItemId = orderItemId
    };
}

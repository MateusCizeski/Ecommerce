namespace Ecommerce.Domain;

public class Cart : TenantEntity
{
    public Guid CustomerId { get; private set; }
    public CartStatus Status { get; private set; } = CartStatus.Active;
    public DateTime? ExpiresAt { get; private set; }

    private readonly List<CartItem> _items = [];
    public IReadOnlyCollection<CartItem> Items => _items.AsReadOnly();
    public decimal Total => _items.Sum(i => i.UnitPrice * i.Quantity);
    public int ItemCount => _items.Sum(i => i.Quantity);

    protected Cart() { }

    public static Cart Create(Guid tenantId, Guid customerId, int expirationHours = 24) => new()
    {
        TenantId = tenantId,
        CustomerId = customerId,
        ExpiresAt = DateTime.UtcNow.AddHours(expirationHours)
    };

    public CartItem AddItem(ProductVariant variant, int quantity)
    {
        if (Status != CartStatus.Active) throw new DomainException("Cannot modify an inactive cart.");
        if (quantity <= 0) throw new DomainException("Quantity must be positive.");
        if (!variant.HasStock(quantity)) throw new DomainException($"Insufficient stock for '{variant.Name}'.");

        var existing = _items.FirstOrDefault(i => i.ProductVariantId == variant.Id);
        if (existing is not null)
        {
            existing.UpdateQuantity(existing.Quantity + quantity);
            MarkUpdated();
            return existing;
        }

        var item = CartItem.Create(Id, variant.Id, quantity, variant.Price);
        _items.Add(item);
        MarkUpdated();
        return item;
    }

    public void UpdateItemQuantity(Guid variantId, int quantity)
    {
        if (Status != CartStatus.Active) throw new DomainException("Cannot modify an inactive cart.");
        var item = _items.FirstOrDefault(i => i.ProductVariantId == variantId)
            ?? throw new DomainException("Item not found in cart.");
        if (quantity <= 0) _items.Remove(item); else item.UpdateQuantity(quantity);
        MarkUpdated();
    }

    public void RemoveItem(Guid variantId)
    {
        var item = _items.FirstOrDefault(i => i.ProductVariantId == variantId)
            ?? throw new DomainException("Item not found in cart.");
        _items.Remove(item);
        MarkUpdated();
    }

    public void Checkout() => Status = CartStatus.CheckedOut;
    public void Abandon() => Status = CartStatus.Abandoned;
    public bool IsExpired() => ExpiresAt.HasValue && ExpiresAt < DateTime.UtcNow;
}

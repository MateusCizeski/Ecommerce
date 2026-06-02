namespace Ecommerce.Domain;

public class ProductVariant : BaseEntity
{
    public Guid ProductId { get; private set; }
    public string SKU { get; private set; } = default!;
    public string Name { get; private set; } = default!;
    public decimal Price { get; private set; }
    public decimal? CompareAtPrice { get; private set; }
    public string? ImageUrl { get; private set; }
    public bool IsActive { get; private set; } = true;
    public int StockQuantity { get; private set; }
    public uint RowVersion { get; private set; }

    public Product Product { get; private set; } = default!;
    private readonly List<VariantAttribute> _attributes = [];
    public IReadOnlyCollection<VariantAttribute> Attributes => _attributes.AsReadOnly();

    private readonly List<StockMovement> _stockMovements = [];
    public IReadOnlyCollection<StockMovement> StockMovements => _stockMovements.AsReadOnly();

    protected ProductVariant() { }

    internal static ProductVariant Create(Guid productId, string sku, string name,
    decimal price, decimal? compareAtPrice) => new()
    {
        ProductId = productId,
        SKU = sku.Trim().ToUpperInvariant(),
        Name = name.Trim(),
        Price = price,
        CompareAtPrice = compareAtPrice
    };

    public void AddAttribute(string attributeName, string attributeValue)
    {
        if (_attributes.Any(a => string.Equals(a.Name, attributeName, StringComparison.OrdinalIgnoreCase)))
            throw new DomainException($"Attribute '{attributeName}' already exists for this variant.");

        _attributes.Add(VariantAttribute.Create(Id, attributeName, attributeValue));
    }

    public StockMovement AddStock(int quantity, string reason = "Purchase")
    {
        if (quantity <= 0) throw new DomainException("Quantity must be positive.");
        return RegisterMovement(quantity, StockMovementType.Purchase, reason);
    }

    public StockMovement DeductStock(int quantity, Guid? orderItemId = null)
    {
        if (quantity <= 0) throw new DomainException("Quantity must be positive.");
        if (StockQuantity < quantity)
            throw new DomainException($"Insufficient stock. Available: {StockQuantity}, requested: {quantity}.");
        return RegisterMovement(-quantity, StockMovementType.Sale, "Sale", orderItemId);
    }

    public StockMovement AdjustStock(int newQuantity, string reason)
    => RegisterMovement(newQuantity - StockQuantity, StockMovementType.Adjustment, reason);

    private StockMovement RegisterMovement(int qty, StockMovementType type, string reason, Guid? orderItemId = null)
    {
        var movement = StockMovement.Create(Id, qty, StockQuantity, StockQuantity + qty, type, reason, orderItemId);

        StockQuantity += qty;
        _stockMovements.Add(movement);
        MarkUpdated();

        if (StockQuantity == 0)
        {
            AddDomainEvent(new StockDepletedEvent(Id, ProductId));
        }
        return movement;
    }

    public bool HasStock(int quantity) => StockQuantity >= quantity;
}

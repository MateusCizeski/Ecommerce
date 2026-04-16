namespace Ecommerce.Domain;

public class Category : TenantEntity
{
  public Guid? ParentCategoryId { get; private set; }
  public string Name { get; private set; } = default!;
  public string Slug { get; private set; } = default!;
  public string? Description { get; private set; }
  public bool IsActive { get; private set; } = true;
  public int SortOrder { get; private set; }

  public Category? ParentCategory { get; private set; }

  private readonly List<Category> _subCategories = [];
  public IReadOnlyCollection<Category> SubCategories => _subCategories.AsReadOnly();

  private readonly List<Product> _products = [];
  public IReadOnlyCollection<Product> Products => _products.AsReadOnly();

  protected Category() { }

  public static Category Create(Guid tenantId, string name, string slug, string? description = null,
      Guid? parentCategoryId = null, int sortOrder = 0)
  {
    if (string.IsNullOrWhiteSpace(name)) throw new DomainException("Category name is required.");
    if (string.IsNullOrWhiteSpace(slug)) throw new DomainException("Category slug is required.");

    return new Category
    {
      TenantId = tenantId,
      Name = name.Trim(),
      Slug = slug.Trim().ToLowerInvariant(),
      Description = description?.Trim(),
      ParentCategoryId = parentCategoryId,
      SortOrder = sortOrder
    };
  }

  public void Update(string name, string slug, string? description, int sortOrder)
  {
    Name = name.Trim();
    Slug = slug.Trim().ToLowerInvariant();
    Description = description?.Trim();
    SortOrder = sortOrder;
    MarkUpdated();
  }

  public void Deactivate() => IsActive = false;
  public void Activate() => IsActive = true;
}

public class Product : TenantEntity
{
  public Guid CategoryId { get; private set; }
  public string Name { get; private set; } = default!;
  public string Slug { get; private set; } = default!;
  public string? Description { get; private set; }
  public ProductStatus Status { get; private set; } = ProductStatus.Draft;
  public bool IsFeatured { get; private set; }
  public decimal BasePrice { get; private set; }

  public Category Category { get; private set; } = default!;

  private readonly List<ProductVariant> _variants = [];
  public IReadOnlyCollection<ProductVariant> Variants => _variants.AsReadOnly();

  protected Product() { }

  public static Product Create(Guid tenantId, Guid categoryId, string name, string slug,
      decimal basePrice, string? description = null)
  {
    if (string.IsNullOrWhiteSpace(name)) throw new DomainException("Product name is required.");
    if (basePrice < 0) throw new DomainException("Base price cannot be negative.");

    return new Product
    {
      TenantId = tenantId,
      CategoryId = categoryId,
      Name = name.Trim(),
      Slug = slug.Trim().ToLowerInvariant(),
      BasePrice = basePrice,
      Description = description?.Trim()
    };
  }

  public ProductVariant AddVariant(string sku, string name, decimal price, decimal? compareAtPrice = null)
  {
    if (_variants.Any(v => v.SKU == sku.Trim().ToUpperInvariant()))
      throw new DomainException($"A variant with SKU '{sku}' already exists.");

    var variant = ProductVariant.Create(Id, sku, name, price, compareAtPrice);
    _variants.Add(variant);
    AddDomainEvent(new ProductVariantAddedEvent(Id, variant.Id, TenantId));
    return variant;
  }

  public void Publish()
  {
    if (!_variants.Any(v => v.IsActive))
      throw new DomainException("Cannot publish a product without active variants.");
    Status = ProductStatus.Active;
    MarkUpdated();
  }

  public void Archive() { Status = ProductStatus.Archived; MarkUpdated(); }

  public void Update(string name, string slug, decimal basePrice, Guid categoryId,
      string? description, bool isFeatured)
  {
    if (basePrice < 0) throw new DomainException("Base price cannot be negative.");
    Name = name.Trim();
    Slug = slug.Trim().ToLowerInvariant();
    BasePrice = basePrice;
    CategoryId = categoryId;
    Description = description?.Trim();
    IsFeatured = isFeatured;
    MarkUpdated();
  }
}

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

  internal static ProductVariant Create(Guid productId, string sku, string name, decimal price, decimal? compareAtPrice) => new()
  {
    ProductId = productId,
    SKU = sku.Trim().ToUpperInvariant(),
    Name = name.Trim(),
    Price = price,
    CompareAtPrice = compareAtPrice
  };

  public void AddAttribute(string attributeName, string attributeValue)
      => _attributes.Add(VariantAttribute.Create(Id, attributeName, attributeValue));

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
    if (StockQuantity == 0)
      AddDomainEvent(new StockDepletedEvent(Id, ProductId));
    return movement;
  }

  public bool HasStock(int quantity) => StockQuantity >= quantity;
}

public class VariantAttribute : BaseEntity
{
  public Guid ProductVariantId { get; private set; }
  public string AttributeName { get; private set; } = default!;
  public string AttributeValue { get; private set; } = default!;

  protected VariantAttribute() { }

  internal static VariantAttribute Create(Guid variantId, string name, string value) => new()
  {
    ProductVariantId = variantId,
    AttributeName = name.Trim(),
    AttributeValue = value.Trim()
  };
}

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
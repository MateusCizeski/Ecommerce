namespace Ecommerce.Domain;

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

    public void Archive()
    {
        Status = ProductStatus.Archived;
        MarkUpdated();
    }

    public void Update(string name, string slug, decimal basePrice, Guid categoryId,
    string? description, bool isFeatured)
    {
        if (string.IsNullOrWhiteSpace(name)) throw new DomainException("Product name is required.");
        if (string.IsNullOrWhiteSpace(slug)) throw new DomainException("Product slug is required.");
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

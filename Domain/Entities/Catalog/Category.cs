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
        if (string.IsNullOrWhiteSpace(name)) throw new DomainException("Category name is required.");
        if (string.IsNullOrWhiteSpace(slug)) throw new DomainException("Category slug is required.");

        Name = name.Trim();
        Slug = slug.Trim().ToLowerInvariant();
        Description = description?.Trim();
        SortOrder = sortOrder;
        MarkUpdated();
    }

    public void Deactivate() => IsActive = false;
    public void Activate() => IsActive = true;
}

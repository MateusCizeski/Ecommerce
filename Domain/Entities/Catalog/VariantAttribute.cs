namespace Ecommerce.Domain;

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

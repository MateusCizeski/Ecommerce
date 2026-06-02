namespace Ecommerce.Domain;

public class Feature : BaseEntity
{
    public string Key { get; private set; } = default!;
    public string Name { get; private set; } = default!;
    public string Description { get; private set; } = default!;

    protected Feature() { }

    public static Feature Create(string key, string name, string description)
    {
        if (string.IsNullOrWhiteSpace(key)) throw new DomainException("Feature key is required.");
        if (string.IsNullOrWhiteSpace(name)) throw new DomainException("Feature name is required.");
        if (string.IsNullOrWhiteSpace(description)) throw new DomainException("Feature description is required.");

        return new Feature
        {
            Key = key.Trim().ToLowerInvariant(),
            Name = name.Trim(),
            Description = description.Trim()
        };
    }
}

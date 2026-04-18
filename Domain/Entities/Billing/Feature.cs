namespace Ecommerce.Domain;

public class Feature : BaseEntity
{
    public string Key { get; private set; } = default!;
    public string Name { get; private set; } = default!;
    public string Description { get; private set; } = default!;

    protected Feature() { }

    public static Feature Create(string key, string name, string description) => new()
    {
        Key = key.Trim().ToLowerInvariant(),
        Name = name.Trim(),
        Description = description.Trim()
    };
}

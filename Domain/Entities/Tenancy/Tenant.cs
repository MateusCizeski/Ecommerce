namespace Ecommerce.Domain;

public class Tenant : BaseEntity
{
    public string Name { get; private set; } = default!;
    public string Subdomain { get; private set; } = default!;
    public string Email { get; private set; } = default!;
    public bool IsActive { get; private set; } = true;

    protected Tenant() { }

    public static Tenant Create(string name, string subdomain, string email)
    {
        if (string.IsNullOrWhiteSpace(name)) throw new DomainException("Tenant name is required.");
        if (string.IsNullOrWhiteSpace(subdomain)) throw new DomainException("Subdomain is required.");
        if (string.IsNullOrWhiteSpace(email)) throw new DomainException("Email is required.");

        return new Tenant
        {
            Name = name.Trim(),
            Subdomain = subdomain.Trim().ToLowerInvariant(),
            Email = email.Trim().ToLowerInvariant()
        };
    }

    public void Update(string name, string email)
    {
        if (string.IsNullOrWhiteSpace(name)) throw new DomainException("Tenant name is required.");
        Name = name.Trim();
        Email = email.Trim().ToLowerInvariant();
        MarkUpdated();
    }

    public void Deactivate() => IsActive = false;
    public void Activate() => IsActive = true;
}
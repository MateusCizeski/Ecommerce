namespace Ecommerce.Domain;

public class Customer : TenantEntity
{
    public string Email { get; private set; } = default!;
    public string FirstName { get; private set; } = default!;
    public string LastName { get; private set; } = default!;
    public string? Phone { get; private set; }
    public bool IsActive { get; private set; } = true;
    public string? StripeCustomerId { get; private set; }

    public string FullName => $"{FirstName} {LastName}";

    private readonly List<Address> _addresses = [];
    public IReadOnlyCollection<Address> Addresses => _addresses.AsReadOnly();

    protected Customer() { }

    public static Customer Create(Guid tenantId, string email, string firstName, string lastName, string? phone = null)
    {
        if (string.IsNullOrWhiteSpace(email)) throw new DomainException("Email is required.");
        if (string.IsNullOrWhiteSpace(firstName)) throw new DomainException("First name is required.");

        return new Customer
        {
            TenantId = tenantId,
            Email = email.Trim().ToLowerInvariant(),
            FirstName = firstName.Trim(),
            LastName = lastName.Trim(),
            Phone = phone?.Trim()
        };
    }

    public Address AddAddress(string label, string street, string number, string city,
    string state, string zipCode, string country, string? complement = null)
    {
        var address = Address.Create(Id, label, street, number, city, state, zipCode, country, complement);
        if (!_addresses.Any()) address.SetAsDefault();
        _addresses.Add(address);
        return address;
    }

    public void SetDefaultAddress(Guid addressId)
    {
        var target = _addresses.FirstOrDefault(a => a.Id == addressId)
            ?? throw new DomainException("Address not found.");
        foreach (var addr in _addresses) addr.UnsetDefault();
        target.SetAsDefault();
    }

    public void SetStripeCustomerId(string id)
    {
        if (string.IsNullOrWhiteSpace(id)) throw new DomainException("Stripe customer identifier is required.");
        StripeCustomerId = id.Trim();
        MarkUpdated();
    }

    public void Update(string firstName, string lastName, string? phone)
    {
        if (string.IsNullOrWhiteSpace(firstName)) throw new DomainException("First name is required.");
        if (string.IsNullOrWhiteSpace(lastName)) throw new DomainException("Last name is required.");

        FirstName = firstName.Trim();
        LastName = lastName.Trim();
        Phone = phone?.Trim();
        MarkUpdated();
    }

    public void Deactivate() => IsActive = false;
}
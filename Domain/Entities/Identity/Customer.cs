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

  public void SetStripeCustomerId(string stripeId) { StripeCustomerId = stripeId; MarkUpdated(); }
  public void Update(string firstName, string lastName, string? phone)
  {
    FirstName = firstName.Trim(); LastName = lastName.Trim(); Phone = phone?.Trim(); MarkUpdated();
  }
  public void Deactivate() => IsActive = false;
}

public class Address : BaseEntity
{
  public Guid CustomerId { get; private set; }
  public string Label { get; private set; } = default!;
  public string Street { get; private set; } = default!;
  public string Number { get; private set; } = default!;
  public string? Complement { get; private set; }
  public string City { get; private set; } = default!;
  public string State { get; private set; } = default!;
  public string ZipCode { get; private set; } = default!;
  public string Country { get; private set; } = default!;
  public bool IsDefault { get; private set; }

  protected Address() { }

  internal static Address Create(Guid customerId, string label, string street, string number,
      string city, string state, string zipCode, string country, string? complement) => new()
      {
        CustomerId = customerId,
        Label = label.Trim(),
        Street = street.Trim(),
        Number = number.Trim(),
        Complement = complement?.Trim(),
        City = city.Trim(),
        State = state.Trim(),
        ZipCode = zipCode.Trim(),
        Country = country.Trim()
      };

  internal void SetAsDefault() => IsDefault = true;
  internal void UnsetDefault() => IsDefault = false;
}
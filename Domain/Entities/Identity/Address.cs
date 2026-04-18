namespace Ecommerce.Domain;

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

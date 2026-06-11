namespace Application.Features.Customers.DTOs;

public record CustomerListItemDto(
    Guid Id,
    string Email,
    string FullName,
    string? Phone,
    bool IsActive,
    int AddressCount,
    DateTime CreatedAt);

public record CustomerDetailDto(
    Guid Id,
    string Email,
    string FirstName,
    string LastName,
    string? Phone,
    bool IsActive,
    DateTime CreatedAt,
    IReadOnlyCollection<AddressDto> Addresses);

public record AddressDto(
    Guid Id,
    string Label,
    string Street,
    string Number,
    string? Complement,
    string City,
    string State,
    string ZipCode,
    string Country,
    bool IsDefault);
using MediatR;

namespace Application.Features.Commerce.Cart.DTOs;

public record CartDto(Guid Id, Guid CustomerId, decimal Total, int ItemCount, IEnumerable<CartItemDto> Items, string Status, DateTime? ExpiresAt);

public record CartItemDto(Guid Id, Guid VariantId, string VariantName, string SKU, int Quantity, decimal UnitPrice, decimal LineTotal);

public record GetOrCreateCartCommand(Guid CustomerId) : IRequest<CartDto>;
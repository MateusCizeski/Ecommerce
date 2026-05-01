using MediatR;

namespace Application.Features.Commerce.Orders.DTOs;

public record OrderListItemDto(Guid Id, string OrderNumber, string Status, decimal TotalAmount, DateTime PlacedAt, int ItemCount);

public record OrderDetailDto(Guid Id, string OrderNumber, string Status, decimal Subtotal, decimal DiscountAmount, decimal ShippingAmount, decimal TaxAmount, decimal TotalAmount, DateTime PlacedAt, string? Notes, IEnumerable<OrderItemDto> Items, IEnumerable<PaymentDto> Payments);

public record OrderItemDto(Guid VariantId, string ProductName, string SKU, int Quantity, decimal UnitPrice, decimal TotalPrice);

public record PaymentDto(Guid Id, string Method, string Status, decimal Amount, string Currency, DateTime? PaidAt);

public record GetOrdersQuery(int Page = 1, int PageSize = 20, string? Status = null) : IRequest<PagedResult<OrderListItemDto>>;

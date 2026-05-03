using MediatR;

namespace Ecommerce.Domain;

public record OrderCreatedEvent(Guid OrderId, Guid TenantId, Guid CustomerId, decimal TotalAmount) : INotification;

public record OrderConfirmedEvent(Guid OrderId, Guid TenantId, Guid CustomerId) : INotification;

public record OrderCancelledEvent(Guid OrderId, Guid TenantId, Guid CustomerId) : INotification;

public record OrderCancelledWithItemsEvent(Guid OrderId, Guid TenantId, IReadOnlyCollection<OrderCancelledItem> Items) : INotification;

public record OrderCancelledItem(Guid ProductVariantId, int Quantity);

public record OrderShippedEvent(Guid OrderId, Guid TenantId) : INotification;

public record OrderDeliveredEvent(Guid OrderId, Guid TenantId) : INotification;

using MediatR;

namespace Ecommerce.Domain;

public record OrderCreatedEvent(Guid OrderId, Guid TenantId, Guid CustomerId, decimal TotalAmount) : INotification;

public record OrderConfirmedEvent(Guid OrderId, Guid TenantId, Guid CustomerId) : INotification;

public record OrderCancelledEvent(Guid OrderId, Guid TenantId, Guid CustomerId) : INotification;
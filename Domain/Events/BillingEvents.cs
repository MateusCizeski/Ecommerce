using MediatR;

namespace Ecommerce.Domain;

public record SubscriptionCreatedEvent(Guid SubscriptionId, Guid TenantId, Guid PlanId) : INotification;

public record SubscriptionCancelledEvent(Guid SubscriptionId, Guid TenantId) : INotification;
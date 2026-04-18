using MediatR;

namespace Ecommerce.Domain;

public record ProductVariantAddedEvent(Guid ProductId, Guid VariantId, Guid TenantId) : INotification;

public record StockDepletedEvent(Guid VariantId, Guid ProductId) : INotification;
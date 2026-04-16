namespace Ecommerce.Domain.Events
{
    public record OrderCreatedEvent(Guid OrderId, Guid TenantId, Guid CustomerId, decimal TotalAmount) : INotification;
}

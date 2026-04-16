namespace Ecommerce.Domain.Interfaces
{
    public interface IDomainEvent
    {
        DateTime OccurredOn { get; }
    }
}

using MediatR;

namespace Ecommerce.Domain
{
    public abstract class BaseEntity
    {
        public Guid Id { get; protected set; } = Guid.NewGuid();
        public DateTime CreatedAt { get; protected set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; protected set; } = DateTime.UtcNow;
        public DateTime? DeletedAt { get; set; }

        private readonly List<INotification> _domainEvents = [];
        public IReadOnlyCollection<INotification> DomainEvents => _domainEvents.AsReadOnly();

        protected void AddDomainEvent(INotification domainEvent) => _domainEvents.Add(domainEvent);
        public void ClearDomainEvents() => _domainEvents.Clear();
        protected void MarkUpdated() => UpdatedAt = DateTime.UtcNow;
        public bool IsDeleted => DeletedAt.HasValue;
    }

    public abstract class TenantEntity : BaseEntity
    {
        public Guid TenantId { get; protected set; }
    }
}

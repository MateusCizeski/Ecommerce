using Ecommerce.Domain.Interfaces;

namespace Ecommerce.Domain
{
    public abstract class BaseEntity : ISoftDelete
    {
        public Guid Id { get; protected set; } = Guid.NewGuid();
        public DateTime CreatedAt { get; protected set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; protected set; } = DateTime.UtcNow;

        public DateTime? DeletedAt { get; protected set; }
        public bool IsDeleted => DeletedAt.HasValue;

        public void Delete()
        {
            if (IsDeleted) return;

            DeletedAt = DateTime.UtcNow;
            MarkUpdated();
        }

        public void Restore()
        {
            if (!IsDeleted) return;

            DeletedAt = null;
            MarkUpdated();
        }

        protected void MarkUpdated() => UpdatedAt = DateTime.UtcNow;
        private readonly List<IDomainEvent> _domainEvents = [];
        public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();
        protected void AddDomainEvent(IDomainEvent domainEvent) => _domainEvents.Add(domainEvent);
        public void ClearDomainEvents() => _domainEvents.Clear();
    }

    public abstract class TenantEntity : BaseEntity
    {
        public Guid TenantId { get; protected set; }
    }
}

namespace Ecommerce.Domain.Interfaces
{
    public interface ISoftDelete
    {
        DateTime? DeletedAt { get; }
        bool IsDeleted { get; }
        void Delete();
        void Restore();
    }
}

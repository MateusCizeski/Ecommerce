using Ecommerce.Domain;

namespace Domain.Interfaces;

public interface ICustomerRepository
{
    Task<Customer?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<Customer?> GetByEmailAsync(Guid tenantId, string email, CancellationToken ct = default);
    Task<bool> EmailExistsAsync(Guid tenantId, string email, CancellationToken ct = default);
    Task AddAsync(Customer customer, CancellationToken ct = default);
    IQueryable<Customer> Query(Guid tenantId);
}


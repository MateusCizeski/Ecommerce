using Ecommerce.Domain;
using Ecommerce.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Repository.Repositories
{
    internal class CustomerRepository : ICustomerRepository
    {
        private readonly AppDbContext _db;

        public CustomerRepository(AppDbContext db) => _db = db;

        public async Task AddAsync(Customer customer, CancellationToken ct = default)
            => await _db.Customers.AddAsync(customer, ct);

        public async Task<bool> EmailExistsAsync(Guid tenantId, string email, CancellationToken ct = default)
            => await _db.Customers.AnyAsync(c => c.TenantId == tenantId && c.Email == email, ct);

        public async Task<Customer?> GetByEmailAsync(Guid tenantId, string email, CancellationToken ct = default)
            => await _db.Customers.FirstOrDefaultAsync(c => c.TenantId == tenantId && c.Email == email, ct);

        public async Task<Customer?> GetByIdAsync(Guid id, CancellationToken ct = default)
            => await _db.Customers.Include(c => c.Addresses)
                                  .FirstOrDefaultAsync(c => c.Id == id, ct);

        public IQueryable<Customer> Query(Guid tenantId)
            => _db.Customers.Where(c => c.TenantId == tenantId);
    }
}


using Ecommerce.Domain;
using Ecommerce.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Repository.Repositories
{
    /// <summary>
    /// Repositório para gerenciar clientes do e-commerce.
    /// </summary>
    public class CustomerRepository : ICustomerRepository
    {
        private readonly AppDbContext _context;
        private readonly ILogger<CustomerRepository>? _logger;

        /// <summary>
        /// Inicializa uma nova instância de CustomerRepository.
        /// </summary>
        /// <param name="context">Contexto de dados do EF Core.</param>
        /// <param name="logger">Logger para registrar eventos (opcional).</param>
        public CustomerRepository(AppDbContext context, ILogger<CustomerRepository>? logger = null)
        {
            ArgumentNullException.ThrowIfNull(context);
            _context = context;
            _logger = logger;
        }

        /// <summary>
        /// Adiciona um novo cliente ao banco de dados.
        /// </summary>
        /// <param name="customer">Cliente a ser adicionado.</param>
        /// <param name="cancellationToken">Token de cancelamento.</param>
        public async Task AddAsync(Customer customer, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(customer);
            await _context.Customers.AddAsync(customer, cancellationToken);
            _logger?.LogInformation("Cliente adicionado: {CustomerId} - {Email}", customer.Id, customer.Email);
        }

        /// <summary>
        /// Verifica se um email já existe para o tenant.
        /// </summary>
        /// <param name="tenantId">ID do tenant.</param>
        /// <param name="email">Email a verificar.</param>
        /// <param name="cancellationToken">Token de cancelamento.</param>
        /// <returns>True se existe, false caso contrário.</returns>
        public async Task<bool> EmailExistsAsync(Guid tenantId, string email, CancellationToken cancellationToken = default)
        {
            if (tenantId == Guid.Empty)
                throw new ArgumentException("ID do tenant não pode estar vazio.", nameof(tenantId));

            if (string.IsNullOrWhiteSpace(email))
                throw new ArgumentException("Email não pode estar vazio.", nameof(email));

            return await _context.Customers.AnyAsync(
                c => c.TenantId == tenantId && c.Email == email,
                cancellationToken);
        }

        /// <summary>
        /// Recupera um cliente pelo email.
        /// </summary>
        /// <param name="tenantId">ID do tenant.</param>
        /// <param name="email">Email do cliente.</param>
        /// <param name="cancellationToken">Token de cancelamento.</param>
        /// <returns>O cliente encontrado ou null.</returns>
        public async Task<Customer?> GetByEmailAsync(Guid tenantId, string email, CancellationToken cancellationToken = default)
        {
            if (tenantId == Guid.Empty)
                throw new ArgumentException("ID do tenant não pode estar vazio.", nameof(tenantId));

            if (string.IsNullOrWhiteSpace(email))
                throw new ArgumentException("Email não pode estar vazio.", nameof(email));

            return await _context.Customers.FirstOrDefaultAsync(
                c => c.TenantId == tenantId && c.Email == email,
                cancellationToken);
        }

        /// <summary>
        /// Recupera um cliente pelo ID com todos os seus endereços.
        /// </summary>
        /// <param name="id">ID do cliente.</param>
        /// <param name="cancellationToken">Token de cancelamento.</param>
        /// <returns>O cliente encontrado ou null.</returns>
        public async Task<Customer?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            if (id == Guid.Empty)
                throw new ArgumentException("ID do cliente não pode estar vazio.", nameof(id));

            return await _context.Customers.Include(c => c.Addresses)
                                  .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);
        }

        /// <summary>
        /// Retorna uma query de clientes para um tenant.
        /// </summary>
        /// <param name="tenantId">ID do tenant.</param>
        /// <returns>Query de clientes.</returns>
        public IQueryable<Customer> Query(Guid tenantId)
        {
            if (tenantId == Guid.Empty)
                throw new ArgumentException("ID do tenant não pode estar vazio.", nameof(tenantId));

            return _context.Customers.Where(c => c.TenantId == tenantId);
        }
    }
}


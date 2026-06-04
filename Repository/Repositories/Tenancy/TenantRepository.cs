using Ecommerce.Domain;
using Ecommerce.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Repository.Repositories
{
    /// <summary>
    /// Repositório para gerenciar tenants da plataforma.
    /// </summary>
    public class TenantRepository : ITenantRepository
    {
        private readonly AppDbContext _context;
        private readonly ILogger<TenantRepository>? _logger;

        /// <summary>
        /// Inicializa uma nova instância de TenantRepository.
        /// </summary>
        /// <param name="context">Contexto de dados do EF Core.</param>
        /// <param name="logger">Logger para registrar eventos (opcional).</param>
        public TenantRepository(AppDbContext context, ILogger<TenantRepository>? logger = null)
        {
            ArgumentNullException.ThrowIfNull(context);
            _context = context;
            _logger = logger;
        }

        /// <summary>
        /// Recupera um tenant pelo ID, ignorando filtros globais.
        /// </summary>
        /// <param name="id">ID do tenant.</param>
        /// <param name="cancellationToken">Token de cancelamento.</param>
        /// <returns>O tenant encontrado ou null.</returns>
        public async Task<Tenant?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            if (id == Guid.Empty)
                throw new ArgumentException("ID do tenant não pode estar vazio.", nameof(id));

            return await _context.Tenants.IgnoreQueryFilters()
                            .FirstOrDefaultAsync(t => t.Id == id, cancellationToken);
        }

        /// <summary>
        /// Recupera um tenant pelo subdomínio, ignorando filtros globais.
        /// </summary>
        /// <param name="subdomain">Subdomínio do tenant.</param>
        /// <param name="cancellationToken">Token de cancelamento.</param>
        /// <returns>O tenant encontrado ou null.</returns>
        public async Task<Tenant?> GetBySubdomainAsync(string subdomain, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(subdomain))
                throw new ArgumentException("Subdomínio não pode estar vazio.", nameof(subdomain));

            return await _context.Tenants.IgnoreQueryFilters()
                            .FirstOrDefaultAsync(t => t.Subdomain == subdomain, cancellationToken);
        }

        /// <summary>
        /// Verifica se um subdomínio já existe e não foi deletado.
        /// </summary>
        /// <param name="subdomain">Subdomínio a verificar.</param>
        /// <param name="cancellationToken">Token de cancelamento.</param>
        /// <returns>True se existe e está ativo, false caso contrário.</returns>
        public async Task<bool> SubdomainExistsAsync(string subdomain, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(subdomain))
                throw new ArgumentException("Subdomínio não pode estar vazio.", nameof(subdomain));

            return await _context.Tenants.IgnoreQueryFilters()
                            .AnyAsync(t => t.Subdomain == subdomain && t.DeletedAt == null, cancellationToken);
        }

        /// <summary>
        /// Adiciona um novo tenant ao banco de dados.
        /// </summary>
        /// <param name="tenant">Tenant a ser adicionado.</param>
        /// <param name="cancellationToken">Token de cancelamento.</param>
        public async Task AddAsync(Tenant tenant, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(tenant);
            await _context.Tenants.AddAsync(tenant, cancellationToken);
            _logger?.LogInformation("Tenant adicionado: {TenantId} - {Subdomain}", tenant.Id, tenant.Subdomain);
        }
    }
}


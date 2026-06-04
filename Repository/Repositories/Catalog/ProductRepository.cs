using Ecommerce.Domain;
using Ecommerce.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Repository.Repositories
{
    /// <summary>
    /// Repositório para gerenciar produtos do catálogo.
    /// </summary>
    public class ProductRepository : IProductRepository
    {
        private readonly AppDbContext _context;
        private readonly ILogger<ProductRepository>? _logger;

        /// <summary>
        /// Inicializa uma nova instância de ProductRepository.
        /// </summary>
        /// <param name="context">Contexto de dados do EF Core.</param>
        /// <param name="logger">Logger para registrar eventos (opcional).</param>
        public ProductRepository(AppDbContext context, ILogger<ProductRepository>? logger = null)
        {
            ArgumentNullException.ThrowIfNull(context);
            _context = context;
            _logger = logger;
        }

        /// <summary>
        /// Adiciona um novo produto ao banco de dados.
        /// </summary>
        /// <param name="product">Produto a ser adicionado.</param>
        /// <param name="cancellationToken">Token de cancelamento.</param>
        public async Task AddAsync(Product product, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(product);
            await _context.Products.AddAsync(product, cancellationToken);
            _logger?.LogDebug("Produto adicionado: {ProductId} - {Name}", product.Id, product.Name);
        }

        /// <summary>
        /// Recupera um produto pelo ID com categoria e variações.
        /// </summary>
        /// <param name="id">ID do produto.</param>
        /// <param name="cancellationToken">Token de cancelamento.</param>
        /// <returns>O produto encontrado ou null.</returns>
        public async Task<Product?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            if (id == Guid.Empty)
                throw new ArgumentException("ID do produto não pode estar vazio.", nameof(id));

            return await _context.Products
                        .Include(p => p.Category)
                        .Include(p => p.Variants).ThenInclude(v => v.Attributes)
                        .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
        }

        /// <summary>
        /// Retorna uma query de produtos para um tenant.
        /// </summary>
        /// <param name="tenantId">ID do tenant.</param>
        /// <returns>Query de produtos.</returns>
        public IQueryable<Product> Query(Guid tenantId)
        {
            if (tenantId == Guid.Empty)
                throw new ArgumentException("ID do tenant não pode estar vazio.", nameof(tenantId));

            return _context.Products
                  .Include(p => p.Category)
                  .Where(p => p.TenantId == tenantId);
        }

        /// <summary>
        /// Verifica se um slug já existe para o tenant.
        /// </summary>
        /// <param name="tenantId">ID do tenant.</param>
        /// <param name="slug">Slug a verificar.</param>
        /// <param name="cancellationToken">Token de cancelamento.</param>
        /// <returns>True se existe, false caso contrário.</returns>
        public async Task<bool> SlugExistsAsync(Guid tenantId, string slug, CancellationToken cancellationToken = default)
        {
            if (tenantId == Guid.Empty)
                throw new ArgumentException("ID do tenant não pode estar vazio.", nameof(tenantId));

            if (string.IsNullOrWhiteSpace(slug))
                throw new ArgumentException("Slug não pode estar vazio.", nameof(slug));

            return await _context.Products.AnyAsync(
                p => p.TenantId == tenantId && p.Slug == slug,
                cancellationToken);
        }
    }
}


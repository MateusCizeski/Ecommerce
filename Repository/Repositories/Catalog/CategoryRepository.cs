using Ecommerce.Domain;
using Ecommerce.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Repository.Repositories
{
    /// <summary>
    /// Repositório para gerenciar categorias de produtos.
    /// </summary>
    public class CategoryRepository : ICategoryRepository
    {
        private readonly AppDbContext _context;
        private readonly ILogger<CategoryRepository>? _logger;

        /// <summary>
        /// Inicializa uma nova instância de CategoryRepository.
        /// </summary>
        /// <param name="context">Contexto de dados do EF Core.</param>
        /// <param name="logger">Logger para registrar eventos (opcional).</param>
        public CategoryRepository(AppDbContext context, ILogger<CategoryRepository>? logger = null)
        {
            ArgumentNullException.ThrowIfNull(context);
            _context = context;
            _logger = logger;
        }

        /// <summary>
        /// Adiciona uma nova categoria ao banco de dados.
        /// </summary>
        /// <param name="category">Categoria a ser adicionada.</param>
        /// <param name="cancellationToken">Token de cancelamento.</param>
        public async Task AddAsync(Category category, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(category);
            await _context.Categories.AddAsync(category, cancellationToken);
            _logger?.LogDebug("Categoria adicionada: {CategoryId} - {Name}", category.Id, category.Name);
        }

        /// <summary>
        /// Recupera uma categoria pelo ID com todas as suas relacionadas.
        /// </summary>
        /// <param name="id">ID da categoria.</param>
        /// <param name="cancellationToken">Token de cancelamento.</param>
        /// <returns>A categoria encontrada ou null.</returns>
        public async Task<Category?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            if (id == Guid.Empty)
                throw new ArgumentException("ID da categoria não pode estar vazio.", nameof(id));

            return await _context.Categories
                        .Include(c => c.ParentCategory)
                        .Include(c => c.SubCategories)
                        .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);
        }

        /// <summary>
        /// Retorna uma query de categorias para um tenant.
        /// </summary>
        /// <param name="tenantId">ID do tenant.</param>
        /// <returns>Query de categorias.</returns>
        public IQueryable<Category> Query(Guid tenantId)
        {
            if (tenantId == Guid.Empty)
                throw new ArgumentException("ID do tenant não pode estar vazio.", nameof(tenantId));

            return _context.Categories
                  .Include(c => c.ParentCategory)
                  .Where(c => c.TenantId == tenantId);
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

            return await _context.Categories.AnyAsync(
                c => c.TenantId == tenantId && c.Slug == slug,
                cancellationToken);
        }
    }
}


using Ecommerce.Domain;
using Ecommerce.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Repository.Repositories
{
    /// <summary>
    /// Repositório para gerenciar variações de produtos.
    /// </summary>
    public class ProductVariantRepository : IProductVariantRepository
    {
        private readonly AppDbContext _context;
        private readonly ILogger<ProductVariantRepository>? _logger;

        /// <summary>
        /// Inicializa uma nova instância de ProductVariantRepository.
        /// </summary>
        /// <param name="context">Contexto de dados do EF Core.</param>
        /// <param name="logger">Logger para registrar eventos (opcional).</param>
        public ProductVariantRepository(AppDbContext context, ILogger<ProductVariantRepository>? logger = null)
        {
            ArgumentNullException.ThrowIfNull(context);
            _context = context;
            _logger = logger;
        }

        /// <summary>
        /// Recupera uma variação pelo ID com o produto relacionado.
        /// </summary>
        /// <param name="id">ID da variação.</param>
        /// <param name="cancellationToken">Token de cancelamento.</param>
        /// <returns>A variação encontrada ou null.</returns>
        public async Task<ProductVariant?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            if (id == Guid.Empty)
                throw new ArgumentException("ID da variação não pode estar vazio.", nameof(id));

            return await _context.ProductVariants
                        .Include(v => v.Product)
                        .FirstOrDefaultAsync(v => v.Id == id, cancellationToken);
        }

        /// <summary>
        /// Restaura estoque de uma variação (usado em reembolsos).
        /// </summary>
        /// <param name="variantId">ID da variação.</param>
        /// <param name="quantity">Quantidade a restaurar.</param>
        /// <param name="cancellationToken">Token de cancelamento.</param>
        /// <exception cref="KeyNotFoundException">Lançada quando a variação não é encontrada.</exception>
        public async Task RestoreStockAsync(Guid variantId, int quantity, CancellationToken cancellationToken = default)
        {
            if (variantId == Guid.Empty)
                throw new ArgumentException("ID da variação não pode estar vazio.", nameof(variantId));

            if (quantity <= 0)
                throw new ArgumentException("Quantidade deve ser maior que zero.", nameof(quantity));

            var variant = await _context.ProductVariants.FindAsync(
                new object[] { variantId },
                cancellationToken: cancellationToken);

            if (variant is null)
            {
                _logger?.LogWarning("Variação não encontrada para restauração de estoque: {VariantId}", variantId);
                throw new KeyNotFoundException($"Variação de produto '{variantId}' não encontrada.");
            }

            variant.AddStock(quantity, "Reembolso restaurou estoque");
            _logger?.LogInformation(
                "Estoque restaurado para variação: {VariantId}, Quantidade: {Quantity}",
                variantId,
                quantity);
        }
    }
}


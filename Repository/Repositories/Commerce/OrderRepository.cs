using Ecommerce.Domain;
using Ecommerce.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Repository.Repositories
{
    /// <summary>
    /// Repositório para gerenciar pedidos de compra.
    /// </summary>
    public class OrderRepository : IOrderRepository
    {
        private readonly AppDbContext _context;
        private readonly ILogger<OrderRepository>? _logger;

        /// <summary>
        /// Inicializa uma nova instância de OrderRepository.
        /// </summary>
        /// <param name="context">Contexto de dados do EF Core.</param>
        /// <param name="logger">Logger para registrar eventos (opcional).</param>
        public OrderRepository(AppDbContext context, ILogger<OrderRepository>? logger = null)
        {
            ArgumentNullException.ThrowIfNull(context);
            _context = context;
            _logger = logger;
        }

        /// <summary>
        /// Adiciona um novo pedido ao banco de dados.
        /// </summary>
        /// <param name="order">Pedido a ser adicionado.</param>
        /// <param name="cancellationToken">Token de cancelamento.</param>
        public async Task AddAsync(Order order, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(order);
            await _context.Orders.AddAsync(order, cancellationToken);
            _logger?.LogInformation("Pedido adicionado: {OrderId} - Total: {Total}", order.Id, order.Total);
        }

        /// <summary>
        /// Recupera um pedido pelo ID com todos os seus itens e pagamentos.
        /// </summary>
        /// <param name="id">ID do pedido.</param>
        /// <param name="cancellationToken">Token de cancelamento.</param>
        /// <returns>O pedido encontrado ou null.</returns>
        public async Task<Order?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            if (id == Guid.Empty)
                throw new ArgumentException("ID do pedido não pode estar vazio.", nameof(id));

            return await _context.Orders
                        .Include(o => o.Items)
                        .Include(o => o.Payments)
                        .FirstOrDefaultAsync(o => o.Id == id, cancellationToken);
        }

        /// <summary>
        /// Recupera um pedido pelo ID da intenção de pagamento Stripe.
        /// </summary>
        /// <param name="paymentIntentId">ID da intenção de pagamento.</param>
        /// <param name="cancellationToken">Token de cancelamento.</param>
        /// <returns>O pedido encontrado ou null.</returns>
        public async Task<Order?> GetByPaymentIntentIdAsync(string paymentIntentId, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(paymentIntentId))
                throw new ArgumentException("ID da intenção de pagamento não pode estar vazio.", nameof(paymentIntentId));

            return await _context.Orders
                        .Include(o => o.Payments)
                        .FirstOrDefaultAsync(
                            o => o.Payments.Any(p => p.StripePaymentIntentId == paymentIntentId),
                            cancellationToken);
        }

        /// <summary>
        /// Recupera um pedido pelo ID da cobrança Stripe.
        /// </summary>
        /// <param name="chargeId">ID da cobrança.</param>
        /// <param name="cancellationToken">Token de cancelamento.</param>
        /// <returns>O pedido encontrado ou null.</returns>
        public async Task<Order?> GetByChargeIdAsync(string chargeId, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(chargeId))
                throw new ArgumentException("ID da cobrança não pode estar vazio.", nameof(chargeId));

            return await _context.Orders
                        .Include(o => o.Payments)
                        .FirstOrDefaultAsync(
                            o => o.Payments.Any(p => p.StripeChargeId == chargeId),
                            cancellationToken);
        }

        /// <summary>
        /// Retorna uma query de pedidos para um tenant.
        /// </summary>
        /// <param name="tenantId">ID do tenant.</param>
        /// <returns>Query de pedidos.</returns>
        public IQueryable<Order> Query(Guid tenantId)
        {
            if (tenantId == Guid.Empty)
                throw new ArgumentException("ID do tenant não pode estar vazio.", nameof(tenantId));

            return _context.Orders.Include(o => o.Items)
                                 .Where(o => o.TenantId == tenantId);
        }
    }
}


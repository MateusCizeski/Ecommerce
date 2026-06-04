using MediatR;
using Ecommerce.Domain;
using Ecommerce.Domain.Interfaces;
using Repository.SettingsEF;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Repository.Constants;

namespace Repository
{
    /// <summary>
    /// Contexto de dados principal da aplicação.
    /// Gerencia todos os DbSets e aplica configurações de EF Core.
    /// Suporta Domain Events dispatching e soft delete com Multi-Tenancy.
    /// </summary>
    public class AppDbContext : DbContext
    {
        private readonly ITenantContext? _tenantContext;
        private readonly IMediator? _mediator;
        private readonly ILogger<AppDbContext>? _logger;

        /// <summary>
        /// Inicializa uma nova instância de AppDbContext.
        /// </summary>
        /// <param name="options">Opções de configuração do DbContext.</param>
        /// <param name="tenantContext">Contexto de tenant (opcional).</param>
        /// <param name="mediator">Mediator para publicar domain events (opcional).</param>
        /// <param name="logger">Logger para operações do contexto (opcional).</param>
        public AppDbContext(
            DbContextOptions<AppDbContext> options,
            ITenantContext? tenantContext = null,
            IMediator? mediator = null,
            ILogger<AppDbContext>? logger = null)
            : base(options)
        {
            _tenantContext = tenantContext;
            _mediator = mediator;
            _logger = logger;
        }

        /// <summary>Tenants da plataforma.</summary>
        public DbSet<Tenant> Tenants => Set<Tenant>();

        /// <summary>Planos de subscriptions.</summary>
        public DbSet<Plan> Plans => Set<Plan>();

        /// <summary>Features disponíveis nos planos.</summary>
        public DbSet<Feature> Features => Set<Feature>();

        /// <summary>Relacionamento entre planos e features.</summary>
        public DbSet<PlanFeature> PlanFeatures => Set<PlanFeature>();

        /// <summary>Subscriptions dos tenants.</summary>
        public DbSet<Subscription> Subscriptions => Set<Subscription>();

        /// <summary>Categorias de produtos.</summary>
        public DbSet<Category> Categories => Set<Category>();

        /// <summary>Produtos do catálogo.</summary>
        public DbSet<Product> Products => Set<Product>();

        /// <summary>Variações de produtos.</summary>
        public DbSet<ProductVariant> ProductVariants => Set<ProductVariant>();

        /// <summary>Atributos de variações.</summary>
        public DbSet<VariantAttribute> VariantAttributes => Set<VariantAttribute>();

        /// <summary>Movimentações de estoque.</summary>
        public DbSet<StockMovement> StockMovements => Set<StockMovement>();

        /// <summary>Clientes do e-commerce.</summary>
        public DbSet<Customer> Customers => Set<Customer>();

        /// <summary>Endereços de clientes.</summary>
        public DbSet<Address> Addresses => Set<Address>();

        /// <summary>Carrinhos de compras.</summary>
        public DbSet<Cart> Carts => Set<Cart>();

        /// <summary>Itens dentro de carrinhos.</summary>
        public DbSet<CartItem> CartItems => Set<CartItem>();

        /// <summary>Pedidos de compra.</summary>
        public DbSet<Order> Orders => Set<Order>();

        /// <summary>Itens dentro de pedidos.</summary>
        public DbSet<OrderItem> OrderItems => Set<OrderItem>();

        /// <summary>Pagamentos processados.</summary>
        public DbSet<Payment> Payments => Set<Payment>();

        /// <summary>Cupons de desconto.</summary>
        public DbSet<Coupon> Coupons => Set<Coupon>();

        /// <summary>Eventos de webhooks Stripe.</summary>
        public DbSet<StripeWebhookEvent> StripeWebhookEvents => Set<StripeWebhookEvent>();

        /// <summary>
        /// Configura o modelo de dados e aplica todas as configurações de entidades.
        /// </summary>
        /// <param name="modelBuilder">Builder para configuração do modelo.</param>
        /// <summary>
        /// Configura o modelo de dados e aplica todas as configurações de entidades.
        /// </summary>
        /// <param name="modelBuilder">Builder para configuração do modelo.</param>
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Aplica todas as configurações de entidades
            ApplyEntityConfigurations(modelBuilder);

            // Aplica filtros globais (soft delete, multi-tenancy)
            ApplyGlobalQueryFilters(modelBuilder);

            _logger?.LogDebug("Modelo de dados configurado com sucesso");
        }

        /// <summary>
        /// Aplica configurações de todas as entidades.
        /// </summary>
        private static void ApplyEntityConfigurations(ModelBuilder modelBuilder)
        {
            modelBuilder.ApplyConfiguration(new TenantConfig());
            modelBuilder.ApplyConfiguration(new PlanConfig());
            modelBuilder.ApplyConfiguration(new FeatureConfig());
            modelBuilder.ApplyConfiguration(new PlanFeatureConfig());
            modelBuilder.ApplyConfiguration(new SubscriptionConfig());
            modelBuilder.ApplyConfiguration(new CategoryConfig());
            modelBuilder.ApplyConfiguration(new ProductConfig());
            modelBuilder.ApplyConfiguration(new ProductVariantConfig());
            modelBuilder.ApplyConfiguration(new VariantAttributeConfig());
            modelBuilder.ApplyConfiguration(new StockMovementConfig());
            modelBuilder.ApplyConfiguration(new CustomerConfig());
            modelBuilder.ApplyConfiguration(new AddressConfig());
            modelBuilder.ApplyConfiguration(new CartConfig());
            modelBuilder.ApplyConfiguration(new CartItemConfig());
            modelBuilder.ApplyConfiguration(new OrderConfig());
            modelBuilder.ApplyConfiguration(new OrderItemConfig());
            modelBuilder.ApplyConfiguration(new PaymentConfig());
            modelBuilder.ApplyConfiguration(new CouponConfig());
            modelBuilder.ApplyConfiguration(new StripeWebhookEventConfig());
        }

        /// <summary>
        /// Aplica filtros globais para soft delete e multi-tenancy.
        /// </summary>
        private void ApplyGlobalQueryFilters(ModelBuilder modelBuilder)
        {
            var tenantId = _tenantContext?.TenantId ?? Guid.Empty;

            modelBuilder.Entity<Tenant>().HasQueryFilter(e => e.DeletedAt == null);

            modelBuilder.Entity<Product>().HasQueryFilter(e => e.TenantId == tenantId && e.DeletedAt == null);
            modelBuilder.Entity<Category>().HasQueryFilter(e => e.TenantId == tenantId && e.DeletedAt == null);
            modelBuilder.Entity<Customer>().HasQueryFilter(e => e.TenantId == tenantId && e.DeletedAt == null);

            modelBuilder.Entity<Order>().HasQueryFilter(e => e.TenantId == tenantId);
            modelBuilder.Entity<Cart>().HasQueryFilter(e => e.TenantId == tenantId);
            modelBuilder.Entity<Coupon>().HasQueryFilter(e => e.TenantId == tenantId);
            modelBuilder.Entity<Subscription>().HasQueryFilter(e => e.TenantId == tenantId);
        }

        /// <summary>
        /// Salva as mudanças no banco de dados de forma assíncrona.
        /// Depois de salvar, publica todos os domain events.
        /// </summary>
        /// <param name="cancellationToken">Token de cancelamento.</param>
        /// <returns>Número de registros afetados.</returns>
        public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                var result = await base.SaveChangesAsync(cancellationToken);

                _logger?.LogInformation(
                    "Mudanças salvas com sucesso. {ChangeCount} registros afetados",
                    result);

                if (_mediator is not null)
                {
                    await DispatchDomainEventsAsync(cancellationToken);
                }

                return result;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Erro ao salvar mudanças no banco de dados");
                throw;
            }
        }

        /// <summary>
        /// Publica todos os domain events das entidades modificadas.
        /// </summary>
        /// <summary>
        /// Publica todos os domain events das entidades modificadas.
        /// </summary>
        private async Task DispatchDomainEventsAsync(CancellationToken cancellationToken)
        {
            var entities = ChangeTracker
                .Entries<BaseEntity>()
                .Where(e => e.Entity.DomainEvents.Any())
                .Select(e => e.Entity)
                .ToList();

            var events = entities.SelectMany(e => e.DomainEvents).ToList();

            if (events.Count == 0)
            {
                return;
            }

            _logger?.LogInformation(
                "Publicando {EventCount} domain events de {EntityCount} entidades",
                events.Count,
                entities.Count);

            entities.ForEach(e => e.ClearDomainEvents());

            foreach (var domainEvent in events)
            {
                try
                {
                    await _mediator!.Publish(domainEvent, cancellationToken);
                }
                catch (Exception ex)
                {
                    _logger?.LogError(
                        ex,
                        "Erro ao publicar domain event: {EventType}",
                        domainEvent.GetType().Name);
                    throw;
                }
            }
        }
    }


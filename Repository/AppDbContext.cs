using MediatR;
using Ecommerce.Domain;
using Domain.Interfaces;
using Repository.SettingsEF;
using Microsoft.EntityFrameworkCore;

namespace Repository;

public class AppDbContext : DbContext
{
    private readonly ITenantContext? _tenantContext;
    private readonly IMediator? _mediator;

    public AppDbContext(DbContextOptions<AppDbContext> options, ITenantContext? tenantContext = null, IMediator? mediator = null) : base(options)
    {
        _tenantContext = tenantContext;
        _mediator = mediator;
    }

    public DbSet<Tenant> Tenants => Set<Tenant>();
    public DbSet<Plan> Plans => Set<Plan>();
    public DbSet<Feature> Features => Set<Feature>();
    public DbSet<PlanFeature> PlanFeatures => Set<PlanFeature>();
    public DbSet<Subscription> Subscriptions => Set<Subscription>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<ProductVariant> ProductVariants => Set<ProductVariant>();
    public DbSet<VariantAttribute> VariantAttributes => Set<VariantAttribute>();
    public DbSet<StockMovement> StockMovements => Set<StockMovement>();
    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<Address> Addresses => Set<Address>();
    public DbSet<Cart> Carts => Set<Cart>();
    public DbSet<CartItem> CartItems => Set<CartItem>();
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderItem> OrderItems => Set<OrderItem>();
    public DbSet<Payment> Payments => Set<Payment>();
    public DbSet<Coupon> Coupons => Set<Coupon>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
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

        ApplyGlobalQueryFilters(modelBuilder);
    }

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

    public override async Task<int> SaveChangesAsync(CancellationToken ct = default)
    {
        var result = await base.SaveChangesAsync(ct);

        if (_mediator is not null) await DispatchDomainEventsAsync(ct);
        
        return result;
    }

    private async Task DispatchDomainEventsAsync(CancellationToken ct)
    {
        var entities = ChangeTracker
            .Entries<BaseEntity>()
            .Where(e => e.Entity.DomainEvents.Any())
            .Select(e => e.Entity)
            .ToList();

        var events = entities.SelectMany(e => e.DomainEvents).ToList();
        entities.ForEach(e => e.ClearDomainEvents());

        foreach (var domainEvent in events)
            await _mediator!.Publish(domainEvent, ct);
    }
}

using Domain.Interfaces;
using Repository.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Repository;

public static class RepositoryDependencyInjection
{
    public static IServiceCollection AddRepository(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<AppDbContext>(options =>
            options.UseNpgsql(
                configuration.GetConnectionString("DefaultConnection"),
                npgsql => npgsql.MigrationsAssembly(typeof(AppDbContext).Assembly.FullName)
            )
        );

        services.AddScoped<IUnitOfWork, UnitOfWork>();

        services.AddScoped<ITenantRepository, TenantRepository>();

        services.AddScoped<ICategoryRepository, CategoryRepository>();
        services.AddScoped<IProductRepository, ProductRepository>();
        services.AddScoped<IProductVariantRepository, ProductVariantRepository>();

        services.AddScoped<ICustomerRepository, CustomerRepository>();

        services.AddScoped<ICartRepository, CartRepository>();
        services.AddScoped<IOrderRepository, OrderRepository>();
        services.AddScoped<ICouponRepository, CouponRepository>();
        services.AddScoped<IStripeWebhookEventRepository, StripeWebhookEventRepository>();

        services.AddScoped<ISubscriptionRepository, SubscriptionRepository>();

        return services;
    }
}


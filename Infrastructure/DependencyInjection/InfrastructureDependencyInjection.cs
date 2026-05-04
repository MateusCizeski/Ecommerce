using Application.Interfaces;
using Domain.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using StackExchange.Redis;
using Stripe;

namespace Infrastructure.DependencyInjection
{
    public static class InfrastructureDependencyInjection
    {
        public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddRepository(configuration);

            services.AddSingleton<IConnectionMultiplexer>(_ =>
                ConnectionMultiplexer.Connect(configuration.GetConnectionString("Redis")!));
            services.AddScoped<ICacheService, Services.RedisCacheService>();

            StripeConfiguration.ApiKey = configuration["Stripe:SecretKey"]
                ?? throw new InvalidOperationException("Stripe:SecretKey is not configured.");
            services.AddScoped<IPaymentGateway, Services.StripePaymentGateway>();

            services.AddHttpContextAccessor();
            services.AddScoped<ITenantContext, Services.HttpTenantContext>();
            services.AddScoped<IOrderNumberGenerator, Services.OrderNumberGenerator>();

            return services;
        }
    }
}

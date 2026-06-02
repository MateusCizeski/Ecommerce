using Application;
using Application.Interfaces;
using Ecommerce.Domain.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Repository;
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
            services.AddScoped<ICacheService, Infrastructure.Caching.RedisCacheService>();

            StripeConfiguration.ApiKey = configuration["Stripe:SecretKey"]
                ?? throw new InvalidOperationException("Stripe:SecretKey is not configured.");
            services.AddScoped<Application.IPaymentGateway, Services.StripePaymentGateway>();

            services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();
            services.AddScoped<ITenantContext, Infrastructure.MultiTenancy.HttpTenantContext>();
            services.AddScoped<IOrderNumberGenerator, Infrastructure.Orders.OrderNumberGenerator>();

            return services;
        }
    }
}


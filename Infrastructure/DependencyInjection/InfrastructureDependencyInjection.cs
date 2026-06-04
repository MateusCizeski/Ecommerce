using Application;
using Application.Interfaces;
using Ecommerce.Domain.Interfaces;
using Infrastructure.Abstractions;
using Infrastructure.Caching;
using Infrastructure.MultiTenancy;
using Infrastructure.Options;
using Infrastructure.Orders;
using Infrastructure.Payments;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Repository;
using StackExchange.Redis;
using Stripe;

namespace Infrastructure.DependencyInjection
{
    /// <summary>
    /// Extensões para registrar serviços de Infrastructure no container de DI.
    /// </summary>
    public static class InfrastructureDependencyInjection
    {
        /// <summary>
        /// Adiciona os serviços de Infrastructure ao container de DI.
        /// </summary>
        /// <param name="services">Coleção de serviços.</param>
        /// <param name="configuration">Configuração da aplicação.</param>
        /// <returns>A coleção de serviços para encadeamento.</returns>
        /// <exception cref="ArgumentNullException">Lançada quando services ou configuration são nulos.</exception>
        /// <exception cref="InvalidOperationException">Lançada quando a configuração é inválida.</exception>
        public static IServiceCollection AddInfrastructure(
            this IServiceCollection services,
            IConfiguration configuration)
        {
            ArgumentNullException.ThrowIfNull(services);
            ArgumentNullException.ThrowIfNull(configuration);

            // Adiciona serviços de Repository e Application
            services.AddRepository(configuration);
            services.AddApplication();

            // Registra opções de configuração
            RegisterOptions(services, configuration);

            // Registra serviços de Cache
            RegisterCacheServices(services);

            // Registra serviços de Payments
            RegisterPaymentServices(services, configuration);

            // Registra serviços de Multi-Tenancy
            RegisterTenancyServices(services);

            // Registra serviços de Orders
            RegisterOrderServices(services);

            return services;
        }

        /// <summary>
        /// Registra as opções de configuração com validação.
        /// </summary>
        private static void RegisterOptions(IServiceCollection services, IConfiguration configuration)
        {
            // Redis Options
            services.Configure<RedisOptions>(configuration.GetSection(RedisOptions.SectionName));
            var redisOptions = configuration.GetSection(RedisOptions.SectionName).Get<RedisOptions>();
            if (redisOptions != null)
            {
                redisOptions.Validate();
            }

            // Stripe Options
            services.Configure<StripeOptions>(configuration.GetSection(StripeOptions.SectionName));
            var stripeOptions = configuration.GetSection(StripeOptions.SectionName).Get<StripeOptions>();
            if (stripeOptions != null)
            {
                stripeOptions.Validate();
                StripeConfiguration.ApiKey = stripeOptions.SecretKey;
            }

            // Order Generation Options
            services.Configure<OrderGenerationOptions>(configuration.GetSection(OrderGenerationOptions.SectionName));
            var orderOptions = configuration.GetSection(OrderGenerationOptions.SectionName).Get<OrderGenerationOptions>();
            if (orderOptions != null)
            {
                orderOptions.Validate();
            }
        }

        /// <summary>
        /// Registra serviços de Cache.
        /// </summary>
        private static void RegisterCacheServices(IServiceCollection services)
        {
            services.AddSingleton<IConnectionMultiplexer>(sp =>
            {
                var options = sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<RedisOptions>>().Value;
                return ConnectionMultiplexer.Connect(options.ConnectionString);
            });

            services.AddScoped<ICacheService, RedisCacheService>();
        }

        /// <summary>
        /// Registra serviços de Payments.
        /// </summary>
        private static void RegisterPaymentServices(IServiceCollection services, IConfiguration configuration)
        {
            // Registra o serviço abstrato do Stripe como internal
            services.AddScoped<IStripePaymentService, StripePaymentService>();

            // Registra o gateway de pagamentos públicos
            services.AddScoped<IPaymentGateway, StripePaymentGateway>();
        }

        /// <summary>
        /// Registra serviços de Multi-Tenancy.
        /// </summary>
        private static void RegisterTenancyServices(IServiceCollection services)
        {
            services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();
            services.AddScoped<ITenantContext, HttpTenantContext>();
        }

        /// <summary>
        /// Registra serviços de Orders.
        /// </summary>
        private static void RegisterOrderServices(IServiceCollection services)
        {
            services.AddScoped<IOrderNumberGenerator, OrderNumberGenerator>();
        }
    }
}


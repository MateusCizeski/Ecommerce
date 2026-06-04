using Ecommerce.Domain.Interfaces;
using Repository.Options;
using Repository.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Repository
{
    /// <summary>
    /// Extensões para registrar serviços de Repository no container de DI.
    /// </summary>
    public static class RepositoryDependencyInjection
    {
        /// <summary>
        /// Adiciona os serviços de Repository ao container de DI.
        /// </summary>
        /// <param name="services">Coleção de serviços.</param>
        /// <param name="configuration">Configuração da aplicação.</param>
        /// <returns>A coleção de serviços para encadeamento.</returns>
        /// <exception cref="ArgumentNullException">Lançada quando services ou configuration são nulos.</exception>
        /// <exception cref="InvalidOperationException">Lançada quando a configuração é inválida.</exception>
        public static IServiceCollection AddRepository(
            this IServiceCollection services,
            IConfiguration configuration)
        {
            ArgumentNullException.ThrowIfNull(services);
            ArgumentNullException.ThrowIfNull(configuration);

            // Registra opções de configuração
            RegisterEntityFrameworkOptions(services, configuration);

            // Configura DbContext
            RegisterDbContext(services, configuration);

            // Registra Unit of Work
            RegisterUnitOfWork(services);

            // Registra todos os repositórios
            RegisterRepositories(services);

            return services;
        }

        /// <summary>
        /// Registra as opções de Entity Framework.
        /// </summary>
        private static void RegisterEntityFrameworkOptions(
            IServiceCollection services,
            IConfiguration configuration)
        {
            services.Configure<EntityFrameworkOptions>(
                configuration.GetSection(EntityFrameworkOptions.SectionName));

            var efOptions = configuration
                .GetSection(EntityFrameworkOptions.SectionName)
                .Get<EntityFrameworkOptions>();

            if (efOptions != null)
            {
                efOptions.Validate();
            }
        }

        /// <summary>
        /// Registra o DbContext com PostgreSQL.
        /// </summary>
        private static void RegisterDbContext(
            IServiceCollection services,
            IConfiguration configuration)
        {
            var connectionString = configuration.GetConnectionString("DefaultConnection")
                ?? throw new InvalidOperationException(
                    "Connection string 'DefaultConnection' não configurada em appsettings.json");

            services.AddDbContext<AppDbContext>(
                (sp, options) =>
                {
                    options.UseNpgsql(
                        connectionString,
                        npgsql => npgsql.MigrationsAssembly(typeof(AppDbContext).Assembly.FullName));

                    // Adiciona logging se o logger estiver disponível
                    var logger = sp.GetService<Microsoft.Extensions.Logging.ILogger<AppDbContext>>();
                    if (logger != null)
                    {
                        options.LogTo(
                            message => logger.LogDebug(message),
                            Microsoft.EntityFrameworkCore.Diagnostics.DbLoggerCategory.Database.Sql.Name);
                    }
                });
        }

        /// <summary>
        /// Registra o Unit of Work.
        /// </summary>
        private static void RegisterUnitOfWork(IServiceCollection services)
        {
            services.AddScoped<IUnitOfWork, UnitOfWork>();
        }

        /// <summary>
        /// Registra todos os repositórios por domínio.
        /// </summary>
        private static void RegisterRepositories(IServiceCollection services)
        {
            // Tenancy
            services.AddScoped<ITenantRepository, TenantRepository>();

            // Catalog
            services.AddScoped<ICategoryRepository, CategoryRepository>();
            services.AddScoped<IProductRepository, ProductRepository>();
            services.AddScoped<IProductVariantRepository, ProductVariantRepository>();

            // Identity
            services.AddScoped<ICustomerRepository, CustomerRepository>();

            // Commerce
            services.AddScoped<ICartRepository, CartRepository>();
            services.AddScoped<IOrderRepository, OrderRepository>();
            services.AddScoped<ICouponRepository, CouponRepository>();
            services.AddScoped<IStripeWebhookEventRepository, StripeWebhookEventRepository>();

            // Billing
            services.AddScoped<ISubscriptionRepository, SubscriptionRepository>();
            services.AddScoped<IPlanRepository, PlanRepository>();
        }
    }
}



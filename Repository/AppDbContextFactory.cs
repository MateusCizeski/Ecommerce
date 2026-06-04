using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.EntityFrameworkCore.Design;
using Repository.Constants;

namespace Repository
{
    /// <summary>
    /// Factory para criar instâncias de AppDbContext durante design-time.
    /// Usado apenas pelo EF Core tooling (dotnet ef migrations add / database update).
    /// Nunca é instanciado em tempo de execução.
    /// </summary>
    /// <remarks>
    /// Instruções de uso:
    /// Executar do diretório raiz da solução:
    /// 
    ///   dotnet ef migrations add InitialCreate \
    ///     --project Repository \
    ///     --startup-project Api
    /// 
    /// Para atualizar o banco:
    /// 
    ///   dotnet ef database update \
    ///     --project Repository \
    ///     --startup-project Api
    /// </remarks>
    public class AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
    {
        /// <summary>
        /// Cria uma instância de AppDbContext para uso do EF Core tooling.
        /// </summary>
        /// <param name="args">Argumentos da linha de comando.</param>
        /// <returns>Uma instância configurada de AppDbContext.</returns>
        /// <exception cref="InvalidOperationException">Lançada quando a connection string não está configurada.</exception>
        public AppDbContext CreateDbContext(string[] args)
        {
            var basePath = ResolveBasePath();
            var configuration = BuildConfiguration(basePath);
            var connectionString = ExtractConnectionString(configuration);

            var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();
            ConfigureOptions(optionsBuilder, connectionString);

            return new AppDbContext(optionsBuilder.Options);
        }

        /// <summary>
        /// Resolve o caminho base para encontrar appsettings.
        /// </summary>
        private static string ResolveBasePath()
        {
            return Path.Combine(
                Directory.GetCurrentDirectory(),
                "..", "Api");
        }

        /// <summary>
        /// Constrói a configuração a partir dos arquivos de appsettings.
        /// </summary>
        private static IConfiguration BuildConfiguration(string basePath)
        {
            return new ConfigurationBuilder()
                .SetBasePath(basePath)
                .AddJsonFile("appsettings.json", optional: false)
                .AddJsonFile("appsettings.Development.json", optional: true)
                .Build();
        }

        /// <summary>
        /// Extrai a connection string da configuração.
        /// </summary>
        /// <exception cref="InvalidOperationException">Lançada quando a connection string não está configurada.</exception>
        private static string ExtractConnectionString(IConfiguration configuration)
        {
            return configuration.GetConnectionString("DefaultConnection")
                ?? throw new InvalidOperationException(
                    "Connection string 'DefaultConnection' não encontrada em appsettings.json");
        }

        /// <summary>
        /// Configura as opções de DbContext para PostgreSQL.
        /// </summary>
        private static void ConfigureOptions(
            DbContextOptionsBuilder<AppDbContext> optionsBuilder,
            string connectionString)
        {
            optionsBuilder.UseNpgsql(
                connectionString,
                npgsql => npgsql.MigrationsAssembly(RepositoryConstants.EntityFramework.MigrationsAssembly));
        }
    }
}

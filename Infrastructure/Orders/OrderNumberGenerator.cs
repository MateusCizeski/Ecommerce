using Application.Interfaces;
using Infrastructure.Constants;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Infrastructure.Orders
{
    /// <summary>
    /// Implementação de gerador de números de ordem.
    /// Gera números únicos e sequenciais para ordens de compra.
    /// </summary>
    public class OrderNumberGenerator : IOrderNumberGenerator
    {
        private readonly ILogger<OrderNumberGenerator> _logger;
        private readonly Options.OrderGenerationOptions _options;
        private static readonly Random _random = new();
        private static readonly object _lockObject = new();

        /// <summary>
        /// Inicializa uma nova instância de OrderNumberGenerator.
        /// </summary>
        /// <param name="options">Opções de configuração para geração de ordem.</param>
        /// <param name="logger">Logger para registrar eventos.</param>
        public OrderNumberGenerator(
            IOptions<Options.OrderGenerationOptions> options,
            ILogger<OrderNumberGenerator> logger)
        {
            ArgumentNullException.ThrowIfNull(options);
            ArgumentNullException.ThrowIfNull(logger);

            _options = options.Value;
            _logger = logger;

            _options.Validate();
        }

        /// <summary>
        /// Gera um número único de ordem de forma assíncrona.
        /// Formato: ORD-{TENANT_PREFIX}-{TIMESTAMP}-{RANDOM_SUFFIX}
        /// </summary>
        /// <param name="tenantId">ID do Tenant para o qual gerar o número.</param>
        /// <param name="cancellationToken">Token de cancelamento.</param>
        /// <returns>Número de ordem gerado.</returns>
        /// <exception cref="ArgumentException">Lançada quando o TenantId é vazio.</exception>
        public Task<string> GenerateAsync(Guid tenantId, CancellationToken cancellationToken = default)
        {
            if (tenantId == Guid.Empty)
            {
                _logger.LogWarning("Tentativa de gerar número de ordem com TenantId vazio.");
                throw new ArgumentException("TenantId não pode ser vazio.", nameof(tenantId));
            }

            try
            {
                var tenantPrefix = ExtractTenantPrefix(tenantId);
                var timestamp = GetTimestamp();
                var suffix = GenerateRandomSuffix();

                var orderNumber = $"{_options.Prefix}-{tenantPrefix}-{timestamp}-{suffix}";

                _logger.LogDebug(
                    "Número de ordem gerado para TenantId: {TenantId}. OrderNumber: {OrderNumber}",
                    tenantId,
                    orderNumber);

                return Task.FromResult(orderNumber);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao gerar número de ordem para TenantId: {TenantId}", tenantId);
                throw;
            }
        }

        /// <summary>
        /// Extrai um prefixo do ID do Tenant.
        /// </summary>
        private string ExtractTenantPrefix(Guid tenantId)
        {
            var guidString = tenantId.ToString("N");
            var prefixLength = Math.Min(_options.TenantIdPrefixLength, guidString.Length);
            return guidString[..prefixLength].ToUpperInvariant();
        }

        /// <summary>
        /// Obtém o timestamp para incluir no número de ordem.
        /// </summary>
        private string GetTimestamp()
        {
            if (_options.UseUnixTimestamp)
            {
                return DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString();
            }

            return DateTime.UtcNow.Ticks.ToString();
        }

        /// <summary>
        /// Gera um sufixo aleatório thread-safe.
        /// </summary>
        private string GenerateRandomSuffix()
        {
            lock (_lockObject)
            {
                var suffix = _random.Next(
                    InfrastructureConstants.Orders.SuffixMinValue,
                    InfrastructureConstants.Orders.SuffixMaxValue + 1);
                return suffix.ToString();
            }
        }
    }
}

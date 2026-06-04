using Application.Exceptions;
using Ecommerce.Domain.Interfaces;
using Infrastructure.Constants;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace Infrastructure.MultiTenancy
{
    /// <summary>
    /// Implementação de contexto de Tenant baseada em HTTP Context.
    /// Resolve o Tenant a partir de items do contexto ou headers HTTP.
    /// </summary>
    public class HttpTenantContext : ITenantContext
    {
        private readonly ILogger<HttpTenantContext> _logger;

        /// <summary>
        /// ID do Tenant.
        /// </summary>
        public Guid TenantId { get; }

        /// <summary>
        /// Subdomínio do Tenant (se disponível).
        /// </summary>
        public string Subdomain { get; }

        /// <summary>
        /// Inicializa uma nova instância de HttpTenantContext.
        /// </summary>
        /// <param name="accessor">Acesso ao HTTP Context.</param>
        /// <param name="logger">Logger para registrar eventos.</param>
        /// <exception cref="InvalidOperationException">Lançada quando não há HTTP context disponível.</exception>
        /// <exception cref="ForbiddenException">Lançada quando o Tenant não pode ser resolvido.</exception>
        public HttpTenantContext(IHttpContextAccessor accessor, ILogger<HttpTenantContext> logger)
        {
            ArgumentNullException.ThrowIfNull(accessor);
            ArgumentNullException.ThrowIfNull(logger);

            _logger = logger;

            var httpContext = accessor.HttpContext
                ?? throw new InvalidOperationException("HTTP context não está disponível.");

            // Tenta recuperar do HttpContext Items primeiro (mais eficiente)
            if (TryResolveTenantFromItems(httpContext, out var tenantId, out var subdomain))
            {
                TenantId = tenantId;
                Subdomain = subdomain;
                _logger.LogDebug("Tenant resolvido a partir do HttpContext Items. TenantId: {TenantId}", TenantId);
                return;
            }

            // Tenta recuperar do header HTTP
            if (TryResolveTenantFromHeader(httpContext, out tenantId))
            {
                TenantId = tenantId;
                Subdomain = string.Empty;
                _logger.LogDebug("Tenant resolvido a partir do header HTTP. TenantId: {TenantId}", TenantId);
                return;
            }

            _logger.LogWarning("Falha ao resolver Tenant. Nenhum identificador encontrado.");
            throw new ForbiddenException(InfrastructureConstants.MultiTenancy.TenantResolutionErrorMessage);
        }

        /// <summary>
        /// Tenta resolver o Tenant a partir dos items do HttpContext.
        /// </summary>
        private static bool TryResolveTenantFromItems(
            HttpContext context,
            out Guid tenantId,
            out string subdomain)
        {
            tenantId = Guid.Empty;
            subdomain = string.Empty;

            if (context.Items.TryGetValue(InfrastructureConstants.MultiTenancy.TenantIdItemKey, out var obj) &&
                obj is Guid id)
            {
                tenantId = id;
                subdomain = context.Items.TryGetValue(
                    InfrastructureConstants.MultiTenancy.TenantSubdomainItemKey,
                    out var subObj) && subObj is string sub
                    ? sub
                    : string.Empty;

                return true;
            }

            return false;
        }

        /// <summary>
        /// Tenta resolver o Tenant a partir do header HTTP.
        /// </summary>
        private static bool TryResolveTenantFromHeader(HttpContext context, out Guid tenantId)
        {
            tenantId = Guid.Empty;

            var headerValue = context.Request.Headers[
                InfrastructureConstants.MultiTenancy.TenantIdHeaderName].FirstOrDefault();

            if (!string.IsNullOrWhiteSpace(headerValue) && Guid.TryParse(headerValue, out var parsedId))
            {
                tenantId = parsedId;
                return true;
            }

            return false;
        }
    }
}


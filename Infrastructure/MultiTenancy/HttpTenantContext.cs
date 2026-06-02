using Application.Exceptions;
using Ecommerce.Domain.Interfaces;
using Microsoft.AspNetCore.Http;

namespace Infrastructure.MultiTenancy
{
    public class HttpTenantContext : ITenantContext
    {
        public Guid TenantId { get; }
        public string Subdomain { get; }

        public HttpTenantContext(IHttpContextAccessor accessor)
        {
            var http = accessor.HttpContext
                ?? throw new InvalidOperationException("No HTTP context available.");

            if (http.Items.TryGetValue("TenantId", out var obj) && obj is Guid id)
            {
                TenantId = id;
                Subdomain = http.Items["TenantSubdomain"] as string ?? string.Empty;
                return;
            }

            var header = http.Request.Headers["X-Tenant-Id"].FirstOrDefault();
            if (!string.IsNullOrWhiteSpace(header) && Guid.TryParse(header, out var parsedId))
            {
                TenantId = parsedId;
                Subdomain = string.Empty;
                return;
            }

            throw new ForbiddenException("Tenant context could not be resolved.");
        }
    }
}


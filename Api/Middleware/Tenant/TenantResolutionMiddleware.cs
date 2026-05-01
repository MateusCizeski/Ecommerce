namespace Api.Middleware.Tenant;

public class TenantResolutionMiddleware(RequestDelegate next)
{
    private static readonly HashSet<string> _bypass =
        ["/api/v1/tenants", "/swagger", "/health"];

    public async Task InvokeAsync(HttpContext context, AppDbContext db)
    {
        var path = context.Request.Path.Value ?? string.Empty;
        if (_bypass.Any(p => path.StartsWith(p, StringComparison.OrdinalIgnoreCase)))
        {
            await next(context);
            return;
        }

        var header = context.Request.Headers["X-Tenant-Id"].FirstOrDefault();
        if (string.IsNullOrWhiteSpace(header))
        {
            await WriteProblem(context, 400, "Missing tenant", "The X-Tenant-Id header is required.");
            return;
        }

        if (!Guid.TryParse(header, out var tenantId))
        {
            await WriteProblem(context, 400, "Invalid tenant", "X-Tenant-Id must be a valid GUID.");
            return;
        }

        var tenant = await db.Tenants.IgnoreQueryFilters()
                                     .FirstOrDefaultAsync(t => t.Id == tenantId);

        if (tenant is null) { await WriteProblem(context, 404, "Tenant not found", $"No tenant with id '{tenantId}' was found."); return; }
        if (!tenant.IsActive) { await WriteProblem(context, 403, "Tenant inactive", "This tenant account is currently inactive."); return; }
        if (tenant.DeletedAt.HasValue) { await WriteProblem(context, 410, "Tenant deleted", "This tenant account has been deleted."); return; }

        context.Items["TenantId"] = tenantId;
        context.Items["TenantSubdomain"] = tenant.Subdomain;

        await next(context);
    }

    private static async Task WriteProblem(HttpContext ctx, int status, string title, string detail)
    {
        ctx.Response.StatusCode = status;
        ctx.Response.ContentType = "application/problem+json";
        await ctx.Response.WriteAsJsonAsync(new { status, title, detail });
    }
}

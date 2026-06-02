using System.Threading.Tasks;
using Application.Interfaces;
using Ecommerce.Domain.Interfaces;
using Ecommerce.Domain;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace Api.Middleware.Billing;

public class PlanFeatureMiddleware
{
  private readonly RequestDelegate _next;
  private readonly ICacheService _cacheService;
  private readonly ISubscriptionRepository _subscriptionRepo;
  private readonly IProductRepository _productRepo;
  private readonly ITenantContext _tenantContext;

  public PlanFeatureMiddleware(
      RequestDelegate next,
      ICacheService cacheService,
      ISubscriptionRepository subscriptionRepo,
      IProductRepository productRepo,
      ITenantContext tenantContext)
  {
    _next = next;
    _cacheService = cacheService;
    _subscriptionRepo = subscriptionRepo;
    _productRepo = productRepo;
    _tenantContext = tenantContext;
  }

  public async Task InvokeAsync(HttpContext context)
  {
    var endpoint = context.GetEndpoint();
    if (endpoint is null)
    {
      await _next(context);
      return;
    }

    var feature = endpoint.Metadata.GetMetadata<SubscriptionFeatureAttribute>();
    if (feature is null)
    {
      await _next(context);
      return;
    }

    var tenantId = _tenantContext.TenantId;
    var cacheKey = BuildSubscriptionCacheKey(tenantId);
    var subscription = await _cacheService.GetAsync<CachedSubscription>(cacheKey, context.RequestAborted);

    if (subscription is null)
    {
      var active = await _subscriptionRepo.GetActiveByTenantAsync(tenantId, context.RequestAborted);
      if (active is null)
      {
        await WriteProblem(context, 402, "Subscription required", "An active subscription is required to access this endpoint.");
        return;
      }

      subscription = CachedSubscription.From(active);
      await _cacheService.SetAsync(cacheKey, subscription, TimeSpan.FromMinutes(5), context.RequestAborted);
    }

    var planFeature = subscription.PlanFeatures.FirstOrDefault(f => f.FeatureKey == feature.FeatureKey);
    if (planFeature is null)
    {
      await WriteProblem(context, 402, "Feature unavailable", "This tenant does not have the required subscription feature.");
      return;
    }

    if (feature.FeatureKey == "max_products")
    {
      if (!int.TryParse(planFeature.LimitValue, out var maxProducts) || maxProducts < 0)
      {
        await WriteProblem(context, 402, "Feature misconfigured", "The max_products feature is not configured correctly for this plan.");
        return;
      }

      var currentProductCount = await _productRepo.Query(tenantId).CountAsync(context.RequestAborted);
      if (currentProductCount >= maxProducts)
      {
        await WriteProblem(context, 402, "Plan limit reached", $"Your current plan allows up to {maxProducts} products. Upgrade to create more.");
        return;
      }
    }

    await _next(context);
  }

  private static string BuildSubscriptionCacheKey(Guid tenantId) => $"tenant:{tenantId}:subscription:active";

  private static async Task WriteProblem(HttpContext ctx, int status, string title, string detail)
  {
    ctx.Response.StatusCode = status;
    ctx.Response.ContentType = "application/problem+json";
    await ctx.Response.WriteAsJsonAsync(new { status, title, detail });
  }

  private sealed record CachedSubscription(
      Guid SubscriptionId,
      Guid PlanId,
      IReadOnlyCollection<CachedPlanFeature> PlanFeatures
  )
  {
    public static CachedSubscription From(Subscription subscription)
        => new(
            subscription.Id,
            subscription.PlanId,
            subscription.Plan.PlanFeatures.Select(pf => new CachedPlanFeature(pf.Feature.Key, pf.LimitValue)).ToList()
        );
  }

  private sealed record CachedPlanFeature(string FeatureKey, string? LimitValue);
}


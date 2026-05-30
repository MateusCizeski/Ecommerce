using System;

namespace Api.Middleware.Billing;

[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = true, Inherited = true)]
public sealed class SubscriptionFeatureAttribute : Attribute
{
  public string FeatureKey { get; }

  public SubscriptionFeatureAttribute(string featureKey)
  {
    FeatureKey = featureKey?.Trim().ToLowerInvariant() ?? throw new ArgumentNullException(nameof(featureKey));
  }
}

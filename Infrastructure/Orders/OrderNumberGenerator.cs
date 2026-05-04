using Application.Interfaces;

namespace Infrastructure.Orders
{
    public class OrderNumberGenerator : IOrderNumberGenerator
    {
        public Task<string> GenerateAsync(Guid tenantId, CancellationToken ct = default)
        {
            var prefix = tenantId.ToString("N")[..4].ToUpperInvariant();
            var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            var suffix = Random.Shared.Next(1000, 9999);
            return Task.FromResult($"ORD-{prefix}-{timestamp}-{suffix}");
        }
    }
}

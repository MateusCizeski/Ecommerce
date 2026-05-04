using Application.Interfaces;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;
using System.Text.Json;

namespace Infrastructure.Caching
{
    public class RedisCacheService(IConnectionMultiplexer redis, ILogger<RedisCacheService> logger) : ICacheService
    {
        private readonly IDatabase _db = redis.GetDatabase();

        public async Task<T?> GetAsync<T>(string key, CancellationToken ct = default)
        {
            try { var v = await _db.StringGetAsync(key); return v.HasValue ? JsonSerializer.Deserialize<T>(v!) : default; }
            catch (Exception ex) { logger.LogWarning(ex, "Cache GET failed for {Key}", key); return default; }
        }

        public async Task SetAsync<T>(string key, T value, TimeSpan? expiry = null, CancellationToken ct = default)
        {
            try { await _db.StringSetAsync(key, JsonSerializer.Serialize(value), expiry ?? TimeSpan.FromMinutes(15)); }
            catch (Exception ex) { logger.LogWarning(ex, "Cache SET failed for {Key}", key); }
        }

        public async Task RemoveAsync(string key, CancellationToken ct = default)
            => await _db.KeyDeleteAsync(key);

        public async Task RemoveByPrefixAsync(string prefix, CancellationToken ct = default)
        {
            var server = redis.GetServer(redis.GetEndPoints().First());
            var keys = server.Keys(pattern: $"{prefix}*").ToArray();
            if (keys.Length > 0) await _db.KeyDeleteAsync(keys);
        }
    }
}

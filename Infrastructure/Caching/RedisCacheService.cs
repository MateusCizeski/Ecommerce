using Application.Interfaces;
using Infrastructure.Constants;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using StackExchange.Redis;
using System.Text.Json;

namespace Infrastructure.Caching
{
    /// <summary>
    /// Implementação de cache utilizando Redis.
    /// </summary>
    public class RedisCacheService : ICacheService
    {
        private readonly IDatabase _database;
        private readonly IConnectionMultiplexer _redis;
        private readonly ILogger<RedisCacheService> _logger;
        private readonly TimeSpan _defaultExpiry;

        /// <summary>
        /// Inicializa uma nova instância de RedisCacheService.
        /// </summary>
        /// <param name="redis">Multiplexador de conexão Redis.</param>
        /// <param name="options">Opções de configuração Redis.</param>
        /// <param name="logger">Logger para registrar eventos.</param>
        public RedisCacheService(
            IConnectionMultiplexer redis,
            IOptions<Options.RedisOptions> options,
            ILogger<RedisCacheService> logger)
        {
            ArgumentNullException.ThrowIfNull(redis);
            ArgumentNullException.ThrowIfNull(options);
            ArgumentNullException.ThrowIfNull(logger);

            _redis = redis;
            _database = redis.GetDatabase();
            _logger = logger;
            _defaultExpiry = TimeSpan.FromMinutes(options.Value.DefaultTtlMinutes);
        }

        /// <summary>
        /// Recupera um valor do cache de forma assíncrona.
        /// </summary>
        /// <typeparam name="T">Tipo do valor a ser recuperado.</typeparam>
        /// <param name="key">Chave do cache.</param>
        /// <param name="cancellationToken">Token de cancelamento.</param>
        /// <returns>O valor recuperado ou null se não existir ou ocorrer erro.</returns>
        public async Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                _logger.LogWarning("Tentativa de recuperar cache com chave vazia.");
                return default;
            }

            try
            {
                var cachedValue = await _database.StringGetAsync(key);

                if (!cachedValue.HasValue)
                {
                    _logger.LogDebug("Cache miss para chave: {Key}", key);
                    return default;
                }

                var deserialized = JsonSerializer.Deserialize<T>(cachedValue!.ToString()!);
                _logger.LogDebug("Cache hit para chave: {Key}", key);
                return deserialized;
            }
            catch (JsonException ex)
            {
                _logger.LogWarning(ex, "Erro ao desserializar valor do cache para chave: {Key}", key);
                await RemoveAsync(key, cancellationToken);
                return default;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Erro ao recuperar valor do cache para chave: {Key}", key);
                return default;
            }
        }

        /// <summary>
        /// Armazena um valor no cache de forma assíncrona.
        /// </summary>
        /// <typeparam name="T">Tipo do valor a ser armazenado.</typeparam>
        /// <param name="key">Chave do cache.</param>
        /// <param name="value">Valor a ser armazenado.</param>
        /// <param name="expiry">Tempo de expiração (opcional).</param>
        /// <param name="cancellationToken">Token de cancelamento.</param>
        public async Task SetAsync<T>(
            string key,
            T value,
            TimeSpan? expiry = null,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                _logger.LogWarning("Tentativa de armazenar cache com chave vazia.");
                return;
            }

            if (value == null)
            {
                _logger.LogWarning("Tentativa de armazenar valor nulo no cache para chave: {Key}", key);
                return;
            }

            try
            {
                var serialized = JsonSerializer.Serialize(value);
                var cacheExpiry = expiry ?? _defaultExpiry;

                await _database.StringSetAsync(key, serialized, cacheExpiry);
                _logger.LogDebug("Valor armazenado em cache para chave: {Key} com TTL: {Ttl}ms", key, cacheExpiry.TotalMilliseconds);
            }
            catch (JsonException ex)
            {
                _logger.LogWarning(ex, "Erro ao serializar valor para cache com chave: {Key}", key);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Erro ao armazenar valor em cache para chave: {Key}", key);
            }
        }

        /// <summary>
        /// Remove um valor do cache de forma assíncrona.
        /// </summary>
        /// <param name="key">Chave do cache.</param>
        /// <param name="cancellationToken">Token de cancelamento.</param>
        public async Task RemoveAsync(string key, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                _logger.LogWarning("Tentativa de remover cache com chave vazia.");
                return;
            }

            try
            {
                await _database.KeyDeleteAsync(key);
                _logger.LogDebug("Valor removido do cache para chave: {Key}", key);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Erro ao remover valor do cache para chave: {Key}", key);
            }
        }

        /// <summary>
        /// Remove múltiplos valores do cache pelo prefixo de forma assíncrona.
        /// </summary>
        /// <param name="prefix">Prefixo das chaves a remover.</param>
        /// <param name="cancellationToken">Token de cancelamento.</param>
        public async Task RemoveByPrefixAsync(string prefix, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(prefix))
            {
                _logger.LogWarning("Tentativa de remover cache com prefixo vazio.");
                return;
            }

            try
            {
                var server = _redis.GetServer(_redis.GetEndPoints().First());
                var keys = server.Keys(pattern: $"{prefix}*").ToArray();

                if (keys.Length == 0)
                {
                    _logger.LogDebug("Nenhuma chave encontrada com prefixo: {Prefix}", prefix);
                    return;
                }

                await _database.KeyDeleteAsync(keys);
                _logger.LogDebug("Removidas {Count} chaves com prefixo: {Prefix}", keys.Length, prefix);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Erro ao remover chaves com prefixo: {Prefix}", prefix);
            }
        }
    }
}

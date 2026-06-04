namespace Infrastructure.Options
{
  /// <summary>
  /// Opções de configuração para Redis.
  /// </summary>
  public class RedisOptions
  {
    /// <summary>Nome da seção de configuração.</summary>
    public const string SectionName = "Redis";

    /// <summary>String de conexão do Redis.</summary>
    public string ConnectionString { get; set; } = string.Empty;

    /// <summary>TTL padrão para itens em cache em minutos.</summary>
    public int DefaultTtlMinutes { get; set; } = 15;

    /// <summary>Valida as opções de Redis.</summary>
    /// <exception cref="ArgumentException">Lançada quando a configuração é inválida.</exception>
    public void Validate()
    {
      if (string.IsNullOrWhiteSpace(ConnectionString))
        throw new ArgumentException($"{nameof(ConnectionString)} não pode estar vazio.", nameof(ConnectionString));

      if (DefaultTtlMinutes <= 0)
        throw new ArgumentException($"{nameof(DefaultTtlMinutes)} deve ser maior que zero.", nameof(DefaultTtlMinutes));
    }
  }
}

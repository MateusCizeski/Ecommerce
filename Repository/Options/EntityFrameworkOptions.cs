namespace Repository.Options
{
  /// <summary>
  /// Opções de configuração para Entity Framework Core.
  /// </summary>
  public class EntityFrameworkOptions
  {
    /// <summary>Nome da seção de configuração.</summary>
    public const string SectionName = "EntityFramework";

    /// <summary>Connection string para o banco de dados.</summary>
    public string ConnectionString { get; set; } = string.Empty;

    /// <summary>Enable SQL Logging para debug.</summary>
    public bool EnableSensitiveDataLogging { get; set; } = false;

    /// <summary>Enable detailed error messages.</summary>
    public bool EnableDetailedErrors { get; set; } = false;

    /// <summary>Usa query tracking por padrão.</summary>
    public bool UseQueryTracking { get; set; } = true;

    /// <summary>Número de registros a carregar em Include.</summary>
    public int DefaultIncludeDepth { get; set; } = 1;

    /// <summary>Valida as opções de Entity Framework.</summary>
    /// <exception cref="ArgumentException">Lançada quando a configuração é inválida.</exception>
    public void Validate()
    {
      if (string.IsNullOrWhiteSpace(ConnectionString))
        throw new ArgumentException($"{nameof(ConnectionString)} não pode estar vazio.", nameof(ConnectionString));

      if (DefaultIncludeDepth < 0)
        throw new ArgumentException($"{nameof(DefaultIncludeDepth)} não pode ser negativo.", nameof(DefaultIncludeDepth));
    }
  }
}

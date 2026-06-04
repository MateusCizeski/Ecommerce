namespace Infrastructure.Options
{
  /// <summary>
  /// Opções de configuração para geração de números de ordem.
  /// </summary>
  public class OrderGenerationOptions
  {
    /// <summary>Nome da seção de configuração.</summary>
    public const string SectionName = "OrderGeneration";

    /// <summary>Prefixo utilizado para números de ordem.</summary>
    public string Prefix { get; set; } = "ORD";

    /// <summary>Comprimento do prefixo extraído do ID do tenant.</summary>
    public int TenantIdPrefixLength { get; set; } = 4;

    /// <summary>Usa timestamp Unix na geração do número de ordem.</summary>
    public bool UseUnixTimestamp { get; set; } = true;

    /// <summary>Valida as opções de geração de ordem.</summary>
    /// <exception cref="ArgumentException">Lançada quando a configuração é inválida.</exception>
    public void Validate()
    {
      if (string.IsNullOrWhiteSpace(Prefix))
        throw new ArgumentException($"{nameof(Prefix)} não pode estar vazio.", nameof(Prefix));

      if (TenantIdPrefixLength <= 0)
        throw new ArgumentException($"{nameof(TenantIdPrefixLength)} deve ser maior que zero.", nameof(TenantIdPrefixLength));
    }
  }
}

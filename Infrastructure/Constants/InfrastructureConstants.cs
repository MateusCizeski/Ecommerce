namespace Infrastructure.Constants
{
  /// <summary>
  /// Constantes utilizadas na camada de Infrastructure.
  /// </summary>
  public static class InfrastructureConstants
  {
    /// <summary>
    /// Constantes relacionadas a Multi-Tenancy.
    /// </summary>
    public static class MultiTenancy
    {
      /// <summary>Chave do item HTTP Context para armazenar ID do Tenant.</summary>
      public const string TenantIdItemKey = "TenantId";

      /// <summary>Chave do item HTTP Context para armazenar Subdomínio do Tenant.</summary>
      public const string TenantSubdomainItemKey = "TenantSubdomain";

      /// <summary>Nome do header HTTP para extrair ID do Tenant.</summary>
      public const string TenantIdHeaderName = "X-Tenant-Id";

      /// <summary>Mensagem de erro quando Tenant não pode ser resolvido.</summary>
      public const string TenantResolutionErrorMessage = "Tenant context could not be resolved.";
    }

    /// <summary>
    /// Constantes relacionadas a Cache.
    /// </summary>
    public static class Cache
    {
      /// <summary>TTL padrão para cache em minutos.</summary>
      public const int DefaultTtlMinutes = 15;

      /// <summary>Tamanho mínimo válido para chave de cache.</summary>
      public const int MinimumKeyLength = 1;
    }

    /// <summary>
    /// Constantes relacionadas a Orders.
    /// </summary>
    public static class Orders
    {
      /// <summary>Prefixo padrão para números de ordem.</summary>
      public const string OrderNumberPrefix = "ORD";

      /// <summary>Tamanho do código de sufixo (valores entre este intervalo).</summary>
      public const int SuffixMinValue = 1000;

      /// <summary>Tamanho máximo do código de sufixo.</summary>
      public const int SuffixMaxValue = 9999;

      /// <summary>Comprimento do ID de tenant para extrair no prefixo.</summary>
      public const int TenantIdPrefixLength = 4;
    }

    /// <summary>
    /// Constantes relacionadas a Payments.
    /// </summary>
    public static class Payments
    {
      /// <summary>Fator de conversão de decimal para centavos no Stripe.</summary>
      public const long CentsConversionFactor = 100;

      /// <summary>Status de pagamento bem-sucedido.</summary>
      public const string SucceededStatus = "succeeded";

      /// <summary>Status que requer ação adicional.</summary>
      public const string RequiresActionStatus = "requires_action";
    }
  }
}

namespace Repository.Constants
{
  /// <summary>
  /// Constantes utilizadas na camada de Repository/Data Access.
  /// </summary>
  public static class RepositoryConstants
  {
    /// <summary>
    /// Constantes relacionadas a Entity Framework Core.
    /// </summary>
    public static class EntityFramework
    {
      /// <summary>Nome do assembly para migrations.</summary>
      public const string MigrationsAssembly = "Repository";

      /// <summary>Provider de banco de dados PostgreSQL.</summary>
      public const string PostgresProvider = "Npgsql.EntityFrameworkCore.PostgreSQL";

      /// <summary>Tamanho máximo padrão de strings sem comprimento explícito.</summary>
      public const int DefaultMaxStringLength = 512;

      /// <summary>Precision padrão para valores decimais.</summary>
      public const int DefaultDecimalPrecision = 18;

      /// <summary>Scale padrão para valores decimais.</summary>
      public const int DefaultDecimalScale = 2;
    }

    /// <summary>
    /// Constantes relacionadas a Queries.
    /// </summary>
    public static class Queries
    {
      /// <summary>Número padrão de registros a carregar antecipadamente.</summary>
      public const int DefaultIncludeDepth = 1;

      /// <summary>Indica se deve usar NoTracking por padrão.</summary>
      public const bool DefaultNoTracking = false;
    }

    /// <summary>
    /// Constantes relacionadas a Soft Delete.
    /// </summary>
    public static class SoftDelete
    {
      /// <summary>Propriedade padrão para data de exclusão.</summary>
      public const string DeletedAtPropertyName = "DeletedAt";

      /// <summary>Valor padrão para nenhuma exclusão.</summary>
      public static readonly DateTime NoDeletedValue = DateTime.MinValue;
    }

    /// <summary>
    /// Constantes relacionadas a Auditoria.
    /// </summary>
    public static class Audit
    {
      /// <summary>Propriedade padrão para data de criação.</summary>
      public const string CreatedAtPropertyName = "CreatedAt";

      /// <summary>Propriedade padrão para data de atualização.</summary>
      public const string UpdatedAtPropertyName = "UpdatedAt";

      /// <summary>Propriedade padrão para user que criou.</summary>
      public const string CreatedByPropertyName = "CreatedBy";

      /// <summary>Propriedade padrão para user que atualizou.</summary>
      public const string UpdatedByPropertyName = "UpdatedBy";
    }

    /// <summary>
    /// Constantes relacionadas a Multi-Tenancy em Repository.
    /// </summary>
    public static class MultiTenancy
    {
      /// <summary>Propriedade padrão para ID do tenant.</summary>
      public const string TenantIdPropertyName = "TenantId";
    }
  }
}

using Ecommerce.Domain;
using Microsoft.EntityFrameworkCore;

namespace Repository.Extensions
{
  /// <summary>
  /// Extensões para facilitar queries comuns no EF Core.
  /// </summary>
  public static class QueryExtensions
  {
    /// <summary>
    /// Filtra entidades pelo ID do tenant.
    /// </summary>
    /// <typeparam name="TEntity">Tipo da entidade.</typeparam>
    /// <param name="query">Query base.</param>
    /// <param name="tenantId">ID do tenant.</param>
    /// <returns>Query filtrada por tenant.</returns>
    public static IQueryable<TEntity> ByTenant<TEntity>(
        this IQueryable<TEntity> query,
        Guid tenantId)
        where TEntity : BaseEntity
    {
      if (tenantId == Guid.Empty)
        throw new ArgumentException("TenantId não pode estar vazio.", nameof(tenantId));

      return query.Where(e => e.TenantId == tenantId);
    }

    /// <summary>
    /// Exclui entidades com soft delete.
    /// </summary>
    /// <typeparam name="TEntity">Tipo da entidade.</typeparam>
    /// <param name="query">Query base.</param>
    /// <returns>Query sem entidades deletadas.</returns>
    public static IQueryable<TEntity> OnlyActive<TEntity>(
        this IQueryable<TEntity> query)
        where TEntity : BaseEntity
    {
      return query.Where(e => e.DeletedAt == null);
    }

    /// <summary>
    /// Pagina resultados de uma query.
    /// </summary>
    /// <typeparam name="TEntity">Tipo da entidade.</typeparam>
    /// <param name="query">Query base.</param>
    /// <param name="pageNumber">Número da página (1-indexed).</param>
    /// <param name="pageSize">Tamanho da página.</param>
    /// <returns>Query paginada.</returns>
    public static IQueryable<TEntity> Paginate<TEntity>(
        this IQueryable<TEntity> query,
        int pageNumber,
        int pageSize)
        where TEntity : BaseEntity
    {
      if (pageNumber <= 0)
        throw new ArgumentException("Número da página deve ser maior que zero.", nameof(pageNumber));

      if (pageSize <= 0)
        throw new ArgumentException("Tamanho da página deve ser maior que zero.", nameof(pageSize));

      return query
          .Skip((pageNumber - 1) * pageSize)
          .Take(pageSize);
    }

    /// <summary>
    /// Executa a query de forma assíncrona com rastreamento desativado.
    /// </summary>
    /// <typeparam name="TEntity">Tipo da entidade.</typeparam>
    /// <param name="query">Query base.</param>
    /// <returns>Query com rastreamento desativado.</returns>
    public static IQueryable<TEntity> AsNoTrackingQuery<TEntity>(
        this IQueryable<TEntity> query)
        where TEntity : BaseEntity
    {
      return query.AsNoTracking();
    }
  }
}

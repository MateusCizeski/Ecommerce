using Ecommerce.Domain;
using Ecommerce.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Repository.Base
{
  /// <summary>
  /// Classe base para todos os repositórios.
  /// Fornece funcionalidade comum como Add, GetById, Query, etc.
  /// </summary>
  /// <typeparam name="TEntity">Tipo da entidade gerenciada pelo repositório.</typeparam>
  public abstract class BaseRepository<TEntity> where TEntity : BaseEntity
  {
    /// <summary>
    /// Contexto de dados do EF Core.
    /// </summary>
    protected readonly AppDbContext Context;

    /// <summary>
    /// DbSet para a entidade.
    /// </summary>
    protected DbSet<TEntity> DbSet => Context.Set<TEntity>();

    /// <summary>
    /// Inicializa uma nova instância de BaseRepository.
    /// </summary>
    /// <param name="context">Contexto de dados do EF Core.</param>
    protected BaseRepository(AppDbContext context)
    {
      ArgumentNullException.ThrowIfNull(context);
      Context = context;
    }

    /// <summary>
    /// Adiciona uma entidade ao banco de dados.
    /// </summary>
    /// <param name="entity">Entidade a ser adicionada.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    public virtual async Task AddAsync(TEntity entity, CancellationToken cancellationToken = default)
    {
      ArgumentNullException.ThrowIfNull(entity);
      await DbSet.AddAsync(entity, cancellationToken);
    }

    /// <summary>
    /// Recupera uma entidade pelo ID.
    /// </summary>
    /// <param name="id">ID da entidade.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>A entidade encontrada ou null.</returns>
    public virtual async Task<TEntity?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
      if (id == Guid.Empty)
        throw new ArgumentException("ID não pode estar vazio.", nameof(id));

      return await DbSet.FirstOrDefaultAsync(e => e.Id == id, cancellationToken);
    }

    /// <summary>
    /// Retorna uma query IQueryable para a entidade.
    /// </summary>
    /// <returns>Query base sem filtros.</returns>
    public virtual IQueryable<TEntity> Query() => DbSet.AsQueryable();

    /// <summary>
    /// Marca uma entidade como deletada (soft delete).
    /// </summary>
    /// <param name="id">ID da entidade.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    public virtual async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
      if (id == Guid.Empty)
        throw new ArgumentException("ID não pode estar vazio.", nameof(id));

      var entity = await GetByIdAsync(id, cancellationToken);
      if (entity is not null)
      {
        entity.Delete();
      }
    }

    /// <summary>
    /// Verifica se uma entidade existe pelo ID.
    /// </summary>
    /// <param name="id">ID da entidade.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>True se existe, false caso contrário.</returns>
    public virtual async Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default)
    {
      if (id == Guid.Empty)
        throw new ArgumentException("ID não pode estar vazio.", nameof(id));

      return await DbSet.AnyAsync(e => e.Id == id, cancellationToken);
    }

    /// <summary>
    /// Obtém o número total de entidades.
    /// </summary>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Número de entidades.</returns>
    public virtual async Task<int> CountAsync(CancellationToken cancellationToken = default)
        => await DbSet.CountAsync(cancellationToken);
  }
}

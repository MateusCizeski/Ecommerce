using Ecommerce.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Repository
{
    /// <summary>
    /// Implementação do padrão Unit of Work.
    /// Coordena a persistência de múltiplas agregações em uma única transação.
    /// </summary>
    public class UnitOfWork : IUnitOfWork
    {
        private readonly AppDbContext _context;
        private readonly ILogger<UnitOfWork>? _logger;

        /// <summary>
        /// Inicializa uma nova instância de UnitOfWork.
        /// </summary>
        /// <param name="context">Contexto de dados do EF Core.</param>
        /// <param name="logger">Logger para operações (opcional).</param>
        public UnitOfWork(AppDbContext context, ILogger<UnitOfWork>? logger = null)
        {
            ArgumentNullException.ThrowIfNull(context);
            _context = context;
            _logger = logger;
        }

        /// <summary>
        /// Confirma todas as mudanças realizadas no contexto.
        /// </summary>
        /// <param name="cancellationToken">Token de cancelamento.</param>
        /// <returns>Número de registros afetados.</returns>
        public async Task<int> CommitAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                var changeCount = await _context.SaveChangesAsync(cancellationToken);

                _logger?.LogInformation(
                    "Unit of Work confirmada com sucesso. {ChangeCount} registros afetados",
                    changeCount);

                return changeCount;
            }
            catch (DbUpdateException ex)
            {
                _logger?.LogError(ex, "Erro ao persistir mudanças no banco de dados");
                throw;
            }
            catch (OperationCanceledException ex)
            {
                _logger?.LogWarning(ex, "Operação de commit foi cancelada");
                throw;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Erro inesperado durante commit");
                throw;
            }
        }

        /// <summary>
        /// Descarta todas as mudanças não-confirmadas.
        /// </summary>
        /// <param name="cancellationToken">Token de cancelamento.</param>
        /// <remarks>
        /// EF Core não possui um método explícito de rollback.
        /// As mudanças são simplesmente descartadas ao não chamar SaveChanges.
        /// Se estiver dentro de uma transação manual, use o rollback da transação.
        /// </remarks>
        public Task RollbackAsync(CancellationToken cancellationToken = default)
        {
            return Task.Run(() =>
            {
                foreach (var entry in _context.ChangeTracker.Entries())
                {
                    entry.State = EntityState.Detached;
                }

                _logger?.LogInformation("Unit of Work revertida. Todas as mudanças foram descartadas");
            }, cancellationToken);
        }
    }
}


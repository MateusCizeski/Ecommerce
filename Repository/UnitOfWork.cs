using Ecommerce.Domain.Interfaces;

namespace Repository;

public class UnitOfWork : IUnitOfWork
{
    private readonly AppDbContext _context;
    public UnitOfWork(AppDbContext context) => _context = context;

    public async Task<int> CommitAsync(CancellationToken ct = default)
        => await _context.SaveChangesAsync(ct);

    public Task RollbackAsync(CancellationToken ct = default)
    {
        // EF Core does not have an explicit rollback on DbContext.
        // Changes are simply discarded by not calling SaveChanges.
        // If inside a manual transaction, use that transaction's RollbackAsync.
        foreach (var entry in _context.ChangeTracker.Entries())
            entry.State = Microsoft.EntityFrameworkCore.EntityState.Detached;

        return Task.CompletedTask;
    }
}


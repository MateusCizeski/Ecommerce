using Ecommerce.Domain;
using Ecommerce.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Repository.Repositories;

public class PlanRepository : IPlanRepository
{
  private readonly AppDbContext _db;

  public PlanRepository(AppDbContext db) => _db = db;

  public async Task<Plan?> GetByIdAsync(Guid id, CancellationToken ct = default)
      => await _db.Plans
                  .Include(p => p.PlanFeatures)
                  .ThenInclude(pf => pf.Feature)
                  .FirstOrDefaultAsync(p => p.Id == id, ct);

  public async Task<IEnumerable<Plan>> GetActiveAsync(CancellationToken ct = default)
      => await _db.Plans
                  .Where(p => p.IsActive)
                  .Include(p => p.PlanFeatures)
                  .ThenInclude(pf => pf.Feature)
                  .ToListAsync(ct);

  public async Task AddAsync(Plan plan, CancellationToken ct = default)
      => await _db.Plans.AddAsync(plan, ct);
}


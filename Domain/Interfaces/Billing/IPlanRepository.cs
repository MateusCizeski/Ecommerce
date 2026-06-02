using Ecommerce.Domain;

namespace Ecommerce.Domain.Interfaces;

public interface IPlanRepository
{
  Task<Plan?> GetByIdAsync(Guid id, CancellationToken ct = default);
  Task<IEnumerable<Plan>> GetActiveAsync(CancellationToken ct = default);
  Task AddAsync(Plan plan, CancellationToken ct = default);
}


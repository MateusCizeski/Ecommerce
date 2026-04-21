using Ecommerce.Domain;

namespace Domain.Interfaces;

public interface ICouponRepository
{
    Task<Coupon?> GetByCodeAsync(Guid tenantId, string code, CancellationToken ct = default);
    Task AddAsync(Coupon coupon, CancellationToken ct = default);
}

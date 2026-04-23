using Ecommerce.Domain;
using Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Repository.Repositories;

public class CouponRepository : ICouponRepository
{
    private readonly AppDbContext _db;
    public CouponRepository(AppDbContext db) => _db = db;

    public async Task AddAsync(Coupon coupon, CancellationToken ct = default)
        => await _db.Coupons.AddAsync(coupon, ct);

    public async Task<Coupon?> GetByCodeAsync(Guid tenantId, string code, CancellationToken ct = default)
        => await _db.Coupons.FirstOrDefaultAsync(c => c.TenantId == tenantId && c.Code == code, ct);
}

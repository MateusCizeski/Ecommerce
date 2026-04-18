namespace Ecommerce.Domain;

public class Coupon : TenantEntity
{
    public string Code { get; private set; } = default!;
    public DiscountType DiscountType { get; private set; }
    public decimal DiscountValue { get; private set; }
    public decimal? MinOrderValue { get; private set; }
    public int? MaxUses { get; private set; }
    public int UsedCount { get; private set; }
    public DateTime? ValidFrom { get; private set; }
    public DateTime? ValidUntil { get; private set; }
    public bool IsActive { get; private set; } = true;

    protected Coupon() { }

    public static Coupon Create(Guid tenantId, string code, DiscountType discountType, decimal discountValue,
    decimal? minOrderValue = null, int? maxUses = null, DateTime? validFrom = null, DateTime? validUntil = null)
    {
        if (discountValue <= 0)
            throw new DomainException("Discount value must be positive.");
        if (discountType == DiscountType.Percentage && discountValue > 100)
            throw new DomainException("Percentage discount cannot exceed 100.");

        return new Coupon
        {
            TenantId = tenantId,
            Code = code.Trim().ToUpperInvariant(),
            DiscountType = discountType,
            DiscountValue = discountValue,
            MinOrderValue = minOrderValue,
            MaxUses = maxUses,
            ValidFrom = validFrom,
            ValidUntil = validUntil
        };
    }

    public bool IsValid(decimal orderTotal)
    {
        if (!IsActive) return false;
        if (ValidFrom.HasValue && DateTime.UtcNow < ValidFrom) return false;
        if (ValidUntil.HasValue && DateTime.UtcNow > ValidUntil) return false;
        if (MaxUses.HasValue && UsedCount >= MaxUses) return false;
        if (MinOrderValue.HasValue && orderTotal < MinOrderValue.Value) return false;
        return true;
    }

    public decimal CalculateDiscount(decimal orderTotal)
    {
        if (!IsValid(orderTotal)) return 0;
        return DiscountType == DiscountType.Percentage
            ? orderTotal * (DiscountValue / 100)
            : Math.Min(DiscountValue, orderTotal);
    }

    public void Redeem()
    {
        if (MaxUses.HasValue && UsedCount >= MaxUses)
            throw new DomainException("Coupon usage limit reached.");
        UsedCount++;
    }
}

using Ecommerce.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Repository.SettingsEF;

public class CouponConfig : IEntityTypeConfiguration<Coupon>
{
    public void Configure(EntityTypeBuilder<Coupon> builder)
    {
        builder.ToTable("Coupons");

        builder.HasKey(c => c.Id);

        builder.Property(c => c.Code)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(c => c.DiscountType)
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.Property(c => c.DiscountValue)
            .HasColumnType("decimal(18,2)");

        builder.Property(c => c.MinOrderValue)
            .HasColumnType("decimal(18,2)");

        builder.Property(c => c.IsActive)
            .HasDefaultValue(true);

        builder.HasIndex(c => new { c.TenantId, c.Code }).IsUnique();
        builder.HasIndex(c => new { c.TenantId, c.IsActive, c.ValidUntil });
    }
}

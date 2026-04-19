using Ecommerce.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Repository.SettingsEF;

public class OrderConfig : IEntityTypeConfiguration<Order>
{
    public void Configure(EntityTypeBuilder<Order> builder)
    {
        builder.ToTable("Orders");

        builder.HasKey(o => o.Id);

        builder.Property(o => o.OrderNumber)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(o => o.Status)
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.Property(o => o.Subtotal)
            .HasColumnType("decimal(18,2)");

        builder.Property(o => o.DiscountAmount)
            .HasColumnType("decimal(18,2)")
            .HasDefaultValue(0m);

        builder.Property(o => o.ShippingAmount)
            .HasColumnType("decimal(18,2)")
            .HasDefaultValue(0m);

        builder.Property(o => o.TaxAmount)
            .HasColumnType("decimal(18,2)")
            .HasDefaultValue(0m);

        builder.Property(o => o.TotalAmount)
            .HasColumnType("decimal(18,2)");

        builder.Property(o => o.Notes).HasMaxLength(1000);

        builder.HasMany(o => o.Items)
         .WithOne()
         .HasForeignKey(i => i.OrderId)
         .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(o => o.Payments)
         .WithOne()
         .HasForeignKey(p => p.OrderId)
         .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<Address>()
         .WithMany()
         .HasForeignKey(o => o.ShippingAddressId)
         .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<Coupon>()
         .WithMany()
         .HasForeignKey(o => o.CouponId)
         .OnDelete(DeleteBehavior.SetNull)
         .IsRequired(false);

        builder.HasIndex(o => new { o.TenantId, o.OrderNumber }).IsUnique();
        builder.HasIndex(o => new { o.TenantId, o.CustomerId });
        builder.HasIndex(o => new { o.TenantId, o.Status });
        builder.HasIndex(o => o.PlacedAt);
    }
}

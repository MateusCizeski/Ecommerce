using Ecommerce.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Repository.SettingsEF;

public class OrderItemConfig : IEntityTypeConfiguration<OrderItem>
{
    public void Configure(EntityTypeBuilder<OrderItem> builder)
    {
        builder.ToTable("OrderItems");

        builder.HasKey(i => i.Id);

        builder.Property(i => i.SKUSnapshot)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(i => i.ProductNameSnapshot)
            .IsRequired()
            .HasMaxLength(300);

        builder.Property(i => i.UnitPrice)
            .HasColumnType("decimal(18,2)");

        builder.Property(i => i.TotalPrice)
            .HasColumnType("decimal(18,2)");

        builder.HasIndex(i => i.ProductVariantId);
    }
}

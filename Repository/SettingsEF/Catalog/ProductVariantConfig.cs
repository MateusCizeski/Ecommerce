using Ecommerce.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Repository.SettingsEF;

public class ProductVariantConfig : IEntityTypeConfiguration<ProductVariant>
{
    public void Configure(EntityTypeBuilder<ProductVariant> builder)
    {
        builder.ToTable("ProductVariants");

        builder.HasKey(v => v.Id);

        builder.Property(v => v.SKU)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(v => v.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(v => v.Price)
            .HasColumnType("decimal(18,2)");

        builder.Property(v => v.CompareAtPrice)
            .HasColumnType("decimal(18,2)");

        builder.Property(v => v.ImageUrl)
            .HasMaxLength(1000);

        builder.Property(v => v.IsActive)
            .HasDefaultValue(true);

        builder.Property(v => v.StockQuantity)
            .HasDefaultValue(0);

        builder.HasMany(v => v.Attributes)
         .WithOne()
         .HasForeignKey(a => a.ProductVariantId)
         .OnDelete(DeleteBehavior.Cascade);
        
        builder.HasMany(v => v.StockMovements)
         .WithOne()
         .HasForeignKey(m => m.ProductVariantId)
         .OnDelete(DeleteBehavior.Restrict);
        
        builder.HasIndex(v => v.SKU).IsUnique()
         .HasFilter("\"DeletedAt\" IS NULL");
        
        builder.UseXminAsConcurrencyToken();
    }
}

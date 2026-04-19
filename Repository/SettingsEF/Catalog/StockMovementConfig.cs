using Ecommerce.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Repository.SettingsEF;

public class StockMovementConfig : IEntityTypeConfiguration<StockMovement>
{
    public void Configure(EntityTypeBuilder<StockMovement> builder)
    {
        builder.ToTable("StockMovements");

        builder.HasKey(m => m.Id);

        builder.Property(m => m.MovementType)
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.Property(m => m.Reason)
            .IsRequired()
            .HasMaxLength(500);

        builder.HasIndex(m => m.ProductVariantId);
        builder.HasIndex(m => m.CreatedAt);
    }
}

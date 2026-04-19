using Ecommerce.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Repository.SettingsEF;

public class CartConfig : IEntityTypeConfiguration<Cart>
{
    public void Configure(EntityTypeBuilder<Cart> builder)
    {
        builder.ToTable("Carts");

        builder.HasKey(c => c.Id);

        builder.Property(c => c.Status)
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.HasMany(c => c.Items)
         .WithOne()
         .HasForeignKey(i => i.CartId)
         .OnDelete(DeleteBehavior.Cascade);

        builder.Ignore(c => c.Total);
        builder.Ignore(c => c.ItemCount);

        builder.HasIndex(c => new { c.TenantId, c.CustomerId, c.Status });
        builder.HasIndex(c => c.ExpiresAt);
    }
}

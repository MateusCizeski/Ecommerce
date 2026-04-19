using Ecommerce.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Repository.SettingsEF;

public class VariantAttributeConfig : IEntityTypeConfiguration<VariantAttribute>
{
    public void Configure(EntityTypeBuilder<VariantAttribute> builder)
    {
        builder.ToTable("VariantAttributes");

        builder.HasKey(a => a.Id);

        builder.Property(a => a.AttributeName)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(a => a.AttributeValue)
            .IsRequired()
            .HasMaxLength(200);

        builder.HasIndex(a => new { a.ProductVariantId, a.AttributeName });
    }
}

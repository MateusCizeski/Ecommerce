using Ecommerce.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Repository.SettingsEF;

public class CategoryConfig : IEntityTypeConfiguration<Category>
{
    public void Configure(EntityTypeBuilder<Category> builder)
    {
        builder.ToTable("Categories");

        builder.HasKey(c => c.Id);

        builder.Property(c => c.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(c => c.Slug)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(c => c.Description)
            .HasMaxLength(1000);

        builder.Property(c => c.IsActive)
            .HasDefaultValue(true);

        builder.Property(c => c.SortOrder)
            .HasDefaultValue(0);

        builder.HasOne(c => c.ParentCategory)
         .WithMany(c => c.SubCategories)
         .HasForeignKey(c => c.ParentCategoryId)
         .OnDelete(DeleteBehavior.Restrict)
         .IsRequired(false);

        builder.HasIndex(c => new { c.TenantId, c.Slug })
            .IsUnique()
            .HasFilter("\"DeletedAt\" IS NULL");
    }
}

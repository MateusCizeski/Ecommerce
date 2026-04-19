using Ecommerce.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Repository.SettingsEF;

public class PlanFeatureConfig : IEntityTypeConfiguration<PlanFeature>
{
    public void Configure(EntityTypeBuilder<PlanFeature> builder)
    {
        builder.ToTable("PlanFeatures");

        builder.HasKey(pf => pf.Id);

        builder.Property(pf => pf.LimitValue)
            .HasMaxLength(100);

        builder.HasOne(pf => pf.Feature)
         .WithMany()
         .HasForeignKey(pf => pf.FeatureId)
         .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(pf => new { pf.PlanId, pf.FeatureId })
            .IsUnique();
    }
}

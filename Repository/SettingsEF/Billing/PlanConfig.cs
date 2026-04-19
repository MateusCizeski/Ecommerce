using Ecommerce.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Repository.SettingsEF;

public class PlanConfig : IEntityTypeConfiguration<Plan>
{
    public void Configure(EntityTypeBuilder<Plan> builder)
    {
        builder.ToTable("Plans");

        builder.HasKey(p => p.Id);

        builder.Property(p => p.Name)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(p => p.Description)
            .HasMaxLength(500);

        builder.Property(p => p.Price)
            .HasColumnType("decimal(18,2)");

        builder.Property(p => p.BillingCycle)
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.Property(p => p.IsActive)
            .HasDefaultValue(true);

        builder.HasMany(p => p.PlanFeatures)
         .WithOne(pf => pf.Plan)
         .HasForeignKey(pf => pf.PlanId)
         .OnDelete(DeleteBehavior.Cascade);
    }
}

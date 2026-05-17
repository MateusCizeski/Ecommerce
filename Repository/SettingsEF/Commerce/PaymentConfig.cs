using Ecommerce.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Repository.SettingsEF;

internal class PaymentConfig : IEntityTypeConfiguration<Payment>
{
    public void Configure(EntityTypeBuilder<Payment> builder)
    {
        builder.ToTable("Payments");

        builder.HasKey(p => p.Id);

        builder.Property(p => p.Method)
            .HasConversion<string>()
            .HasMaxLength(30);

        builder.Property(p => p.Status)
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.Property(p => p.Amount)
            .HasColumnType("decimal(18,2)");

        builder.Property(p => p.RefundedAmount)
            .HasColumnType("decimal(18,2)")
            .HasDefaultValue(0m);

        builder.Property(p => p.Currency)
            .IsRequired()
            .HasMaxLength(3);

        builder.Property(p => p.StripePaymentIntentId)
            .HasMaxLength(200);

        builder.Property(p => p.StripeChargeId)
            .HasMaxLength(200);

        builder.Property(p => p.GatewayResponse)
            .HasColumnType("text");

        builder.Ignore(p => p.RefundableAmount);

        builder.HasIndex(p => p.StripePaymentIntentId);
        builder.HasIndex(p => p.StripeChargeId);
        builder.HasIndex(p => new { p.OrderId, p.Status });
    }
}

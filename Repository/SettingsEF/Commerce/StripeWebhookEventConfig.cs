namespace Repository.SettingsEF;

public class StripeWebhookEventConfig : IEntityTypeConfiguration<StripeWebhookEvent>
{
    public void Configure(EntityTypeBuilder<StripeWebhookEvent> builder)
    {
        builder.ToTable("StripeWebhookEvents");
        builder.HasKey(e => e.Id);

        builder.Property(e => e.StripeEventId).IsRequired().HasMaxLength(200);
        builder.Property(e => e.EventType).IsRequired().HasMaxLength(100);
        builder.Property(e => e.Payload).HasColumnType("text").IsRequired();
        builder.Property(e => e.Processed).HasDefaultValue(false);
        builder.Property(e => e.Error).HasMaxLength(1000);

        // Unique index — the core of idempotency
        // If the same StripeEventId arrives twice, the second insert fails
        // at DB level even if the application check is bypassed
        builder.HasIndex(e => e.StripeEventId).IsUnique();
        builder.HasIndex(e => new { e.Processed, e.CreatedAt });
    }
}


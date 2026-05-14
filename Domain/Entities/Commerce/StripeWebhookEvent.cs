namespace Ecommerce.Domain;

public class StripeWebhookEvent : BaseEntity
{
    public string StripeEventId { get; private set; } = default!;
    public string EventType { get; private set; } = default!;
    public string Payload { get; private set; } = default!;
    public bool Processed { get; private set; }
    public string? Error { get; private set; }
    public DateTime? ProcessedAt { get; private set; }

    protected StripeWebhookEvent() { }

    public static StripeWebhookEvent Create(string stripeEventId, string eventType, string payload) =>
    new()
    {
        StripeEventId = stripeEventId,
        EventType = eventType,
        Payload = payload,
        Processed = false
    };

    public void MarkProcessed()
    {
        Processed = true;
        ProcessedAt = DateTime.UtcNow;
        MarkUpdated();
    }

    public void MarkFailed(string error)
    {
        Processed = false;
        Error = error;
        ProcessedAt = DateTime.UtcNow;
        MarkUpdated();
    }
}

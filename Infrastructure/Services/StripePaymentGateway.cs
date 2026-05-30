using Application;
using Ecommerce.Domain;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Stripe;

namespace Infrastructure.Services;

public class StripePaymentGateway(
    IConfiguration configuration,
    ILogger<StripePaymentGateway> logger) : IPaymentGateway
{
    private string WebhookSecret => configuration["Stripe:WebhookSecret"]
        ?? throw new InvalidOperationException("Stripe:WebhookSecret is not configured.");

    public async Task<CreatePaymentIntentResult> CreatePaymentIntentAsync(
        decimal amount, string currency, string stripeCustomerId, CancellationToken ct = default)
    {
        var options = new PaymentIntentCreateOptions
        {
            Amount = (long)(amount * 100),
            Currency = currency.ToLowerInvariant(),
            Customer = stripeCustomerId,
            AutomaticPaymentMethods = new() { Enabled = true }
        };

        var intent = await new PaymentIntentService().CreateAsync(options, cancellationToken: ct);
        return new(intent.Id, intent.ClientSecret, intent.Status == "requires_action");
    }

    public async Task<ConfirmPaymentResult> ConfirmPaymentAsync(
        string paymentIntentId, CancellationToken ct = default)
    {
        var intent = await new PaymentIntentService().GetAsync(paymentIntentId, cancellationToken: ct);
        return new(intent.Status == "succeeded", intent.LatestChargeId ?? string.Empty, intent.Status);
    }

    public async Task<RefundResult> RefundAsync(
        string chargeId, decimal? amount = null, CancellationToken ct = default)
    {
        var options = new RefundCreateOptions
        {
            Charge = chargeId,
            Amount = amount.HasValue ? (long)(amount.Value * 100) : null
        };

        var refund = await new RefundService().CreateAsync(options, cancellationToken: ct);

        return new(
            refund.Status == "succeeded",
            refund.Id,
            refund.Amount / 100m
        );
    }

    public async Task<string> CreateOrGetCustomerAsync(
        string email, string name, CancellationToken ct = default)
    {
        var search = await new CustomerService().SearchAsync(
            new CustomerSearchOptions { Query = $"email:'{email}'" },
            cancellationToken: ct);

        if (search.Data.Count > 0) return search.Data[0].Id;

        var customer = await new CustomerService().CreateAsync(
            new CustomerCreateOptions { Email = email, Name = name },
            cancellationToken: ct);

        return customer.Id;
    }

    public async Task<string> CreateStripeSubscriptionAsync(
        string stripeCustomerId,
        string planName,
        decimal amount,
        BillingCycle billingCycle,
        CancellationToken ct = default)
    {
        var subscriptionOptions = new SubscriptionCreateOptions
        {
            Customer = stripeCustomerId,
            Items = new List<SubscriptionItemOptions>
            {
                new SubscriptionItemOptions
                {
                    PriceData = new SubscriptionItemPriceDataOptions
                    {
                        Currency = "usd",
                        UnitAmount = (long)(amount * 100),
                        Recurring = new SubscriptionItemPriceDataRecurringOptions
                        {
                            Interval = billingCycle == BillingCycle.Yearly ? "year" : "month"
                        },
                                        Product = planName
                    }
                }
            }
        };

        var subscription = await new SubscriptionService().CreateAsync(subscriptionOptions, cancellationToken: ct);
        return subscription.Id;
    }

    public async Task<bool> CancelStripeSubscriptionAsync(
        string stripeSubscriptionId,
        CancellationToken ct = default)
    {
        var subscription = await new SubscriptionService().CancelAsync(stripeSubscriptionId, cancellationToken: ct);
        return subscription.Status == "canceled" || subscription.Status == "canceled";
    }

    /// <summary>
    /// Validates the Stripe-Signature header using the webhook secret.
    /// Throws StripeException if invalid (Stripe SDK handles the check).
    /// </summary>
    public StripeWebhookParseResult ParseWebhookEvent(string payload, string signatureHeader)
    {
        // Stripe SDK validates timestamp tolerance (default 300s) + HMAC signature
        var stripeEvent = EventUtility.ConstructEvent(payload, signatureHeader, WebhookSecret);

        return stripeEvent.Type switch
        {
            "payment_intent.succeeded" => ParsePaymentIntentSucceeded(stripeEvent),
            "payment_intent.payment_failed" => ParsePaymentIntentFailed(stripeEvent),
            "charge.refunded" => ParseChargeRefunded(stripeEvent),
            "customer.subscription.deleted" => ParseCustomerSubscriptionDeleted(stripeEvent),
            "invoice.payment_failed" => ParseInvoicePaymentFailed(stripeEvent),
            _ => new StripeWebhookParseResult(
                stripeEvent.Id, stripeEvent.Type,
                string.Empty, null, null, null, null, null, stripeEvent.ToJson())
        };
    }

    private static StripeWebhookParseResult ParsePaymentIntentSucceeded(Event e)
    {
        var intent = e.Data.Object as PaymentIntent;
        return new(
            e.Id, e.Type,
            intent?.Id ?? string.Empty,
            intent?.LatestChargeId,
            null,
            null,
            intent?.Amount / 100m,
            null,
            e.ToJson());
    }

    private static StripeWebhookParseResult ParsePaymentIntentFailed(Event e)
    {
        var intent = e.Data.Object as PaymentIntent;
        return new(
            e.Id, e.Type,
            intent?.Id ?? string.Empty,
            null,
            null,
            null,
            intent?.Amount / 100m,
            intent?.LastPaymentError?.Message,
            e.ToJson());
    }

    private static StripeWebhookParseResult ParseChargeRefunded(Event e)
    {
        var charge = e.Data.Object as Charge;
        return new(
            e.Id, e.Type,
            charge?.PaymentIntentId ?? string.Empty,
            charge?.Id,
            null,
            charge?.Invoice?.CustomerId,
            charge?.AmountRefunded / 100m,
            null,
            e.ToJson());
    }

    private static StripeWebhookParseResult ParseCustomerSubscriptionDeleted(Event e)
    {
        var stripeSubscription = e.Data.Object as Stripe.Subscription;
        return new(
            e.Id, e.Type,
            string.Empty,
            null,
            stripeSubscription?.Id,
            stripeSubscription?.CustomerId,
            null,
            null,
            e.ToJson());
    }

    private static StripeWebhookParseResult ParseInvoicePaymentFailed(Event e)
    {
        var invoice = e.Data.Object as Invoice;
        return new(
            e.Id, e.Type,
            invoice?.PaymentIntentId ?? string.Empty,
            invoice?.ChargeId,
            invoice?.SubscriptionId,
            invoice?.CustomerId,
            invoice?.AmountDue / 100m,
            invoice?.LastFinalizationError?.Message,
            e.ToJson());
    }
}

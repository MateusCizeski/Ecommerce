using Application.Interfaces;
using Microsoft.Extensions.Logging;
using Stripe;

namespace Infrastructure.Payments
{
    public class StripePaymentGateway(ILogger<StripePaymentGateway> logger) : IPaymentGateway
    {
        public async Task<Application.Interfaces.CreatePaymentIntentResult> CreatePaymentIntentAsync(decimal amount, string currency, string customerId, CancellationToken ct = default)
        {
            var options = new PaymentIntentCreateOptions
            {
                Amount = (long)(amount * 100),
                Currency = currency.ToLowerInvariant(),
                Customer = customerId,
                AutomaticPaymentMethods = new() { Enabled = true }
            };
            var intent = await new PaymentIntentService().CreateAsync(options, cancellationToken: ct);
            return new(intent.Id, intent.ClientSecret, intent.Status == "requires_action");
        }

        public async Task<ConfirmPaymentResult> ConfirmPaymentAsync(string paymentIntentId, CancellationToken ct = default)
        {
            var intent = await new PaymentIntentService().GetAsync(paymentIntentId, cancellationToken: ct);
            return new(intent.Status == "succeeded", intent.LatestChargeId ?? string.Empty, intent.Status);
        }

        public async Task<RefundResult> RefundAsync(string chargeId, decimal? amount = null, CancellationToken ct = default)
        {
            var options = new RefundCreateOptions { Charge = chargeId, Amount = amount.HasValue ? (long)(amount.Value * 100) : null };
            var refund = await new RefundService().CreateAsync(options, cancellationToken: ct);
            return new(refund.Status == "succeeded", refund.Id);
        }

        public async Task<string> CreateOrGetCustomerAsync(string email, string name, CancellationToken ct = default)
        {
            var search = await new CustomerService().SearchAsync(new CustomerSearchOptions { Query = $"email:'{email}'" }, cancellationToken: ct);
            if (search.Data.Count > 0) return search.Data[0].Id;
            var customer = await new CustomerService().CreateAsync(new CustomerCreateOptions { Email = email, Name = name }, cancellationToken: ct);
            return customer.Id;
        }
    }
}

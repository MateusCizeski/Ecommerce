using Stripe;

namespace Infrastructure.Abstractions
{
  /// <summary>
  /// Abstração para interações com API Stripe.
  /// Desacopla a implementação específica do Stripe da lógica de negócio.
  /// </summary>
  public interface IStripePaymentService
  {
    /// <summary>
    /// Cria uma intenção de pagamento no Stripe.
    /// </summary>
    /// <param name="amount">Valor em centavos.</param>
    /// <param name="currency">Código da moeda (ex: 'usd', 'brl').</param>
    /// <param name="customerId">ID do cliente no Stripe.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>A intenção de pagamento criada.</returns>
    Task<PaymentIntent> CreatePaymentIntentAsync(long amount, string currency, string customerId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Recupera uma intenção de pagamento existente.
    /// </summary>
    /// <param name="paymentIntentId">ID da intenção de pagamento.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>A intenção de pagamento.</returns>
    Task<PaymentIntent> GetPaymentIntentAsync(string paymentIntentId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Reembolsa uma cobrança.
    /// </summary>
    /// <param name="chargeId">ID da cobrança.</param>
    /// <param name="amount">Valor em centavos a reembolsar (null para reembolso completo).</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>O reembolso criado.</returns>
    Task<Refund> RefundAsync(string chargeId, long? amount = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Cria ou recupera um cliente no Stripe.
    /// </summary>
    /// <param name="email">Email do cliente.</param>
    /// <param name="name">Nome do cliente.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>ID do cliente no Stripe.</returns>
    Task<string> CreateOrGetCustomerAsync(string email, string name, CancellationToken cancellationToken = default);
  }
}

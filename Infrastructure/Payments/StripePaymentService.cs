using Infrastructure.Abstractions;
using Microsoft.Extensions.Logging;
using Stripe;

namespace Infrastructure.Payments
{
  /// <summary>
  /// Implementação abstrata para interações com Stripe.
  /// Encapsula a complexidade do SDK Stripe e fornece uma interface limpa.
  /// </summary>
  internal class StripePaymentService : IStripePaymentService
  {
    private readonly ILogger<StripePaymentService> _logger;
    private readonly PaymentIntentService _paymentIntentService;
    private readonly CustomerService _customerService;
    private readonly RefundService _refundService;

    /// <summary>
    /// Inicializa uma nova instância de StripePaymentService.
    /// </summary>
    /// <param name="logger">Logger para registrar eventos.</param>
    public StripePaymentService(ILogger<StripePaymentService> logger)
    {
      ArgumentNullException.ThrowIfNull(logger);

      _logger = logger;
      _paymentIntentService = new PaymentIntentService();
      _customerService = new CustomerService();
      _refundService = new RefundService();
    }

    /// <summary>
    /// Cria uma intenção de pagamento no Stripe.
    /// </summary>
    public async Task<PaymentIntent> CreatePaymentIntentAsync(
        long amount,
        string currency,
        string customerId,
        CancellationToken cancellationToken = default)
    {
      if (amount <= 0)
        throw new ArgumentException("Valor deve ser maior que zero.", nameof(amount));

      if (string.IsNullOrWhiteSpace(currency))
        throw new ArgumentException("Moeda não pode estar vazia.", nameof(currency));

      if (string.IsNullOrWhiteSpace(customerId))
        throw new ArgumentException("ID do cliente não pode estar vazio.", nameof(customerId));

      try
      {
        var options = new PaymentIntentCreateOptions
        {
          Amount = amount,
          Currency = currency.ToLowerInvariant(),
          Customer = customerId,
          AutomaticPaymentMethods = new() { Enabled = true }
        };

        var intent = await _paymentIntentService.CreateAsync(options, cancellationToken: cancellationToken);

        _logger.LogInformation(
            "PaymentIntent criada com sucesso. ID: {PaymentIntentId}, Valor: {Amount} {Currency}",
            intent.Id,
            amount,
            currency);

        return intent;
      }
      catch (StripeException ex)
      {
        _logger.LogError(ex, "Erro do Stripe ao criar PaymentIntent para cliente: {CustomerId}", customerId);
        throw;
      }
      catch (Exception ex)
      {
        _logger.LogError(ex, "Erro inesperado ao criar PaymentIntent para cliente: {CustomerId}", customerId);
        throw;
      }
    }

    /// <summary>
    /// Recupera uma intenção de pagamento existente.
    /// </summary>
    public async Task<PaymentIntent> GetPaymentIntentAsync(
        string paymentIntentId,
        CancellationToken cancellationToken = default)
    {
      if (string.IsNullOrWhiteSpace(paymentIntentId))
        throw new ArgumentException("ID da intenção de pagamento não pode estar vazio.", nameof(paymentIntentId));

      try
      {
        var intent = await _paymentIntentService.GetAsync(paymentIntentId, cancellationToken: cancellationToken);

        _logger.LogDebug("PaymentIntent recuperada. ID: {PaymentIntentId}, Status: {Status}", paymentIntentId, intent.Status);

        return intent;
      }
      catch (StripeException ex)
      {
        _logger.LogError(ex, "Erro do Stripe ao recuperar PaymentIntent: {PaymentIntentId}", paymentIntentId);
        throw;
      }
      catch (Exception ex)
      {
        _logger.LogError(ex, "Erro inesperado ao recuperar PaymentIntent: {PaymentIntentId}", paymentIntentId);
        throw;
      }
    }

    /// <summary>
    /// Reembolsa uma cobrança.
    /// </summary>
    public async Task<Refund> RefundAsync(
        string chargeId,
        long? amount = null,
        CancellationToken cancellationToken = default)
    {
      if (string.IsNullOrWhiteSpace(chargeId))
        throw new ArgumentException("ID da cobrança não pode estar vazio.", nameof(chargeId));

      if (amount.HasValue && amount <= 0)
        throw new ArgumentException("Valor de reembolso deve ser maior que zero.", nameof(amount));

      try
      {
        var options = new RefundCreateOptions
        {
          Charge = chargeId,
          Amount = amount
        };

        var refund = await _refundService.CreateAsync(options, cancellationToken: cancellationToken);

        _logger.LogInformation(
            "Reembolso processado com sucesso. RefundId: {RefundId}, Charge: {ChargeId}, Status: {Status}",
            refund.Id,
            chargeId,
            refund.Status);

        return refund;
      }
      catch (StripeException ex)
      {
        _logger.LogError(ex, "Erro do Stripe ao processar reembolso para cobrança: {ChargeId}", chargeId);
        throw;
      }
      catch (Exception ex)
      {
        _logger.LogError(ex, "Erro inesperado ao processar reembolso para cobrança: {ChargeId}", chargeId);
        throw;
      }
    }

    /// <summary>
    /// Cria ou recupera um cliente no Stripe.
    /// </summary>
    public async Task<string> CreateOrGetCustomerAsync(
        string email,
        string name,
        CancellationToken cancellationToken = default)
    {
      if (string.IsNullOrWhiteSpace(email))
        throw new ArgumentException("Email não pode estar vazio.", nameof(email));

      if (string.IsNullOrWhiteSpace(name))
        throw new ArgumentException("Nome não pode estar vazio.", nameof(name));

      try
      {
        // Busca cliente existente
        var search = await _customerService.SearchAsync(
            new CustomerSearchOptions { Query = $"email:'{email}'" },
            cancellationToken: cancellationToken);

        if (search.Data.Count > 0)
        {
          _logger.LogDebug("Cliente encontrado no Stripe. ID: {CustomerId}, Email: {Email}", search.Data[0].Id, email);
          return search.Data[0].Id;
        }

        // Cria novo cliente
        var customer = await _customerService.CreateAsync(
            new CustomerCreateOptions { Email = email, Name = name },
            cancellationToken: cancellationToken);

        _logger.LogInformation("Novo cliente criado no Stripe. ID: {CustomerId}, Email: {Email}", customer.Id, email);

        return customer.Id;
      }
      catch (StripeException ex)
      {
        _logger.LogError(ex, "Erro do Stripe ao criar/recuperar cliente com email: {Email}", email);
        throw;
      }
      catch (Exception ex)
      {
        _logger.LogError(ex, "Erro inesperado ao criar/recuperar cliente com email: {Email}", email);
        throw;
      }
    }
  }
}

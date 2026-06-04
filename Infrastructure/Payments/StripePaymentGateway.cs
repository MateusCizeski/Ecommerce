using Application.Interfaces;
using Infrastructure.Abstractions;
using Infrastructure.Constants;
using Microsoft.Extensions.Logging;
using Stripe;

namespace Infrastructure.Payments
{
    /// <summary>
    /// Implementação de gateway de pagamentos utilizando Stripe.
    /// Adapta a API do Stripe para o contrato de IPaymentGateway.
    /// </summary>
    public class StripePaymentGateway : IPaymentGateway
    {
        private readonly IStripePaymentService _stripeService;
        private readonly ILogger<StripePaymentGateway> _logger;

        /// <summary>
        /// Inicializa uma nova instância de StripePaymentGateway.
        /// </summary>
        /// <param name="stripeService">Serviço abstrato de Stripe.</param>
        /// <param name="logger">Logger para registrar eventos.</param>
        public StripePaymentGateway(IStripePaymentService stripeService, ILogger<StripePaymentGateway> logger)
        {
            ArgumentNullException.ThrowIfNull(stripeService);
            ArgumentNullException.ThrowIfNull(logger);

            _stripeService = stripeService;
            _logger = logger;
        }

        /// <summary>
        /// Cria uma intenção de pagamento.
        /// </summary>
        /// <param name="amount">Valor em unidades monetárias (será convertido para centavos).</param>
        /// <param name="currency">Código da moeda.</param>
        /// <param name="customerId">ID do cliente no Stripe.</param>
        /// <param name="cancellationToken">Token de cancelamento.</param>
        /// <returns>Resultado com informações de pagamento.</returns>
        public async Task<CreatePaymentIntentResult> CreatePaymentIntentAsync(
            decimal amount,
            string currency,
            string customerId,
            CancellationToken cancellationToken = default)
        {
            ValidatePaymentAmount(amount);

            try
            {
                var amountInCents = ConvertToCents(amount);

                var intent = await _stripeService.CreatePaymentIntentAsync(
                    amountInCents,
                    currency,
                    customerId,
                    cancellationToken);

                return new CreatePaymentIntentResult(
                    intent.Id,
                    intent.ClientSecret,
                    intent.Status == InfrastructureConstants.Payments.RequiresActionStatus);
            }
            catch (StripeException ex)
            {
                _logger.LogError(ex, "Erro do Stripe ao criar intenção de pagamento para cliente: {CustomerId}", customerId);
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao criar intenção de pagamento para cliente: {CustomerId}", customerId);
                throw;
            }
        }

        /// <summary>
        /// Confirma uma intenção de pagamento.
        /// </summary>
        /// <param name="paymentIntentId">ID da intenção de pagamento.</param>
        /// <param name="cancellationToken">Token de cancelamento.</param>
        /// <returns>Resultado da confirmação de pagamento.</returns>
        public async Task<ConfirmPaymentResult> ConfirmPaymentAsync(
            string paymentIntentId,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(paymentIntentId))
                throw new ArgumentException("ID da intenção de pagamento não pode estar vazio.", nameof(paymentIntentId));

            try
            {
                var intent = await _stripeService.GetPaymentIntentAsync(paymentIntentId, cancellationToken);

                var succeeded = intent.Status == InfrastructureConstants.Payments.SucceededStatus;
                var chargeId = intent.LatestChargeId ?? string.Empty;

                return new ConfirmPaymentResult(succeeded, chargeId, intent.Status);
            }
            catch (StripeException ex)
            {
                _logger.LogError(ex, "Erro do Stripe ao confirmar pagamento: {PaymentIntentId}", paymentIntentId);
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao confirmar pagamento: {PaymentIntentId}", paymentIntentId);
                throw;
            }
        }

        /// <summary>
        /// Reembolsa um pagamento.
        /// </summary>
        /// <param name="chargeId">ID da cobrança a reembolsar.</param>
        /// <param name="amount">Valor a reembolsar em unidades monetárias (opcional para reembolso completo).</param>
        /// <param name="cancellationToken">Token de cancelamento.</param>
        /// <returns>Resultado do reembolso.</returns>
        public async Task<RefundResult> RefundAsync(
            string chargeId,
            decimal? amount = null,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(chargeId))
                throw new ArgumentException("ID da cobrança não pode estar vazio.", nameof(chargeId));

            if (amount.HasValue && amount <= 0)
                throw new ArgumentException("Valor de reembolso deve ser maior que zero.", nameof(amount));

            try
            {
                var amountInCents = amount.HasValue ? ConvertToCents(amount.Value) : (long?)null;

                var refund = await _stripeService.RefundAsync(chargeId, amountInCents, cancellationToken);

                var succeeded = refund.Status == InfrastructureConstants.Payments.SucceededStatus;

                return new RefundResult(succeeded, refund.Id);
            }
            catch (StripeException ex)
            {
                _logger.LogError(ex, "Erro do Stripe ao reembolsar cobrança: {ChargeId}", chargeId);
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao reembolsar cobrança: {ChargeId}", chargeId);
                throw;
            }
        }

        /// <summary>
        /// Cria ou obtém um cliente no Stripe.
        /// </summary>
        /// <param name="email">Email do cliente.</param>
        /// <param name="name">Nome do cliente.</param>
        /// <param name="cancellationToken">Token de cancelamento.</param>
        /// <returns>ID do cliente no Stripe.</returns>
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
                return await _stripeService.CreateOrGetCustomerAsync(email, name, cancellationToken);
            }
            catch (StripeException ex)
            {
                _logger.LogError(ex, "Erro do Stripe ao criar/obter cliente: {Email}", email);
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao criar/obter cliente: {Email}", email);
                throw;
            }
        }

        /// <summary>
        /// Valida o valor de um pagamento.
        /// </summary>
        /// <exception cref="ArgumentException">Lançada quando o valor é inválido.</exception>
        private static void ValidatePaymentAmount(decimal amount)
        {
            if (amount <= 0)
                throw new ArgumentException("Valor de pagamento deve ser maior que zero.", nameof(amount));
        }

        /// <summary>
        /// Converte um valor em unidades monetárias para centavos.
        /// </summary>
        private static long ConvertToCents(decimal amount)
        {
            return (long)(amount * InfrastructureConstants.Payments.CentsConversionFactor);
        }
    }
}

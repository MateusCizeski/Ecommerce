using Application.Common.Interfaces.Payments;

namespace Application.Features.Commerce.Orders;

public record RefundPaymentCommand(Guid OrderId, Guid PaymentId, decimal? Amount, string Reason, bool RestoreStock)
    : IRequest<RefundPaymentResult>;

public record RefundPaymentResult(Guid PaymentId, decimal RefundedAmount, bool IsFullRefund);

public class RefundPaymentCommandValidator : AbstractValidator<RefundPaymentCommand>
{
    public RefundPaymentCommandValidator()
    {
        RuleFor(x => x.OrderId).NotEmpty();
        RuleFor(x => x.PaymentId).NotEmpty();
        RuleFor(x => x.Reason).NotEmpty().MaximumLength(500).WithMessage("Motivo do estorno é obrigatório.");
        RuleFor(x => x.Amount).GreaterThan(0).When(x => x.Amount.HasValue).WithMessage("O valor parcial de estorno deve ser maior que zero.");
    }
}

public class RefundPaymentCommandHandler(
    IOrderRepository orderRepo,
    IProductVariantRepository variantRepo,
    IPaymentService paymentService,
    IUnitOfWork uow,
    ITenantContext tenant,
    ILogger<RefundPaymentCommandHandler> logger)
    : IRequestHandler<RefundPaymentCommand, RefundPaymentResult>
{
    public async Task<RefundPaymentResult> Handle(RefundPaymentCommand cmd, CancellationToken ct)
    {
        var order = await orderRepo.GetByIdAsync(cmd.OrderId, ct)
            ?? throw new NotFoundException("Pedido", cmd.OrderId);

        if (order.TenantId != tenant.TenantId)
            throw new ForbiddenException();

        var payment = order.Payments.FirstOrDefault(p => p.Id == cmd.PaymentId)
            ?? throw new NotFoundException("Transação de Pagamento", cmd.PaymentId);

        if (payment.Status != PaymentStatus.Succeeded && payment.Status != PaymentStatus.PartiallyRefunded)
            throw new ConflictException("Apenas transações concluídas ou parcialmente estornadas admitem reembolso.");

        var refundAmount = cmd.Amount ?? payment.RefundableAmount;

        if (refundAmount > payment.RefundableAmount)
            throw new ConflictException($"O valor solicitado (R$ {refundAmount}) excede o saldo estornável (R$ {payment.RefundableAmount}).");

        if (string.IsNullOrWhiteSpace(payment.ExternalChargeId))
            throw new ConflictException("Falha operacional: esta transação não possui registro de identificador externo para estorno.");

        var result = await paymentService.RefundAsync(payment.ExternalChargeId, refundAmount, ct);

        if (!result.Succeeded)
            throw new PaymentException($"Falha no estorno do gateway para a transação {payment.ExternalChargeId}.");

        payment.RegisterRefund(refundAmount);
        var isFullRefund = payment.Status == PaymentStatus.Refunded;

        if (cmd.RestoreStock)
        {
            foreach (var item in order.Items)
            {
                await variantRepo.RestoreStockAsync(item.ProductVariantId, item.Quantity, ct);
                logger.LogInformation("Estoque devolvido: +{Qty} unidades na variante {VariantId}.", item.Quantity, item.ProductVariantId);
            }
        }

        await uow.CommitAsync(ct);

        logger.LogInformation("Estorno executado com sucesso: R$ {Amount} devolvidos no pedido {OrderId}.", refundAmount, order.Id);
        return new RefundPaymentResult(payment.Id, refundAmount, isFullRefund);
    }
}
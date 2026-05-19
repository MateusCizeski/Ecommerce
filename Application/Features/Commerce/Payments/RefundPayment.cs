namespace Application;

public record RefundPaymentCommand(
    Guid OrderId,
    Guid PaymentId,
    decimal? Amount,     // null = full refund
    string Reason,
    bool RestoreStock
) : IRequest<RefundPaymentResult>;

public record RefundPaymentResult(
    Guid PaymentId,
    decimal RefundedAmount,
    bool IsFullRefund
);

public class RefundPaymentCommandValidator : AbstractValidator<RefundPaymentCommand>
{
    public RefundPaymentCommandValidator()
    {
        RuleFor(x => x.OrderId).NotEmpty();
        RuleFor(x => x.PaymentId).NotEmpty();
        RuleFor(x => x.Reason).NotEmpty().MaximumLength(500);
        RuleFor(x => x.Amount)
            .GreaterThan(0)
            .When(x => x.Amount.HasValue)
            .WithMessage("Refund amount must be positive when specified.");
    }
}

public class RefundPaymentCommandHandler(
    IOrderRepository orderRepo,
    IProductVariantRepository variantRepo,
    IPaymentGateway paymentGateway,
    IUnitOfWork uow,
    ITenantContext tenant,
    ILogger<RefundPaymentCommandHandler> logger)
    : IRequestHandler<RefundPaymentCommand, RefundPaymentResult>
{
    public async Task<RefundPaymentResult> Handle(RefundPaymentCommand cmd, CancellationToken ct)
    {
        var order = await orderRepo.GetByIdAsync(cmd.OrderId, ct)
            ?? throw new NotFoundException(nameof(Order), cmd.OrderId);

        if (order.TenantId != tenant.TenantId)
            throw new TenantAccessException();

        var payment = order.Payments.FirstOrDefault(p => p.Id == cmd.PaymentId)
            ?? throw new NotFoundException(nameof(Payment), cmd.PaymentId);

        if (payment.Status != PaymentStatus.Succeeded && payment.Status != PaymentStatus.PartiallyRefunded)
            throw new DomainException("Only succeeded or partially refunded payments can be refunded.");

        var refundAmount = cmd.Amount ?? payment.RefundableAmount;

        if (refundAmount > payment.RefundableAmount)
            throw new DomainException(
                $"Requested refund ({refundAmount}) exceeds refundable amount ({payment.RefundableAmount}).");

        // 1. Issue refund via Stripe
        if (payment.StripeChargeId is null)
            throw new DomainException("Cannot refund — no Stripe charge ID on this payment.");

        var result = await paymentGateway.RefundAsync(payment.StripeChargeId, refundAmount, ct);

        if (!result.Succeeded)
            throw new PaymentException($"Stripe refund failed for charge {payment.StripeChargeId}.");

        // 2. Register on domain entity
        payment.RegisterRefund(refundAmount);

        var isFullRefund = payment.Status == PaymentStatus.Refunded;

        // 3. Restore stock if requested (e.g. full refund + cancellation)
        if (cmd.RestoreStock)
        {
            foreach (var item in order.Items)
            {
                await variantRepo.RestoreStockAsync(item.ProductVariantId, item.Quantity, ct: ct);

                logger.LogInformation(
                    "Restored {Qty} units of variant {VariantId} after refund on order {OrderId}",
                    item.Quantity, item.ProductVariantId, order.Id);
            }
        }

        await uow.CommitAsync(ct);

        logger.LogInformation(
            "Refund of {Amount} {Currency} issued for order {OrderId}, payment {PaymentId}. Full: {IsFullRefund}",
            refundAmount, payment.Currency, order.Id, payment.Id, isFullRefund);

        return new RefundPaymentResult(payment.Id, refundAmount, isFullRefund);
    }
}
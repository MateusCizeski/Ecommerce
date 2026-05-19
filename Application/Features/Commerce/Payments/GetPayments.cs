namespace Application;

public record GetOrderPaymentsQuery(Guid OrderId) : IRequest<IEnumerable<PaymentDto>>;

public record PaymentDto(
    Guid Id,
    string Method,
    string Status,
    decimal Amount,
    decimal RefundedAmount,
    decimal RefundableAmount,
    string Currency,
    DateTime? PaidAt,
    DateTime? RefundedAt,
    string? StripePaymentIntentId,
    string? StripeChargeId
);

public class GetOrderPaymentsQueryHandler(
    IOrderRepository orderRepo,
    ITenantContext tenant)
    : IRequestHandler<GetOrderPaymentsQuery, IEnumerable<PaymentDto>>
{
    public async Task<IEnumerable<PaymentDto>> Handle(GetOrderPaymentsQuery q, CancellationToken ct)
    {
        var order = await orderRepo.GetByIdAsync(q.OrderId, ct)
            ?? throw new NotFoundException("Order", q.OrderId);

        if (order.TenantId != tenant.TenantId)
            throw new TenantAccessException();

        return order.Payments.Select(p => new PaymentDto(
            p.Id,
            p.Method.ToString(),
            p.Status.ToString(),
            p.Amount,
            p.RefundedAmount,
            p.RefundableAmount,
            p.Currency,
            p.PaidAt,
            p.RefundedAt,
            p.StripePaymentIntentId,
            p.StripeChargeId
        ));
    }
}

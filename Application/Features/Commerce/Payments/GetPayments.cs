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
    string? ExternalPaymentIntentId,
    string? ExternalChargeId
);

public class GetOrderPaymentsQueryHandler(IOrderRepository orderRepo, ITenantContext tenant)
    : IRequestHandler<GetOrderPaymentsQuery, IReadOnlyCollection<PaymentDto>>
{
    public async Task<IReadOnlyCollection<PaymentDto>> Handle(GetOrderPaymentsQuery q, CancellationToken ct)
    {
        var order = await orderRepo.GetByIdAsync(q.OrderId, ct)
            ?? throw new NotFoundException("Pedido", q.OrderId);

        if (order.TenantId != tenant.TenantId)
            throw new ForbiddenException();

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
            p.ExternalPaymentIntentId,
            p.ExternalChargeId
        )).ToList().AsReadOnly();
    }
}
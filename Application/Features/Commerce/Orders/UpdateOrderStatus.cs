using Application.Common.Interfaces.Payments;

namespace Application.Features.Commerce.Orders;

public record ConfirmOrderPaymentCommand(Guid OrderId, string PaymentIntentId) : IRequest;

public class ConfirmOrderPaymentCommandHandler(
    IOrderRepository orderRepo,
    IPaymentService paymentService,
    IUnitOfWork uow,
    ITenantContext tenant) : IRequestHandler<ConfirmOrderPaymentCommand>
{
    public async Task Handle(ConfirmOrderPaymentCommand cmd, CancellationToken ct)
    {
        var order = await orderRepo.GetByIdAsync(cmd.OrderId, ct)
            ?? throw new NotFoundException("Pedido", cmd.OrderId);

        if (order.TenantId != tenant.TenantId)
            throw new ForbiddenException();

        var result = await paymentService.ConfirmPaymentAsync(cmd.PaymentIntentId, ct);

        var payment = order.Payments.FirstOrDefault(p => p.ExternalPaymentIntentId == cmd.PaymentIntentId)
            ?? throw new ConflictException("Intenção de pagamento informada não pertence a este pedido.");

        if (result.Succeeded)
        {
            payment.MarkSucceeded(result.ChargeId, result.GatewayResponse);
            order.ConfirmPayment();
        }
        else
        {
            payment.MarkFailed(result.GatewayResponse);
            throw new PaymentException("A confirmação do pagamento falhou na operadora.", result.GatewayResponse);
        }

        await uow.CommitAsync(ct);
    }
}
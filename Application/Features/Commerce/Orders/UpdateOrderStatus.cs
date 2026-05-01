using MediatR;
using Ecommerce.Domain;
using Domain.Interfaces;
using Application.Interfaces;
using Application.Exceptions;

namespace Application.Features.Commerce.Orders;

public record ConfirmOrderPaymentCommand(Guid OrderId, string PaymentIntentId) : IRequest;

public class ConfirmOrderPaymentCommandHandler(IOrderRepository orderRepo, IPaymentGateway paymentGateway, IUnitOfWork uow, ITenantContext tenant) : IRequestHandler<ConfirmOrderPaymentCommand>
{
    public async Task Handle(ConfirmOrderPaymentCommand cmd, CancellationToken ct)
    {
        var order = await orderRepo.GetByIdAsync(cmd.OrderId, ct) ?? throw new NotFoundException(nameof(Order), cmd.OrderId);
        if (order.TenantId != tenant.TenantId) throw new TenantAccessException();
        var result = await paymentGateway.ConfirmPaymentAsync(cmd.PaymentIntentId, ct);
        var payment = order.Payments.FirstOrDefault(p => p.StripePaymentIntentId == cmd.PaymentIntentId)
            ?? throw new DomainException("Payment intent not found on this order.");
        if (result.Succeeded) { payment.MarkSucceeded(result.ChargeId, result.GatewayResponse); order.ConfirmPayment(); }
        else { payment.MarkFailed(result.GatewayResponse); throw new PaymentException("Payment confirmation failed.", result.GatewayResponse); }
        await uow.CommitAsync(ct);
    }
}

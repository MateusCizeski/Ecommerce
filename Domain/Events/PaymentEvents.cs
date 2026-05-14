using MediatR;

namespace Ecommerce.Domain;

public record PaymentSucceededEvent(
    Guid PaymentId,
    Guid OrderId,
    Guid TenantId,
    decimal Amount,
    string Currency
) : INotification;

public record PaymentFailedEvent(
    Guid PaymentId,
    Guid OrderId,
    Guid TenantId,
    string Reason
) : INotification;

public record PaymentRefundedEvent(
    Guid PaymentId,
    Guid OrderId,
    Guid TenantId,
    decimal RefundAmount,
    bool IsFullRefund
) : INotification;

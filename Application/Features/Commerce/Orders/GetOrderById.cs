using Application.Features.Commerce.Orders.DTOs;

namespace Application.Features.Commerce.Orders;

public record GetOrderByIdQuery(Guid Id) : IRequest<OrderDetailDto>;

public class GetOrderByIdQueryHandler(IOrderRepository orderRepo, ITenantContext tenant) : IRequestHandler<GetOrderByIdQuery, OrderDetailDto>
{
    public async Task<OrderDetailDto> Handle(GetOrderByIdQuery q, CancellationToken ct)
    {
        var o = await orderRepo.GetByIdAsync(q.Id, ct)
            ?? throw new NotFoundException("Pedido", q.Id);

        if (o.TenantId != tenant.TenantId)
            throw new ForbiddenException();

        var items = o.Items
            .Select(i => new OrderItemDto(i.ProductVariantId, i.ProductNameSnapshot, i.SKUSnapshot, i.Quantity, i.UnitPrice, i.TotalPrice))
            .ToList()
            .AsReadOnly();

        var payments = o.Payments
            .Select(p => new PaymentDto(p.Id, p.Method.ToString(), p.Status.ToString(), p.Amount, p.Currency, p.PaidAt))
            .ToList()
            .AsReadOnly();

        return new OrderDetailDto(
            o.Id,
            o.OrderNumber,
            o.Status.ToString(),
            o.Subtotal,
            o.DiscountAmount,
            o.ShippingAmount,
            o.TaxAmount,
            o.TotalAmount,
            o.PlacedAt,
            o.Notes,
            items,
            payments
        );
    }
}
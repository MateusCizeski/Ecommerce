using Application.Features.Commerce.Orders.DTOs;
using Domain.Interfaces;
using Ecommerce.Domain;
using MediatR;

namespace Application.Features.Commerce.Orders;

public class GetOrdersQueryHandler(IOrderRepository orderRepo, ITenantContext tenant) : IRequestHandler<GetOrdersQuery, PagedResult<OrderListItemDto>>
{
    public async Task<PagedResult<OrderListItemDto>> Handle(GetOrdersQuery q, CancellationToken ct)
    {
        var query = orderRepo.Query(tenant.TenantId);
        if (!string.IsNullOrWhiteSpace(q.Status) && Enum.TryParse<OrderStatus>(q.Status, true, out var status))
            query = query.Where(o => o.Status == status);
        var total = await query.CountAsync(ct);
        var items = await query.OrderByDescending(o => o.PlacedAt).Skip((q.Page - 1) * q.PageSize).Take(q.PageSize)
            .Select(o => new OrderListItemDto(o.Id, o.OrderNumber, o.Status.ToString(), o.TotalAmount, o.PlacedAt, o.Items.Count))
            .ToListAsync(ct);
        return new PagedResult<OrderListItemDto>(items, total, q.Page, q.PageSize);
    }
}

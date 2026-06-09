using Application.Common.Models;
using Application.Features.Commerce.Orders.DTOs;

namespace Application.Features.Commerce.Orders;

public class GetOrdersQueryHandler(IOrderRepository orderRepo, ITenantContext tenant)
    : IRequestHandler<GetOrdersQuery, PagedResult<OrderListItemDto>>
{
    public async Task<PagedResult<OrderListItemDto>> Handle(GetOrdersQuery q, CancellationToken ct)
    {
        var query = orderRepo.Query(tenant.TenantId);

        if (!string.IsNullOrWhiteSpace(q.Status) && Enum.TryParse<OrderStatus>(q.Status, true, out var status))
        {
            query = query.Where(o => o.Status == status);
        }

        var projectedQuery = query
            .OrderByDescending(o => o.PlacedAt)
            .Select(o => new OrderListItemDto(
                o.Id,
                o.OrderNumber,
                o.Status.ToString(),
                o.TotalAmount,
                o.PlacedAt,
                o.Items.Count
            ));

        return await projectedQuery.ToPagedResultAsync(q.Page, q.PageSize, ct);
    }
}
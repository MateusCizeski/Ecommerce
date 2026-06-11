using Application.Common.Models;


namespace Application.Features.Customers.Queries;

public record GetCustomersQuery(
    int Page = 1,
    int PageSize = 20,
    string? Search = null,
    bool? IsActive = null) : IRequest<PagedResult<CustomerListItemDto>>;

public class GetCustomersQueryHandler(ICustomerRepository customerRepo, ITenantContext tenant)
    : IRequestHandler<GetCustomersQuery, PagedResult<CustomerListItemDto>>
{
    public async Task<PagedResult<CustomerListItemDto>> Handle(GetCustomersQuery q, CancellationToken ct)
    {
        var query = customerRepo.Query(tenant.TenantId);

        if (!string.IsNullOrWhiteSpace(q.Search))
        {
            var searchLower = q.Search.ToLower();
            query = query.Where(c => c.Email.ToLower().Contains(searchLower) ||
                                     c.FirstName.ToLower().Contains(searchLower) ||
                                     c.LastName.ToLower().Contains(searchLower));
        }

        if (q.IsActive.HasValue)
            query = query.Where(c => c.IsActive == q.IsActive);

        var projectedQuery = query
            .OrderByDescending(c => c.CreatedAt)
            .Select(c => new CustomerListItemDto(
                c.Id,
                c.Email,
                c.FirstName + " " + c.LastName,
                c.Phone,
                c.IsActive,
                c.Addresses.Count,
                c.CreatedAt
            ));

        return await projectedQuery.ToPagedResultAsync(q.Page, q.PageSize, ct);
    }
}

public record GetCustomerByIdQuery(Guid Id) : IRequest<CustomerDetailDto>;

public class GetCustomerByIdQueryHandler(ICustomerRepository customerRepo, ITenantContext tenant)
    : IRequestHandler<GetCustomerByIdQuery, CustomerDetailDto>
{
    public async Task<CustomerDetailDto> Handle(GetCustomerByIdQuery q, CancellationToken ct)
    {
        var c = await customerRepo.GetByIdAsync(q.Id, ct)
            ?? throw new NotFoundException("Cliente", q.Id);

        if (c.TenantId != tenant.TenantId)
            throw new ForbiddenException();

        var addresses = c.Addresses
            .Select(a => new AddressDto(
                a.Id,
                a.Label,
                a.Street,
                a.Number,
                a.Complement,
                a.City,
                a.State,
                a.ZipCode,
                a.Country,
                a.IsDefault
            ))
            .ToList()
            .AsReadOnly();

        return new CustomerDetailDto(
            c.Id,
            c.Email,
            c.FirstName,
            c.LastName,
            c.Phone,
            c.IsActive,
            c.CreatedAt,
            addresses
        );
    }
}
using Application.Exceptions;
using Domain.Interfaces;
using Ecommerce.Domain;
using FluentValidation;
using MediatR;

namespace Application;

public record CreateCustomerCommand(string Email, string FirstName, string LastName, string? Phone) : IRequest<Guid>;

public class CreateCustomerCommandValidator : AbstractValidator<CreateCustomerCommand>
{
    public CreateCustomerCommandValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(320);
        RuleFor(x => x.FirstName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.LastName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Phone).MaximumLength(30).When(x => x.Phone is not null);
    }
}

public class CreateCustomerCommandHandler(ICustomerRepository customerRepo, IUnitOfWork uow, ITenantContext tenant) : IRequestHandler<CreateCustomerCommand, Guid>
{
    public async Task<Guid> Handle(CreateCustomerCommand cmd, CancellationToken ct)
    {
        if (await customerRepo.EmailExistsAsync(tenant.TenantId, cmd.Email.ToLowerInvariant(), ct))
            throw new ConflictException($"A customer with email '{cmd.Email}' already exists.");
        var customer = Customer.Create(tenant.TenantId, cmd.Email, cmd.FirstName, cmd.LastName, cmd.Phone);
        await customerRepo.AddAsync(customer, ct);
        await uow.CommitAsync(ct);
        return customer.Id;
    }
}

public record UpdateCustomerCommand(Guid Id, string FirstName, string LastName, string? Phone) : IRequest;

public class UpdateCustomerCommandHandler(ICustomerRepository customerRepo, IUnitOfWork uow, ITenantContext tenant) : IRequestHandler<UpdateCustomerCommand>
{
    public async Task Handle(UpdateCustomerCommand cmd, CancellationToken ct)
    {
        var customer = await customerRepo.GetByIdAsync(cmd.Id, ct) ?? throw new NotFoundException(nameof(Customer), cmd.Id);
        if (customer.TenantId != tenant.TenantId) throw new TenantAccessException();
        customer.Update(cmd.FirstName, cmd.LastName, cmd.Phone);
        await uow.CommitAsync(ct);
    }
}

public record AddCustomerAddressCommand(Guid CustomerId, string Label, string Street, string Number, string? Complement, string City, string State, string ZipCode, string Country) : IRequest<Guid>;

public class AddCustomerAddressCommandValidator : AbstractValidator<AddCustomerAddressCommand>
{
    public AddCustomerAddressCommandValidator()
    {
        RuleFor(x => x.CustomerId).NotEmpty();
        RuleFor(x => x.Label).NotEmpty().MaximumLength(50);
        RuleFor(x => x.Street).NotEmpty().MaximumLength(300);
        RuleFor(x => x.Number).NotEmpty().MaximumLength(20);
        RuleFor(x => x.City).NotEmpty().MaximumLength(100);
        RuleFor(x => x.State).NotEmpty().MaximumLength(100);
        RuleFor(x => x.ZipCode).NotEmpty().MaximumLength(20);
        RuleFor(x => x.Country).NotEmpty().Length(2, 3);
    }
}

public class AddCustomerAddressCommandHandler(ICustomerRepository customerRepo, IUnitOfWork uow, ITenantContext tenant) : IRequestHandler<AddCustomerAddressCommand, Guid>
{
    public async Task<Guid> Handle(AddCustomerAddressCommand cmd, CancellationToken ct)
    {
        var customer = await customerRepo.GetByIdAsync(cmd.CustomerId, ct) ?? throw new NotFoundException(nameof(Customer), cmd.CustomerId);
        if (customer.TenantId != tenant.TenantId) throw new TenantAccessException();
        var address = customer.AddAddress(cmd.Label, cmd.Street, cmd.Number, cmd.City, cmd.State, cmd.ZipCode, cmd.Country, cmd.Complement);
        await uow.CommitAsync(ct);
        return address.Id;
    }
}

public record CustomerListItemDto(Guid Id, string Email, string FullName, string? Phone, bool IsActive, int AddressCount, DateTime CreatedAt);

public record CustomerDetailDto(Guid Id, string Email, string FirstName, string LastName, string? Phone, bool IsActive, DateTime CreatedAt, IEnumerable<AddressDto> Addresses);

public record AddressDto(Guid Id, string Label, string Street, string Number, string? Complement, string City, string State, string ZipCode, string Country, bool IsDefault);

public record GetCustomersQuery(int Page = 1, int PageSize = 20, string? Search = null, bool? IsActive = null) : IRequest<PagedResult<CustomerListItemDto>>;

public class GetCustomersQueryHandler(ICustomerRepository customerRepo, ITenantContext tenant) : IRequestHandler<GetCustomersQuery, PagedResult<CustomerListItemDto>>
{
    public async Task<PagedResult<CustomerListItemDto>> Handle(GetCustomersQuery q, CancellationToken ct)
    {
        var query = customerRepo.Query(tenant.TenantId);
        if (!string.IsNullOrWhiteSpace(q.Search))
            query = query.Where(c => c.Email.Contains(q.Search) || c.FirstName.Contains(q.Search) || c.LastName.Contains(q.Search));
        if (q.IsActive.HasValue) query = query.Where(c => c.IsActive == q.IsActive);
        var total = await query.CountAsync(ct);
        var items = await query.OrderByDescending(c => c.CreatedAt).Skip((q.Page - 1) * q.PageSize).Take(q.PageSize)
            .Select(c => new CustomerListItemDto(c.Id, c.Email, c.FirstName + " " + c.LastName, c.Phone, c.IsActive, c.Addresses.Count, c.CreatedAt))
            .ToListAsync(ct);
        return new PagedResult<CustomerListItemDto>(items, total, q.Page, q.PageSize);
    }
}

public record GetCustomerByIdQuery(Guid Id) : IRequest<CustomerDetailDto>;

public class GetCustomerByIdQueryHandler(ICustomerRepository customerRepo, ITenantContext tenant) : IRequestHandler<GetCustomerByIdQuery, CustomerDetailDto>
{
    public async Task<CustomerDetailDto> Handle(GetCustomerByIdQuery q, CancellationToken ct)
    {
        var c = await customerRepo.GetByIdAsync(q.Id, ct) ?? throw new NotFoundException(nameof(Customer), q.Id);
        if (c.TenantId != tenant.TenantId) throw new TenantAccessException();
        return new CustomerDetailDto(c.Id, c.Email, c.FirstName, c.LastName, c.Phone, c.IsActive, c.CreatedAt,
            c.Addresses.Select(a => new AddressDto(a.Id, a.Label, a.Street, a.Number, a.Complement, a.City, a.State, a.ZipCode, a.Country, a.IsDefault)));
    }
}
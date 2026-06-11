namespace Application.Features.Customers.Commands;

public record CreateCustomerCommand(string Email, string FirstName, string LastName, string? Phone) : IRequest<Guid>;

public class CreateCustomerCommandValidator : AbstractValidator<CreateCustomerCommand>
{
    public CreateCustomerCommandValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(320).WithMessage("E-mail em formato inválido.");
        RuleFor(x => x.FirstName).NotEmpty().MaximumLength(100).WithMessage("O primeiro nome é obrigatório.");
        RuleFor(x => x.LastName).NotEmpty().MaximumLength(100).WithMessage("O sobrenome é obrigatório.");
        RuleFor(x => x.Phone).MaximumLength(30).When(x => x.Phone is not null);
    }
}

public class CreateCustomerCommandHandler(ICustomerRepository customerRepo, IUnitOfWork uow, ITenantContext tenant)
    : IRequestHandler<CreateCustomerCommand, Guid>
{
    public async Task<Guid> Handle(CreateCustomerCommand cmd, CancellationToken ct)
    {
        var standardizedEmail = cmd.Email.Trim().ToLowerInvariant();

        if (await customerRepo.EmailExistsAsync(tenant.TenantId, standardizedEmail, ct))
            throw new ConflictException($"Já existe um cliente cadastrado com o e-mail '{cmd.Email}'.");

        var customer = Customer.Create(tenant.TenantId, standardizedEmail, cmd.FirstName, cmd.LastName, cmd.Phone);

        await customerRepo.AddAsync(customer, ct);
        await uow.CommitAsync(ct);

        return customer.Id;
    }
}

public record UpdateCustomerCommand(Guid Id, string FirstName, string LastName, string? Phone) : IRequest;

public class UpdateCustomerCommandHandler(ICustomerRepository customerRepo, IUnitOfWork uow, ITenantContext tenant)
    : IRequestHandler<UpdateCustomerCommand>
{
    public async Task Handle(UpdateCustomerCommand cmd, CancellationToken ct)
    {
        var customer = await customerRepo.GetByIdAsync(cmd.Id, ct)
            ?? throw new NotFoundException("Cliente", cmd.Id);

        if (customer.TenantId != tenant.TenantId)
            throw new ForbiddenException();

        customer.Update(cmd.FirstName, cmd.LastName, cmd.Phone);
        await uow.CommitAsync(ct);
    }
}

public record AddCustomerAddressCommand(
    Guid CustomerId,
    string Label,
    string Street,
    string Number,
    string? Complement,
    string City,
    string State,
    string ZipCode,
    string Country) : IRequest<Guid>;

public class AddCustomerAddressCommandValidator : AbstractValidator<AddCustomerAddressCommand>
{
    public AddCustomerAddressCommandValidator()
    {
        RuleFor(x => x.CustomerId).NotEmpty();
        RuleFor(x => x.Label).NotEmpty().MaximumLength(50).WithMessage("Identificação do endereço (Ex: Casa, Trabalho) é obrigatória.");
        RuleFor(x => x.Street).NotEmpty().MaximumLength(300);
        RuleFor(x => x.Number).NotEmpty().MaximumLength(20);
        RuleFor(x => x.City).NotEmpty().MaximumLength(100);
        RuleFor(x => x.State).NotEmpty().MaximumLength(100);
        RuleFor(x => x.ZipCode).NotEmpty().MaximumLength(20);
        RuleFor(x => x.Country).NotEmpty().Length(2, 3).WithMessage("O código do país deve possuir entre 2 e 3 caracteres (ISO).");
    }
}

public class AddCustomerAddressCommandHandler(ICustomerRepository customerRepo, IUnitOfWork uow, ITenantContext tenant)
    : IRequestHandler<AddCustomerAddressCommand, Guid>
{
    public async Task<Guid> Handle(AddCustomerAddressCommand cmd, CancellationToken ct)
    {
        var customer = await customerRepo.GetByIdAsync(cmd.CustomerId, ct)
            ?? throw new NotFoundException("Cliente", cmd.CustomerId);

        if (customer.TenantId != tenant.TenantId)
            throw new ForbiddenException();

        var address = customer.AddAddress(cmd.Label, cmd.Street, cmd.Number, cmd.City, cmd.State, cmd.ZipCode, cmd.Country, cmd.Complement);
        await uow.CommitAsync(ct);

        return address.Id;
    }
}
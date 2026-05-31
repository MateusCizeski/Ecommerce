using System.Text.RegularExpressions;
using Application;
using Application.Exceptions;
using Application.Features.Tenancy.Tenants;
using Domain.Interfaces;
using Ecommerce.Domain;
using FluentAssertions;
using Infrastructure.Orders;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Test;

[TestClass]
public sealed class DomainTests
{
    [TestMethod]
    public void Tenant_Create_ShouldNormalizeValues()
    {
        var tenant = Tenant.Create(" Acme ", "My-Subdomain", "TEST@EXAMPLE.COM ");

        tenant.Name.Should().Be("Acme");
        tenant.Subdomain.Should().Be("my-subdomain");
        tenant.Email.Should().Be("test@example.com");
        tenant.IsActive.Should().BeTrue();
    }

    [TestMethod]
    public void Tenant_Create_WithEmptyName_ShouldThrowDomainException()
    {
        Action action = () => Tenant.Create("", "subdomain", "test@example.com");
        action.Should().Throw<DomainException>().WithMessage("Tenant name is required.");
    }

    [TestMethod]
    public void Customer_CreateAndAddAddress_ShouldReturnDefaultAddress()
    {
        var tenantId = Guid.NewGuid();
        var customer = Customer.Create(tenantId, "user@example.com", "John", "Doe", " +55 11 99999-9999 ");

        customer.Email.Should().Be("user@example.com");
        customer.FullName.Should().Be("John Doe");
        customer.Phone.Should().Be("+55 11 99999-9999");
        customer.Addresses.Should().BeEmpty();

        var address = customer.AddAddress("Home", "Street", "123", "City", "State", "00000-000", "BR", "Apt 1");

        address.CustomerId.Should().Be(customer.Id);
        address.IsDefault.Should().BeTrue();
        customer.Addresses.Should().ContainSingle().Which.Id.Should().Be(address.Id);
    }

    [TestMethod]
    public void Subscription_Create_WithValidDates_ShouldBeActiveAndRaiseEvent()
    {
        var plan = Plan.Create("Basic", "Basic plan", 10m, BillingCycle.Monthly);
        var startDate = DateTime.UtcNow;
        var endDate = startDate.AddDays(30);

        var subscription = Subscription.Create(Guid.NewGuid(), plan, startDate, endDate);

        subscription.Status.Should().Be(SubscriptionStatus.Active);
        subscription.IsActive().Should().BeTrue();
        subscription.DomainEvents.Should().ContainSingle().Which.Should().BeOfType<SubscriptionCreatedEvent>();
    }

    [TestMethod]
    public void Subscription_Create_WithInvalidDates_ShouldThrowDomainException()
    {
        var plan = Plan.Create("Basic", "Basic plan", 10m, BillingCycle.Monthly);
        var startDate = DateTime.UtcNow;
        var endDate = startDate;

        Action action = () => Subscription.Create(Guid.NewGuid(), plan, startDate, endDate);
        action.Should().Throw<DomainException>().WithMessage("End date must be after start date.");
    }

    [TestMethod]
    public void Subscription_Cancel_ShouldSetStatusCancelledAndRaiseEvent()
    {
        var plan = Plan.Create("Basic", "Basic plan", 10m, BillingCycle.Monthly);
        var subscription = Subscription.Create(Guid.NewGuid(), plan, DateTime.UtcNow.AddDays(-1), DateTime.UtcNow.AddDays(30));

        subscription.Cancel();

        subscription.Status.Should().Be(SubscriptionStatus.Cancelled);
        subscription.CancelledAt.Should().NotBeNull();
        subscription.DomainEvents.Should().ContainSingle().Which.Should().BeOfType<SubscriptionCancelledEvent>();
    }

    [TestMethod]
    public void Subscription_MarkPastDue_WhenCancelled_ShouldThrowDomainException()
    {
        var plan = Plan.Create("Basic", "Basic plan", 10m, BillingCycle.Monthly);
        var subscription = Subscription.Create(Guid.NewGuid(), plan, DateTime.UtcNow.AddDays(-1), DateTime.UtcNow.AddDays(30));
        subscription.Cancel();

        Action action = () => subscription.MarkPastDue();

        action.Should().Throw<DomainException>().WithMessage("Cannot mark a cancelled subscription as past due.");
    }

    [TestMethod]
    public void Subscription_SetStripeId_WithEmptyValue_ShouldThrowDomainException()
    {
        var plan = Plan.Create("Basic", "Basic plan", 10m, BillingCycle.Monthly);
        var subscription = Subscription.Create(Guid.NewGuid(), plan, DateTime.UtcNow.AddDays(-1), DateTime.UtcNow.AddDays(30));

        Action action = () => subscription.SetStripeId(string.Empty);

        action.Should().Throw<DomainException>().WithMessage("Stripe subscription ID cannot be empty.");
    }

    [TestMethod]
    public void OrderNumberGenerator_GenerateAsync_ShouldReturnExpectedPattern()
    {
        var generator = new OrderNumberGenerator();
        var tenantId = Guid.Parse("11111111-2222-3333-4444-555555555555");

        var orderNumber = generator.GenerateAsync(tenantId).GetAwaiter().GetResult();

        orderNumber.Should().StartWith("ORD-1111-");
        Regex.IsMatch(orderNumber, "^ORD-[A-Z0-9]{4}-[0-9]+-[0-9]{4}$").Should().BeTrue();
    }
}

[TestClass]
public sealed class ApplicationTests
{
    [TestMethod]
    public void CreateTenantCommandValidator_InvalidSubdomain_ShouldFailValidation()
    {
        var validator = new CreateTenantCommandValidator();
        var command = new CreateTenantCommand("Tenant", "Invalid Subdomain!", "test@example.com");

        var result = validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(x => x.PropertyName == "Subdomain");
    }

    [TestMethod]
    public async Task CreateTenantCommandHandler_WhenSubdomainExists_ShouldThrowConflictException()
    {
        var tenantRepo = new Mock<ITenantRepository>();
        tenantRepo.Setup(x => x.SubdomainExistsAsync("existing", default)).ReturnsAsync(true);
        var uow = new Mock<IUnitOfWork>();

        var handler = new CreateTenantCommandHandler(tenantRepo.Object, uow.Object);

        Func<Task> action = () => handler.Handle(new CreateTenantCommand("Name", "existing", "test@example.com"), default);

        await action.Should().ThrowAsync<ConflictException>().WithMessage("Subdomain 'existing' is already taken.");
    }

    [TestMethod]
    public async Task CreateTenantCommandHandler_ShouldAddTenantAndCommit()
    {
        var tenantRepo = new Mock<ITenantRepository>();
        tenantRepo.Setup(x => x.SubdomainExistsAsync("newtenant", default)).ReturnsAsync(false);
        var uow = new Mock<IUnitOfWork>();
        uow.Setup(x => x.CommitAsync(default)).ReturnsAsync(1);

        var handler = new CreateTenantCommandHandler(tenantRepo.Object, uow.Object);
        var command = new CreateTenantCommand("Tenant", "newtenant", "test@example.com");

        var result = await handler.Handle(command, default);

        result.Should().NotBeEmpty();
        tenantRepo.Verify(x => x.AddAsync(It.Is<Tenant>(t => t.Subdomain == "newtenant" && t.Email == "test@example.com"), default), Times.Once);
        uow.Verify(x => x.CommitAsync(default), Times.Once);
    }

    [TestMethod]
    public async Task CreateCustomerCommandHandler_WhenEmailExists_ShouldThrowConflictException()
    {
        var customerRepo = new Mock<ICustomerRepository>();
        customerRepo.Setup(x => x.EmailExistsAsync(It.IsAny<Guid>(), "user@example.com", default)).ReturnsAsync(true);
        var tenantContext = new TestTenantContext(Guid.NewGuid(), "tenant");
        var uow = new Mock<IUnitOfWork>();

        var handler = new CreateCustomerCommandHandler(customerRepo.Object, uow.Object, tenantContext);

        Func<Task> action = () => handler.Handle(new CreateCustomerCommand("user@example.com", "John", "Doe", null), default);

        await action.Should().ThrowAsync<ConflictException>().WithMessage("A customer with email 'user@example.com' already exists.");
    }

    [TestMethod]
    public async Task CreateCustomerCommandHandler_ShouldAddCustomerAndCommit()
    {
        var customerRepo = new Mock<ICustomerRepository>();
        customerRepo.Setup(x => x.EmailExistsAsync(It.IsAny<Guid>(), "user@example.com", default)).ReturnsAsync(false);
        var tenantId = Guid.NewGuid();
        var tenantContext = new TestTenantContext(tenantId, "tenant");
        var uow = new Mock<IUnitOfWork>();
        uow.Setup(x => x.CommitAsync(default)).ReturnsAsync(1);

        var handler = new CreateCustomerCommandHandler(customerRepo.Object, uow.Object, tenantContext);
        var command = new CreateCustomerCommand("user@example.com", "John", "Doe", "123");

        var result = await handler.Handle(command, default);

        result.Should().NotBeEmpty();
        customerRepo.Verify(x => x.AddAsync(It.Is<Customer>(c => c.Email == "user@example.com" && c.TenantId == tenantId), default), Times.Once);
        uow.Verify(x => x.CommitAsync(default), Times.Once);
    }

    [TestMethod]
    public async Task UpdateCustomerCommandHandler_WhenTenantMismatch_ShouldThrowTenantAccessException()
    {
        var customer = Customer.Create(Guid.NewGuid(), "user@example.com", "John", "Doe", null);
        var customerRepo = new Mock<ICustomerRepository>();
        customerRepo.Setup(x => x.GetByIdAsync(customer.Id, default)).ReturnsAsync(customer);
        var tenantContext = new TestTenantContext(Guid.NewGuid(), "tenant");
        var uow = new Mock<IUnitOfWork>();

        var handler = new UpdateCustomerCommandHandler(customerRepo.Object, uow.Object, tenantContext);

        Func<Task> action = () => handler.Handle(new UpdateCustomerCommand(customer.Id, "Jane", "Doe", null), default);

        await action.Should().ThrowAsync<TenantAccessException>();
    }

    [TestMethod]
    public async Task AddCustomerAddressCommandHandler_ShouldAddAddressAndCommit()
    {
        var tenantId = Guid.NewGuid();
        var customer = Customer.Create(tenantId, "user@example.com", "John", "Doe", null);
        var customerRepo = new Mock<ICustomerRepository>();
        customerRepo.Setup(x => x.GetByIdAsync(customer.Id, default)).ReturnsAsync(customer);
        var tenantContext = new TestTenantContext(tenantId, "tenant");
        var uow = new Mock<IUnitOfWork>();
        uow.Setup(x => x.CommitAsync(default)).ReturnsAsync(1);

        var handler = new AddCustomerAddressCommandHandler(customerRepo.Object, uow.Object, tenantContext);
        var command = new AddCustomerAddressCommand(customer.Id, "Home", "Street", "123", "Apt 1", "City", "State", "00000-000", "BR");

        var result = await handler.Handle(command, default);

        result.Should().NotBeEmpty();
        customer.Addresses.Should().ContainSingle(a => a.Id == result && a.IsDefault);
        uow.Verify(x => x.CommitAsync(default), Times.Once);
    }

    private sealed class TestTenantContext : ITenantContext
    {
        public TestTenantContext(Guid tenantId, string subdomain)
        {
            TenantId = tenantId;
            Subdomain = subdomain;
        }

        public Guid TenantId { get; }
        public string Subdomain { get; }
    }
}

using Application;
using Application.Exceptions;
using Ecommerce.Domain.Interfaces;
using Ecommerce.Domain;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Ecommerce.Test.Application;

[TestClass]
public sealed class CustomerCommandHandlerTests
{
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


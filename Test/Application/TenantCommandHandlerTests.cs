using Application.Exceptions;
using Application.Features.Tenancy.Tenants;
using Domain.Interfaces;
using Ecommerce.Domain;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Ecommerce.Test.Application;

[TestClass]
public sealed class TenantCommandHandlerTests
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
}

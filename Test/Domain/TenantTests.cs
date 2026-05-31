using Ecommerce.Domain;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Ecommerce.Test.Domain;

[TestClass]
public sealed class TenantTests
{
  [TestMethod]
  public void Create_ShouldNormalizeValues()
  {
    var tenant = Tenant.Create(" Acme ", "My-Subdomain", "TEST@EXAMPLE.COM ");

    tenant.Name.Should().Be("Acme");
    tenant.Subdomain.Should().Be("my-subdomain");
    tenant.Email.Should().Be("test@example.com");
    tenant.IsActive.Should().BeTrue();
  }

  [TestMethod]
  public void Create_WithEmptyName_ShouldThrowDomainException()
  {
    Action action = () => Tenant.Create("", "subdomain", "test@example.com");

    action.Should().Throw<DomainException>().WithMessage("Tenant name is required.");
  }
}

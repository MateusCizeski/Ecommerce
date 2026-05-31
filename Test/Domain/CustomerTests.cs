using Ecommerce.Domain;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Ecommerce.Test.Domain;

[TestClass]
public sealed class CustomerTests
{
  [TestMethod]
  public void CreateAndAddAddress_ShouldReturnDefaultAddress()
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
}

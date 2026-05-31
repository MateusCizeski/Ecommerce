using FluentAssertions;
using Infrastructure.Orders;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Ecommerce.Test.Infrastructure;

[TestClass]
public sealed class OrderNumberGeneratorTests
{
  [TestMethod]
  public async Task GenerateAsync_ShouldReturnExpectedPattern()
  {
    var generator = new OrderNumberGenerator();
    var tenantId = Guid.Parse("11111111-2222-3333-4444-555555555555");

    var orderNumber = await generator.GenerateAsync(tenantId);

    orderNumber.Should().StartWith("ORD-1111-");
    orderNumber.Should().MatchRegex("^ORD-[A-Z0-9]{4}-[0-9]+-[0-9]{4}$");
  }
}

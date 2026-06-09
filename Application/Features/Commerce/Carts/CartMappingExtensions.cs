using Application.Features.Commerce.Cart.DTOs;

namespace Application.Features.Commerce.Cart;

public static class CartMappingExtensions
{
  public static CartDto ToDto(this Ecommerce.Domain.Cart cart)
  {
    var itemsDto = cart.Items
        .Select(i => new CartItemDto(
            i.Id,
            i.ProductVariantId,
            i.Quantity,
            i.UnitPrice,
            i.LineTotal
        ))
        .ToList()
        .AsReadOnly();

    return new CartDto(
        cart.Id,
        cart.CustomerId,
        cart.Total,
        cart.ItemCount,
        itemsDto,
        cart.Status.ToString(),
        cart.ExpiresAt
    );
  }
}
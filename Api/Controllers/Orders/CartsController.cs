using MediatR;
using Microsoft.AspNetCore.Mvc;
using Application.Features.Commerce.Cart;
using Application.Features.Commerce.Cart.DTOs;

namespace Api.Controllers.Orders;

[ApiController]
[Route("api/v1/[controller]")]
public class CartsController(ISender sender) : ControllerBase
{
    [HttpPost("get-or-create")]
    public async Task<IActionResult> GetOrCreate([FromBody] GetOrCreateCartCommand cmd, CancellationToken ct)
        => Ok(await sender.Send(cmd, ct));

    [HttpPost("{id:guid}/items")]
    public async Task<IActionResult> AddItem(Guid id, [FromBody] AddCartItemCommand cmd, CancellationToken ct)
        => Ok(await sender.Send(cmd with { CartId = id }, ct));

    [HttpPut("{id:guid}/items/{variantId:guid}")]
    public async Task<IActionResult> UpdateItem(Guid id, Guid variantId, [FromBody] int quantity, CancellationToken ct)
    { await sender.Send(new UpdateCartItemCommand(id, variantId, quantity), ct); return NoContent(); }

    [HttpDelete("{id:guid}/items/{variantId:guid}")]
    public async Task<IActionResult> RemoveItem(Guid id, Guid variantId, CancellationToken ct)
    { await sender.Send(new RemoveCartItemCommand(id, variantId), ct); return NoContent(); }
}

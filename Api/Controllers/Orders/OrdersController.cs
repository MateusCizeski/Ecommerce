using MediatR;
using Microsoft.AspNetCore.Mvc;
using Application.Features.Commerce.Orders;
using Application.Features.Commerce.Orders.DTOs;

namespace Api.Controllers.Orders;

[ApiController]
[Route("api/v1/[controller]")]
public class OrdersController(ISender sender) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] GetOrdersQuery q, CancellationToken ct)
        => Ok(await sender.Send(q, ct));

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
        => Ok(await sender.Send(new GetOrderByIdQuery(id), ct));

    [HttpPost]
    public async Task<IActionResult> Place([FromBody] PlaceOrderCommand cmd, CancellationToken ct)
    { var result = await sender.Send(cmd, ct); return CreatedAtAction(nameof(GetById), new { id = result.OrderId }, result); }

    [HttpPost("{id:guid}/confirm-payment")]
    public async Task<IActionResult> ConfirmPayment(Guid id, [FromBody] string paymentIntentId, CancellationToken ct)
    { await sender.Send(new ConfirmOrderPaymentCommand(id, paymentIntentId), ct); return NoContent(); }

    [HttpPost("{id:guid}/cancel")]
    public async Task<IActionResult> Cancel(Guid id, [FromBody] string reason, CancellationToken ct)
    { await sender.Send(new CancelOrderCommand(id, reason), ct); return NoContent(); }
}

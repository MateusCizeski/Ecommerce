using MediatR;
using Application;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers.Customers;

[ApiController]
[Route("api/v1/[controller]")]
public class CustomersController(ISender sender) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] GetCustomersQuery q, CancellationToken ct)
        => Ok(await sender.Send(q, ct));

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
        => Ok(await sender.Send(new GetCustomerByIdQuery(id), ct));

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateCustomerCommand cmd, CancellationToken ct)
    { var id = await sender.Send(cmd, ct); return CreatedAtAction(nameof(GetById), new { id }, id); }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateCustomerCommand cmd, CancellationToken ct)
    { await sender.Send(cmd with { Id = id }, ct); return NoContent(); }

    [HttpPost("{id:guid}/addresses")]
    public async Task<IActionResult> AddAddress(Guid id, [FromBody] AddCustomerAddressCommand cmd, CancellationToken ct)
        => Ok(new { addressId = await sender.Send(cmd with { CustomerId = id }, ct) });
}

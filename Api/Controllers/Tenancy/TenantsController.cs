using MediatR;
using Microsoft.AspNetCore.Mvc;
using Application.Features.Tenancy.Tenants;

namespace Api.Controllers.Tenancy;

[ApiController]
[Route("api/v1/[controller]")]
public class TenantsController(ISender sender) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateTenantCommand cmd, CancellationToken ct)
        => Ok(new { id = await sender.Send(cmd, ct) });

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateTenantCommand cmd, CancellationToken ct)
    { await sender.Send(cmd with { Id = id }, ct); return NoContent(); }
}

using MediatR;
using Microsoft.AspNetCore.Mvc;
using Application.Features.Catalog.Categories;

namespace Api.Controllers.Catalog;

[ApiController]
[Route("api/v1/[controller]")]
public class CategoriesController(ISender sender) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] GetCategoriesQuery q, CancellationToken ct)
        => Ok(await sender.Send(q, ct));

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
        => Ok(await sender.Send(new GetCategoryByIdQuery(id), ct));

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateCategoryCommand cmd, CancellationToken ct)
    { var id = await sender.Send(cmd, ct); return CreatedAtAction(nameof(GetById), new { id }, id); }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateCategoryCommand cmd, CancellationToken ct)
    { await sender.Send(cmd with { Id = id }, ct); return NoContent(); }

    [HttpPost("{id:guid}/deactivate")]
    public async Task<IActionResult> Deactivate(Guid id, CancellationToken ct)
    { await sender.Send(new DeactivateCategoryCommand(id), ct); return NoContent(); }
}

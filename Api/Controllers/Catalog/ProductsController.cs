using MediatR;
using Microsoft.AspNetCore.Mvc;
using Application.Features.Catalog.Products;

namespace Api.Controllers.Catalog
{
    [ApiController]
    [Route("api/v1/[controller]")]
    public class ProductsController(ISender sender) : ControllerBase
    {
        [HttpGet]
        public async Task<IActionResult> GetAll([FromQuery] GetProductsQuery q, CancellationToken ct)
            => Ok(await sender.Send(q, ct));

        [HttpGet("{id:guid}")]
        public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
            => Ok(await sender.Send(new GetProductByIdQuery(id), ct));

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateProductCommand cmd, CancellationToken ct)
        { var id = await sender.Send(cmd, ct); return CreatedAtAction(nameof(GetById), new { id }, id); }

        [HttpPut("{id:guid}")]
        public async Task<IActionResult> Update(Guid id, [FromBody] UpdateProductCommand cmd, CancellationToken ct)
        { await sender.Send(cmd with { Id = id }, ct); return NoContent(); }

        [HttpPost("{id:guid}/publish")]
        public async Task<IActionResult> Publish(Guid id, CancellationToken ct)
        { await sender.Send(new PublishProductCommand(id), ct); return NoContent(); }

        [HttpPost("{id:guid}/archive")]
        public async Task<IActionResult> Archive(Guid id, CancellationToken ct)
        { await sender.Send(new ArchiveProductCommand(id), ct); return NoContent(); }

        [HttpPost("{id:guid}/variants")]
        public async Task<IActionResult> AddVariant(Guid id, [FromBody] AddProductVariantCommand cmd, CancellationToken ct)
            => Ok(new { variantId = await sender.Send(cmd with { ProductId = id }, ct) });
    }
}

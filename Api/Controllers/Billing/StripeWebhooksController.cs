using MediatR;
using Microsoft.AspNetCore.Mvc;
using Application;
using System.IO;

namespace Api.Controllers.Billing;

[ApiController]
[Route("api/v1/webhooks/stripe")]
[Produces("application/json")]
public class StripeWebhooksController(ISender sender) : ControllerBase
{
  [HttpPost]
  public async Task<IActionResult> Receive(CancellationToken ct)
  {
    var signature = Request.Headers["Stripe-Signature"].FirstOrDefault();
    if (string.IsNullOrWhiteSpace(signature))
      return BadRequest(new { error = "Missing Stripe-Signature header." });

    using var reader = new StreamReader(Request.Body);
    var payload = await reader.ReadToEndAsync(ct);

    var result = await sender.Send(new ProcessStripeWebhookCommand(payload, signature), ct);
    return Ok(result);
  }
}

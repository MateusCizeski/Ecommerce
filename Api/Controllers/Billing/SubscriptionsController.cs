using MediatR;
using Microsoft.AspNetCore.Mvc;
using Application.Features.Billing.Subscriptions;

namespace Api.Controllers.Billing;

/// <summary>
/// Gerencia subscriptions de tenants — criação, cancelamento e visualização
/// </summary>
[ApiController]
[Route("api/v1/subscriptions")]
[Produces("application/json")]
public class SubscriptionsController(ISender sender) : ControllerBase
{
  /// <summary>
  /// Cria uma nova subscription para o tenant atual
  /// </summary>
  /// <remarks>
  /// Precisa que o tenant ainda não tenha uma subscription ativa.
  /// O plano deve estar ativo.
  /// </remarks>
  /// <response code="200">Subscription criada com sucesso</response>
  /// <response code="409">Tenant já tem subscription ativa</response>
  /// <response code="404">Plano não encontrado</response>
  [HttpPost]
  public async Task<IActionResult> Create([FromBody] CreateSubscriptionCommand cmd, CancellationToken ct)
  {
    var result = await sender.Send(cmd, ct);
    return CreatedAtAction(nameof(GetCurrent), new { }, result);
  }

  /// <summary>
  /// Retorna a subscription ativa do tenant atual
  /// </summary>
  /// <response code="200">Subscription encontrada</response>
  /// <response code="204">Nenhuma subscription ativa</response>
  [HttpGet("current")]
  public async Task<IActionResult> GetCurrent(CancellationToken ct)
  {
    var result = await sender.Send(new GetSubscriptionByTenantQuery(), ct);
    if (result is null)
      return NoContent();
    return Ok(result);
  }
}

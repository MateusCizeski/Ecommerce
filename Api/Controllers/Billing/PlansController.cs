using MediatR;
using Microsoft.AspNetCore.Mvc;
using Application.Features.Billing.Plans;

namespace Api.Controllers.Billing;

/// <summary>
/// Expõe os planos disponíveis para subscription
/// </summary>
[ApiController]
[Route("api/v1/plans")]
[Produces("application/json")]
public class PlansController(ISender sender) : ControllerBase
{
  /// <summary>
  /// Lista todos os planos ativos disponíveis
  /// </summary>
  /// <response code="200">Lista de planos retornada com sucesso</response>
  [HttpGet]
  public async Task<IActionResult> GetAll(CancellationToken ct)
  {
    var plans = await sender.Send(new GetPlansQuery(), ct);
    return Ok(plans);
  }

  /// <summary>
  /// Retorna detalhe de um plano específico com suas features
  /// </summary>
  /// <response code="200">Plano encontrado</response>
  /// <response code="404">Plano não encontrado</response>
  [HttpGet("{id:guid}")]
  public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
  {
    var plan = await sender.Send(new GetPlanByIdQuery(id), ct);
    return Ok(plan);
  }
}

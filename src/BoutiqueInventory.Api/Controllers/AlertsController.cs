using BoutiqueInventory.Application.DTOs.Responses;
using BoutiqueInventory.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BoutiqueInventory.Api.Controllers;

/// <summary>Lists and acknowledges expiration alerts produced by the monitor.</summary>
[ApiController]
[Authorize]
[Route("api/alerts")]
[Produces("application/json")]
public class AlertsController(
    IAlertService service,
    IExpirationMonitor monitor) : ControllerBase
{
    /// <summary>List unacknowledged expiration alerts, soonest-to-expire first.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<ExpirationAlertResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<ExpirationAlertResponse>>> List(CancellationToken ct)
        => Ok(await service.ListUnacknowledgedAsync(ct));

    /// <summary>Mark an alert as acknowledged.</summary>
    [HttpPatch("{id:guid}/acknowledge")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Acknowledge(Guid id, CancellationToken ct)
    {
        await service.AcknowledgeAsync(id, ct);
        return NoContent();
    }

    /// <summary>
    /// Manually trigger an expiration scan. The same logic runs daily at
    /// 08:00 in the background; this endpoint exists for testing/ops.
    /// </summary>
    [HttpPost("scan")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    public async Task<IActionResult> Scan([FromQuery] int withinDays = 30, CancellationToken ct = default)
    {
        var created = await monitor.RunAsync(withinDays, ct);
        return Ok(new { created, windowDays = withinDays });
    }
}

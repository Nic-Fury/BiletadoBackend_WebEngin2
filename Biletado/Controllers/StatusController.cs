using Biletado.Services;
using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace Biletado.Controllers;

[ApiController]
[Route("api/v3/reservations")]
public class StatusController(IReservationStatusService reservationStatusService) : ControllerBase
{
    [HttpGet("status")]
    public IActionResult GetStatus()
    {
        return Ok(new
        {
            authors = new[] { "Nic Nouisser & Jakob Kaufmann" },
            api_version = "3.0.0"
        });
    }

    [HttpGet("health")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> GetHealth(CancellationToken ct = default)
    {
        var assetsConnected = await reservationStatusService.IsAssetsServiceReadyAsync(ct);
        var reservationsConnected = await reservationStatusService.IsReservationsDatabaseConnectedAsync(ct);

        var isReady = assetsConnected && reservationsConnected;

        var response = new
        {
            live = true,
            ready = isReady,
            databases = new
            {
                assets = new { connected = assetsConnected }
            }
        };

        if (!isReady)
        {
            return StatusCode(503, response);
        }

        return Ok(response);
    }

    [HttpGet("health/live")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public IActionResult GetLive()
    {
        return Ok(new { live = true });
    }

    [HttpGet("health/ready")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> GetReady(CancellationToken ct = default)
    {
        var traceId = Activity.Current?.Id ?? HttpContext.TraceIdentifier;
        var assetsConnected = await reservationStatusService.IsAssetsServiceReadyAsync(ct);
        var reservationsConnected = await reservationStatusService.IsReservationsDatabaseConnectedAsync(ct);

        if (!assetsConnected)
        {
            return StatusCode(503, new
            {
                errors = new[]
                {
                    new
                    {
                        code = "service_unreachable",
                        message = "Assets service is not reachable.",
                        more_info = "Check Assets service."
                    }
                },
                trace = traceId
            });
        }

        if (!reservationsConnected)
        {
            return StatusCode(503, new
            {
                errors = new[]
                {
                    new
                    {
                        code = "database_unreachable",
                        message = "Reservation database is not reachable.",
                        more_info = "Check connection string or database availability."
                    }
                },
                trace = traceId
            });
        }

        return Ok(new { ready = true });
    }
}


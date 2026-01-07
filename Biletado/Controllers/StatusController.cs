using Biletado.Services;
using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace Biletado.Controllers;

[ApiController]
[Route("api/v3/reservations")]
public class StatusController(IReservationStatusService reservationStatusService, ILogger<StatusController> logger) : ControllerBase
{
    [HttpGet("status")]
    public IActionResult GetStatus()
    {
        logger.LogInformation("Status endpoint called");
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
        logger.LogInformation("Health check started");
        
        var assetsConnected = await reservationStatusService.IsAssetsServiceReadyAsync(ct);
        var reservationsConnected = await reservationStatusService.IsReservationsDatabaseConnectedAsync(ct);

        var isReady = assetsConnected && reservationsConnected;

        logger.LogInformation("Health check completed: Ready={IsReady}, AssetsConnected={AssetsConnected}, ReservationsDbConnected={ReservationsDbConnected}",
            isReady, assetsConnected, reservationsConnected);

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
            logger.LogWarning("Service not ready - returning 503");
            return StatusCode(503, response);
        }

        return Ok(response);
    }

    [HttpGet("health/live")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public IActionResult GetLive()
    {
        logger.LogDebug("Liveness probe called");
        return Ok(new { live = true });
    }

    [HttpGet("health/ready")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> GetReady(CancellationToken ct = default)
    {
        var traceId = Activity.Current?.Id ?? HttpContext.TraceIdentifier;
        logger.LogInformation("Readiness probe started: TraceId={TraceId}", traceId);
        
        var assetsConnected = await reservationStatusService.IsAssetsServiceReadyAsync(ct);
        var reservationsConnected = await reservationStatusService.IsReservationsDatabaseConnectedAsync(ct);

        if (!assetsConnected)
        {
            logger.LogError("Assets service is not reachable: TraceId={TraceId}", traceId);
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
            logger.LogError("Reservation database is not reachable: TraceId={TraceId}", traceId);
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

        logger.LogInformation("Readiness probe successful: TraceId={TraceId}", traceId);
        return Ok(new { ready = true });
    }
}


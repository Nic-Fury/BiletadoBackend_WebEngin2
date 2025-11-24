using Biletado.Services;
using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace Biletado.Controllers;

[ApiController]
[Route("api/v3/reservations")]

public class StatusController : Controller
{
    
    
    [HttpGet("status")]
    public async Task<IActionResult> GetAll()
    {
        var result = new
        {
            authors = new[] { "Nic Nouisser & Jakob Kaufmann" },
            api_version = "3.0.0" // TODO: Richtige Version einfügen
        };
        return Ok(result);
    }
    
    [HttpGet("health")]
    public async Task<IActionResult> GetHealth()
    {
        return Ok();
    }
    
    [HttpGet("health/live")]
    public async Task<IActionResult> GetLive()
    {
        return Ok(new{live = true});
    }
    
    [HttpGet("health/ready")]
    public async Task<IActionResult> GetReady()
    {
        var traceId = Activity.Current?.Id ?? Guid.NewGuid().ToString();
        var assetsConnected = await ReservationStatusService.IsAssetsServiceReadyAsync();
        var reservationsConnected = await ReservationStatusService.IsReservationsDatabaseConnectedAsync();
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


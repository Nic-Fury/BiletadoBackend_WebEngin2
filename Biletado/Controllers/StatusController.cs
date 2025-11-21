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
        return Ok();
    }
    
    [HttpGet("health/ready")]
    public async Task<IActionResult> GetReady()
    {
        return Ok();
    }
    
}


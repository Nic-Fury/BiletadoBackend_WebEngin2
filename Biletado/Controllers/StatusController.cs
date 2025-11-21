using Microsoft.AspNetCore.Mvc;

namespace Biletado.Controllers;

[ApiController]
[Route("api/v3/reservations")]

public class StatusController : Controller
{
    
    
    [HttpGet("status")]
    public async Task<IActionResult> GetAll()
    {
        return Ok();
    }
    
    [HttpGet("status")]
    public async Task<IActionResult> GetLife()
    {
        return Ok();
    }
    
}
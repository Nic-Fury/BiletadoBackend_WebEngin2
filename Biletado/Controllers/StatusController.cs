using Microsoft.AspNetCore.Mvc;

namespace Biletado.Controllers;

[ApiController]
[Route("api/[controller]")]

public class StatusController : Controller
{
    
    
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        return Ok();
    }
}
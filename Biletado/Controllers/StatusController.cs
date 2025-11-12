using Microsoft.AspNetCore.Mvc;

namespace Biletado.Controllers;

[]

public class StatusController : Controller
{
    
    
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        return Ok();
    }
}
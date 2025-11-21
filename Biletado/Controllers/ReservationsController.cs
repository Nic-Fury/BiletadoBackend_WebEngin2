using Microsoft.AspNetCore.Mvc;

namespace Biletado.Controllers;

[ApiController]
[Route("api/v3/reservations/reservations")]
public class ReservationsController : Controller
{
    // GET
    [HttpGet]
    public async Task<IActionResult> GetAllReservations()
    {
        
        return Ok();
    }
    
    // POST
    [HttpPost]
    public async Task<IActionResult> PostAllReservations()
    {
        return Ok();
    }
    
    // GET
    [HttpGet("{id}")]
    public async Task<IActionResult> GetReservationByID()
    {
        return Ok();
    }
    
    // PUT
    [HttpPut("{id}")]
    public async Task<IActionResult> PutReservationByID()
    {
        return Ok();
    }
    
    // DELETE
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteReservationByID()
    {
        return Ok();
    }

}





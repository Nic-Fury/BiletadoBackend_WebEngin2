using Microsoft.AspNetCore.Mvc;

namespace Biletado.Controllers;

[ApiController]
[Route("api/v3/reservations/reservations")]
public class ReservationsController : Controller
{
    // GET
    [HttpGet]
    public async Task<IActionResult> GetAllReservations(
            [FromQuery(Name = "include_deleted")] bool includeDeleted = false,
            [FromQuery(Name = "room_id")] string? roomId = null,
            [FromQuery] string? before = null,
            [FromQuery] string? after = null,
            CancellationToken ct = default)
    {
        // optional room id parameter
        Guid? roomGuid = null;
        if (!string.IsNullOrWhiteSpace(roomId))
        {
            if (Guid.TryParse(roomId, out var parsed)) roomGuid = parsed;
            else
            {
                return BadRequest(ErrorResponse("bad_request", "Invalid room_id."));
            }
        }
        
        
        // Parse optional before date.
        DateOnly? beforeDate = null;
        if (!string.IsNullOrWhiteSpace(before))
        {
            if (DateOnly.TryParse(before, out var b)) beforeDate = b;
            else
            {
                return BadRequest(ErrorResponse("bad_request", "Invalid before date (YYYY-MM-DD)."));
            }
        }


        
        
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
    
    
    
    
    private object ErrorResponse(string code, string message)
    {
        return new { error = new { code, message } };
    }

}





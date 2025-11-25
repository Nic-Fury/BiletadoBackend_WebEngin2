using System.Diagnostics;
using Biletado.DTOs;
using Biletado.Services;
using Microsoft.AspNetCore.Mvc;

namespace Biletado.Controllers;

[ApiController]
[Route("api/v3/reservations/reservations")]
public class ReservationsController(IReservationService reservationService) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(ReservationListResponse), 200)]
    [ProducesResponseType(typeof(ErrorResponse), 400)]
    public async Task<IActionResult> GetAllReservations(
        [FromQuery(Name = "include_deleted")] bool includeDeleted = false,
        [FromQuery(Name = "room_id")] string? roomId = null,
        [FromQuery] string? before = null,
        [FromQuery] string? after = null,
        CancellationToken ct = default)
    {
        Guid? roomGuid = null;
        if (!string.IsNullOrWhiteSpace(roomId))
        {
            if (Guid.TryParse(roomId, out var parsed))
            {
                roomGuid = parsed;
            }
            else
            {
                return BadRequest(BuildErrorResponse("bad_request", "Invalid room_id."));
            }
        }

        DateOnly? beforeDate = null;
        if (!string.IsNullOrWhiteSpace(before))
        {
            if (DateOnly.TryParse(before, out var b)) beforeDate = b;
            else
            {
                return BadRequest(BuildErrorResponse("bad_request", "Invalid before date (YYYY-MM-DD)."));
            }
        }

        DateOnly? afterDate = null;
        if (!string.IsNullOrWhiteSpace(after))
        {
            if (DateOnly.TryParse(after, out var a)) afterDate = a;
            else
            {
                return BadRequest(BuildErrorResponse("bad_request", "Invalid after date (YYYY-MM-DD)."));
            }
        }

        var data = await reservationService.GetAllReservationsAsync(
            includeDeleted,
            roomGuid,
            beforeDate,
            afterDate,
            ct
        );

        return Ok(new ReservationListResponse { Reservations = data });
    }

    [HttpPost]
    [ProducesResponseType(typeof(ReservationResponse), 201)]
    [ProducesResponseType(typeof(ErrorResponse), 400)]
    public async Task<IActionResult> PostReservation([FromBody] CreateReservationRequest request, CancellationToken ct)
    {
        var errors = new List<DeleteError>();
        if (request.RoomId == Guid.Empty)
        {
            errors.Add(new DeleteError("bad_request", "room_id must not be empty."));
        }

        if (request.From > request.To)
        {
            errors.Add(new DeleteError("bad_request", "from must not be after to."));
        }

        var roomExists = await reservationService.IsRoomExistingAsync(request.RoomId, ct);
        if (!roomExists)
        {
            errors.Add(new DeleteError("bad_request", "room_id refers to a non-existing room."));
        }

        var roomFree = await reservationService.IsRoomFree(request.RoomId, request.From, request.To, ct);
        if (!roomFree)
        {
            errors.Add(new DeleteError("bad_request", "room is already reserved for the given date range."));
        }

        if (errors.Count > 0)
        {
            return BadRequest(BuildErrorResponse(errors));
        }

        var created = await reservationService.CreateReservationAsync(request, ct);
        return CreatedAtAction(nameof(GetReservationById), new { id = created.Id }, created);
    }

    [HttpGet("{id}")]
    [ProducesResponseType(typeof(ReservationResponse), 200)]
    [ProducesResponseType(typeof(ErrorResponse), 404)]
    [ProducesResponseType(typeof(ErrorResponse), 400)]
    public async Task<IActionResult> GetReservationById(string id, CancellationToken ct)
    {
        if (!Guid.TryParse(id, out var guid))
        {
            return BadRequest(BuildErrorResponse("bad_request", "Invalid Id."));
        }

        var res = await reservationService.GetReservationByIdAsync(guid, ct);
        if (res is null)
        {
            return NotFound(BuildErrorResponse("bad_request", "Reservation not found."));
        }

        return Ok(res);
    }

    [HttpPut("{id}")]
    [ProducesResponseType(typeof(ReservationResponse), 200)]
    [ProducesResponseType(typeof(ReservationResponse), 201)]
    [ProducesResponseType(typeof(ErrorResponse), 400)]
    public async Task<IActionResult> PutReservationById(string id, [FromBody] CreateReservationRequest request, CancellationToken ct)
    {
        if (!Guid.TryParse(id, out var guid))
        {
            return BadRequest(BuildErrorResponse("bad_request", "Invalid Id."));
        }

        var result = await reservationService.ReplaceOrCreateReservationAsync(guid, request, ct);
        if (!result.Success)
        {
            return BadRequest(BuildErrorResponse(result.Errors));
        }

        var body = result.Response!;
        if (result.Created)
        {
            return CreatedAtAction(nameof(GetReservationById), new { id = body.Id }, body);
        }

        return Ok(body);
    }

    [HttpDelete("{id}")]
    [ProducesResponseType(204)]
    [ProducesResponseType(typeof(ErrorResponse), 404)]
    [ProducesResponseType(typeof(ErrorResponse), 400)]
    public async Task<IActionResult> DeleteReservationById(string id, CancellationToken ct, [FromQuery] bool permanent = false)
    {
        if (!Guid.TryParse(id, out var guid))
        {
            return BadRequest(BuildErrorResponse("bad_request", "Invalid Id."));
        }

        var result = await reservationService.DeleteReservationAsync(guid, permanent, ct);
        if (result.Success)
        {
            return NoContent();
        }

        var hasNotFound = result.Errors.Any(e => e.Code is "reservation_not_found" or "reservation_already_deleted");
        if (hasNotFound)
        {
            return NotFound(BuildErrorResponse(result.Errors));
        }

        return BadRequest(BuildErrorResponse(result.Errors));
    }

    private ErrorResponse BuildErrorResponse(string code, string message)
    {
        return BuildErrorResponse([new DeleteError(code, message)]);
    }

    private ErrorResponse BuildErrorResponse(IEnumerable<DeleteError> errors)
    {
        var trace = Activity.Current?.Id ?? HttpContext.TraceIdentifier;
        var errorDtos = errors.Select(e => new ErrorDetail
        {
            Code = e.Code,
            Message = e.Message,
            MoreInfo = e.MoreInfo
        }).ToList();

        return new ErrorResponse
        {
            Errors = errorDtos,
            Trace = trace
        };
    }
}
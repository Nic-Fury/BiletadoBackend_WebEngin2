using System.Diagnostics;
using Biletado.DTOs;
using Biletado.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Biletado.Controllers;

[ApiController]
[Route("api/v3/reservations/reservations")]
[Authorize]
public class ReservationsController(IReservationService reservationService, ILogger<ReservationsController> logger) : ControllerBase
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
        logger.LogInformation("Getting all reservations: IncludeDeleted={IncludeDeleted}, RoomId={RoomId}, Before={Before}, After={After}",
            includeDeleted, roomId, before, after);

        Guid? roomGuid = null;
        if (!string.IsNullOrWhiteSpace(roomId))
        {
            if (Guid.TryParse(roomId, out var parsed))
            {
                roomGuid = parsed;
            }
            else
            {
                logger.LogWarning("Invalid room_id provided: {RoomId}", roomId);
                return BadRequest(BuildErrorResponse("bad_request", "Invalid room_id."));
            }
        }

        DateOnly? beforeDate = null;
        if (!string.IsNullOrWhiteSpace(before))
        {
            if (DateOnly.TryParse(before, out var b)) beforeDate = b;
            else
            {
                logger.LogWarning("Invalid before date provided: {Before}", before);
                return BadRequest(BuildErrorResponse("bad_request", "Invalid before date (YYYY-MM-DD)."));
            }
        }

        DateOnly? afterDate = null;
        if (!string.IsNullOrWhiteSpace(after))
        {
            if (DateOnly.TryParse(after, out var a)) afterDate = a;
            else
            {
                logger.LogWarning("Invalid after date provided: {After}", after);
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

        logger.LogInformation("Retrieved {Count} reservations", data.Count);
        return Ok(new ReservationListResponse { Reservations = data });
    }

    [HttpPost]
    [ProducesResponseType(typeof(ReservationResponse), 201)]
    [ProducesResponseType(typeof(ErrorResponse), 400)]
    public async Task<IActionResult> PostReservation([FromBody] CreateReservationRequest request, CancellationToken ct)
    {
        logger.LogInformation("Creating reservation: RoomId={RoomId}, From={From}, To={To}",
            request.RoomId, request.From, request.To);

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
            logger.LogWarning("Room not found: RoomId={RoomId}", request.RoomId);
            errors.Add(new DeleteError("bad_request", "room_id refers to a non-existing room."));
        }

        var roomFree = await reservationService.IsRoomFree(request.RoomId, request.From, request.To, ct);
        if (!roomFree)
        {
            logger.LogWarning("Room already reserved: RoomId={RoomId}, From={From}, To={To}",
                request.RoomId, request.From, request.To);
            errors.Add(new DeleteError("bad_request", "room is already reserved for the given date range."));
        }

        if (errors.Count > 0)
        {
            logger.LogWarning("Reservation creation failed with {ErrorCount} validation errors", errors.Count);
            return BadRequest(BuildErrorResponse(errors));
        }

        var created = await reservationService.CreateReservationAsync(request, ct);
        logger.LogInformation("Reservation created successfully: ReservationId={ReservationId}, RoomId={RoomId}",
            created.Id, created.RoomId);
        
        return CreatedAtAction(nameof(GetReservationById), new { id = created.Id }, created);
    }

    [HttpGet("{id}")]
    [ProducesResponseType(typeof(ReservationResponse), 200)]
    [ProducesResponseType(typeof(ErrorResponse), 404)]
    [ProducesResponseType(typeof(ErrorResponse), 400)]
    public async Task<IActionResult> GetReservationById(string id, CancellationToken ct)
    {
        logger.LogInformation("Getting reservation by ID: ReservationId={ReservationId}", id);

        if (!Guid.TryParse(id, out var guid))
        {
            logger.LogWarning("Invalid reservation ID format: {ReservationId}", id);
            return BadRequest(BuildErrorResponse("bad_request", "Invalid Id."));
        }

        var res = await reservationService.GetReservationByIdAsync(guid, ct);
        if (res is null)
        {
            logger.LogWarning("Reservation not found: ReservationId={ReservationId}", guid);
            return NotFound(BuildErrorResponse("bad_request", "Reservation not found."));
        }

        logger.LogInformation("Reservation retrieved: ReservationId={ReservationId}, RoomId={RoomId}",
            res.Id, res.RoomId);
        return Ok(res);
    }

    [HttpPut("{id}")]
    [ProducesResponseType(typeof(ReservationResponse), 200)]
    [ProducesResponseType(typeof(ReservationResponse), 201)]
    [ProducesResponseType(typeof(ErrorResponse), 400)]
    public async Task<IActionResult> PutReservationById(string id, [FromBody] CreateReservationRequest request, CancellationToken ct)
    {
        logger.LogInformation("Replacing or creating reservation: ReservationId={ReservationId}, RoomId={RoomId}, From={From}, To={To}",
            id, request.RoomId, request.From, request.To);

        if (!Guid.TryParse(id, out var guid))
        {
            logger.LogWarning("Invalid reservation ID format: {ReservationId}", id);
            return BadRequest(BuildErrorResponse("bad_request", "Invalid Id."));
        }

        var result = await reservationService.ReplaceOrCreateReservationAsync(guid, request, ct);
        if (!result.Success)
        {
            logger.LogWarning("Reservation upsert failed: ReservationId={ReservationId}, ErrorCount={ErrorCount}",
                guid, result.Errors.Count);
            return BadRequest(BuildErrorResponse(result.Errors));
        }

        var body = result.Response!;
        if (result.Created)
        {
            logger.LogInformation("Reservation created via PUT: ReservationId={ReservationId}, RoomId={RoomId}",
                body.Id, body.RoomId);
            return CreatedAtAction(nameof(GetReservationById), new { id = body.Id }, body);
        }

        logger.LogInformation("Reservation updated via PUT: ReservationId={ReservationId}, RoomId={RoomId}",
            body.Id, body.RoomId);
        return Ok(body);
    }

    [HttpDelete("{id}")]
    [ProducesResponseType(204)]
    [ProducesResponseType(typeof(ErrorResponse), 404)]
    [ProducesResponseType(typeof(ErrorResponse), 400)]
    public async Task<IActionResult> DeleteReservationById(string id, CancellationToken ct, [FromQuery] bool permanent = false)
    {
        logger.LogInformation("Deleting reservation: ReservationId={ReservationId}, Permanent={Permanent}",
            id, permanent);

        if (!Guid.TryParse(id, out var guid))
        {
            logger.LogWarning("Invalid reservation ID format: {ReservationId}", id);
            return BadRequest(BuildErrorResponse("bad_request", "Invalid Id."));
        }

        var result = await reservationService.DeleteReservationAsync(guid, permanent, ct);
        if (result.Success)
        {
            var deleteType = result.WasHardDeleted ? "permanently" : "soft";
            logger.LogInformation("Reservation deleted {DeleteType}: ReservationId={ReservationId}",
                deleteType, guid);
            return NoContent();
        }

        var hasNotFound = result.Errors.Any(e => e.Code is "reservation_not_found" or "reservation_already_deleted");
        if (hasNotFound)
        {
            logger.LogWarning("Reservation delete failed - not found: ReservationId={ReservationId}", guid);
            return NotFound(BuildErrorResponse(result.Errors));
        }

        logger.LogWarning("Reservation delete failed: ReservationId={ReservationId}, ErrorCount={ErrorCount}",
            guid, result.Errors.Count);
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
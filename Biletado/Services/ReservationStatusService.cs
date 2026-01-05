using System.Text.Json;
using Biletado.DTOs;
using Biletado.Domain;
using Biletado.Repository;
using Microsoft.EntityFrameworkCore;


namespace Biletado.Services;

public interface IReservationStatusService
{
    Task<bool> IsAssetsServiceReadyAsync(CancellationToken ct = default);
    Task<bool> IsReservationsDatabaseConnectedAsync(CancellationToken ct = default);
    Task<IReadOnlyCollection<ReservationResponse>> GetAllReservationsAsync(
        bool includeDeleted = false,
        Guid? roomId = null,
        DateOnly? before = null,
        DateOnly? after = null,
        CancellationToken ct = default);
    Task<bool> IsRoomExistingAsync(Guid roomId, CancellationToken ct = default);
    Task<bool> IsRoomFree(Guid roomId, DateOnly from, DateOnly to, CancellationToken ct = default);
}
public class ReservationStatusService (ReservationServiceRepository ReservationServiceRepository,Contexts.ReservationsDbContext db, IConfiguration config, ILogger<ReservationStatusService> logger) : IReservationStatusService, IReservationService
{
    public async Task<bool> IsAssetsServiceReadyAsync(CancellationToken ct = default)
    {
        var baseUrl = config["Services:Assets:BaseUrl"];
        var port = config["Services:Assets:Port"];
        var url = $"{baseUrl}:{port}";
        var readyPath = config["Services:Assets:ReadyPath"];

        logger.LogDebug("Checking assets service readiness: Url={Url}, Path={Path}", url, readyPath);

        var sw = System.Diagnostics.Stopwatch.StartNew();
        using var client = new HttpClient();
        client.BaseAddress = new Uri(url!);
        try
        {
            var response = await client.GetAsync(readyPath, ct);
            sw.Stop();
            if (!response.IsSuccessStatusCode)
            {
                logger.LogWarning("Assets service returned non-success status: StatusCode={StatusCode}, ElapsedMs={ElapsedMs}",
                    response.StatusCode, sw.ElapsedMilliseconds);
                return false;
            }

            var json = await response.Content.ReadAsStringAsync(ct);
            using var doc = JsonDocument.Parse(json);
            if (doc.RootElement.TryGetProperty("ready", out var readyProperty) &&
                readyProperty.ValueKind == JsonValueKind.True)
            {
                logger.LogInformation("Assets service is ready: ElapsedMs={ElapsedMs}", sw.ElapsedMilliseconds);
                return true;
            }
            
            logger.LogWarning("Assets service is not ready: ElapsedMs={ElapsedMs}", sw.ElapsedMilliseconds);
        }
        catch (Exception ex)
        {
            sw.Stop();
            logger.LogError(ex, "Failed to check assets service readiness: Url={Url}, ElapsedMs={ElapsedMs}",
                url, sw.ElapsedMilliseconds);
            return false;
        }

        return false;
    }
    
    public async Task<bool> IsReservationsDatabaseConnectedAsync(CancellationToken ct = default)
    {
        logger.LogDebug("Checking database connectivity");
        
        var sw = System.Diagnostics.Stopwatch.StartNew();
        try
        {
            await db.Database.ExecuteSqlRawAsync("SELECT 1;", ct);
            sw.Stop();
            logger.LogInformation("Database connection successful: ElapsedMs={ElapsedMs}", sw.ElapsedMilliseconds);
            return true;
        }
        catch (Exception ex)
        {
            sw.Stop();
            logger.LogError(ex, "Database connection failed: ElapsedMs={ElapsedMs}", sw.ElapsedMilliseconds);
            return false;
        }
    }

    
    public async Task<IReadOnlyCollection<ReservationResponse>> GetAllReservationsAsync(
        bool includeDeleted = false,
        Guid? roomId = null,
        DateOnly? before = null,
        DateOnly? after = null,
        CancellationToken ct = default)
    {
        logger.LogInformation("Fetching all reservations: IncludeDeleted={IncludeDeleted}, RoomId={RoomId}, Before={Before}, After={After}",
            includeDeleted, roomId, before, after);
        
        var sw = System.Diagnostics.Stopwatch.StartNew();
        
        var list = await ReservationServiceRepository.GetAllAsync(includeDeleted, ct);

        // Apply optional filters in-memory (could be pushed to repository for larger datasets).
        if (roomId is not null) list = list.Where(r => r.RoomId == roomId).ToList();
        if (before is not null) list = list.Where(r => before.Value >= r.From).ToList();
        if (after is not null) list = list.Where(r => after.Value <= r.To).ToList();
        sw.Stop();

        logger.LogInformation("Fetched {Count} reservations: ElapsedMs={ElapsedMs}", list.Count, sw.ElapsedMilliseconds);
        
        return list.Select(r => new ReservationResponse(r.Id, r.From, r.To, r.RoomId, r.DeletedAt)).ToList();
    }
    
    public async Task<bool> IsRoomExistingAsync(Guid roomId, CancellationToken ct = default)
    {
        var baseUrl = config["Services:Assets:BaseUrl"];
        var port = config["Services:Assets:Port"];
        var url = $"{baseUrl}:{port}";
        var roomPath = config["Services:Assets:RoomPath"];

        logger.LogDebug("Checking if room exists: RoomId={RoomId}", roomId);

        var sw = System.Diagnostics.Stopwatch.StartNew();
        
        var relativePath = roomPath!.Replace("{id}", roomId.ToString());

        using var client = new HttpClient();
        client.BaseAddress = new Uri(url!);
        try {
            var response = await client.GetAsync(relativePath, ct);
            sw.Stop();

            if (response.StatusCode != System.Net.HttpStatusCode.OK)
            {
                logger.LogWarning("Room check returned non-OK status: RoomId={RoomId}, StatusCode={StatusCode}, ElapsedMs={ElapsedMs}",
                    roomId, response.StatusCode, sw.ElapsedMilliseconds);
                return response.StatusCode == System.Net.HttpStatusCode.OK;
            }

            var json = await response.Content.ReadAsStringAsync(ct);
            try {
                using var doc = JsonDocument.Parse(json);
                if (!doc.RootElement.TryGetProperty("deleted_at", out var deletedAtProp)) 
                {
                    logger.LogInformation("Room exists and not deleted: RoomId={RoomId}, ElapsedMs={ElapsedMs}",
                        roomId, sw.ElapsedMilliseconds);
                    return true;
                }
                if (deletedAtProp.ValueKind == JsonValueKind.Null) 
                {
                    logger.LogInformation("Room exists and not deleted: RoomId={RoomId}, ElapsedMs={ElapsedMs}",
                        roomId, sw.ElapsedMilliseconds);
                    return true;
                }
                logger.LogWarning("Room is deleted: RoomId={RoomId}, ElapsedMs={ElapsedMs}",
                    roomId, sw.ElapsedMilliseconds);
                return false;
            }
            catch (JsonException jex) {
                logger.LogError(jex, "Failed to parse room response JSON: RoomId={RoomId}", roomId);
                return false;
            }
        }
        catch (Exception ex)
        {
            sw.Stop();
            logger.LogError(ex, "Failed to check room existence: RoomId={RoomId}, ElapsedMs={ElapsedMs}",
                roomId, sw.ElapsedMilliseconds);
            return false;
        }
    }

    public async Task<bool> IsRoomFree(Guid roomId, DateOnly from, DateOnly to, CancellationToken ct = default)
    {
        logger.LogDebug("Checking if room is free: RoomId={RoomId}, From={From}, To={To}",
            roomId, from, to);
        
        var sw = System.Diagnostics.Stopwatch.StartNew();
        try
        {
            var existingReservations = await ReservationServiceRepository.GetAllAsync(false, ct);
            var conflictingReservations = existingReservations.Where(r => 
                r.RoomId == roomId && 
                r.From < to && 
                r.To > from
            ).ToList();
            
            sw.Stop();
            
            var isFree = conflictingReservations.Count == 0;
            if (isFree)
            {
                logger.LogInformation("Room is free: RoomId={RoomId}, From={From}, To={To}, ElapsedMs={ElapsedMs}",
                    roomId, from, to, sw.ElapsedMilliseconds);
            }
            else
            {
                logger.LogWarning("Room has {ConflictCount} conflicting reservations: RoomId={RoomId}, From={From}, To={To}, ElapsedMs={ElapsedMs}",
                    conflictingReservations.Count, roomId, from, to, sw.ElapsedMilliseconds);
            }
            
            return isFree;
        }
        catch (Exception ex)
        {
            sw.Stop();
            logger.LogError(ex, "Failed to check room availability: RoomId={RoomId}, From={From}, To={To}, ElapsedMs={ElapsedMs}",
                roomId, from, to, sw.ElapsedMilliseconds);
            return false;
        }
    }

    public async Task<bool> IsRoomFree(Guid roomId, DateOnly from, DateOnly to, Guid? excludeReservationId, CancellationToken ct = default)
    {
        logger.LogDebug("Checking if room is free (excluding reservation): RoomId={RoomId}, From={From}, To={To}, ExcludeReservationId={ExcludeReservationId}",
            roomId, from, to, excludeReservationId);
        
        var existingReservations = await ReservationServiceRepository.GetAllAsync(false, ct);
        var conflictingReservations = existingReservations.Where(r => 
            r.RoomId == roomId && 
            r.From < to && 
            r.To > from &&
            r.Id != excludeReservationId
        ).ToList();
        
        var isFree = conflictingReservations.Count == 0;
        if (!isFree)
        {
            logger.LogWarning("Room has {ConflictCount} conflicting reservations: RoomId={RoomId}, From={From}, To={To}",
                conflictingReservations.Count, roomId, from, to);
        }
        
        return isFree;
    }

    public async Task<ReservationResponse?> GetReservationByIdAsync(Guid id, CancellationToken ct = default)
    {
        logger.LogInformation("Fetching reservation by ID: ReservationId={ReservationId}", id);
        
        var list = await ReservationServiceRepository.GetAllAsync(true, ct);
        var reservation = list.FirstOrDefault(r => r.Id == id);
        if (reservation == null) 
        {
            logger.LogWarning("Reservation not found: ReservationId={ReservationId}", id);
            return null;
        }
        
        logger.LogInformation("Reservation found: ReservationId={ReservationId}, RoomId={RoomId}",
            reservation.Id, reservation.RoomId);
        return new ReservationResponse(reservation.Id, reservation.From, reservation.To, reservation.RoomId, reservation.DeletedAt);
    }

    public async Task<ReservationResponse> CreateReservationAsync(CreateReservationRequest request, CancellationToken ct = default)
    {
        var newId = Guid.NewGuid();
        logger.LogInformation("Creating new reservation: ReservationId={ReservationId}, RoomId={RoomId}, From={From}, To={To}",
            newId, request.RoomId, request.From, request.To);
        
        var reservation = new Reservation
        {
            Id = newId,
            RoomId = request.RoomId,
            From = request.From,
            To = request.To,
            DeletedAt = null
        };

        db.Reservations.Add(reservation);
        await db.SaveChangesAsync(ct);

        logger.LogInformation("Reservation created successfully: ReservationId={ReservationId}", newId);
        return new ReservationResponse(reservation.Id, reservation.From, reservation.To, reservation.RoomId, reservation.DeletedAt);
    }

    public async Task<ReservationResponse?> UpdateReservationAsync(Guid id, CreateReservationRequest request, CancellationToken ct = default)
    {
        logger.LogInformation("Updating reservation: ReservationId={ReservationId}, RoomId={RoomId}, From={From}, To={To}",
            id, request.RoomId, request.From, request.To);
        
        var reservation = await db.Reservations.IgnoreQueryFilters().FirstOrDefaultAsync(r => r.Id == id, ct);
        if (reservation == null) 
        {
            logger.LogWarning("Reservation not found for update: ReservationId={ReservationId}", id);
            return null;
        }

        reservation.RoomId = request.RoomId;
        reservation.From = request.From;
        reservation.To = request.To;
        reservation.DeletedAt = null;

        await db.SaveChangesAsync(ct);

        logger.LogInformation("Reservation updated successfully: ReservationId={ReservationId}", id);
        return new ReservationResponse(reservation.Id, reservation.From, reservation.To, reservation.RoomId, reservation.DeletedAt);
    }

    public async Task<UpsertReservationResult> ReplaceOrCreateReservationAsync(Guid id, CreateReservationRequest request, CancellationToken ct = default)
    {
        logger.LogInformation("Upserting reservation: ReservationId={ReservationId}, RoomId={RoomId}, From={From}, To={To}",
            id, request.RoomId, request.From, request.To);
        
        var errors = new List<DeleteError>();

        if (request.RoomId == Guid.Empty)
        {
            errors.Add(new DeleteError("bad_request", "room_id must not be empty."));
        }

        if (request.From > request.To)
        {
            errors.Add(new DeleteError("bad_request", "from must not be after to."));
        }

        var roomExists = await IsRoomExistingAsync(request.RoomId, ct);
        if (!roomExists)
        {
            errors.Add(new DeleteError("bad_request", "room_id refers to a non-existing room."));
        }

        var existing = await db.Reservations.IgnoreQueryFilters().FirstOrDefaultAsync(r => r.Id == id, ct);
        
        var roomFree = await IsRoomFree(request.RoomId, request.From, request.To, existing?.Id, ct);
        if (!roomFree)
        {
            errors.Add(new DeleteError("bad_request", "room is already reserved for the given date range."));
        }

        if (errors.Count > 0)
        {
            logger.LogWarning("Reservation upsert validation failed: ReservationId={ReservationId}, ErrorCount={ErrorCount}",
                id, errors.Count);
            return UpsertReservationResult.Failure([.. errors]);
        }

        if (existing != null)
        {
            existing.RoomId = request.RoomId;
            existing.From = request.From;
            existing.To = request.To;
            existing.DeletedAt = null;

            await db.SaveChangesAsync(ct);
            logger.LogInformation("Reservation updated via upsert: ReservationId={ReservationId}", id);
            return UpsertReservationResult.UpdatedResult(new ReservationResponse(existing.Id, existing.From, existing.To, existing.RoomId, existing.DeletedAt));
        }

        var newReservation = new Reservation
        {
            Id = id,
            RoomId = request.RoomId,
            From = request.From,
            To = request.To,
            DeletedAt = null
        };

        db.Reservations.Add(newReservation);
        await db.SaveChangesAsync(ct);

        logger.LogInformation("Reservation created via upsert: ReservationId={ReservationId}", id);
        return UpsertReservationResult.CreatedResult(new ReservationResponse(newReservation.Id, newReservation.From, newReservation.To, newReservation.RoomId, newReservation.DeletedAt));
    }

    public async Task<DeleteReservationResult> DeleteReservationAsync(Guid id, bool permanent = false, CancellationToken ct = default)
    {
        logger.LogInformation("Deleting reservation: ReservationId={ReservationId}, Permanent={Permanent}",
            id, permanent);
        
        var reservation = await db.Reservations.IgnoreQueryFilters().FirstOrDefaultAsync(r => r.Id == id, ct);

        if (reservation == null)
        {
            logger.LogWarning("Reservation not found for deletion: ReservationId={ReservationId}", id);
            return DeleteReservationResult.Failure(new DeleteError("reservation_not_found", "Reservation not found."));
        }

        if (permanent)
        {
            db.Reservations.Remove(reservation);
            await db.SaveChangesAsync(ct);
            logger.LogInformation("Reservation permanently deleted: ReservationId={ReservationId}", id);
            return DeleteReservationResult.HardDeleted();
        }

        if (reservation.DeletedAt != null)
        {
            logger.LogWarning("Reservation already soft-deleted: ReservationId={ReservationId}", id);
            return DeleteReservationResult.Failure(new DeleteError("reservation_already_deleted", "Reservation is already deleted."));
        }

        reservation.DeletedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(ct);
        logger.LogInformation("Reservation soft-deleted: ReservationId={ReservationId}", id);
        return DeleteReservationResult.SoftDeleted();
    }
}
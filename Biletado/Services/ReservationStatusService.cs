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
public class ReservationStatusService (ReservationServiceRepository ReservationServiceRepository,Contexts.ReservationsDbContext db, IConfiguration config) : IReservationStatusService, IReservationService
{
    public async Task<bool> IsAssetsServiceReadyAsync(CancellationToken ct = default)
    {
        var baseUrl = config["Services:Assets:BaseUrl"];
        var port = config["Services:Assets:Port"];
        var url = $"{baseUrl}:{port}";
        var readyPath = config["Services:Assets:ReadyPath"];

        var sw = System.Diagnostics.Stopwatch.StartNew();
        using var client = new HttpClient();
        client.BaseAddress = new Uri(url!);
        try
        {
            var response = await client.GetAsync(readyPath, ct);
            sw.Stop();
            if (!response.IsSuccessStatusCode)
            {
                return false;
            }

            var json = await response.Content.ReadAsStringAsync(ct);
            using var doc = JsonDocument.Parse(json);
            if (doc.RootElement.TryGetProperty("ready", out var readyProperty) &&
                readyProperty.ValueKind == JsonValueKind.True)
            {
                return true;
            }
        }
        catch (Exception ex)
        {
            sw.Stop();
            return false;
        }

        return false;
    }
    
    public async Task<bool> IsReservationsDatabaseConnectedAsync(CancellationToken ct = default)
    {
        var sw = System.Diagnostics.Stopwatch.StartNew();
        try
        {
            await db.Database.ExecuteSqlRawAsync("SELECT 1;", ct);
            sw.Stop();
            return true;
        }
        catch (Exception ex)
        {
            sw.Stop();
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
        var sw = System.Diagnostics.Stopwatch.StartNew();
        
        var list = await ReservationServiceRepository.GetAllAsync(includeDeleted, ct);

        // Apply optional filters in-memory (could be pushed to repository for larger datasets).
        if (roomId is not null) list = list.Where(r => r.RoomId == roomId).ToList();
        if (before is not null) list = list.Where(r => before.Value >= r.From).ToList();
        if (after is not null) list = list.Where(r => after.Value <= r.To).ToList();
        sw.Stop();

        
        return list.Select(r => new ReservationResponse(r.Id, r.From, r.To, r.RoomId, r.DeletedAt)).ToList();
    }
    
    public async Task<bool> IsRoomExistingAsync(Guid roomId, CancellationToken ct = default)
    {
        var baseUrl = config["Services:Assets:BaseUrl"];
        var port = config["Services:Assets:Port"];
        var url = $"{baseUrl}:{port}";
        var roomPath = config["Services:Assets:RoomPath"];

        var sw = System.Diagnostics.Stopwatch.StartNew();
        
        var relativePath = roomPath!.Replace("{id}", roomId.ToString());

        using var client = new HttpClient();
        client.BaseAddress = new Uri(url!);
        try {
            var response = await client.GetAsync(relativePath, ct);
            sw.Stop();

            if (response.StatusCode != System.Net.HttpStatusCode.OK)
                return response.StatusCode == System.Net.HttpStatusCode.OK;

            var json = await response.Content.ReadAsStringAsync(ct);
            try {
                using var doc = JsonDocument.Parse(json);
                if (!doc.RootElement.TryGetProperty("deleted_at", out var deletedAtProp)) return true;
                if (deletedAtProp.ValueKind == JsonValueKind.Null) return true;
                return false;
            }
            catch (JsonException jex) {
              return false;
            }
        }
        catch (Exception ex)
        {
            sw.Stop();
           return false;
        }
    }

    public async Task<bool> IsRoomFree(Guid roomId, DateOnly from, DateOnly to, CancellationToken ct = default)
    {
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
            return conflictingReservations.Count == 0;
        }
        catch (Exception ex)
        {
            sw.Stop();
            return false;
        }
    }

    public async Task<bool> IsRoomFree(Guid roomId, DateOnly from, DateOnly to, Guid? excludeReservationId, CancellationToken ct = default)
    {
        var existingReservations = await ReservationServiceRepository.GetAllAsync(false, ct);
        var conflictingReservations = existingReservations.Where(r => 
            r.RoomId == roomId && 
            r.From < to && 
            r.To > from &&
            r.Id != excludeReservationId
        ).ToList();
        
        return conflictingReservations.Count == 0;
    }

    public async Task<ReservationResponse?> GetReservationByIdAsync(Guid id, CancellationToken ct = default)
    {
        var list = await ReservationServiceRepository.GetAllAsync(true, ct);
        var reservation = list.FirstOrDefault(r => r.Id == id);
        if (reservation == null) return null;
        return new ReservationResponse(reservation.Id, reservation.From, reservation.To, reservation.RoomId, reservation.DeletedAt);
    }

    public async Task<ReservationResponse> CreateReservationAsync(CreateReservationRequest request, CancellationToken ct = default)
    {
        var reservation = new Reservation
        {
            Id = Guid.NewGuid(),
            RoomId = request.RoomId,
            From = request.From,
            To = request.To,
            DeletedAt = null
        };

        db.Reservations.Add(reservation);
        await db.SaveChangesAsync(ct);

        return new ReservationResponse(reservation.Id, reservation.From, reservation.To, reservation.RoomId, reservation.DeletedAt);
    }

    public async Task<ReservationResponse?> UpdateReservationAsync(Guid id, CreateReservationRequest request, CancellationToken ct = default)
    {
        var reservation = await db.Reservations.IgnoreQueryFilters().FirstOrDefaultAsync(r => r.Id == id, ct);
        if (reservation == null) return null;

        reservation.RoomId = request.RoomId;
        reservation.From = request.From;
        reservation.To = request.To;
        reservation.DeletedAt = null;

        await db.SaveChangesAsync(ct);

        return new ReservationResponse(reservation.Id, reservation.From, reservation.To, reservation.RoomId, reservation.DeletedAt);
    }

    public async Task<UpsertReservationResult> ReplaceOrCreateReservationAsync(Guid id, CreateReservationRequest request, CancellationToken ct = default)
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
            return UpsertReservationResult.Failure([.. errors]);
        }

        if (existing != null)
        {
            existing.RoomId = request.RoomId;
            existing.From = request.From;
            existing.To = request.To;
            existing.DeletedAt = null;

            await db.SaveChangesAsync(ct);
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

        return UpsertReservationResult.CreatedResult(new ReservationResponse(newReservation.Id, newReservation.From, newReservation.To, newReservation.RoomId, newReservation.DeletedAt));
    }

    public async Task<DeleteReservationResult> DeleteReservationAsync(Guid id, bool permanent = false, CancellationToken ct = default)
    {
        var reservation = await db.Reservations.IgnoreQueryFilters().FirstOrDefaultAsync(r => r.Id == id, ct);

        if (reservation == null)
        {
            return DeleteReservationResult.Failure(new DeleteError("reservation_not_found", "Reservation not found."));
        }

        if (permanent)
        {
            db.Reservations.Remove(reservation);
            await db.SaveChangesAsync(ct);
            return DeleteReservationResult.HardDeleted();
        }

        if (reservation.DeletedAt != null)
        {
            return DeleteReservationResult.Failure(new DeleteError("reservation_already_deleted", "Reservation is already deleted."));
        }

        reservation.DeletedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(ct);
        return DeleteReservationResult.SoftDeleted();
    }
}
using System.Text.Json;
using Biletado.DTOs;
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
}
public class ReservationStatusService (ReservationServiceRepository ReservationServiceRepository,Contexts.ReservationsDbContext db, IConfiguration config) : IReservationStatusService
{
    public async Task<bool> IsAssetsServiceReadyAsync(CancellationToken ct = default)
    {
        var baseUrl = config["Services:Assets:BaseUrl"];
        var readyPath = config["Services:Assets:ReadyPath"];

        var sw = System.Diagnostics.Stopwatch.StartNew();
        using var client = new HttpClient();
        client.BaseAddress = new Uri(baseUrl!);
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


}
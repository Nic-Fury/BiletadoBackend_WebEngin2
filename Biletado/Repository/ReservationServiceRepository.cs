using Biletado.Contexts;
using Biletado.Domain;
using Microsoft.EntityFrameworkCore;

namespace Biletado.Repository;

public class ReservationServiceRepository(ReservationsDbContext ctx, ILogger<ReservationServiceRepository> logger)
{
    public Task<List<Reservation>> GetAllAsync(bool includeDeleted = false, CancellationToken ct = default)
    {
        logger.LogDebug("Fetching reservations from database: IncludeDeleted={IncludeDeleted}", includeDeleted);
        
        return includeDeleted
            ? ctx.Reservations.IgnoreQueryFilters().AsNoTracking().ToListAsync(ct).ContinueWith(t =>
            {
                logger.LogInformation("Fetched {Count} reservations (including deleted)", t.Result.Count);
                return t.Result;
            }, ct)
            : ctx.Reservations.AsNoTracking().Where(r => r.DeletedAt == null).ToListAsync(ct).ContinueWith(t =>
            {
                logger.LogInformation("Fetched {Count} active reservations", t.Result.Count);
                return t.Result;
            }, ct);
    }

}
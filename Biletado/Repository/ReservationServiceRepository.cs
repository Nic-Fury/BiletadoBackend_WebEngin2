namespace Biletado.Repository;

public class ReservationServiceRepository
{
    public Task<List<Reservation>> GetAllAsync(bool includeDeleted = false, CancellationToken ct = default)
    {
        return includeDeleted
            ? ctx.Reservations.IgnoreQueryFilters().AsNoTracking().ToListAsync(ct).ContinueWith(t =>
            {
                return t.Result;
            }, ct)
            : ctx.Reservations.AsNoTracking().Where(r => r.DeletedAt == null).ToListAsync(ct).ContinueWith(t =>
            {
                return t.Result;
            }, ct);
    }

}
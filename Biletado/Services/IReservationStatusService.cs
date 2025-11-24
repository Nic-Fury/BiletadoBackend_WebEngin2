using Biletado.DTOs;

namespace Biletado.Services;

public interface IReservationService
{
    Task<IReadOnlyCollection<ReservationResponse>> GetAllReservationsAsync(
        bool includeDeleted = false,
        Guid? roomId = null,
        DateOnly? before = null,
        DateOnly? after = null,
        CancellationToken ct = default);
    
    Task<ReservationResponse?> GetReservationByIdAsync(Guid id, CancellationToken ct = default);
    
    Task<ReservationResponse> CreateReservationAsync(CreateReservationRequest request, CancellationToken ct = default);
    
    Task<ReservationResponse?> UpdateReservationAsync(Guid id, CreateReservationRequest request,
        CancellationToken ct = default);

    Task<UpsertReservationResult> ReplaceOrCreateReservationAsync(Guid id, CreateReservationRequest request,
        CancellationToken ct = default);
    
    Task<DeleteReservationResult> DeleteReservationAsync(Guid id, bool permanent = false,
        CancellationToken ct = default);
    
    Task<bool> IsAssetsServiceReadyAsync(CancellationToken ct = default);
    
    Task<bool> IsReservationsDatabaseConnectedAsync(CancellationToken ct = default);
    
    Task<bool> IsRoomExistingAsync(Guid roomId, CancellationToken ct = default);
    
    Task<bool> IsRoomFree(Guid roomId, DateOnly form, DateOnly to, CancellationToken ct);
}


public record DeleteReservationResult(
    bool Success,
    bool WasSoftDeleted,
    bool WasHardDeleted,
    IReadOnlyList<DeleteError> Errors)
{
    public static DeleteReservationResult SoftDeleted()
    {
        return new DeleteReservationResult(true, true, false, []);
    }
    
    public static DeleteReservationResult HardDeleted()
    {
        return new DeleteReservationResult(true, false, true, []);
    }

    public static DeleteReservationResult Failure(params DeleteError[] errors)
    {
        return new DeleteReservationResult(false, false, false, errors);
    }
}

public record UpsertReservationResult(
    bool Success,
    bool Created,
    ReservationResponse? Response,
    IReadOnlyList<DeleteError> Errors)
{
    public static UpsertReservationResult CreatedResult(ReservationResponse response)
    {
        return new UpsertReservationResult(true, true, response, []);
    }
    
    public static UpsertReservationResult UpdatedResult(ReservationResponse response)
    {
        return new UpsertReservationResult(true, false, response, []);
    }
    
    public static UpsertReservationResult Failure(params DeleteError[] errors)
    {
        return new UpsertReservationResult(false, false, null, errors);
    }
}

public record DeleteError(string Code, string Message, string? MoreInfo = null);
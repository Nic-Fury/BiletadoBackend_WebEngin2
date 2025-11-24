using System.Text.Json.Serialization;

namespace Biletado.DTOs;

/// <summary>
/// Request payload to create a reservation for a room.
/// Time range is treated as a half-open interval: <c>From</c> inclusive, <c>To</c> exclusive.
/// </summary>
/// <param name="RoomId">Target room to reserve.</param>
/// <param name="From">Start date (inclusive).</param>
/// <param name="To">End date (exclusive).</param>
public record CreateReservationRequest(
    [property: JsonPropertyName("room_id")]
    Guid RoomId,
    DateOnly From,
    DateOnly To
);
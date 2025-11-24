using System.Text.Json.Serialization;

namespace Biletado.DTOs;

public record ReservationResponse(
    Guid Id,
    DateOnly From,
    DateOnly To,
    [property: JsonPropertyName("room_id")]
    Guid RoomId,
    [property: JsonPropertyName("deleted_at"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    DateTimeOffset? DeletedAt
);
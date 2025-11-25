using System.Text.Json.Serialization;

namespace Biletado.DTOs;

public record ReservationResponse(
    [property: JsonPropertyName("id")]
    Guid Id,
    [property: JsonPropertyName("from")]
    DateOnly From,
    [property: JsonPropertyName("to")]
    DateOnly To,
    [property: JsonPropertyName("room_id")]
    Guid RoomId,
    [property: JsonPropertyName("deleted_at"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    DateTimeOffset? DeletedAt
);
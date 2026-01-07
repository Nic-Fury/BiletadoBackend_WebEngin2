using System.Text.Json.Serialization;

namespace Biletado.DTOs;

public class ReservationListResponse
{
    [JsonPropertyName("reservations")]
    public IReadOnlyCollection<ReservationResponse> Reservations { get; set; } = [];
}

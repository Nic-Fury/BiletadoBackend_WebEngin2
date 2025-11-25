using System.Text.Json.Serialization;

namespace Biletado.DTOs;

public class ErrorResponse
{
    [JsonPropertyName("errors")]
    public List<ErrorDetail> Errors { get; set; } = [];

    [JsonPropertyName("trace")]
    public string? Trace { get; set; }
}

public class ErrorDetail
{
    [JsonPropertyName("code")]
    public string Code { get; set; } = string.Empty;

    [JsonPropertyName("message")]
    public string Message { get; set; } = string.Empty;

    [JsonPropertyName("more_info")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? MoreInfo { get; set; }
}

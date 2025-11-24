using System.Text.Json;


namespace Biletado.Services;

public class ReservationStatusService
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

}
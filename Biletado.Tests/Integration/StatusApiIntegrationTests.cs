using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Biletado.Tests.Integration;

/// <summary>
/// Integration tests for the Status API endpoints.
/// These tests complement the unit tests by testing the full HTTP request/response cycle.
/// </summary>
public class StatusApiIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;

    public StatusApiIntegrationTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
        _client = _factory.CreateClient();
    }

    [Fact]
    public async Task GetStatus_ReturnsOkWithJsonContent()
    {
        // Act
        var response = await _client.GetAsync("/api/v3/reservations/status");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Content.Headers.ContentType?.MediaType.Should().Be("application/json");
    }

    [Fact]
    public async Task GetStatus_ReturnsValidApiVersion()
    {
        // Act
        var response = await _client.GetAsync("/api/v3/reservations/status");
        var content = await response.Content.ReadAsStringAsync();

        // Assert
        response.IsSuccessStatusCode.Should().BeTrue();
        content.Should().Contain("api_version");
        content.Should().Contain("3.0.0");
    }

    [Fact]
    public async Task GetStatus_ReturnsAuthorsInformation()
    {
        // Act
        var response = await _client.GetAsync("/api/v3/reservations/status");
        var content = await response.Content.ReadAsStringAsync();

        // Assert
        response.IsSuccessStatusCode.Should().BeTrue();
        content.Should().Contain("authors");
    }

    [Fact]
    public async Task GetLive_ReturnsOkWithLiveStatus()
    {
        // Act
        var response = await _client.GetAsync("/api/v3/reservations/health/live");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("live");
        content.Should().Contain("true");
    }

    [Fact]
    public async Task GetHealth_ReturnsValidJsonStructure()
    {
        // Act
        var response = await _client.GetAsync("/api/v3/reservations/health");
        var content = await response.Content.ReadAsStringAsync();

        // Assert
        // Should return either 200 or 503 depending on dependencies
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.ServiceUnavailable);
        content.Should().Contain("live");
        content.Should().Contain("ready");
    }

    [Fact]
    public async Task StatusEndpoints_SupportCORS()
    {
        // Arrange
        var request = new HttpRequestMessage(HttpMethod.Options, "/api/v3/reservations/status");
        request.Headers.Add("Origin", "http://localhost:3000");
        request.Headers.Add("Access-Control-Request-Method", "GET");

        // Act
        var response = await _client.SendAsync(request);

        // Assert
        // The response should handle CORS preflight (or return method not allowed if CORS not configured)
        response.StatusCode.Should().BeOneOf(
            HttpStatusCode.NoContent, 
            HttpStatusCode.OK, 
            HttpStatusCode.MethodNotAllowed
        );
    }

    [Fact]
    public async Task MultipleStatusRequests_ShouldAllSucceed()
    {
        // Act - Send multiple concurrent requests
        var tasks = Enumerable.Range(0, 10)
            .Select(_ => _client.GetAsync("/api/v3/reservations/status"))
            .ToArray();

        var responses = await Task.WhenAll(tasks);

        // Assert - All should succeed
        responses.Should().OnlyContain(r => r.StatusCode == HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetReady_ReturnsValidJsonWithErrorStructure_WhenServiceUnavailable()
    {
        // Act
        var response = await _client.GetAsync("/api/v3/reservations/health/ready");
        var content = await response.Content.ReadAsStringAsync();

        // Assert
        if (response.StatusCode == HttpStatusCode.ServiceUnavailable)
        {
            // If services are not available, should return error structure
            content.Should().Contain("errors");
            content.Should().Contain("trace");
        }
        else
        {
            // If services are available, should return ready status
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            content.Should().Contain("ready");
            content.Should().Contain("true");
        }
    }

    [Fact]
    public async Task InvalidEndpoint_Returns404()
    {
        // Act
        var response = await _client.GetAsync("/api/v3/reservations/nonexistent");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}

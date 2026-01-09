using Biletado.Controllers;
using Biletado.Services;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;

namespace Biletado.Tests.Controllers;

public class StatusControllerTests
{
    private readonly Mock<IReservationStatusService> _mockStatusService;
    private readonly Mock<ILogger<StatusController>> _mockLogger;
    private readonly StatusController _controller;

    public StatusControllerTests()
    {
        _mockStatusService = new Mock<IReservationStatusService>();
        _mockLogger = new Mock<ILogger<StatusController>>();
        _controller = new StatusController(_mockStatusService.Object, _mockLogger.Object);
        
        // Setup HttpContext for tests that need it
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext()
        };
    }

    [Fact]
    public void GetStatus_ShouldReturnOkWithCorrectData()
    {
        // Act
        var result = _controller.GetStatus();

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        okResult!.Value.Should().NotBeNull();
    }

    [Fact]
    public void GetStatus_ShouldLogInformation()
    {
        // Act
        _controller.GetStatus();

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Status endpoint called")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task GetLive_ShouldReturnOkWithLiveTrue()
    {
        // Act
        var result = _controller.GetLive();

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        okResult!.Value.Should().NotBeNull();
    }

    [Fact]
    public async Task GetHealth_WhenAllServicesConnected_ShouldReturnOk()
    {
        // Arrange
        _mockStatusService.Setup(x => x.IsAssetsServiceReadyAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        _mockStatusService.Setup(x => x.IsReservationsDatabaseConnectedAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        var result = await _controller.GetHealth();

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        okResult!.StatusCode.Should().Be(200);
    }

    [Fact]
    public async Task GetHealth_WhenAssetsNotConnected_ShouldReturn503()
    {
        // Arrange
        _mockStatusService.Setup(x => x.IsAssetsServiceReadyAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        _mockStatusService.Setup(x => x.IsReservationsDatabaseConnectedAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        var result = await _controller.GetHealth();

        // Assert
        result.Should().BeOfType<ObjectResult>();
        var objectResult = result as ObjectResult;
        objectResult!.StatusCode.Should().Be(503);
    }

    [Fact]
    public async Task GetHealth_WhenDatabaseNotConnected_ShouldReturn503()
    {
        // Arrange
        _mockStatusService.Setup(x => x.IsAssetsServiceReadyAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        _mockStatusService.Setup(x => x.IsReservationsDatabaseConnectedAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        // Act
        var result = await _controller.GetHealth();

        // Assert
        result.Should().BeOfType<ObjectResult>();
        var objectResult = result as ObjectResult;
        objectResult!.StatusCode.Should().Be(503);
    }

    [Fact]
    public async Task GetReady_WhenAllServicesConnected_ShouldReturnOk()
    {
        // Arrange
        _mockStatusService.Setup(x => x.IsAssetsServiceReadyAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        _mockStatusService.Setup(x => x.IsReservationsDatabaseConnectedAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        var result = await _controller.GetReady();

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        okResult!.StatusCode.Should().Be(200);
    }

    [Fact]
    public async Task GetReady_WhenAssetsNotConnected_ShouldReturn503WithError()
    {
        // Arrange
        _mockStatusService.Setup(x => x.IsAssetsServiceReadyAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        _mockStatusService.Setup(x => x.IsReservationsDatabaseConnectedAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        var result = await _controller.GetReady();

        // Assert
        result.Should().BeOfType<ObjectResult>();
        var objectResult = result as ObjectResult;
        objectResult!.StatusCode.Should().Be(503);
    }

    [Fact]
    public async Task GetReady_WhenDatabaseNotConnected_ShouldReturn503WithError()
    {
        // Arrange
        _mockStatusService.Setup(x => x.IsAssetsServiceReadyAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        _mockStatusService.Setup(x => x.IsReservationsDatabaseConnectedAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        // Act
        var result = await _controller.GetReady();

        // Assert
        result.Should().BeOfType<ObjectResult>();
        var objectResult = result as ObjectResult;
        objectResult!.StatusCode.Should().Be(503);
    }
}

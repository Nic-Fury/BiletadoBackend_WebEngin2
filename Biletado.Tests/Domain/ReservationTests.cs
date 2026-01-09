using Biletado.Domain;
using FluentAssertions;

namespace Biletado.Tests.Domain;

public class ReservationTests
{
    [Fact]
    public void Reservation_ShouldInitializeWithDefaultValues()
    {
        // Arrange & Act
        var reservation = new Reservation();

        // Assert
        reservation.Id.Should().BeEmpty();
        reservation.DeletedAt.Should().BeNull();
        reservation.IsDeleted.Should().BeFalse();
    }

    [Fact]
    public void Reservation_ShouldSetProperties()
    {
        // Arrange
        var reservationId = Guid.NewGuid();
        var roomId = Guid.NewGuid();
        var from = DateOnly.FromDateTime(DateTime.Now);
        var to = DateOnly.FromDateTime(DateTime.Now.AddDays(7));

        // Act
        var reservation = new Reservation
        {
            Id = reservationId,
            RoomId = roomId,
            From = from,
            To = to
        };

        // Assert
        reservation.Id.Should().Be(reservationId);
        reservation.RoomId.Should().Be(roomId);
        reservation.From.Should().Be(from);
        reservation.To.Should().Be(to);
        reservation.IsDeleted.Should().BeFalse();
    }

    [Fact]
    public void IsDeleted_WhenDeletedAtIsNull_ShouldReturnFalse()
    {
        // Arrange
        var reservation = new Reservation
        {
            DeletedAt = null
        };

        // Act & Assert
        reservation.IsDeleted.Should().BeFalse();
    }

    [Fact]
    public void IsDeleted_WhenDeletedAtIsSet_ShouldReturnTrue()
    {
        // Arrange
        var reservation = new Reservation
        {
            DeletedAt = DateTimeOffset.UtcNow
        };

        // Act & Assert
        reservation.IsDeleted.Should().BeTrue();
    }

    [Fact]
    public void Reservation_ShouldSupportDateRanges()
    {
        // Arrange
        var from = new DateOnly(2026, 1, 15);
        var to = new DateOnly(2026, 1, 20);

        var reservation = new Reservation
        {
            From = from,
            To = to
        };

        // Act & Assert
        reservation.From.Should().Be(from);
        reservation.To.Should().Be(to);
        reservation.To.Should().BeAfter(reservation.From);
    }

    [Fact]
    public void Reservation_ShouldHandleSoftDelete()
    {
        // Arrange
        var reservation = new Reservation
        {
            Id = Guid.NewGuid(),
            RoomId = Guid.NewGuid(),
            From = DateOnly.FromDateTime(DateTime.Now),
            To = DateOnly.FromDateTime(DateTime.Now.AddDays(1))
        };

        reservation.IsDeleted.Should().BeFalse();

        // Act - Simulate soft delete
        reservation.DeletedAt = DateTimeOffset.UtcNow;

        // Assert
        reservation.IsDeleted.Should().BeTrue();
        reservation.DeletedAt.Should().NotBeNull();
    }
}

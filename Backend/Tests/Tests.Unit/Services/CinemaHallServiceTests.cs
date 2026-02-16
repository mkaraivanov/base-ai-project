using System.Text.Json;
using Application.DTOs.CinemaHalls;
using Application.Services;
using Domain.Entities;
using Domain.ValueObjects;
using FluentAssertions;
using Infrastructure.Repositories;
using Infrastructure.Services;
using Microsoft.Extensions.Logging;
using Moq;

namespace Tests.Unit.Services;

public class CinemaHallServiceTests
{
    private readonly Mock<ICinemaHallRepository> _hallRepositoryMock;
    private readonly Mock<ILogger<CinemaHallService>> _loggerMock;
    private readonly ICinemaHallService _hallService;
    private readonly FakeTimeProvider _timeProvider;

    public CinemaHallServiceTests()
    {
        _hallRepositoryMock = new Mock<ICinemaHallRepository>();
        _loggerMock = new Mock<ILogger<CinemaHallService>>();
        _timeProvider = new FakeTimeProvider(new DateTime(2026, 2, 16, 10, 0, 0, DateTimeKind.Utc));
        _hallService = new CinemaHallService(
            _hallRepositoryMock.Object,
            _loggerMock.Object,
            _timeProvider);
    }

    [Fact]
    public async Task GetAllHallsAsync_WhenActiveOnly_ReturnsOnlyActiveHalls()
    {
        // Arrange
        var seatLayout = new SeatLayout { Rows = 5, SeatsPerRow = 10, Seats = [] };
        var seatLayoutJson = JsonSerializer.Serialize(seatLayout);

        var halls = new List<CinemaHall>
        {
            new() { Id = Guid.NewGuid(), Name = "Hall 1", TotalSeats = 50, SeatLayoutJson = seatLayoutJson, IsActive = true },
            new() { Id = Guid.NewGuid(), Name = "Hall 2", TotalSeats = 40, SeatLayoutJson = seatLayoutJson, IsActive = false }
        };

        _hallRepositoryMock
            .Setup(x => x.GetAllAsync(true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(halls.Where(h => h.IsActive).ToList());

        // Act
        var result = await _hallService.GetAllHallsAsync(true);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.Should().HaveCount(1);
        result.Value![0].Name.Should().Be("Hall 1");
    }

    [Fact]
    public async Task GetHallByIdAsync_WhenHallExists_ReturnsHall()
    {
        // Arrange
        var hallId = Guid.NewGuid();
        var seatLayout = new SeatLayout
        {
            Rows = 10,
            SeatsPerRow = 15,
            Seats = [
                new SeatDefinition { SeatNumber = "A1", Row = 1, Column = 1, SeatType = "Regular", PriceMultiplier = 1.0m, IsAvailable = true }
            ]
        };
        var seatLayoutJson = JsonSerializer.Serialize(seatLayout);

        var hall = new CinemaHall
        {
            Id = hallId,
            Name = "Main Hall",
            TotalSeats = 150,
            SeatLayoutJson = seatLayoutJson,
            IsActive = true,
            CreatedAt = new DateTime(2026, 2, 1, 0, 0, 0, DateTimeKind.Utc)
        };

        _hallRepositoryMock
            .Setup(x => x.GetByIdAsync(hallId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(hall);

        // Act
        var result = await _hallService.GetHallByIdAsync(hallId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.Id.Should().Be(hallId);
        result.Value.Name.Should().Be("Main Hall");
        result.Value.TotalSeats.Should().Be(150);
        result.Value.SeatLayout.Rows.Should().Be(10);
        result.Value.SeatLayout.SeatsPerRow.Should().Be(15);
    }

    [Fact]
    public async Task GetHallByIdAsync_WhenHallDoesNotExist_ReturnsFailure()
    {
        // Arrange
        var hallId = Guid.NewGuid();

        _hallRepositoryMock
            .Setup(x => x.GetByIdAsync(hallId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((CinemaHall?)null);

        // Act
        var result = await _hallService.GetHallByIdAsync(hallId);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be("Cinema hall not found");
    }

    [Fact]
    public async Task CreateHallAsync_WithValidDto_CreatesHall()
    {
        // Arrange
        var seatLayout = new SeatLayout
        {
            Rows = 8,
            SeatsPerRow = 12,
            Seats = [
                new SeatDefinition { SeatNumber = "A1", Row = 1, Column = 1, SeatType = "Regular", PriceMultiplier = 1.0m, IsAvailable = true },
                new SeatDefinition { SeatNumber = "A2", Row = 1, Column = 2, SeatType = "Regular", PriceMultiplier = 1.0m, IsAvailable = true }
            ]
        };

        var dto = new CreateCinemaHallDto(
            Name: "VIP Hall",
            SeatLayout: seatLayout
        );

        _hallRepositoryMock
            .Setup(x => x.CreateAsync(It.IsAny<CinemaHall>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((CinemaHall h, CancellationToken ct) => h);

        // Act
        var result = await _hallService.CreateHallAsync(dto);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.Name.Should().Be("VIP Hall");
        result.Value.TotalSeats.Should().Be(2); // Only available seats
        result.Value.SeatLayout.Rows.Should().Be(8);
        result.Value.IsActive.Should().BeTrue();
        result.Value.CreatedAt.Should().Be(new DateTime(2026, 2, 16, 10, 0, 0, DateTimeKind.Utc));

        _hallRepositoryMock.Verify(
            x => x.CreateAsync(It.IsAny<CinemaHall>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task UpdateHallAsync_WhenHallExists_UpdatesHall()
    {
        // Arrange
        var hallId = Guid.NewGuid();
        var oldSeatLayout = new SeatLayout { Rows = 5, SeatsPerRow = 10, Seats = [] };
        var oldSeatLayoutJson = JsonSerializer.Serialize(oldSeatLayout);

        var existingHall = new CinemaHall
        {
            Id = hallId,
            Name = "Old Hall Name",
            TotalSeats = 50,
            SeatLayoutJson = oldSeatLayoutJson,
            IsActive = true,
            CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc)
        };

        var newSeatLayout = new SeatLayout
        {
            Rows = 10,
            SeatsPerRow = 12,
            Seats = [
                new SeatDefinition { SeatNumber = "A1", Row = 1, Column = 1, SeatType = "Premium", PriceMultiplier = 1.5m, IsAvailable = true }
            ]
        };

        var updateDto = new UpdateCinemaHallDto(
            Name: "Updated Hall Name",
            SeatLayout: newSeatLayout,
            IsActive: true
        );

        _hallRepositoryMock
            .Setup(x => x.GetByIdAsync(hallId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingHall);

        _hallRepositoryMock
            .Setup(x => x.UpdateAsync(It.IsAny<CinemaHall>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((CinemaHall h, CancellationToken ct) => h);

        // Act
        var result = await _hallService.UpdateHallAsync(hallId, updateDto);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.Name.Should().Be("Updated Hall Name");
        result.Value.TotalSeats.Should().Be(1);

        _hallRepositoryMock.Verify(
            x => x.UpdateAsync(It.Is<CinemaHall>(h =>
                h.Id == hallId &&
                h.Name == "Updated Hall Name"),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task UpdateHallAsync_WhenHallDoesNotExist_ReturnsFailure()
    {
        // Arrange
        var hallId = Guid.NewGuid();
        var seatLayout = new SeatLayout { Rows = 5, SeatsPerRow = 10, Seats = [] };
        var updateDto = new UpdateCinemaHallDto(
            Name: "Updated Hall",
            SeatLayout: seatLayout,
            IsActive: true
        );

        _hallRepositoryMock
            .Setup(x => x.GetByIdAsync(hallId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((CinemaHall?)null);

        // Act
        var result = await _hallService.UpdateHallAsync(hallId, updateDto);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be("Cinema hall not found");
    }

    [Fact]
    public async Task DeleteHallAsync_WhenHallExists_DeletesHall()
    {
        // Arrange
        var hallId = Guid.NewGuid();
        var seatLayout = new SeatLayout { Rows = 5, SeatsPerRow = 10, Seats = [] };
        var seatLayoutJson = JsonSerializer.Serialize(seatLayout);

        var existingHall = new CinemaHall
        {
            Id = hallId,
            Name = "Hall to Delete",
            TotalSeats = 50,
            SeatLayoutJson = seatLayoutJson,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        _hallRepositoryMock
            .Setup(x => x.GetByIdAsync(hallId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingHall);

        _hallRepositoryMock
            .Setup(x => x.DeleteAsync(hallId, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _hallService.DeleteHallAsync(hallId);

        // Assert
        result.IsSuccess.Should().BeTrue();

        _hallRepositoryMock.Verify(
            x => x.DeleteAsync(hallId, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task DeleteHallAsync_WhenHallDoesNotExist_ReturnsFailure()
    {
        // Arrange
        var hallId = Guid.NewGuid();

        _hallRepositoryMock
            .Setup(x => x.GetByIdAsync(hallId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((CinemaHall?)null);

        // Act
        var result = await _hallService.DeleteHallAsync(hallId);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be("Cinema hall not found");
    }
}

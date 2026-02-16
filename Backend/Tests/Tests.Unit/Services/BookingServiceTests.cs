using Application.DTOs.Reservations;
using Application.Services;
using Domain.Entities;
using Infrastructure.Repositories;
using Infrastructure.Services;
using Infrastructure.UnitOfWork;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Time.Testing;
using Moq;
using Tests.Unit.Builders;
using Xunit;

namespace Tests.Unit.Services;

public class BookingServiceTests
{
    private readonly Mock<ISeatRepository> _seatRepositoryMock;
    private readonly Mock<IReservationRepository> _reservationRepositoryMock;
    private readonly Mock<IShowtimeRepository> _showtimeRepositoryMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<ILogger<BookingService>> _loggerMock;
    private readonly FakeTimeProvider _timeProvider;
    private readonly IBookingService _bookingService;

    public BookingServiceTests()
    {
        _seatRepositoryMock = new Mock<ISeatRepository>();
        _reservationRepositoryMock = new Mock<IReservationRepository>();
        _showtimeRepositoryMock = new Mock<IShowtimeRepository>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _loggerMock = new Mock<ILogger<BookingService>>();
        _timeProvider = new FakeTimeProvider(new DateTime(2024, 1, 15, 10, 0, 0, DateTimeKind.Utc));

        _bookingService = new BookingService(
            _seatRepositoryMock.Object,
            _reservationRepositoryMock.Object,
            _showtimeRepositoryMock.Object,
            _unitOfWorkMock.Object,
            _loggerMock.Object,
            _timeProvider
        );
    }

    #region GetSeatAvailabilityAsync Tests

    [Fact]
    public async Task GetSeatAvailabilityAsync_ValidShowtime_ReturnsCorrectGrouping()
    {
        // Arrange
        var showtimeId = Guid.NewGuid();
        var seats = new List<Seat>
        {
            new SeatBuilder().WithShowtimeId(showtimeId).WithSeatNumber("A1").AsAvailable().Build(),
            new SeatBuilder().WithShowtimeId(showtimeId).WithSeatNumber("A2").WithStatus(SeatStatus.Reserved).Build(),
            new SeatBuilder().WithShowtimeId(showtimeId).WithSeatNumber("A3").AsBooked().Build()
        };

        _seatRepositoryMock
            .Setup(x => x.GetByShowtimeIdAsync(showtimeId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(seats);

        // Act
        var result = await _bookingService.GetSeatAvailabilityAsync(showtimeId);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Single(result.Value.AvailableSeats);
        Assert.Single(result.Value.ReservedSeats);
        Assert.Single(result.Value.BookedSeats);
        Assert.Equal(3, result.Value.TotalSeats);
    }

    [Fact]
    public async Task GetSeatAvailabilityAsync_NoSeats_ReturnsEmptyLists()
    {
        // Arrange
        var showtimeId = Guid.NewGuid();
        _seatRepositoryMock
            .Setup(x => x.GetByShowtimeIdAsync(showtimeId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Seat>());

        // Act
        var result = await _bookingService.GetSeatAvailabilityAsync(showtimeId);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Empty(result.Value.AvailableSeats);
        Assert.Empty(result.Value.ReservedSeats);
        Assert.Empty(result.Value.BookedSeats);
        Assert.Equal(0, result.Value.TotalSeats);
    }

    [Fact]
    public async Task GetSeatAvailabilityAsync_RepositoryThrows_ReturnsFailure()
    {
        // Arrange
        var showtimeId = Guid.NewGuid();
        _seatRepositoryMock
            .Setup(x => x.GetByShowtimeIdAsync(showtimeId, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Database error"));

        // Act
        var result = await _bookingService.GetSeatAvailabilityAsync(showtimeId);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("Failed to retrieve seat availability", result.Error);
    }

    #endregion

    #region CreateReservationAsync Tests

    [Fact]
    public async Task CreateReservationAsync_HappyPath_CreatesReservation()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var showtimeId = Guid.NewGuid();
        var dto = new CreateReservationDto(showtimeId, new List<string> { "A1", "A2" });

        var showtime = new Showtime
        {
            Id = showtimeId,
            StartTime = _timeProvider.GetUtcNow().AddHours(2).DateTime,
            MovieId = Guid.NewGuid(),
            CinemaHallId = Guid.NewGuid(),
            BasePrice = 10.00m,
            IsActive = true
        };

        var seats = new List<Seat>
        {
            new SeatBuilder().WithShowtimeId(showtimeId).WithSeatNumber("A1").WithPrice(10m).AsAvailable().Build(),
            new SeatBuilder().WithShowtimeId(showtimeId).WithSeatNumber("A2").WithPrice(12m).AsAvailable().Build()
        };

        _showtimeRepositoryMock
            .Setup(x => x.GetByIdAsync(showtimeId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(showtime);

        _seatRepositoryMock
            .Setup(x => x.GetByShowtimeAndNumbersAsync(showtimeId, dto.SeatNumbers, It.IsAny<CancellationToken>()))
            .ReturnsAsync(seats);

        // Act
        var result = await _bookingService.CreateReservationAsync(userId, dto);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal(showtimeId, result.Value.ShowtimeId);
        Assert.Equal(22m, result.Value.TotalAmount);
        Assert.Equal(_timeProvider.GetUtcNow().AddMinutes(5).DateTime, result.Value.ExpiresAt);
        Assert.Equal("Pending", result.Value.Status);

        _unitOfWorkMock.Verify(x => x.BeginTransactionAsync(It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWorkMock.Verify(x => x.CommitTransactionAsync(It.IsAny<CancellationToken>()), Times.Once);
        _seatRepositoryMock.Verify(x => x.UpdateRangeAsync(It.IsAny<List<Seat>>(), It.IsAny<CancellationToken>()), Times.Exactly(2));
        _reservationRepositoryMock.Verify(x => x.CreateAsync(It.IsAny<Reservation>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateReservationAsync_ShowtimeNotFound_ReturnsFailure()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var showtimeId = Guid.NewGuid();
        var dto = new CreateReservationDto(showtimeId, new List<string> { "A1" });

        _showtimeRepositoryMock
            .Setup(x => x.GetByIdAsync(showtimeId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Showtime?)null);

        // Act
        var result = await _bookingService.CreateReservationAsync(userId, dto);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("Showtime not found", result.Error);
        _unitOfWorkMock.Verify(x => x.RollbackTransactionAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateReservationAsync_PastShowtime_ReturnsFailure()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var showtimeId = Guid.NewGuid();
        var dto = new CreateReservationDto(showtimeId, new List<string> { "A1" });

        var showtime = new Showtime
        {
            Id = showtimeId,
            StartTime = _timeProvider.GetUtcNow().AddHours(-1).DateTime, // In the past
            MovieId = Guid.NewGuid(),
            CinemaHallId = Guid.NewGuid(),
            BasePrice = 10.00m,
            IsActive = true
        };

        _showtimeRepositoryMock
            .Setup(x => x.GetByIdAsync(showtimeId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(showtime);

        // Act
        var result = await _bookingService.CreateReservationAsync(userId, dto);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("Cannot book past or ongoing showtimes", result.Error);
        _unitOfWorkMock.Verify(x => x.RollbackTransactionAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateReservationAsync_MissingSeats_ReturnsFailure()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var showtimeId = Guid.NewGuid();
        var dto = new CreateReservationDto(showtimeId, new List<string> { "A1", "A2", "A3" });

        var showtime = new Showtime
        {
            Id = showtimeId,
            StartTime = _timeProvider.GetUtcNow().AddHours(2).DateTime,
            MovieId = Guid.NewGuid(),
            CinemaHallId = Guid.NewGuid(),
            BasePrice = 10.00m,
            IsActive = true
        };

        var seats = new List<Seat>
        {
            new SeatBuilder().WithShowtimeId(showtimeId).WithSeatNumber("A1").AsAvailable().Build()
            // A2 and A3 missing
        };

        _showtimeRepositoryMock
            .Setup(x => x.GetByIdAsync(showtimeId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(showtime);

        _seatRepositoryMock
            .Setup(x => x.GetByShowtimeAndNumbersAsync(showtimeId, dto.SeatNumbers, It.IsAny<CancellationToken>()))
            .ReturnsAsync(seats);

        // Act
        var result = await _bookingService.CreateReservationAsync(userId, dto);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Contains("Seats not found", result.Error!);
        _unitOfWorkMock.Verify(x => x.RollbackTransactionAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateReservationAsync_UnavailableSeats_ReturnsFailure()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var showtimeId = Guid.NewGuid();
        var dto = new CreateReservationDto(showtimeId, new List<string> { "A1", "A2" });

        var showtime = new Showtime
        {
            Id = showtimeId,
            StartTime = _timeProvider.GetUtcNow().AddHours(2).DateTime,
            MovieId = Guid.NewGuid(),
            CinemaHallId = Guid.NewGuid(),
            BasePrice = 10.00m,
            IsActive = true
        };

        var seats = new List<Seat>
        {
            new SeatBuilder().WithShowtimeId(showtimeId).WithSeatNumber("A1").AsAvailable().Build(),
            new SeatBuilder().WithShowtimeId(showtimeId).WithSeatNumber("A2").WithStatus(SeatStatus.Reserved).Build()
        };

        _showtimeRepositoryMock
            .Setup(x => x.GetByIdAsync(showtimeId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(showtime);

        _seatRepositoryMock
            .Setup(x => x.GetByShowtimeAndNumbersAsync(showtimeId, dto.SeatNumbers, It.IsAny<CancellationToken>()))
            .ReturnsAsync(seats);

        // Act
        var result = await _bookingService.CreateReservationAsync(userId, dto);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Contains("Seats not available", result.Error!);
        _unitOfWorkMock.Verify(x => x.RollbackTransactionAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateReservationAsync_ConcurrencyException_ReturnsUserFriendlyError()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var showtimeId = Guid.NewGuid();
        var dto = new CreateReservationDto(showtimeId, new List<string> { "A1" });

        var showtime = new Showtime
        {
            Id = showtimeId,
            StartTime = _timeProvider.GetUtcNow().AddHours(2).DateTime,
            MovieId = Guid.NewGuid(),
            CinemaHallId = Guid.NewGuid(),
            BasePrice = 10.00m,
            IsActive = true
        };

        var seats = new List<Seat>
        {
            new SeatBuilder().WithShowtimeId(showtimeId).WithSeatNumber("A1").AsAvailable().Build()
        };

        _showtimeRepositoryMock
            .Setup(x => x.GetByIdAsync(showtimeId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(showtime);

        _seatRepositoryMock
            .Setup(x => x.GetByShowtimeAndNumbersAsync(showtimeId, dto.SeatNumbers, It.IsAny<CancellationToken>()))
            .ReturnsAsync(seats);

        _unitOfWorkMock
            .Setup(x => x.CommitTransactionAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new DbUpdateConcurrencyException());

        // Act
        var result = await _bookingService.CreateReservationAsync(userId, dto);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("Selected seats are no longer available. Please try again.", result.Error);
        _unitOfWorkMock.Verify(x => x.RollbackTransactionAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateReservationAsync_GeneralException_ReturnsGenericError()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var showtimeId = Guid.NewGuid();
        var dto = new CreateReservationDto(showtimeId, new List<string> { "A1" });

        _showtimeRepositoryMock
            .Setup(x => x.GetByIdAsync(showtimeId, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Database error"));

        // Act
        var result = await _bookingService.CreateReservationAsync(userId, dto);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("Failed to create reservation", result.Error);
        _unitOfWorkMock.Verify(x => x.RollbackTransactionAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    #endregion

    #region CancelReservationAsync Tests

    [Fact]
    public async Task CancelReservationAsync_HappyPath_CancelsReservation()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var reservationId = Guid.NewGuid();

        var reservation = new ReservationBuilder()
            .WithId(reservationId)
            .WithUserId(userId)
            .AsPending()
            .Build();

        var seats = new List<Seat>
        {
            new SeatBuilder().WithStatus(SeatStatus.Reserved).Build(),
            new SeatBuilder().WithStatus(SeatStatus.Reserved).Build()
        };

        _reservationRepositoryMock
            .Setup(x => x.GetByIdAsync(reservationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(reservation);

        _seatRepositoryMock
            .Setup(x => x.GetByReservationIdAsync(reservationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(seats);

        // Act
        var result = await _bookingService.CancelReservationAsync(userId, reservationId);

        // Assert
        Assert.True(result.IsSuccess);
        _unitOfWorkMock.Verify(x => x.BeginTransactionAsync(It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWorkMock.Verify(x => x.CommitTransactionAsync(It.IsAny<CancellationToken>()), Times.Once);
        _seatRepositoryMock.Verify(x => x.UpdateRangeAsync(It.IsAny<List<Seat>>(), It.IsAny<CancellationToken>()), Times.Once);
        _reservationRepositoryMock.Verify(x => x.UpdateAsync(It.IsAny<Reservation>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CancelReservationAsync_ReservationNotFound_ReturnsFailure()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var reservationId = Guid.NewGuid();

        _reservationRepositoryMock
            .Setup(x => x.GetByIdAsync(reservationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Reservation?)null);

        // Act
        var result = await _bookingService.CancelReservationAsync(userId, reservationId);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("Reservation not found", result.Error);
        _unitOfWorkMock.Verify(x => x.RollbackTransactionAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CancelReservationAsync_UnauthorizedUser_ReturnsFailure()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var otherUserId = Guid.NewGuid();
        var reservationId = Guid.NewGuid();

        var reservation = new ReservationBuilder()
            .WithId(reservationId)
            .WithUserId(otherUserId) // Different user
            .AsPending()
            .Build();

        _reservationRepositoryMock
            .Setup(x => x.GetByIdAsync(reservationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(reservation);

        // Act
        var result = await _bookingService.CancelReservationAsync(userId, reservationId);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("Unauthorized", result.Error);
        _unitOfWorkMock.Verify(x => x.RollbackTransactionAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CancelReservationAsync_AlreadyCancelled_ReturnsFailure()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var reservationId = Guid.NewGuid();

        var reservation = new ReservationBuilder()
            .WithId(reservationId)
            .WithUserId(userId)
            .AsCancelled()
            .Build();

        _reservationRepositoryMock
            .Setup(x => x.GetByIdAsync(reservationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(reservation);

        // Act
        var result = await _bookingService.CancelReservationAsync(userId, reservationId);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("Reservation cannot be cancelled", result.Error);
        _unitOfWorkMock.Verify(x => x.RollbackTransactionAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CancelReservationAsync_AlreadyExpired_ReturnsFailure()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var reservationId = Guid.NewGuid();

        var reservation = new ReservationBuilder()
            .WithId(reservationId)
            .WithUserId(userId)
            .AsExpired()
            .Build();

        _reservationRepositoryMock
            .Setup(x => x.GetByIdAsync(reservationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(reservation);

        // Act
        var result = await _bookingService.CancelReservationAsync(userId, reservationId);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("Reservation cannot be cancelled", result.Error);
    }

    [Fact]
    public async Task CancelReservationAsync_Exception_ReturnsFailure()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var reservationId = Guid.NewGuid();

        _reservationRepositoryMock
            .Setup(x => x.GetByIdAsync(reservationId, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Database error"));

        // Act
        var result = await _bookingService.CancelReservationAsync(userId, reservationId);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("Failed to cancel reservation", result.Error);
        _unitOfWorkMock.Verify(x => x.RollbackTransactionAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    #endregion
}

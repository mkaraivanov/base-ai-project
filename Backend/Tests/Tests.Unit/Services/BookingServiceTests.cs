using Application.DTOs.Bookings;
using Application.DTOs.Payments;
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
    private static readonly Guid AdultTicketTypeId = new Guid("a1b2c3d4-e5f6-7890-abcd-ef1234567890");
    private static readonly TicketType AdultTicketType = new TicketType
    {
        Id = new Guid("a1b2c3d4-e5f6-7890-abcd-ef1234567890"),
        Name = "Adult",
        Description = "Standard adult ticket",
        PriceModifier = 1.0m,
        IsActive = true,
        SortOrder = 1
    };

    private readonly Mock<ISeatRepository> _seatRepositoryMock;
    private readonly Mock<IReservationRepository> _reservationRepositoryMock;
    private readonly Mock<IShowtimeRepository> _showtimeRepositoryMock;
    private readonly Mock<IPaymentService> _paymentServiceMock;
    private readonly Mock<IPaymentRepository> _paymentRepositoryMock;
    private readonly Mock<IBookingRepository> _bookingRepositoryMock;
    private readonly Mock<ITicketTypeRepository> _ticketTypeRepositoryMock;
    private readonly Mock<IReservationTicketRepository> _reservationTicketRepositoryMock;
    private readonly Mock<IBookingTicketRepository> _bookingTicketRepositoryMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<ILoyaltyService> _loyaltyServiceMock;
    private readonly Mock<ILogger<BookingService>> _loggerMock;
    private readonly FakeTimeProvider _timeProvider;
    private readonly IBookingService _bookingService;

    private static SeatSelectionDto SeatDto(string seatNumber) => new(seatNumber, new Guid("a1b2c3d4-e5f6-7890-abcd-ef1234567890"));

    public BookingServiceTests()
    {
        _seatRepositoryMock = new Mock<ISeatRepository>();
        _reservationRepositoryMock = new Mock<IReservationRepository>();
        _showtimeRepositoryMock = new Mock<IShowtimeRepository>();
        _paymentServiceMock = new Mock<IPaymentService>();
        _paymentRepositoryMock = new Mock<IPaymentRepository>();
        _bookingRepositoryMock = new Mock<IBookingRepository>();
        _ticketTypeRepositoryMock = new Mock<ITicketTypeRepository>();
        _reservationTicketRepositoryMock = new Mock<IReservationTicketRepository>();
        _bookingTicketRepositoryMock = new Mock<IBookingTicketRepository>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _loyaltyServiceMock = new Mock<ILoyaltyService>();
        _loggerMock = new Mock<ILogger<BookingService>>();
        _timeProvider = new FakeTimeProvider(new DateTime(2024, 1, 15, 10, 0, 0, DateTimeKind.Utc));

        _ticketTypeRepositoryMock
            .Setup(x => x.GetByIdsAsync(It.IsAny<IEnumerable<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<TicketType> { AdultTicketType });

        _reservationTicketRepositoryMock
            .Setup(x => x.CreateRangeAsync(It.IsAny<IEnumerable<ReservationTicket>>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _reservationTicketRepositoryMock
            .Setup(x => x.GetByReservationIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ReservationTicket>());

        _bookingTicketRepositoryMock
            .Setup(x => x.CreateRangeAsync(It.IsAny<IEnumerable<BookingTicket>>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _bookingTicketRepositoryMock
            .Setup(x => x.GetByBookingIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<BookingTicket>());

        _bookingTicketRepositoryMock
            .Setup(x => x.GetByBookingIdsAsync(It.IsAny<IEnumerable<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<Guid, List<BookingTicket>>());

        _loyaltyServiceMock
            .Setup(x => x.AddStampAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _loyaltyServiceMock
            .Setup(x => x.RemoveStampAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _bookingService = new BookingService(
            _seatRepositoryMock.Object,
            _reservationRepositoryMock.Object,
            _showtimeRepositoryMock.Object,
            _paymentServiceMock.Object,
            _paymentRepositoryMock.Object,
            _bookingRepositoryMock.Object,
            _ticketTypeRepositoryMock.Object,
            _reservationTicketRepositoryMock.Object,
            _bookingTicketRepositoryMock.Object,
            _unitOfWorkMock.Object,
            _loyaltyServiceMock.Object,
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

        _showtimeRepositoryMock
            .Setup(x => x.GetByIdAsync(showtimeId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Showtime {
                Id = showtimeId,
                StartTime = _timeProvider.GetUtcNow().AddHours(2).DateTime,
                MovieId = Guid.NewGuid(),
                CinemaHallId = Guid.NewGuid(),
                BasePrice = 10m,
                IsActive = true
            });

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

        _showtimeRepositoryMock
            .Setup(x => x.GetByIdAsync(showtimeId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Showtime {
                Id = showtimeId,
                StartTime = _timeProvider.GetUtcNow().AddHours(2).DateTime,
                MovieId = Guid.NewGuid(),
                CinemaHallId = Guid.NewGuid(),
                BasePrice = 10m,
                IsActive = true
            });

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

        _showtimeRepositoryMock
            .Setup(x => x.GetByIdAsync(showtimeId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Showtime {
                Id = showtimeId,
                StartTime = _timeProvider.GetUtcNow().AddHours(2).DateTime,
                MovieId = Guid.NewGuid(),
                CinemaHallId = Guid.NewGuid(),
                BasePrice = 10m,
                IsActive = true
            });

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
        var dto = new CreateReservationDto(showtimeId, new List<SeatSelectionDto> { SeatDto("A1"), SeatDto("A2") });

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
            .Setup(x => x.GetByShowtimeAndNumbersAsync(showtimeId, It.IsAny<IReadOnlyList<string>>(), It.IsAny<CancellationToken>()))
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
        var dto = new CreateReservationDto(showtimeId, new List<SeatSelectionDto> { SeatDto("A1") });

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
        var dto = new CreateReservationDto(showtimeId, new List<SeatSelectionDto> { SeatDto("A1") });

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
        var dto = new CreateReservationDto(showtimeId, new List<SeatSelectionDto> { SeatDto("A1"), SeatDto("A2"), SeatDto("A3") });

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
            .Setup(x => x.GetByShowtimeAndNumbersAsync(showtimeId, It.IsAny<IReadOnlyList<string>>(), It.IsAny<CancellationToken>()))
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
        var dto = new CreateReservationDto(showtimeId, new List<SeatSelectionDto> { SeatDto("A1"), SeatDto("A2") });

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
            .Setup(x => x.GetByShowtimeAndNumbersAsync(showtimeId, It.IsAny<IReadOnlyList<string>>(), It.IsAny<CancellationToken>()))
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
        var dto = new CreateReservationDto(showtimeId, new List<SeatSelectionDto> { SeatDto("A1") });

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
            .Setup(x => x.GetByShowtimeAndNumbersAsync(showtimeId, It.IsAny<IReadOnlyList<string>>(), It.IsAny<CancellationToken>()))
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
        var dto = new CreateReservationDto(showtimeId, new List<SeatSelectionDto> { SeatDto("A1") });

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

    #region ConfirmBookingAsync Tests

    [Fact]
    public async Task ConfirmBookingAsync_HappyPath_CreatesBooking()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var reservationId = Guid.NewGuid();
        var showtimeId = Guid.NewGuid();
        var dto = new ConfirmBookingDto(
            reservationId,
            "CreditCard",
            "4111111111111111",
            "John Doe",
            "12/25",
            "123"
        );

        var reservation = new Reservation
        {
            Id = reservationId,
            UserId = userId,
            ShowtimeId = showtimeId,
            SeatNumbers = new List<string> { "A1", "A2" },
            TotalAmount = 22m,
            ExpiresAt = _timeProvider.GetUtcNow().AddMinutes(3).DateTime,
            Status = ReservationStatus.Pending,
            CreatedAt = _timeProvider.GetUtcNow().DateTime
        };

        var showtime = new Showtime
        {
            Id = showtimeId,
            StartTime = _timeProvider.GetUtcNow().AddHours(2).DateTime,
            MovieId = Guid.NewGuid(),
            CinemaHallId = Guid.NewGuid(),
            BasePrice = 10m,
            IsActive = true,
            Movie = new Movie { Id = Guid.NewGuid(), Title = "Test Movie", Genre = "Action", Rating = "PG-13", DurationMinutes = 120, IsActive = true },
            CinemaHall = new CinemaHall { Id = Guid.NewGuid(), Name = "Hall 1", TotalSeats = 100, IsActive = true, SeatLayoutJson = "{}" }
        };

        var paymentResult = new PaymentResultDto(
            Guid.NewGuid(),
            "TXN-12345678",
            "Completed",
            22m,
            _timeProvider.GetUtcNow().DateTime
        );

        _reservationRepositoryMock
            .Setup(x => x.GetByIdAsync(reservationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(reservation);

        _paymentServiceMock
            .Setup(x => x.ProcessPaymentAsync(It.IsAny<ProcessPaymentDto>(), 22m, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Domain.Common.Result<PaymentResultDto>.Success(paymentResult));

        _showtimeRepositoryMock
            .Setup(x => x.GetByIdAsync(showtimeId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(showtime);

        _seatRepositoryMock
            .Setup(x => x.GetByReservationIdAsync(reservationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Seat>
            {
                new SeatBuilder().WithShowtimeId(showtimeId).WithSeatNumber("A1").AsReserved(reservationId, DateTime.UtcNow.AddMinutes(10)).Build(),
                new SeatBuilder().WithShowtimeId(showtimeId).WithSeatNumber("A2").AsReserved(reservationId, DateTime.UtcNow.AddMinutes(10)).Build()
            });

        // Act
        var result = await _bookingService.ConfirmBookingAsync(userId, dto);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.NotEmpty(result.Value.BookingNumber);
        Assert.Equal(showtimeId, result.Value.ShowtimeId);
        Assert.Equal(22m, result.Value.TotalAmount);
        Assert.Equal("Confirmed", result.Value.Status);

        _unitOfWorkMock.Verify(x => x.BeginTransactionAsync(It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWorkMock.Verify(x => x.CommitTransactionAsync(It.IsAny<CancellationToken>()), Times.Once);
        _paymentServiceMock.Verify(x => x.ProcessPaymentAsync(It.IsAny<ProcessPaymentDto>(), 22m, It.IsAny<CancellationToken>()), Times.Once);
        _paymentRepositoryMock.Verify(x => x.CreateAsync(It.IsAny<Payment>(), It.IsAny<CancellationToken>()), Times.Once);
        _bookingRepositoryMock.Verify(x => x.CreateAsync(It.IsAny<Booking>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ConfirmBookingAsync_ReservationNotFound_ReturnsFailure()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var reservationId = Guid.NewGuid();
        var dto = new ConfirmBookingDto(reservationId, "CreditCard", "4111111111111111", "John Doe", "12/25", "123");

        _reservationRepositoryMock
            .Setup(x => x.GetByIdAsync(reservationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Reservation?)null);

        // Act
        var result = await _bookingService.ConfirmBookingAsync(userId, dto);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("Reservation not found", result.Error);
        _unitOfWorkMock.Verify(x => x.RollbackTransactionAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ConfirmBookingAsync_Unauthorized_ReturnsFailure()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var differentUserId = Guid.NewGuid();
        var reservationId = Guid.NewGuid();
        var dto = new ConfirmBookingDto(reservationId, "CreditCard", "4111111111111111", "John Doe", "12/25", "123");

        var reservation = new Reservation
        {
            Id = reservationId,
            UserId = differentUserId, // Different user
            ShowtimeId = Guid.NewGuid(),
            SeatNumbers = new List<string> { "A1" },
            TotalAmount = 10m,
            ExpiresAt = _timeProvider.GetUtcNow().AddMinutes(3).DateTime,
            Status = ReservationStatus.Pending,
            CreatedAt = _timeProvider.GetUtcNow().DateTime
        };

        _reservationRepositoryMock
            .Setup(x => x.GetByIdAsync(reservationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(reservation);

        // Act
        var result = await _bookingService.ConfirmBookingAsync(userId, dto);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("Unauthorized", result.Error);
        _unitOfWorkMock.Verify(x => x.RollbackTransactionAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ConfirmBookingAsync_ExpiredReservation_ReturnsFailure()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var reservationId = Guid.NewGuid();
        var dto = new ConfirmBookingDto(reservationId, "CreditCard", "4111111111111111", "John Doe", "12/25", "123");

        var reservation = new Reservation
        {
            Id = reservationId,
            UserId = userId,
            ShowtimeId = Guid.NewGuid(),
            SeatNumbers = new List<string> { "A1" },
            TotalAmount = 10m,
            ExpiresAt = _timeProvider.GetUtcNow().AddMinutes(-1).DateTime, // Expired
            Status = ReservationStatus.Pending,
            CreatedAt = _timeProvider.GetUtcNow().DateTime
        };

        _reservationRepositoryMock
            .Setup(x => x.GetByIdAsync(reservationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(reservation);

        // Act
        var result = await _bookingService.ConfirmBookingAsync(userId, dto);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("Reservation has expired", result.Error);
        _unitOfWorkMock.Verify(x => x.RollbackTransactionAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ConfirmBookingAsync_PaymentFailure_ReturnsFailure()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var reservationId = Guid.NewGuid();
        var dto = new ConfirmBookingDto(reservationId, "CreditCard", "0000111122223333", "John Doe", "12/25", "123");

        var reservation = new Reservation
        {
            Id = reservationId,
            UserId = userId,
            ShowtimeId = Guid.NewGuid(),
            SeatNumbers = new List<string> { "A1" },
            TotalAmount = 10m,
            ExpiresAt = _timeProvider.GetUtcNow().AddMinutes(3).DateTime,
            Status = ReservationStatus.Pending,
            CreatedAt = _timeProvider.GetUtcNow().DateTime
        };

        _reservationRepositoryMock
            .Setup(x => x.GetByIdAsync(reservationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(reservation);

        _paymentServiceMock
            .Setup(x => x.ProcessPaymentAsync(It.IsAny<ProcessPaymentDto>(), 10m, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Domain.Common.Result<PaymentResultDto>.Failure("Payment declined"));

        // Act
        var result = await _bookingService.ConfirmBookingAsync(userId, dto);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Contains("Payment failed", result.Error);
        _unitOfWorkMock.Verify(x => x.RollbackTransactionAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    #endregion

    #region CancelBookingAsync Tests

    [Fact]
    public async Task CancelBookingAsync_HappyPath_CancelsBooking()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var bookingId = Guid.NewGuid();
        var showtimeId = Guid.NewGuid();

        var booking = new Booking
        {
            Id = bookingId,
            BookingNumber = "BK-240115-ABC12",
            UserId = userId,
            ShowtimeId = showtimeId,
            SeatNumbers = new List<string> { "A1", "A2" },
            TotalAmount = 22m,
            Status = BookingStatus.Confirmed,
            PaymentId = Guid.NewGuid(),
            BookedAt = _timeProvider.GetUtcNow().DateTime
        };

        var showtime = new Showtime
        {
            Id = showtimeId,
            StartTime = _timeProvider.GetUtcNow().AddHours(2).DateTime, // Future showtime
            MovieId = Guid.NewGuid(),
            CinemaHallId = Guid.NewGuid(),
            BasePrice = 10m,
            IsActive = true,
            Movie = new Movie { Id = Guid.NewGuid(), Title = "Test Movie", Genre = "Action", Rating = "PG-13", DurationMinutes = 120, IsActive = true },
            CinemaHall = new CinemaHall { Id = Guid.NewGuid(), Name = "Hall 1", TotalSeats = 100, IsActive = true, SeatLayoutJson = "{}" }
        };

        _bookingRepositoryMock
            .Setup(x => x.GetByIdAsync(bookingId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(booking);

        _showtimeRepositoryMock
            .Setup(x => x.GetByIdAsync(showtimeId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(showtime);

        _paymentServiceMock
            .Setup(x => x.RefundPaymentAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Domain.Common.Result<PaymentResultDto>.Success(new PaymentResultDto(
                booking.PaymentId.Value,
                "RFN-12345678",
                "Refunded",
                0m,
                _timeProvider.GetUtcNow().DateTime
            )));

        _seatRepositoryMock
            .Setup(x => x.GetByShowtimeAndNumbersAsync(showtimeId, booking.SeatNumbers, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Seat>
            {
                new SeatBuilder().WithShowtimeId(showtimeId).WithSeatNumber("A1").AsBooked().Build(),
                new SeatBuilder().WithShowtimeId(showtimeId).WithSeatNumber("A2").AsBooked().Build()
            });

        // Act
        var result = await _bookingService.CancelBookingAsync(userId, bookingId);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal("Cancelled", result.Value.Status);
        Assert.NotNull(result.Value);

        _unitOfWorkMock.Verify(x => x.BeginTransactionAsync(It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWorkMock.Verify(x => x.CommitTransactionAsync(It.IsAny<CancellationToken>()), Times.Once);
        _paymentServiceMock.Verify(x => x.RefundPaymentAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Once);
        _bookingRepositoryMock.Verify(x => x.UpdateAsync(It.IsAny<Booking>(), It.IsAny<CancellationToken>()), Times.Once);
        _seatRepositoryMock.Verify(x => x.UpdateRangeAsync(It.IsAny<List<Seat>>(), It.IsAny<CancellationToken>()), Times.Once);
        _loyaltyServiceMock.Verify(x => x.RemoveStampAsync(userId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CancelBookingAsync_BookingNotFound_ReturnsFailure()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var bookingId = Guid.NewGuid();

        _bookingRepositoryMock
            .Setup(x => x.GetByIdAsync(bookingId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Booking?)null);

        // Act
        var result = await _bookingService.CancelBookingAsync(userId, bookingId);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("Booking not found", result.Error);
        _unitOfWorkMock.Verify(x => x.RollbackTransactionAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CancelBookingAsync_PastShowtime_ReturnsFailure()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var bookingId = Guid.NewGuid();
        var showtimeId = Guid.NewGuid();

        var booking = new Booking
        {
            Id = bookingId,
            BookingNumber = "BK-240115-ABC12",
            UserId = userId,
            ShowtimeId = showtimeId,
            SeatNumbers = new List<string> { "A1" },
            TotalAmount = 10m,
            Status = BookingStatus.Confirmed,
            PaymentId = Guid.NewGuid(),
            BookedAt = _timeProvider.GetUtcNow().DateTime
        };

        var showtime = new Showtime
        {
            Id = showtimeId,
            StartTime = _timeProvider.GetUtcNow().AddHours(-1).DateTime, // Past showtime
            MovieId = Guid.NewGuid(),
            CinemaHallId = Guid.NewGuid(),
            BasePrice = 10m,
            IsActive = true
        };

        _bookingRepositoryMock
            .Setup(x => x.GetByIdAsync(bookingId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(booking);

        _showtimeRepositoryMock
            .Setup(x => x.GetByIdAsync(showtimeId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(showtime);

        // Act
        var result = await _bookingService.CancelBookingAsync(userId, bookingId);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("Cannot cancel past or ongoing showtimes", result.Error);
        _unitOfWorkMock.Verify(x => x.RollbackTransactionAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    #endregion

    #region GetMyBookingsAsync Tests

    [Fact]
    public async Task GetMyBookingsAsync_ReturnsUserBookings()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var bookings = new List<Booking>
        {
            new Booking
            {
                Id = Guid.NewGuid(),
                BookingNumber = "BK-240115-ABC12",
                UserId = userId,
                ShowtimeId = Guid.NewGuid(),
                SeatNumbers = new List<string> { "A1", "A2" },
                TotalAmount = 22m,
                Status = BookingStatus.Confirmed,
                BookedAt = _timeProvider.GetUtcNow().DateTime,
                Showtime = new Showtime
                {
                    Id = Guid.NewGuid(),
                    StartTime = _timeProvider.GetUtcNow().AddHours(2).DateTime,
                    MovieId = Guid.NewGuid(),
                    CinemaHallId = Guid.NewGuid(),
                    BasePrice = 10m,
                    IsActive = true,
                    Movie = new Movie { Id = Guid.NewGuid(), Title = "Test Movie", Genre = "Action", Rating = "PG-13", DurationMinutes = 120, IsActive = true },
                    CinemaHall = new CinemaHall { Id = Guid.NewGuid(), Name = "Hall 1", TotalSeats = 100, IsActive = true, SeatLayoutJson = "{}" }
                }
            }
        };

        _bookingRepositoryMock
            .Setup(x => x.GetByUserIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(bookings);

        // Act
        var result = await _bookingService.GetMyBookingsAsync(userId);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Single(result.Value);
    }

    [Fact]
    public async Task GetMyBookingsAsync_FutureShowtime_ReturnsConfirmedStatus()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var bookings = new List<Booking>
        {
            new Booking
            {
                Id = Guid.NewGuid(),
                BookingNumber = "BK-240115-FUT01",
                UserId = userId,
                ShowtimeId = Guid.NewGuid(),
                SeatNumbers = new List<string> { "B1" },
                TotalAmount = 10m,
                Status = BookingStatus.Confirmed,
                BookedAt = _timeProvider.GetUtcNow().DateTime,
                Showtime = new Showtime
                {
                    Id = Guid.NewGuid(),
                    StartTime = _timeProvider.GetUtcNow().AddHours(3).DateTime, // Future showtime
                    MovieId = Guid.NewGuid(),
                    CinemaHallId = Guid.NewGuid(),
                    BasePrice = 10m,
                    IsActive = true,
                    Movie = new Movie { Id = Guid.NewGuid(), Title = "Future Movie", Genre = "Drama", Rating = "PG", DurationMinutes = 100, IsActive = true },
                    CinemaHall = new CinemaHall { Id = Guid.NewGuid(), Name = "Hall 1", TotalSeats = 100, IsActive = true, SeatLayoutJson = "{}" }
                }
            }
        };

        _bookingRepositoryMock
            .Setup(x => x.GetByUserIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(bookings);
        _bookingTicketRepositoryMock
            .Setup(x => x.GetByBookingIdsAsync(It.IsAny<IEnumerable<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<Guid, List<BookingTicket>>());

        // Act
        var result = await _bookingService.GetMyBookingsAsync(userId);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Single(result.Value!);
        Assert.Equal("Confirmed", result.Value![0].Status);
    }

    [Fact]
    public async Task GetMyBookingsAsync_PastShowtime_ReturnsCompletedStatus()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var bookings = new List<Booking>
        {
            new Booking
            {
                Id = Guid.NewGuid(),
                BookingNumber = "BK-240115-PST01",
                UserId = userId,
                ShowtimeId = Guid.NewGuid(),
                SeatNumbers = new List<string> { "C1" },
                TotalAmount = 10m,
                Status = BookingStatus.Confirmed,
                BookedAt = _timeProvider.GetUtcNow().DateTime,
                Showtime = new Showtime
                {
                    Id = Guid.NewGuid(),
                    StartTime = _timeProvider.GetUtcNow().AddHours(-2).DateTime, // Past showtime
                    MovieId = Guid.NewGuid(),
                    CinemaHallId = Guid.NewGuid(),
                    BasePrice = 10m,
                    IsActive = true,
                    Movie = new Movie { Id = Guid.NewGuid(), Title = "Past Movie", Genre = "Comedy", Rating = "G", DurationMinutes = 90, IsActive = true },
                    CinemaHall = new CinemaHall { Id = Guid.NewGuid(), Name = "Hall 2", TotalSeats = 80, IsActive = true, SeatLayoutJson = "{}" }
                }
            }
        };

        _bookingRepositoryMock
            .Setup(x => x.GetByUserIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(bookings);
        _bookingTicketRepositoryMock
            .Setup(x => x.GetByBookingIdsAsync(It.IsAny<IEnumerable<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<Guid, List<BookingTicket>>());

        // Act
        var result = await _bookingService.GetMyBookingsAsync(userId);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Single(result.Value!);
        Assert.Equal("Completed", result.Value![0].Status);
    }

    #endregion

    #region GetBookingByNumberAsync Tests

    [Fact]
    public async Task GetBookingByNumberAsync_ValidBookingNumber_ReturnsBooking()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var bookingNumber = "BK-240115-ABC12";
        var booking = new Booking
        {
            Id = Guid.NewGuid(),
            BookingNumber = bookingNumber,
            UserId = userId,
            ShowtimeId = Guid.NewGuid(),
            SeatNumbers = new List<string> { "A1", "A2" },
            TotalAmount = 22m,
            Status = BookingStatus.Confirmed,
            BookedAt = _timeProvider.GetUtcNow().DateTime,
            Showtime = new Showtime
            {
                Id = Guid.NewGuid(),
                StartTime = _timeProvider.GetUtcNow().AddHours(2).DateTime,
                MovieId = Guid.NewGuid(),
                CinemaHallId = Guid.NewGuid(),
                BasePrice = 10m,
                IsActive = true,
                Movie = new Movie { Id = Guid.NewGuid(), Title = "Test Movie", Genre = "Action", Rating = "PG-13", DurationMinutes = 120, IsActive = true },
                CinemaHall = new CinemaHall { Id = Guid.NewGuid(), Name = "Hall 1", TotalSeats = 100, IsActive = true, SeatLayoutJson = "{}" }
            }
        };

        _bookingRepositoryMock
            .Setup(x => x.GetByBookingNumberAsync(bookingNumber, It.IsAny<CancellationToken>()))
            .ReturnsAsync(booking);

        // Act
        var result = await _bookingService.GetBookingByNumberAsync(userId, bookingNumber);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal(bookingNumber, result.Value.BookingNumber);
    }

    [Fact]
    public async Task GetBookingByNumberAsync_BookingNotFound_ReturnsFailure()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var bookingNumber = "BK-240115-ABC12";

        _bookingRepositoryMock
            .Setup(x => x.GetByBookingNumberAsync(bookingNumber, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Booking?)null);

        // Act
        var result = await _bookingService.GetBookingByNumberAsync(userId, bookingNumber);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("Booking not found", result.Error);
    }

    [Fact]
    public async Task GetBookingByNumberAsync_UnauthorizedUser_ReturnsFailure()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var differentUserId = Guid.NewGuid();
        var bookingNumber = "BK-240115-ABC12";
        var booking = new Booking
        {
            Id = Guid.NewGuid(),
            BookingNumber = bookingNumber,
            UserId = differentUserId, // Different user
            ShowtimeId = Guid.NewGuid(),
            SeatNumbers = new List<string> { "A1" },
            TotalAmount = 10m,
            Status = BookingStatus.Confirmed,
            BookedAt = _timeProvider.GetUtcNow().DateTime,
            Showtime = new Showtime
            {
                Id = Guid.NewGuid(),
                StartTime = _timeProvider.GetUtcNow().AddHours(2).DateTime,
                MovieId = Guid.NewGuid(),
                CinemaHallId = Guid.NewGuid(),
                BasePrice = 10m,
                IsActive = true,
                Movie = new Movie { Id = Guid.NewGuid(), Title = "Test Movie", Genre = "Action", Rating = "PG-13", DurationMinutes = 120, IsActive = true },
                CinemaHall = new CinemaHall { Id = Guid.NewGuid(), Name = "Hall 1", TotalSeats = 100, IsActive = true, SeatLayoutJson = "{}" }
            }
        };

        _bookingRepositoryMock
            .Setup(x => x.GetByBookingNumberAsync(bookingNumber, It.IsAny<CancellationToken>()))
            .ReturnsAsync(booking);

        // Act
        var result = await _bookingService.GetBookingByNumberAsync(userId, bookingNumber);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("Unauthorized", result.Error);
    }

    #endregion

    #region CarLicensePlate Tests

    [Fact]
    public async Task ConfirmBookingAsync_WithCarLicensePlate_NormalizesAndStoresPlate()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var reservationId = Guid.NewGuid();
        var showtimeId = Guid.NewGuid();
        var dto = new ConfirmBookingDto(
            reservationId,
            "CreditCard",
            "4111111111111111",
            "John Doe",
            "12/25",
            "123",
            " cb1234ab " // Should be normalized to "CB1234AB"
        );

        var reservation = new Reservation
        {
            Id = reservationId,
            UserId = userId,
            ShowtimeId = showtimeId,
            SeatNumbers = new List<string> { "A1" },
            TotalAmount = 10m,
            ExpiresAt = _timeProvider.GetUtcNow().AddMinutes(3).DateTime,
            Status = ReservationStatus.Pending,
            CreatedAt = _timeProvider.GetUtcNow().DateTime
        };

        var showtime = new Showtime
        {
            Id = showtimeId,
            StartTime = _timeProvider.GetUtcNow().AddHours(2).DateTime,
            MovieId = Guid.NewGuid(),
            CinemaHallId = Guid.NewGuid(),
            BasePrice = 10m,
            IsActive = true,
            Movie = new Movie { Id = Guid.NewGuid(), Title = "Test Movie", Genre = "Action", Rating = "PG-13", DurationMinutes = 120, IsActive = true },
            CinemaHall = new CinemaHall { Id = Guid.NewGuid(), Name = "Hall 1", TotalSeats = 100, IsActive = true, SeatLayoutJson = "{}" }
        };

        var paymentResult = new PaymentResultDto(
            Guid.NewGuid(),
            "TXN-12345678",
            "Completed",
            10m,
            _timeProvider.GetUtcNow().DateTime
        );

        _reservationRepositoryMock
            .Setup(x => x.GetByIdAsync(reservationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(reservation);

        _paymentServiceMock
            .Setup(x => x.ProcessPaymentAsync(It.IsAny<ProcessPaymentDto>(), 10m, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Domain.Common.Result<PaymentResultDto>.Success(paymentResult));

        _showtimeRepositoryMock
            .Setup(x => x.GetByIdAsync(showtimeId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(showtime);

        _seatRepositoryMock
            .Setup(x => x.GetByReservationIdAsync(reservationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Seat>
            {
                new SeatBuilder().WithShowtimeId(showtimeId).WithSeatNumber("A1").AsReserved(reservationId, DateTime.UtcNow.AddMinutes(10)).Build()
            });

        Booking? capturedBooking = null;
        _bookingRepositoryMock
            .Setup(x => x.CreateAsync(It.IsAny<Booking>(), It.IsAny<CancellationToken>()))
            .Callback<Booking, CancellationToken>((b, _) => capturedBooking = b);

        // Act
        var result = await _bookingService.ConfirmBookingAsync(userId, dto);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal("CB1234AB", result.Value.CarLicensePlate);
        Assert.NotNull(capturedBooking);
        Assert.Equal("CB1234AB", capturedBooking!.CarLicensePlate);
    }

    [Fact]
    public async Task ConfirmBookingAsync_WithoutCarLicensePlate_StoresNullPlate()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var reservationId = Guid.NewGuid();
        var showtimeId = Guid.NewGuid();
        var dto = new ConfirmBookingDto(
            reservationId,
            "CreditCard",
            "4111111111111111",
            "John Doe",
            "12/25",
            "123"
        );

        var reservation = new Reservation
        {
            Id = reservationId,
            UserId = userId,
            ShowtimeId = showtimeId,
            SeatNumbers = new List<string> { "A1" },
            TotalAmount = 10m,
            ExpiresAt = _timeProvider.GetUtcNow().AddMinutes(3).DateTime,
            Status = ReservationStatus.Pending,
            CreatedAt = _timeProvider.GetUtcNow().DateTime
        };

        var showtime = new Showtime
        {
            Id = showtimeId,
            StartTime = _timeProvider.GetUtcNow().AddHours(2).DateTime,
            MovieId = Guid.NewGuid(),
            CinemaHallId = Guid.NewGuid(),
            BasePrice = 10m,
            IsActive = true,
            Movie = new Movie { Id = Guid.NewGuid(), Title = "Test Movie", Genre = "Action", Rating = "PG-13", DurationMinutes = 120, IsActive = true },
            CinemaHall = new CinemaHall { Id = Guid.NewGuid(), Name = "Hall 1", TotalSeats = 100, IsActive = true, SeatLayoutJson = "{}" }
        };

        var paymentResult = new PaymentResultDto(
            Guid.NewGuid(),
            "TXN-12345679",
            "Completed",
            10m,
            _timeProvider.GetUtcNow().DateTime
        );

        _reservationRepositoryMock
            .Setup(x => x.GetByIdAsync(reservationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(reservation);

        _paymentServiceMock
            .Setup(x => x.ProcessPaymentAsync(It.IsAny<ProcessPaymentDto>(), 10m, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Domain.Common.Result<PaymentResultDto>.Success(paymentResult));

        _showtimeRepositoryMock
            .Setup(x => x.GetByIdAsync(showtimeId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(showtime);

        _seatRepositoryMock
            .Setup(x => x.GetByReservationIdAsync(reservationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Seat>
            {
                new SeatBuilder().WithShowtimeId(showtimeId).WithSeatNumber("A1").AsReserved(reservationId, DateTime.UtcNow.AddMinutes(10)).Build()
            });

        // Act
        var result = await _bookingService.ConfirmBookingAsync(userId, dto);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Null(result.Value.CarLicensePlate);
    }

    #endregion
}

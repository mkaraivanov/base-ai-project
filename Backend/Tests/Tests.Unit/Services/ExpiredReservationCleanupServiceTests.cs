using Domain.Entities;
using Infrastructure.BackgroundServices;
using Infrastructure.Repositories;
using Infrastructure.UnitOfWork;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Time.Testing;
using Moq;
using Tests.Unit.Builders;
using Xunit;

namespace Tests.Unit.Services;

public class ExpiredReservationCleanupServiceTests
{
    private readonly Mock<IServiceProvider> _serviceProviderMock;
    private readonly Mock<IServiceScope> _serviceScopeMock;
    private readonly Mock<IServiceScopeFactory> _serviceScopeFactoryMock;
    private readonly Mock<IReservationRepository> _reservationRepositoryMock;
    private readonly Mock<ISeatRepository> _seatRepositoryMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<ILogger<ExpiredReservationCleanupService>> _loggerMock;
    private readonly FakeTimeProvider _timeProvider;

    public ExpiredReservationCleanupServiceTests()
    {
        _serviceProviderMock = new Mock<IServiceProvider>();
        _serviceScopeMock = new Mock<IServiceScope>();
        _serviceScopeFactoryMock = new Mock<IServiceScopeFactory>();
        _reservationRepositoryMock = new Mock<IReservationRepository>();
        _seatRepositoryMock = new Mock<ISeatRepository>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _loggerMock = new Mock<ILogger<ExpiredReservationCleanupService>>();
        _timeProvider = new FakeTimeProvider(new DateTime(2024, 1, 15, 10, 0, 0, DateTimeKind.Utc));

        // Setup DI scope chain
        _serviceScopeFactoryMock
            .Setup(x => x.CreateScope())
            .Returns(_serviceScopeMock.Object);

        _serviceScopeMock
            .Setup(x => x.ServiceProvider)
            .Returns(_serviceProviderMock.Object);

        _serviceProviderMock
            .Setup(x => x.GetService(typeof(IServiceScopeFactory)))
            .Returns(_serviceScopeFactoryMock.Object);

        _serviceProviderMock
            .Setup(x => x.GetService(typeof(IReservationRepository)))
            .Returns(_reservationRepositoryMock.Object);

        _serviceProviderMock
            .Setup(x => x.GetService(typeof(ISeatRepository)))
            .Returns(_seatRepositoryMock.Object);

        _serviceProviderMock
            .Setup(x => x.GetService(typeof(IUnitOfWork)))
            .Returns(_unitOfWorkMock.Object);
    }

    [Fact]
    public async Task ProcessExpiredReservations_WithExpiredReservations_ReleasesSeats()
    {
        // Arrange
        var reservationId = Guid.NewGuid();
        var expiredReservations = new List<Reservation>
        {
            new ReservationBuilder()
                .WithId(reservationId)
                .WithExpiresAt(_timeProvider.GetUtcNow().AddMinutes(-1).DateTime)
                .AsPending()
                .Build()
        };

        var seats = new List<Seat>
        {
            new SeatBuilder().WithStatus(SeatStatus.Reserved).Build(),
            new SeatBuilder().WithStatus(SeatStatus.Reserved).Build()
        };

        _reservationRepositoryMock
            .Setup(x => x.GetExpiredReservationsAsync(It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expiredReservations);

        _seatRepositoryMock
            .Setup(x => x.GetByReservationIdAsync(reservationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(seats);

        var service = new ExpiredReservationCleanupService(
            _serviceProviderMock.Object,
            _loggerMock.Object,
            _timeProvider
        );

        // Act
        var cts = new CancellationTokenSource();
        var executeTask = service.StartAsync(cts.Token);

        // Give it a moment to process
        await Task.Delay(100);
        cts.Cancel();

        try
        {
            await executeTask;
        }
        catch (TaskCanceledException)
        {
            // Expected
        }

        // Assert
        _reservationRepositoryMock.Verify(
            x => x.GetExpiredReservationsAsync(It.IsAny<DateTime>(), It.IsAny<CancellationToken>()),
            Times.AtLeastOnce
        );

        _seatRepositoryMock.Verify(
            x => x.GetByReservationIdAsync(reservationId, It.IsAny<CancellationToken>()),
            Times.Once
        );

        _seatRepositoryMock.Verify(
            x => x.UpdateRangeAsync(It.IsAny<List<Seat>>(), It.IsAny<CancellationToken>()),
            Times.Once
        );

        _reservationRepositoryMock.Verify(
            x => x.UpdateAsync(It.IsAny<Reservation>(), It.IsAny<CancellationToken>()),
            Times.Once
        );

        _unitOfWorkMock.Verify(x => x.BeginTransactionAsync(It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWorkMock.Verify(x => x.CommitTransactionAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ProcessExpiredReservations_NoExpiredReservations_DoesNothing()
    {
        // Arrange
        _reservationRepositoryMock
            .Setup(x => x.GetExpiredReservationsAsync(It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Reservation>());

        var service = new ExpiredReservationCleanupService(
            _serviceProviderMock.Object,
            _loggerMock.Object,
            _timeProvider
        );

        // Act
        var cts = new CancellationTokenSource();
        var executeTask = service.StartAsync(cts.Token);

        await Task.Delay(100);
        cts.Cancel();

        try
        {
            await executeTask;
        }
        catch (TaskCanceledException)
        {
            // Expected
        }

        // Assert
        _seatRepositoryMock.Verify(
            x => x.GetByReservationIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()),
            Times.Never
        );

        _unitOfWorkMock.Verify(x => x.BeginTransactionAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ProcessExpiredReservations_IndividualFailure_ContinuesProcessing()
    {
        // Arrange
        var reservation1Id = Guid.NewGuid();
        var reservation2Id = Guid.NewGuid();

        var expiredReservations = new List<Reservation>
        {
            new ReservationBuilder().WithId(reservation1Id).AsPending().Build(),
            new ReservationBuilder().WithId(reservation2Id).AsPending().Build()
        };

        _reservationRepositoryMock
            .Setup(x => x.GetExpiredReservationsAsync(It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expiredReservations);

        // First reservation fails
        _seatRepositoryMock
            .Setup(x => x.GetByReservationIdAsync(reservation1Id, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Database error"));

        // Second reservation succeeds
        _seatRepositoryMock
            .Setup(x => x.GetByReservationIdAsync(reservation2Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Seat>
            {
                new SeatBuilder().WithStatus(SeatStatus.Reserved).Build()
            });

        var service = new ExpiredReservationCleanupService(
            _serviceProviderMock.Object,
            _loggerMock.Object,
            _timeProvider
        );

        // Act
        var cts = new CancellationTokenSource();
        var executeTask = service.StartAsync(cts.Token);

        await Task.Delay(100);
        cts.Cancel();

        try
        {
            await executeTask;
        }
        catch (TaskCanceledException)
        {
            // Expected
        }

        // Assert - both reservations were attempted
        _seatRepositoryMock.Verify(
            x => x.GetByReservationIdAsync(reservation1Id, It.IsAny<CancellationToken>()),
            Times.Once
        );

        _seatRepositoryMock.Verify(
            x => x.GetByReservationIdAsync(reservation2Id, It.IsAny<CancellationToken>()),
            Times.Once
        );
    }

    [Fact]
    public async Task Service_StopsGracefully_OnCancellation()
    {
        // Arrange
        _reservationRepositoryMock
            .Setup(x => x.GetExpiredReservationsAsync(It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Reservation>());

        var service = new ExpiredReservationCleanupService(
            _serviceProviderMock.Object,
            _loggerMock.Object,
            _timeProvider
        );

        // Act
        var cts = new CancellationTokenSource();
        var executeTask = service.StartAsync(cts.Token);

        await Task.Delay(50);
        cts.Cancel();

        // Assert - should complete without throwing
        try
        {
            await executeTask;
        }
        catch (TaskCanceledException)
        {
            // Expected - service stopped gracefully
        }

        // Service should have logged startup
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("started")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once
        );
    }
}

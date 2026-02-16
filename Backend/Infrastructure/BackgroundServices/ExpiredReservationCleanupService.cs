using Domain.Entities;
using Infrastructure.Repositories;
using Infrastructure.UnitOfWork;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Infrastructure.BackgroundServices;

public class ExpiredReservationCleanupService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<ExpiredReservationCleanupService> _logger;
    private readonly TimeProvider _timeProvider;

    public ExpiredReservationCleanupService(
        IServiceProvider serviceProvider,
        ILogger<ExpiredReservationCleanupService> logger,
        TimeProvider? timeProvider = null)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        _timeProvider = timeProvider ?? TimeProvider.System;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Expired reservation cleanup service started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessExpiredReservationsAsync(stoppingToken);

                // Run every 30 seconds
                await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);
            }
            catch (OperationCanceledException)
            {
                // Expected when stopping
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in expired reservation cleanup service");
                await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
            }
        }

        _logger.LogInformation("Expired reservation cleanup service stopped");
    }

    private async Task ProcessExpiredReservationsAsync(CancellationToken ct)
    {
        using var scope = _serviceProvider.CreateScope();
        var reservationRepo = scope.ServiceProvider.GetRequiredService<IReservationRepository>();
        var seatRepo = scope.ServiceProvider.GetRequiredService<ISeatRepository>();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

        var currentTime = _timeProvider.GetUtcNow().DateTime;
        var expiredReservations = await reservationRepo.GetExpiredReservationsAsync(currentTime, ct);

        if (!expiredReservations.Any())
        {
            return;
        }

        _logger.LogInformation("Processing {Count} expired reservations", expiredReservations.Count);

        foreach (var reservation in expiredReservations)
        {
            try
            {
                await ReleaseReservationSeatsAsync(reservation, seatRepo, reservationRepo, unitOfWork, ct);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to process expired reservation {ReservationId}", reservation.Id);
            }
        }
    }

    private async Task ReleaseReservationSeatsAsync(
        Reservation reservation,
        ISeatRepository seatRepo,
        IReservationRepository reservationRepo,
        IUnitOfWork unitOfWork,
        CancellationToken ct)
    {
        try
        {
            await unitOfWork.BeginTransactionAsync(ct);

            // Release seats
            var seats = await seatRepo.GetByReservationIdAsync(reservation.Id, ct);
            var releasedSeats = seats.Select(s => s with
            {
                Status = SeatStatus.Available,
                ReservationId = null,
                ReservedUntil = null
            }).ToList();

            await seatRepo.UpdateRangeAsync(releasedSeats, ct);

            // Mark reservation as expired
            var expiredReservation = reservation with { Status = ReservationStatus.Expired };
            await reservationRepo.UpdateAsync(expiredReservation, ct);

            await unitOfWork.CommitTransactionAsync(ct);

            _logger.LogInformation(
                "Released {SeatCount} seats for expired reservation {ReservationId}",
                seats.Count,
                reservation.Id);
        }
        catch (Exception ex)
        {
            await unitOfWork.RollbackTransactionAsync(ct);
            _logger.LogError(ex, "Failed to release seats for reservation {ReservationId}", reservation.Id);
            throw;
        }
    }
}

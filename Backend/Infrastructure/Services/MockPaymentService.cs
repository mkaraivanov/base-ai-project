using Application.DTOs.Payments;
using Application.Resources;
using Application.Services;
using Domain.Common;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Services;

public class MockPaymentService : IPaymentService
{
    private readonly ILogger<MockPaymentService> _logger;
    private readonly TimeProvider _timeProvider;
    private readonly IStringLocalizer<SharedResource> _localizer;

    public MockPaymentService(
        ILogger<MockPaymentService> logger,
        IStringLocalizer<SharedResource> localizer,
        TimeProvider? timeProvider = null)
    {
        _logger = logger;
        _localizer = localizer;
        _timeProvider = timeProvider ?? TimeProvider.System;
    }

    public async Task<Result<PaymentResultDto>> ProcessPaymentAsync(
        ProcessPaymentDto dto,
        decimal amount,
        CancellationToken ct = default)
    {
        try
        {
            // Simulate payment processing delay
            await Task.Delay(TimeSpan.FromSeconds(2), ct);

            // Mock validation - fail if card number starts with "0000"
            if (dto.CardNumber.StartsWith("0000"))
            {
                _logger.LogWarning("Mock payment failed for reservation {ReservationId}", dto.ReservationId);
                return Result<PaymentResultDto>.Failure(_localizer["Payment declined by bank"]);
            }

            var transactionId = $"TXN-{Guid.NewGuid().ToString()[..8].ToUpper()}";
            var result = new PaymentResultDto(
                Guid.NewGuid(),
                transactionId,
                "Completed",
                amount,
                _timeProvider.GetUtcNow().UtcDateTime
            );

            _logger.LogInformation(
                "Mock payment processed successfully: {TransactionId}, Amount: {Amount}",
                transactionId,
                amount);

            return Result<PaymentResultDto>.Success(result);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing mock payment");
            return Result<PaymentResultDto>.Failure(_localizer["Payment processing failed"]);
        }
    }

    public async Task<Result<PaymentResultDto>> RefundPaymentAsync(Guid paymentId, CancellationToken ct = default)
    {
        try
        {
            // Simulate refund processing
            await Task.Delay(TimeSpan.FromSeconds(1), ct);

            var transactionId = $"RFN-{Guid.NewGuid().ToString()[..8].ToUpper()}";
            var result = new PaymentResultDto(
                paymentId,
                transactionId,
                "Refunded",
                0, // Amount will be set by caller
                _timeProvider.GetUtcNow().UtcDateTime
            );

            _logger.LogInformation("Mock refund processed: {TransactionId}", transactionId);

            return Result<PaymentResultDto>.Success(result);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing refund for payment {PaymentId}", paymentId);
            return Result<PaymentResultDto>.Failure(_localizer["Refund processing failed"]);
        }
    }
}

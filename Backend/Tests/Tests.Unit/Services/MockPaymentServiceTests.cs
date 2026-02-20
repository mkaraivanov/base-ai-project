using Application.DTOs.Payments;
using Domain.Common;
using Infrastructure.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Time.Testing;
using Moq;
using Xunit;

namespace Tests.Unit.Services;

public class MockPaymentServiceTests
{
    private readonly Mock<ILogger<MockPaymentService>> _loggerMock;
    private readonly FakeTimeProvider _timeProvider;
    private readonly MockPaymentService _paymentService;

    public MockPaymentServiceTests()
    {
        _loggerMock = new Mock<ILogger<MockPaymentService>>();
        _timeProvider = new FakeTimeProvider(new DateTime(2024, 1, 15, 10, 0, 0, DateTimeKind.Utc));
        _paymentService = new MockPaymentService(_loggerMock.Object, Helpers.LocalizerHelper.CreateDefault(), _timeProvider);
    }

    #region ProcessPaymentAsync Tests

    [Fact]
    public async Task ProcessPaymentAsync_ValidCard_ReturnsSuccess()
    {
        // Arrange
        var dto = new ProcessPaymentDto(
            Guid.NewGuid(),
            "CreditCard",
            "4111111111111111",
            "John Doe",
            "12/25",
            "123"
        );

        // Act
        var result = await _paymentService.ProcessPaymentAsync(dto, 100m);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal("Completed", result.Value.Status);
        Assert.StartsWith("TXN-", result.Value.TransactionId);
        Assert.Equal(100m, result.Value.Amount);
    }

    [Fact]
    public async Task ProcessPaymentAsync_CardStartingWith0000_ReturnsFailure()
    {
        // Arrange
        var dto = new ProcessPaymentDto(
            Guid.NewGuid(),
            "CreditCard",
            "0000111122223333",
            "John Doe",
            "12/25",
            "123"
        );

        // Act
        var result = await _paymentService.ProcessPaymentAsync(dto, 100m);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("Payment declined by bank", result.Error);
    }

    [Fact]
    public async Task ProcessPaymentAsync_CancellationRequested_ThrowsException()
    {
        // Arrange
        var dto = new ProcessPaymentDto(
            Guid.NewGuid(),
            "CreditCard",
            "4111111111111111",
            "John Doe",
            "12/25",
            "123"
        );

        using var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act & Assert
        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            async () => await _paymentService.ProcessPaymentAsync(dto, 100m, cts.Token)
        );
    }

    #endregion

    #region RefundPaymentAsync Tests

    [Fact]
    public async Task RefundPaymentAsync_ValidPaymentId_ReturnsSuccess()
    {
        // Arrange
        var paymentId = Guid.NewGuid();

        // Act
        var result = await _paymentService.RefundPaymentAsync(paymentId);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal("Refunded", result.Value.Status);
        Assert.StartsWith("RFN-", result.Value.TransactionId);
        Assert.Equal(paymentId, result.Value.PaymentId);
    }

    [Fact]
    public async Task RefundPaymentAsync_CancellationRequested_ThrowsException()
    {
        // Arrange
        var paymentId = Guid.NewGuid();
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act & Assert
        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            async () => await _paymentService.RefundPaymentAsync(paymentId, cts.Token)
        );
    }

    #endregion
}

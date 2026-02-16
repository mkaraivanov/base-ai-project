using Application.DTOs.Payments;
using Domain.Common;

namespace Application.Services;

public interface IPaymentService
{
    Task<Result<PaymentResultDto>> ProcessPaymentAsync(ProcessPaymentDto dto, decimal amount, CancellationToken ct = default);
    Task<Result<PaymentResultDto>> RefundPaymentAsync(Guid paymentId, CancellationToken ct = default);
}

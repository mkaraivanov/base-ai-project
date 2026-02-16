namespace Application.DTOs.Payments;

public record PaymentResultDto(
    Guid PaymentId,
    string TransactionId,
    string Status,
    decimal Amount,
    DateTime ProcessedAt
);

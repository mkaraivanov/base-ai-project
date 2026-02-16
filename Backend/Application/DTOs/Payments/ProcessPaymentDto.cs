namespace Application.DTOs.Payments;

public record ProcessPaymentDto(
    Guid ReservationId,
    string PaymentMethod,
    string CardNumber,
    string CardHolderName,
    string ExpiryDate,
    string CVV
);

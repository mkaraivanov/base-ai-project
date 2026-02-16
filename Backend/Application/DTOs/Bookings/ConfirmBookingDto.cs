namespace Application.DTOs.Bookings;

public record ConfirmBookingDto(
    Guid ReservationId,
    string PaymentMethod,
    string CardNumber,
    string CardHolderName,
    string ExpiryDate,
    string CVV
);

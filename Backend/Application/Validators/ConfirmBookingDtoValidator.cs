using Application.DTOs.Bookings;
using FluentValidation;

namespace Application.Validators;

public class ConfirmBookingDtoValidator : AbstractValidator<ConfirmBookingDto>
{
    public ConfirmBookingDtoValidator()
    {
        RuleFor(x => x.ReservationId)
            .NotEmpty()
            .WithMessage("Reservation ID is required");

        RuleFor(x => x.PaymentMethod)
            .NotEmpty()
            .WithMessage("Payment method is required")
            .MaximumLength(50)
            .WithMessage("Payment method must not exceed 50 characters");

        RuleFor(x => x.CardNumber)
            .NotEmpty()
            .WithMessage("Card number is required")
            .Matches(@"^\d{13,19}$")
            .WithMessage("Card number must be between 13 and 19 digits");

        RuleFor(x => x.CardHolderName)
            .NotEmpty()
            .WithMessage("Card holder name is required")
            .MaximumLength(100)
            .WithMessage("Card holder name must not exceed 100 characters");

        RuleFor(x => x.ExpiryDate)
            .NotEmpty()
            .WithMessage("Expiry date is required")
            .Matches(@"^(0[1-9]|1[0-2])\/\d{2}$")
            .WithMessage("Expiry date must be in MM/YY format");

        RuleFor(x => x.CVV)
            .NotEmpty()
            .WithMessage("CVV is required")
            .Matches(@"^\d{3,4}$")
            .WithMessage("CVV must be 3 or 4 digits");
    }
}

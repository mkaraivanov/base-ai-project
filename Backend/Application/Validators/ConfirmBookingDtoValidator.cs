using Application.DTOs.Bookings;
using Application.Resources;
using FluentValidation;
using Microsoft.Extensions.Localization;

namespace Application.Validators;

public class ConfirmBookingDtoValidator : AbstractValidator<ConfirmBookingDto>
{
    private readonly IStringLocalizer<SharedResource> _localizer;

    public ConfirmBookingDtoValidator(IStringLocalizer<SharedResource> localizer)
    {
        _localizer = localizer;

        RuleFor(x => x.ReservationId)
            .NotEmpty()
            .WithMessage(_ => _localizer["Reservation ID is required"]);

        RuleFor(x => x.PaymentMethod)
            .NotEmpty()
            .WithMessage(_ => _localizer["Payment method is required"])
            .MaximumLength(50)
            .WithMessage(_ => _localizer["Payment method must not exceed 50 characters"]);

        RuleFor(x => x.CardNumber)
            .NotEmpty()
            .WithMessage(_ => _localizer["Card number is required"])
            .Matches(@"^\d{13,19}$")
            .WithMessage(_ => _localizer["Card number must be between 13 and 19 digits"]);

        RuleFor(x => x.CardHolderName)
            .NotEmpty()
            .WithMessage(_ => _localizer["Card holder name is required"])
            .MaximumLength(100)
            .WithMessage(_ => _localizer["Card holder name must not exceed 100 characters"]);

        RuleFor(x => x.ExpiryDate)
            .NotEmpty()
            .WithMessage(_ => _localizer["Expiry date is required"])
            .Matches(@"^(0[1-9]|1[0-2])\/\d{2}$")
            .WithMessage(_ => _localizer["Expiry date must be in MM/YY format"]);

        RuleFor(x => x.CVV)
            .NotEmpty()
            .WithMessage(_ => _localizer["CVV is required"])
            .Matches(@"^\d{3,4}$")
            .WithMessage(_ => _localizer["CVV must be 3 or 4 digits"]);

        RuleFor(x => x.CarLicensePlate)
            .Matches(@"^[A-Z]{1,2}\d{4}[A-Z]{2}$")
            .WithMessage(_ => _localizer["Car license plate must be a valid Bulgarian format (e.g. CB1234AB)"])
            .When(x => !string.IsNullOrWhiteSpace(x.CarLicensePlate));
    }
}

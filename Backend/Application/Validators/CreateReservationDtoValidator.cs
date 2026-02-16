using Application.DTOs.Reservations;
using FluentValidation;

namespace Application.Validators;

public class CreateReservationDtoValidator : AbstractValidator<CreateReservationDto>
{
    public CreateReservationDtoValidator()
    {
        RuleFor(x => x.ShowtimeId)
            .NotEmpty().WithMessage("Showtime ID is required");

        RuleFor(x => x.SeatNumbers)
            .NotEmpty().WithMessage("At least one seat must be selected")
            .Must(seats => seats.Count <= 10)
            .WithMessage("Cannot reserve more than 10 seats at once")
            .Must(seats => seats.Distinct().Count() == seats.Count)
            .WithMessage("Duplicate seat numbers are not allowed");

        RuleForEach(x => x.SeatNumbers)
            .NotEmpty().WithMessage("Seat number cannot be empty")
            .Matches(@"^[A-Z]\d{1,2}$").WithMessage("Invalid seat number format (e.g., A1, B12)");
    }
}

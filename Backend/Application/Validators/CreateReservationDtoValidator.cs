using Application.DTOs.Reservations;
using Application.Resources;
using FluentValidation;
using Microsoft.Extensions.Localization;

namespace Application.Validators;

public class CreateReservationDtoValidator : AbstractValidator<CreateReservationDto>
{
    private readonly IStringLocalizer<SharedResource> _localizer;

    public CreateReservationDtoValidator(IStringLocalizer<SharedResource> localizer)
    {
        _localizer = localizer;

        RuleFor(x => x.ShowtimeId)
            .NotEmpty().WithMessage(_ => _localizer["Showtime ID is required"]);

        RuleFor(x => x.Seats)
            .NotEmpty().WithMessage(_ => _localizer["At least one seat must be selected"])
            .Must(seats => seats.Count <= 10)
            .WithMessage(_ => _localizer["Cannot reserve more than 10 seats at once"])
            .Must(seats => seats.Select(s => s.SeatNumber).Distinct().Count() == seats.Count)
            .WithMessage(_ => _localizer["Duplicate seat numbers are not allowed"]);

        RuleForEach(x => x.Seats)
            .ChildRules(seat =>
            {
                seat.RuleFor(s => s.SeatNumber)
                    .NotEmpty().WithMessage(_ => _localizer["Seat number cannot be empty"])
                    .Matches(@"^[A-Z]\d{1,2}$").WithMessage(_ => _localizer["Invalid seat number format (e.g., A1, B12)"]);

                seat.RuleFor(s => s.TicketTypeId)
                    .NotEmpty().WithMessage(_ => _localizer["Ticket type must be specified for each seat"]);
            });
    }
}

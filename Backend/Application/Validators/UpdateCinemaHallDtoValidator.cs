using Application.DTOs.CinemaHalls;
using Application.Resources;
using FluentValidation;
using Microsoft.Extensions.Localization;

namespace Application.Validators;

public class UpdateCinemaHallDtoValidator : AbstractValidator<UpdateCinemaHallDto>
{
    private readonly IStringLocalizer<SharedResource> _localizer;

    public UpdateCinemaHallDtoValidator(IStringLocalizer<SharedResource> localizer)
    {
        _localizer = localizer;

        RuleFor(x => x.Name)
            .NotEmpty().WithMessage(_ => _localizer["Hall name is required"])
            .MaximumLength(100).WithMessage(_ => _localizer["Hall name must not exceed 100 characters"]);

        RuleFor(x => x.SeatLayout)
            .NotNull().WithMessage(_ => _localizer["Seat layout is required"]);

        RuleFor(x => x.SeatLayout.Rows)
            .GreaterThan(0).WithMessage(_ => _localizer["Rows must be greater than 0"])
            .LessThanOrEqualTo(50).WithMessage(_ => _localizer["Rows must not exceed 50"]);

        RuleFor(x => x.SeatLayout.SeatsPerRow)
            .GreaterThan(0).WithMessage(_ => _localizer["Seats per row must be greater than 0"])
            .LessThanOrEqualTo(100).WithMessage(_ => _localizer["Seats per row must not exceed 100"]);

        RuleFor(x => x.SeatLayout.Seats)
            .NotNull().WithMessage(_ => _localizer["Seats list is required"])
            .Must(seats => seats.Count > 0).WithMessage(_ => _localizer["At least one seat must be defined"]);
    }
}

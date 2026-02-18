using Application.DTOs.CinemaHalls;
using FluentValidation;

namespace Application.Validators;

public class CreateCinemaHallDtoValidator : AbstractValidator<CreateCinemaHallDto>
{
    public CreateCinemaHallDtoValidator()
    {
        RuleFor(x => x.CinemaId)
            .NotEmpty().WithMessage("Cinema is required");

        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Hall name is required")
            .MaximumLength(100).WithMessage("Hall name must not exceed 100 characters");

        RuleFor(x => x.SeatLayout)
            .NotNull().WithMessage("Seat layout is required");

        RuleFor(x => x.SeatLayout.Rows)
            .GreaterThan(0).WithMessage("Rows must be greater than 0")
            .LessThanOrEqualTo(50).WithMessage("Rows must not exceed 50");

        RuleFor(x => x.SeatLayout.SeatsPerRow)
            .GreaterThan(0).WithMessage("Seats per row must be greater than 0")
            .LessThanOrEqualTo(100).WithMessage("Seats per row must not exceed 100");

        RuleFor(x => x.SeatLayout.Seats)
            .NotNull().WithMessage("Seats list is required")
            .Must(seats => seats.Count > 0).WithMessage("At least one seat must be defined");
    }
}

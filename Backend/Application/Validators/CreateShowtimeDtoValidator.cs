using Application.DTOs.Showtimes;
using FluentValidation;

namespace Application.Validators;

public class CreateShowtimeDtoValidator : AbstractValidator<CreateShowtimeDto>
{
    public CreateShowtimeDtoValidator()
    {
        RuleFor(x => x.MovieId)
            .NotEmpty().WithMessage("Movie ID is required");

        RuleFor(x => x.CinemaHallId)
            .NotEmpty().WithMessage("Cinema Hall ID is required");

        RuleFor(x => x.StartTime)
            .NotEmpty().WithMessage("Start time is required")
            .GreaterThan(DateTime.UtcNow).WithMessage("Start time must be in the future");

        RuleFor(x => x.BasePrice)
            .GreaterThan(0).WithMessage("Base price must be greater than 0")
            .LessThanOrEqualTo(1000).WithMessage("Base price must not exceed 1000");
    }
}

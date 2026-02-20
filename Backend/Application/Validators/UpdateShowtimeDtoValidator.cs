using Application.DTOs.Showtimes;
using FluentValidation;

namespace Application.Validators;

public class UpdateShowtimeDtoValidator : AbstractValidator<UpdateShowtimeDto>
{
    public UpdateShowtimeDtoValidator()
    {
        RuleFor(x => x.StartTime)
            .NotEmpty().WithMessage("Start time is required");

        RuleFor(x => x.BasePrice)
            .GreaterThan(0).WithMessage("Base price must be greater than 0")
            .LessThanOrEqualTo(1000).WithMessage("Base price must not exceed 1000");
    }
}

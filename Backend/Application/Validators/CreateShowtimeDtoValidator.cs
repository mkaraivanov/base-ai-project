using Application.DTOs.Showtimes;
using Application.Resources;
using FluentValidation;
using Microsoft.Extensions.Localization;

namespace Application.Validators;

public class CreateShowtimeDtoValidator : AbstractValidator<CreateShowtimeDto>
{
    private readonly IStringLocalizer<SharedResource> _localizer;

    public CreateShowtimeDtoValidator(IStringLocalizer<SharedResource> localizer)
    {
        _localizer = localizer;

        RuleFor(x => x.MovieId)
            .NotEmpty().WithMessage(_ => _localizer["Movie ID is required"]);

        RuleFor(x => x.CinemaHallId)
            .NotEmpty().WithMessage(_ => _localizer["Cinema Hall ID is required"]);

        RuleFor(x => x.StartTime)
            .NotEmpty().WithMessage(_ => _localizer["Start time is required"])
            .GreaterThan(DateTime.UtcNow).WithMessage(_ => _localizer["Start time must be in the future"]);

        RuleFor(x => x.BasePrice)
            .GreaterThan(0).WithMessage(_ => _localizer["Base price must be greater than 0"])
            .LessThanOrEqualTo(1000).WithMessage(_ => _localizer["Base price must not exceed 1000"]);
    }
}

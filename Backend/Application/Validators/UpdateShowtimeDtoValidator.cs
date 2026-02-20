using Application.DTOs.Showtimes;
using Application.Resources;
using FluentValidation;
using Microsoft.Extensions.Localization;

namespace Application.Validators;

public class UpdateShowtimeDtoValidator : AbstractValidator<UpdateShowtimeDto>
{
    private readonly IStringLocalizer<SharedResource> _localizer;

    public UpdateShowtimeDtoValidator(IStringLocalizer<SharedResource> localizer)
    {
        _localizer = localizer;

        RuleFor(x => x.StartTime)
            .NotEmpty().WithMessage(_ => _localizer["Start time is required"]);

        RuleFor(x => x.BasePrice)
            .GreaterThan(0).WithMessage(_ => _localizer["Base price must be greater than 0"])
            .LessThanOrEqualTo(1000).WithMessage(_ => _localizer["Base price must not exceed 1000"]);
    }
}

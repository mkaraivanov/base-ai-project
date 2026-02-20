using Application.DTOs.Movies;
using Application.Resources;
using FluentValidation;
using Microsoft.Extensions.Localization;

namespace Application.Validators;

public class CreateMovieDtoValidator : AbstractValidator<CreateMovieDto>
{
    private readonly IStringLocalizer<SharedResource> _localizer;

    public CreateMovieDtoValidator(IStringLocalizer<SharedResource> localizer)
    {
        _localizer = localizer;

        RuleFor(x => x.Title)
            .NotEmpty().WithMessage(_ => _localizer["Title is required"])
            .MaximumLength(200).WithMessage(_ => _localizer["Title must not exceed 200 characters"]);

        RuleFor(x => x.Description)
            .NotEmpty().WithMessage(_ => _localizer["Description is required"])
            .MaximumLength(2000).WithMessage(_ => _localizer["Description must not exceed 2000 characters"]);

        RuleFor(x => x.Genre)
            .NotEmpty().WithMessage(_ => _localizer["Genre is required"])
            .MaximumLength(50).WithMessage(_ => _localizer["Genre must not exceed 50 characters"]);

        RuleFor(x => x.DurationMinutes)
            .GreaterThan(0).WithMessage(_ => _localizer["Duration must be greater than 0"])
            .LessThanOrEqualTo(500).WithMessage(_ => _localizer["Duration must not exceed 500 minutes"]);

        RuleFor(x => x.Rating)
            .NotEmpty().WithMessage(_ => _localizer["Rating is required"])
            .Must(r => new[] { "G", "PG", "PG-13", "R", "NC-17" }.Contains(r))
            .WithMessage(_ => _localizer["Rating must be G, PG, PG-13, R, or NC-17"]);

        RuleFor(x => x.PosterUrl)
            .NotEmpty().WithMessage(_ => _localizer["Poster URL is required"])
            .Must(url => Uri.TryCreate(url, UriKind.Absolute, out _))
            .WithMessage(_ => _localizer["Poster URL must be a valid URL"]);

        RuleFor(x => x.ReleaseDate)
            .NotEmpty().WithMessage(_ => _localizer["Release date is required"]);
    }
}

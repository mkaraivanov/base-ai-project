using Application.DTOs.Movies;
using FluentValidation;

namespace Application.Validators;

public class UpdateMovieDtoValidator : AbstractValidator<UpdateMovieDto>
{
    public UpdateMovieDtoValidator()
    {
        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("Title is required")
            .MaximumLength(200).WithMessage("Title must not exceed 200 characters");

        RuleFor(x => x.Description)
            .NotEmpty().WithMessage("Description is required")
            .MaximumLength(2000).WithMessage("Description must not exceed 2000 characters");

        RuleFor(x => x.Genre)
            .NotEmpty().WithMessage("Genre is required")
            .MaximumLength(50).WithMessage("Genre must not exceed 50 characters");

        RuleFor(x => x.DurationMinutes)
            .GreaterThan(0).WithMessage("Duration must be greater than 0")
            .LessThanOrEqualTo(500).WithMessage("Duration must not exceed 500 minutes");

        RuleFor(x => x.Rating)
            .NotEmpty().WithMessage("Rating is required")
            .Must(r => new[] { "G", "PG", "PG-13", "R", "NC-17" }.Contains(r))
            .WithMessage("Rating must be G, PG, PG-13, R, or NC-17");

        RuleFor(x => x.PosterUrl)
            .NotEmpty().WithMessage("Poster URL is required")
            .Must(url => Uri.TryCreate(url, UriKind.Absolute, out _))
            .WithMessage("Poster URL must be a valid URL");

        RuleFor(x => x.ReleaseDate)
            .NotEmpty().WithMessage("Release date is required");
    }
}

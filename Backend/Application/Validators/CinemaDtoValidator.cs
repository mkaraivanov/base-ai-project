using Application.DTOs.Cinemas;
using Application.Resources;
using FluentValidation;
using Microsoft.Extensions.Localization;

namespace Application.Validators;

public class CreateCinemaDtoValidator : AbstractValidator<CreateCinemaDto>
{
    private readonly IStringLocalizer<SharedResource> _localizer;

    public CreateCinemaDtoValidator(IStringLocalizer<SharedResource> localizer)
    {
        _localizer = localizer;

        RuleFor(x => x.Name)
            .NotEmpty().WithMessage(_ => _localizer["Cinema name is required"])
            .MaximumLength(100).WithMessage(_ => _localizer["Cinema name must not exceed 100 characters"]);

        RuleFor(x => x.Address)
            .NotEmpty().WithMessage(_ => _localizer["Address is required"])
            .MaximumLength(200).WithMessage(_ => _localizer["Address must not exceed 200 characters"]);

        RuleFor(x => x.City)
            .NotEmpty().WithMessage(_ => _localizer["City is required"])
            .MaximumLength(100).WithMessage(_ => _localizer["City must not exceed 100 characters"]);

        RuleFor(x => x.Country)
            .NotEmpty().WithMessage(_ => _localizer["Country is required"])
            .MaximumLength(100).WithMessage(_ => _localizer["Country must not exceed 100 characters"]);

        RuleFor(x => x.PhoneNumber)
            .MaximumLength(20).WithMessage(_ => _localizer["Phone number must not exceed 20 characters"])
            .When(x => x.PhoneNumber is not null);

        RuleFor(x => x.Email)
            .EmailAddress().WithMessage(_ => _localizer["Invalid email address format"])
            .MaximumLength(200).WithMessage(_ => _localizer["Email must not exceed 200 characters"])
            .When(x => x.Email is not null);

        RuleFor(x => x.LogoUrl)
            .MaximumLength(500).WithMessage(_ => _localizer["Logo URL must not exceed 500 characters"])
            .When(x => x.LogoUrl is not null);
    }
}

public class UpdateCinemaDtoValidator : AbstractValidator<UpdateCinemaDto>
{
    private readonly IStringLocalizer<SharedResource> _localizer;

    public UpdateCinemaDtoValidator(IStringLocalizer<SharedResource> localizer)
    {
        _localizer = localizer;

        RuleFor(x => x.Name)
            .NotEmpty().WithMessage(_ => _localizer["Cinema name is required"])
            .MaximumLength(100).WithMessage(_ => _localizer["Cinema name must not exceed 100 characters"]);

        RuleFor(x => x.Address)
            .NotEmpty().WithMessage(_ => _localizer["Address is required"])
            .MaximumLength(200).WithMessage(_ => _localizer["Address must not exceed 200 characters"]);

        RuleFor(x => x.City)
            .NotEmpty().WithMessage(_ => _localizer["City is required"])
            .MaximumLength(100).WithMessage(_ => _localizer["City must not exceed 100 characters"]);

        RuleFor(x => x.Country)
            .NotEmpty().WithMessage(_ => _localizer["Country is required"])
            .MaximumLength(100).WithMessage(_ => _localizer["Country must not exceed 100 characters"]);

        RuleFor(x => x.PhoneNumber)
            .MaximumLength(20).WithMessage(_ => _localizer["Phone number must not exceed 20 characters"])
            .When(x => x.PhoneNumber is not null);

        RuleFor(x => x.Email)
            .EmailAddress().WithMessage(_ => _localizer["Invalid email address format"])
            .MaximumLength(200).WithMessage(_ => _localizer["Email must not exceed 200 characters"])
            .When(x => x.Email is not null);

        RuleFor(x => x.LogoUrl)
            .MaximumLength(500).WithMessage(_ => _localizer["Logo URL must not exceed 500 characters"])
            .When(x => x.LogoUrl is not null);
    }
}

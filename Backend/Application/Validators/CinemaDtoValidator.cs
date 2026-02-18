using Application.DTOs.Cinemas;
using FluentValidation;

namespace Application.Validators;

public class CreateCinemaDtoValidator : AbstractValidator<CreateCinemaDto>
{
    public CreateCinemaDtoValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Cinema name is required")
            .MaximumLength(100).WithMessage("Cinema name must not exceed 100 characters");

        RuleFor(x => x.Address)
            .NotEmpty().WithMessage("Address is required")
            .MaximumLength(200).WithMessage("Address must not exceed 200 characters");

        RuleFor(x => x.City)
            .NotEmpty().WithMessage("City is required")
            .MaximumLength(100).WithMessage("City must not exceed 100 characters");

        RuleFor(x => x.Country)
            .NotEmpty().WithMessage("Country is required")
            .MaximumLength(100).WithMessage("Country must not exceed 100 characters");

        RuleFor(x => x.PhoneNumber)
            .MaximumLength(20).WithMessage("Phone number must not exceed 20 characters")
            .When(x => x.PhoneNumber is not null);

        RuleFor(x => x.Email)
            .EmailAddress().WithMessage("Invalid email address format")
            .MaximumLength(200).WithMessage("Email must not exceed 200 characters")
            .When(x => x.Email is not null);

        RuleFor(x => x.LogoUrl)
            .MaximumLength(500).WithMessage("Logo URL must not exceed 500 characters")
            .When(x => x.LogoUrl is not null);
    }
}

public class UpdateCinemaDtoValidator : AbstractValidator<UpdateCinemaDto>
{
    public UpdateCinemaDtoValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Cinema name is required")
            .MaximumLength(100).WithMessage("Cinema name must not exceed 100 characters");

        RuleFor(x => x.Address)
            .NotEmpty().WithMessage("Address is required")
            .MaximumLength(200).WithMessage("Address must not exceed 200 characters");

        RuleFor(x => x.City)
            .NotEmpty().WithMessage("City is required")
            .MaximumLength(100).WithMessage("City must not exceed 100 characters");

        RuleFor(x => x.Country)
            .NotEmpty().WithMessage("Country is required")
            .MaximumLength(100).WithMessage("Country must not exceed 100 characters");

        RuleFor(x => x.PhoneNumber)
            .MaximumLength(20).WithMessage("Phone number must not exceed 20 characters")
            .When(x => x.PhoneNumber is not null);

        RuleFor(x => x.Email)
            .EmailAddress().WithMessage("Invalid email address format")
            .MaximumLength(200).WithMessage("Email must not exceed 200 characters")
            .When(x => x.Email is not null);

        RuleFor(x => x.LogoUrl)
            .MaximumLength(500).WithMessage("Logo URL must not exceed 500 characters")
            .When(x => x.LogoUrl is not null);
    }
}

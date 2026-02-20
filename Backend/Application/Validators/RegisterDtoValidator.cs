using Application.DTOs.Auth;
using Application.Resources;
using FluentValidation;
using Microsoft.Extensions.Localization;

namespace Application.Validators;

public class RegisterDtoValidator : AbstractValidator<RegisterDto>
{
    private readonly IStringLocalizer<SharedResource> _localizer;

    public RegisterDtoValidator(IStringLocalizer<SharedResource> localizer)
    {
        _localizer = localizer;

        RuleFor(x => x.Email)
            .NotEmpty().WithMessage(_ => _localizer["Email is required"])
            .EmailAddress().WithMessage(_ => _localizer["Invalid email format"])
            .MaximumLength(200).WithMessage(_ => _localizer["Email must not exceed 200 characters"]);

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage(_ => _localizer["Password is required"])
            .MinimumLength(8).WithMessage(_ => _localizer["Password must be at least 8 characters"])
            .Matches(@"[A-Z]").WithMessage(_ => _localizer["Password must contain at least one uppercase letter"])
            .Matches(@"[a-z]").WithMessage(_ => _localizer["Password must contain at least one lowercase letter"])
            .Matches(@"[0-9]").WithMessage(_ => _localizer["Password must contain at least one number"]);

        RuleFor(x => x.FirstName)
            .NotEmpty().WithMessage(_ => _localizer["First name is required"])
            .MaximumLength(100).WithMessage(_ => _localizer["First name must not exceed 100 characters"]);

        RuleFor(x => x.LastName)
            .NotEmpty().WithMessage(_ => _localizer["Last name is required"])
            .MaximumLength(100).WithMessage(_ => _localizer["Last name must not exceed 100 characters"]);

        RuleFor(x => x.PhoneNumber)
            .NotEmpty().WithMessage(_ => _localizer["Phone number is required"])
            .Matches(@"^\+?[1-9]\d{1,14}$").WithMessage(_ => _localizer["Invalid phone number format"]);
    }
}

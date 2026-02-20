using Application.DTOs.Auth;
using Application.Resources;
using FluentValidation;
using Microsoft.Extensions.Localization;

namespace Application.Validators;

public class LoginDtoValidator : AbstractValidator<LoginDto>
{
    private readonly IStringLocalizer<SharedResource> _localizer;

    public LoginDtoValidator(IStringLocalizer<SharedResource> localizer)
    {
        _localizer = localizer;

        RuleFor(x => x.Email)
            .NotEmpty().WithMessage(_ => _localizer["Email is required"])
            .EmailAddress().WithMessage(_ => _localizer["Invalid email format"]);

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage(_ => _localizer["Password is required"]);
    }
}

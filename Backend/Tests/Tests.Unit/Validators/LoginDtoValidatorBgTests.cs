using Application.DTOs.Auth;
using Application.Validators;
using FluentValidation.TestHelper;
using Tests.Unit.Helpers;
using Xunit;

namespace Tests.Unit.Validators;

/// <summary>
/// Verifies that <see cref="LoginDtoValidator"/> emits Bulgarian validation messages
/// when the validator's localizer is configured with the <c>bg-BG</c> culture.
/// </summary>
public class LoginDtoValidatorBgTests
{
    private readonly LoginDtoValidator _validator;

    public LoginDtoValidatorBgTests()
    {
        _validator = new LoginDtoValidator(LocalizerHelper.CreateBg());
    }

    // ─── Email ────────────────────────────────────────────────────────────────

    [Fact]
    public void Validate_EmptyEmail_ReturnsBulgarianMessage()
    {
        var dto = new LoginDto(Email: "", Password: "SomePass1");
        var result = _validator.TestValidate(dto);
        result.ShouldHaveValidationErrorFor(x => x.Email)
              .WithErrorMessage("Имейлът е задължителен");
    }

    [Fact]
    public void Validate_InvalidEmailFormat_ReturnsBulgarianMessage()
    {
        var dto = new LoginDto(Email: "not-an-email", Password: "SomePass1");
        var result = _validator.TestValidate(dto);
        result.ShouldHaveValidationErrorFor(x => x.Email)
              .WithErrorMessage("Невалиден формат на имейл");
    }

    // ─── Password ─────────────────────────────────────────────────────────────

    [Fact]
    public void Validate_EmptyPassword_ReturnsBulgarianMessage()
    {
        var dto = new LoginDto(Email: "user@example.com", Password: "");
        var result = _validator.TestValidate(dto);
        result.ShouldHaveValidationErrorFor(x => x.Password)
              .WithErrorMessage("Паролата е задължителна");
    }

    // ─── Valid ────────────────────────────────────────────────────────────────

    [Fact]
    public void Validate_ValidDto_ShouldHaveNoErrors()
    {
        var dto = new LoginDto(Email: "user@example.com", Password: "AnyPassword");
        var result = _validator.TestValidate(dto);
        result.ShouldNotHaveAnyValidationErrors();
    }
}

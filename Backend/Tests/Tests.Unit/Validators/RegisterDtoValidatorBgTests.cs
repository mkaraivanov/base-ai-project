using Application.DTOs.Auth;
using Application.Validators;
using FluentValidation.TestHelper;
using Tests.Unit.Helpers;
using Xunit;

namespace Tests.Unit.Validators;

/// <summary>
/// Verifies that <see cref="RegisterDtoValidator"/> emits Bulgarian validation messages
/// when the validator's localizer is configured with the <c>bg-BG</c> culture.
/// </summary>
public class RegisterDtoValidatorBgTests
{
    private readonly RegisterDtoValidator _validator;

    public RegisterDtoValidatorBgTests()
    {
        _validator = new RegisterDtoValidator(LocalizerHelper.CreateBg());
    }

    // ─── Email ────────────────────────────────────────────────────────────────

    [Fact]
    public void Validate_EmptyEmail_ReturnsBulgarianMessage()
    {
        var dto = ValidDto() with { Email = "" };
        var result = _validator.TestValidate(dto);
        result.ShouldHaveValidationErrorFor(x => x.Email)
              .WithErrorMessage("Имейлът е задължителен");
    }

    [Fact]
    public void Validate_InvalidEmailFormat_ReturnsBulgarianMessage()
    {
        var dto = ValidDto() with { Email = "not-an-email" };
        var result = _validator.TestValidate(dto);
        result.ShouldHaveValidationErrorFor(x => x.Email)
              .WithErrorMessage("Невалиден формат на имейл");
    }

    [Fact]
    public void Validate_EmailTooLong_ReturnsBulgarianMessage()
    {
        var dto = ValidDto() with { Email = new string('a', 195) + "@b.com" };
        var result = _validator.TestValidate(dto);
        result.ShouldHaveValidationErrorFor(x => x.Email)
              .WithErrorMessage("Имейлът не трябва да надвишава 200 знака");
    }

    // ─── Password ─────────────────────────────────────────────────────────────

    [Fact]
    public void Validate_EmptyPassword_ReturnsBulgarianMessage()
    {
        var dto = ValidDto() with { Password = "" };
        var result = _validator.TestValidate(dto);
        result.ShouldHaveValidationErrorFor(x => x.Password)
              .WithErrorMessage("Паролата е задължителна");
    }

    [Fact]
    public void Validate_PasswordTooShort_ReturnsBulgarianMessage()
    {
        var dto = ValidDto() with { Password = "Ab1" };
        var result = _validator.TestValidate(dto);
        result.ShouldHaveValidationErrorFor(x => x.Password)
              .WithErrorMessage("Паролата трябва да бъде поне 8 знака");
    }

    [Fact]
    public void Validate_PasswordMissingUppercase_ReturnsBulgarianMessage()
    {
        var dto = ValidDto() with { Password = "lowercase1!" };
        var result = _validator.TestValidate(dto);
        result.ShouldHaveValidationErrorFor(x => x.Password)
              .WithErrorMessage("Паролата трябва да съдържа поне една главна буква");
    }

    [Fact]
    public void Validate_PasswordMissingLowercase_ReturnsBulgarianMessage()
    {
        var dto = ValidDto() with { Password = "UPPERCASE1!" };
        var result = _validator.TestValidate(dto);
        result.ShouldHaveValidationErrorFor(x => x.Password)
              .WithErrorMessage("Паролата трябва да съдържа поне една малка буква");
    }

    [Fact]
    public void Validate_PasswordMissingDigit_ReturnsBulgarianMessage()
    {
        var dto = ValidDto() with { Password = "NoDigitPass!" };
        var result = _validator.TestValidate(dto);
        result.ShouldHaveValidationErrorFor(x => x.Password)
              .WithErrorMessage("Паролата трябва да съдържа поне една цифра");
    }

    // ─── First name ───────────────────────────────────────────────────────────

    [Fact]
    public void Validate_EmptyFirstName_ReturnsBulgarianMessage()
    {
        var dto = ValidDto() with { FirstName = "" };
        var result = _validator.TestValidate(dto);
        result.ShouldHaveValidationErrorFor(x => x.FirstName)
              .WithErrorMessage("Името е задължително");
    }

    [Fact]
    public void Validate_FirstNameTooLong_ReturnsBulgarianMessage()
    {
        var dto = ValidDto() with { FirstName = new string('A', 101) };
        var result = _validator.TestValidate(dto);
        result.ShouldHaveValidationErrorFor(x => x.FirstName)
              .WithErrorMessage("Името не трябва да надвишава 100 знака");
    }

    // ─── Last name ────────────────────────────────────────────────────────────

    [Fact]
    public void Validate_EmptyLastName_ReturnsBulgarianMessage()
    {
        var dto = ValidDto() with { LastName = "" };
        var result = _validator.TestValidate(dto);
        result.ShouldHaveValidationErrorFor(x => x.LastName)
              .WithErrorMessage("Фамилното име е задължително");
    }

    [Fact]
    public void Validate_LastNameTooLong_ReturnsBulgarianMessage()
    {
        var dto = ValidDto() with { LastName = new string('A', 101) };
        var result = _validator.TestValidate(dto);
        result.ShouldHaveValidationErrorFor(x => x.LastName)
              .WithErrorMessage("Фамилното име не трябва да надвишава 100 знака");
    }

    // ─── Phone number ─────────────────────────────────────────────────────────

    [Fact]
    public void Validate_EmptyPhoneNumber_ReturnsBulgarianMessage()
    {
        var dto = ValidDto() with { PhoneNumber = "" };
        var result = _validator.TestValidate(dto);
        result.ShouldHaveValidationErrorFor(x => x.PhoneNumber)
              .WithErrorMessage("Телефонният номер е задължителен");
    }

    [Fact]
    public void Validate_InvalidPhoneNumberFormat_ReturnsBulgarianMessage()
    {
        var dto = ValidDto() with { PhoneNumber = "abc" };
        var result = _validator.TestValidate(dto);
        result.ShouldHaveValidationErrorFor(x => x.PhoneNumber)
              .WithErrorMessage("Невалиден формат на телефонен номер");
    }

    // ─── Valid ────────────────────────────────────────────────────────────────

    [Fact]
    public void Validate_ValidDto_ShouldHaveNoErrors()
    {
        var result = _validator.TestValidate(ValidDto());
        result.ShouldNotHaveAnyValidationErrors();
    }

    // ─── Helper ───────────────────────────────────────────────────────────────

    private static RegisterDto ValidDto() => new(
        Email: "ivan.petrov@example.com",
        Password: "ValidPass1",
        FirstName: "Иван",
        LastName: "Петров",
        PhoneNumber: "+35988123456"
    );
}

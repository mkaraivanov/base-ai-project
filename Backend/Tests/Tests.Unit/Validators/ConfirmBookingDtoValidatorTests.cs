using Application.DTOs.Bookings;
using Application.Validators;
using Xunit;

namespace Tests.Unit.Validators;

public class ConfirmBookingDtoValidatorTests
{
    private readonly ConfirmBookingDtoValidator _validator;

    public ConfirmBookingDtoValidatorTests()
    {
        _validator = new ConfirmBookingDtoValidator();
    }

    [Fact]
    public async Task Validate_AllFieldsValid_Passes()
    {
        // Arrange
        var dto = new ConfirmBookingDto(
            Guid.NewGuid(),
            "CreditCard",
            "4111111111111111",
            "John Doe",
            "12/25",
            "123"
        );

        // Act
        var result = await _validator.ValidateAsync(dto);

        // Assert
        Assert.True(result.IsValid);
    }

    [Fact]
    public async Task Validate_EmptyReservationId_Fails()
    {
        // Arrange
        var dto = new ConfirmBookingDto(
            Guid.Empty,
            "CreditCard",
            "4111111111111111",
            "John Doe",
            "12/25",
            "123"
        );

        // Act
        var result = await _validator.ValidateAsync(dto);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == "ReservationId");
    }

    [Fact]
    public async Task Validate_EmptyPaymentMethod_Fails()
    {
        // Arrange
        var dto = new ConfirmBookingDto(
            Guid.NewGuid(),
            "",
            "4111111111111111",
            "John Doe",
            "12/25",
            "123"
        );

        // Act
        var result = await _validator.ValidateAsync(dto);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == "PaymentMethod");
    }

    [Fact]
    public async Task Validate_InvalidCardNumber_Fails()
    {
        // Arrange - Card number too short
        var dto = new ConfirmBookingDto(
            Guid.NewGuid(),
            "CreditCard",
            "41111111",
            "John Doe",
            "12/25",
            "123"
        );

        // Act
        var result = await _validator.ValidateAsync(dto);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == "CardNumber");
    }

    [Fact]
    public async Task Validate_CardNumberWithLetters_Fails()
    {
        // Arrange
        var dto = new ConfirmBookingDto(
            Guid.NewGuid(),
            "CreditCard",
            "411111111111ABCD",
            "John Doe",
            "12/25",
            "123"
        );

        // Act
        var result = await _validator.ValidateAsync(dto);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == "CardNumber");
    }

    [Fact]
    public async Task Validate_EmptyCardHolderName_Fails()
    {
        // Arrange
        var dto = new ConfirmBookingDto(
            Guid.NewGuid(),
            "CreditCard",
            "4111111111111111",
            "",
            "12/25",
            "123"
        );

        // Act
        var result = await _validator.ValidateAsync(dto);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == "CardHolderName");
    }

    [Fact]
    public async Task Validate_InvalidExpiryDateFormat_Fails()
    {
        // Arrange
        var dto = new ConfirmBookingDto(
            Guid.NewGuid(),
            "CreditCard",
            "4111111111111111",
            "John Doe",
            "2025-12",
            "123"
        );

        // Act
        var result = await _validator.ValidateAsync(dto);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == "ExpiryDate");
    }

    [Fact]
    public async Task Validate_InvalidCVV_Fails()
    {
        // Arrange
        var dto = new ConfirmBookingDto(
            Guid.NewGuid(),
            "CreditCard",
            "4111111111111111",
            "John Doe",
            "12/25",
            "12" // Too short
        );

        // Act
        var result = await _validator.ValidateAsync(dto);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == "CVV");
    }

    [Fact]
    public async Task Validate_ValidCVVWithFourDigits_Passes()
    {
        // Arrange - American Express uses 4-digit CVV
        var dto = new ConfirmBookingDto(
            Guid.NewGuid(),
            "CreditCard",
            "4111111111111111",
            "John Doe",
            "12/25",
            "1234"
        );

        // Act
        var result = await _validator.ValidateAsync(dto);

        // Assert
        Assert.True(result.IsValid);
    }

    [Fact]
    public async Task Validate_ValidBulgarianLicensePlate_Passes()
    {
        // Arrange
        var dto = new ConfirmBookingDto(
            Guid.NewGuid(),
            "CreditCard",
            "4111111111111111",
            "John Doe",
            "12/25",
            "123",
            "CB1234AB"
        );

        // Act
        var result = await _validator.ValidateAsync(dto);

        // Assert
        Assert.True(result.IsValid);
    }

    [Fact]
    public async Task Validate_NullLicensePlate_Passes()
    {
        // Arrange - Plate is optional
        var dto = new ConfirmBookingDto(
            Guid.NewGuid(),
            "CreditCard",
            "4111111111111111",
            "John Doe",
            "12/25",
            "123",
            null
        );

        // Act
        var result = await _validator.ValidateAsync(dto);

        // Assert
        Assert.True(result.IsValid);
    }

    [Fact]
    public async Task Validate_InvalidLicensePlateFormat_Fails()
    {
        // Arrange - Non-Bulgarian format
        var dto = new ConfirmBookingDto(
            Guid.NewGuid(),
            "CreditCard",
            "4111111111111111",
            "John Doe",
            "12/25",
            "123",
            "INVALID"
        );

        // Act
        var result = await _validator.ValidateAsync(dto);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == "CarLicensePlate");
    }

    [Fact]
    public async Task Validate_LicensePlateWithLowercase_Fails()
    {
        // Arrange - Validator expects uppercase (normalization is done in service, not here)
        var dto = new ConfirmBookingDto(
            Guid.NewGuid(),
            "CreditCard",
            "4111111111111111",
            "John Doe",
            "12/25",
            "123",
            "cb1234ab"
        );

        // Act
        var result = await _validator.ValidateAsync(dto);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == "CarLicensePlate");
    }
}

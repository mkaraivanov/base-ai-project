using Application.DTOs.Reservations;
using Application.Validators;
using FluentValidation.TestHelper;
using Xunit;

namespace Tests.Unit.Validators;

public class CreateReservationDtoValidatorTests
{
    private readonly CreateReservationDtoValidator _validator;

    public CreateReservationDtoValidatorTests()
    {
        _validator = new CreateReservationDtoValidator();
    }

    [Fact]
    public void Validate_ValidDto_ShouldNotHaveValidationError()
    {
        // Arrange
        var dto = new CreateReservationDto(
            Guid.NewGuid(),
            new List<string> { "A1", "A2", "B3" }
        );

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_EmptyShowtimeId_ShouldHaveValidationError()
    {
        // Arrange
        var dto = new CreateReservationDto(
            Guid.Empty,
            new List<string> { "A1" }
        );

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.ShowtimeId)
            .WithErrorMessage("Showtime ID is required");
    }

    [Fact]
    public void Validate_EmptySeatNumbers_ShouldHaveValidationError()
    {
        // Arrange
        var dto = new CreateReservationDto(
            Guid.NewGuid(),
            new List<string>()
        );

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.SeatNumbers)
            .WithErrorMessage("At least one seat must be selected");
    }

    [Fact]
    public void Validate_MoreThan10Seats_ShouldHaveValidationError()
    {
        // Arrange
        var seats = Enumerable.Range(1, 11).Select(i => $"A{i}").ToList();
        var dto = new CreateReservationDto(Guid.NewGuid(), seats);

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.SeatNumbers)
            .WithErrorMessage("Cannot reserve more than 10 seats at once");
    }

    [Fact]
    public void Validate_DuplicateSeatNumbers_ShouldHaveValidationError()
    {
        // Arrange
        var dto = new CreateReservationDto(
            Guid.NewGuid(),
            new List<string> { "A1", "A2", "A1" }
        );

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.SeatNumbers)
            .WithErrorMessage("Duplicate seat numbers are not allowed");
    }

    [Theory]
    [InlineData("1A")]
    [InlineData("AA")]
    [InlineData("a1")]
    [InlineData("A")]
    [InlineData("")]
    [InlineData("A123")]
    public void Validate_InvalidSeatFormat_ShouldHaveValidationError(string invalidSeat)
    {
        // Arrange
        var dto = new CreateReservationDto(
            Guid.NewGuid(),
            new List<string> { invalidSeat }
        );

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.SeatNumbers);
    }

    [Theory]
    [InlineData("A1")]
    [InlineData("B12")]
    [InlineData("Z9")]
    [InlineData("C10")]
    public void Validate_ValidSeatFormat_ShouldNotHaveValidationError(string validSeat)
    {
        // Arrange
        var dto = new CreateReservationDto(
            Guid.NewGuid(),
            new List<string> { validSeat }
        );

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }
}

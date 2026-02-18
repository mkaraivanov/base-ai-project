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
            new List<SeatSelectionDto> { new("A1", Guid.NewGuid()), new("A2", Guid.NewGuid()), new("B3", Guid.NewGuid()) }
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
            new List<SeatSelectionDto> { new("A1", Guid.NewGuid()) }
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
            new List<SeatSelectionDto>()
        );

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Seats)
            .WithErrorMessage("At least one seat must be selected");
    }

    [Fact]
    public void Validate_MoreThan10Seats_ShouldHaveValidationError()
    {
        // Arrange
        var seats = Enumerable.Range(1, 11).Select(i => new SeatSelectionDto($"A{i}", Guid.NewGuid())).ToList();
        var dto = new CreateReservationDto(Guid.NewGuid(), seats);

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Seats)
            .WithErrorMessage("Cannot reserve more than 10 seats at once");
    }

    [Fact]
    public void Validate_DuplicateSeatNumbers_ShouldHaveValidationError()
    {
        // Arrange
        var dto = new CreateReservationDto(
            Guid.NewGuid(),
            new List<SeatSelectionDto> { new("A1", Guid.NewGuid()), new("A2", Guid.NewGuid()), new("A1", Guid.NewGuid()) }
        );

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Seats)
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
            new List<SeatSelectionDto> { new(invalidSeat, Guid.NewGuid()) }
        );

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor("Seats[0].SeatNumber");
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
            new List<SeatSelectionDto> { new(validSeat, Guid.NewGuid()) }
        );

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }
}

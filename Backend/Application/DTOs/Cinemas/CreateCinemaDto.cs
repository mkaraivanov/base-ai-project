namespace Application.DTOs.Cinemas;

public record CreateCinemaDto(
    string Name,
    string Address,
    string City,
    string Country,
    string? PhoneNumber,
    string? Email,
    string? LogoUrl,
    TimeOnly OpenTime,
    TimeOnly CloseTime
);

namespace Application.DTOs.Cinemas;

public record UpdateCinemaDto(
    string Name,
    string Address,
    string City,
    string Country,
    string? PhoneNumber,
    string? Email,
    string? LogoUrl,
    TimeOnly OpenTime,
    TimeOnly CloseTime,
    bool IsActive
);

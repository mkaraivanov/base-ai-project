namespace Application.DTOs.Cinemas;

public record CinemaDto(
    Guid Id,
    string Name,
    string Address,
    string City,
    string Country,
    string? PhoneNumber,
    string? Email,
    string? LogoUrl,
    TimeOnly OpenTime,
    TimeOnly CloseTime,
    bool IsActive,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    int HallCount
);

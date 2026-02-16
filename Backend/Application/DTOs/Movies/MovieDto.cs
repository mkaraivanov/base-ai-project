namespace Application.DTOs.Movies;

public record MovieDto(
    Guid Id,
    string Title,
    string Description,
    string Genre,
    int DurationMinutes,
    string Rating,
    string PosterUrl,
    DateOnly ReleaseDate,
    bool IsActive,
    DateTime CreatedAt
);

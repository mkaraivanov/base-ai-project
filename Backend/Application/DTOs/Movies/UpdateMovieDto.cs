namespace Application.DTOs.Movies;

public record UpdateMovieDto(
    string Title,
    string Description,
    string Genre,
    int DurationMinutes,
    string Rating,
    string PosterUrl,
    DateOnly ReleaseDate,
    bool IsActive
);

namespace Application.DTOs.Movies;

public record CreateMovieDto(
    string Title,
    string Description,
    string Genre,
    int DurationMinutes,
    string Rating,
    string PosterUrl,
    DateOnly ReleaseDate
);

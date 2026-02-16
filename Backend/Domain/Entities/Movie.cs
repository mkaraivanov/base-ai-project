namespace Domain.Entities;

public record Movie
{
    public Guid Id { get; init; }
    public string Title { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public string Genre { get; init; } = string.Empty;
    public int DurationMinutes { get; init; }
    public string Rating { get; init; } = string.Empty; // PG, PG-13, R, etc.
    public string PosterUrl { get; init; } = string.Empty;
    public DateOnly ReleaseDate { get; init; }
    public bool IsActive { get; init; } = true;
    public DateTime CreatedAt { get; init; }
    public DateTime UpdatedAt { get; init; }
}

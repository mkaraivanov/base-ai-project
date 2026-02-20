using Application.DTOs.Movies;
using Application.Services;
using Domain.Common;
using Domain.Entities;
using FluentAssertions;
using Infrastructure.Repositories;
using Infrastructure.Services;
using Microsoft.Extensions.Logging;
using Moq;

namespace Tests.Unit.Services;

public class MovieServiceTests
{
    private readonly Mock<IMovieRepository> _movieRepositoryMock;
    private readonly Mock<ILogger<MovieService>> _loggerMock;
    private readonly IMovieService _movieService;
    private readonly FakeTimeProvider _timeProvider;

    public MovieServiceTests()
    {
        _movieRepositoryMock = new Mock<IMovieRepository>();
        _loggerMock = new Mock<ILogger<MovieService>>();
        _timeProvider = new FakeTimeProvider(new DateTime(2026, 2, 16, 10, 0, 0, DateTimeKind.Utc));
        _movieService = new MovieService(
            _movieRepositoryMock.Object,
            _loggerMock.Object,
            Helpers.LocalizerHelper.CreateDefault(),
            _timeProvider);
    }

    [Fact]
    public async Task GetAllMoviesAsync_WhenActiveOnly_ReturnsOnlyActiveMovies()
    {
        // Arrange
        var movies = new List<Movie>
        {
            new() { Id = Guid.NewGuid(), Title = "Movie 1", IsActive = true },
            new() { Id = Guid.NewGuid(), Title = "Movie 2", IsActive = false }
        };

        _movieRepositoryMock
            .Setup(x => x.GetAllAsync(true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(movies.Where(m => m.IsActive).ToList());

        // Act
        var result = await _movieService.GetAllMoviesAsync(true);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.Should().HaveCount(1);
        result.Value![0].Title.Should().Be("Movie 1");
    }

    [Fact]
    public async Task GetMovieByIdAsync_WhenMovieExists_ReturnsMovie()
    {
        // Arrange
        var movieId = Guid.NewGuid();
        var movie = new Movie
        {
            Id = movieId,
            Title = "Test Movie",
            Description = "Test Description",
            Genre = "Action",
            DurationMinutes = 120,
            Rating = "PG-13",
            PosterUrl = "https://example.com/poster.jpg",
            ReleaseDate = new DateOnly(2026, 3, 1),
            IsActive = true,
            CreatedAt = new DateTime(2026, 2, 1, 0, 0, 0, DateTimeKind.Utc)
        };

        _movieRepositoryMock
            .Setup(x => x.GetByIdAsync(movieId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(movie);

        // Act
        var result = await _movieService.GetMovieByIdAsync(movieId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.Id.Should().Be(movieId);
        result.Value.Title.Should().Be("Test Movie");
    }

    [Fact]
    public async Task GetMovieByIdAsync_WhenMovieDoesNotExist_ReturnsFailure()
    {
        // Arrange
        var movieId = Guid.NewGuid();

        _movieRepositoryMock
            .Setup(x => x.GetByIdAsync(movieId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Movie?)null);

        // Act
        var result = await _movieService.GetMovieByIdAsync(movieId);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be("Movie not found");
    }

    [Fact]
    public async Task CreateMovieAsync_WithValidDto_CreatesMovie()
    {
        // Arrange
        var dto = new CreateMovieDto(
            Title: "New Movie",
            Description: "New Description",
            Genre: "Comedy",
            DurationMinutes: 90,
            Rating: "PG",
            PosterUrl: "https://example.com/poster.jpg",
            ReleaseDate: new DateOnly(2026, 4, 1)
        );

        _movieRepositoryMock
            .Setup(x => x.CreateAsync(It.IsAny<Movie>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Movie m, CancellationToken ct) => m);

        // Act
        var result = await _movieService.CreateMovieAsync(dto);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.Title.Should().Be("New Movie");
        result.Value.Description.Should().Be("New Description");
        result.Value.Genre.Should().Be("Comedy");
        result.Value.DurationMinutes.Should().Be(90);
        result.Value.Rating.Should().Be("PG");
        result.Value.IsActive.Should().BeTrue();
        result.Value.CreatedAt.Should().Be(new DateTime(2026, 2, 16, 10, 0, 0, DateTimeKind.Utc));

        _movieRepositoryMock.Verify(
            x => x.CreateAsync(It.IsAny<Movie>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task UpdateMovieAsync_WhenMovieExists_UpdatesMovie()
    {
        // Arrange
        var movieId = Guid.NewGuid();
        var existingMovie = new Movie
        {
            Id = movieId,
            Title = "Old Title",
            Description = "Old Description",
            Genre = "Drama",
            DurationMinutes = 100,
            Rating = "R",
            PosterUrl = "https://example.com/old.jpg",
            ReleaseDate = new DateOnly(2025, 1, 1),
            IsActive = true,
            CreatedAt = new DateTime(2025, 12, 1, 0, 0, 0, DateTimeKind.Utc),
            UpdatedAt = new DateTime(2025, 12, 1, 0, 0, 0, DateTimeKind.Utc)
        };

        var updateDto = new UpdateMovieDto(
            Title: "Updated Title",
            Description: "Updated Description",
            Genre: "Action",
            DurationMinutes: 120,
            Rating: "PG-13",
            PosterUrl: "https://example.com/new.jpg",
            ReleaseDate: new DateOnly(2026, 3, 1),
            IsActive: true
        );

        _movieRepositoryMock
            .Setup(x => x.GetByIdAsync(movieId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingMovie);

        _movieRepositoryMock
            .Setup(x => x.UpdateAsync(It.IsAny<Movie>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Movie m, CancellationToken ct) => m);

        // Act
        var result = await _movieService.UpdateMovieAsync(movieId, updateDto);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.Title.Should().Be("Updated Title");
        result.Value.Description.Should().Be("Updated Description");

        _movieRepositoryMock.Verify(
            x => x.UpdateAsync(It.Is<Movie>(m =>
                m.Id == movieId &&
                m.Title == "Updated Title" &&
                m.UpdatedAt == new DateTime(2026, 2, 16, 10, 0, 0, DateTimeKind.Utc)),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task UpdateMovieAsync_WhenMovieDoesNotExist_ReturnsFailure()
    {
        // Arrange
        var movieId = Guid.NewGuid();
        var updateDto = new UpdateMovieDto(
            Title: "Updated Title",
            Description: "Updated Description",
            Genre: "Action",
            DurationMinutes: 120,
            Rating: "PG-13",
            PosterUrl: "https://example.com/new.jpg",
            ReleaseDate: new DateOnly(2026, 3, 1),
            IsActive: true
        );

        _movieRepositoryMock
            .Setup(x => x.GetByIdAsync(movieId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Movie?)null);

        // Act
        var result = await _movieService.UpdateMovieAsync(movieId, updateDto);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be("Movie not found");
    }

    [Fact]
    public async Task DeleteMovieAsync_WhenMovieExists_DeletesMovie()
    {
        // Arrange
        var movieId = Guid.NewGuid();
        var existingMovie = new Movie
        {
            Id = movieId,
            Title = "Movie to Delete",
            IsActive = true
        };

        _movieRepositoryMock
            .Setup(x => x.GetByIdAsync(movieId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingMovie);

        _movieRepositoryMock
            .Setup(x => x.DeleteAsync(movieId, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _movieService.DeleteMovieAsync(movieId);

        // Assert
        result.IsSuccess.Should().BeTrue();

        _movieRepositoryMock.Verify(
            x => x.DeleteAsync(movieId, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task DeleteMovieAsync_WhenMovieDoesNotExist_ReturnsFailure()
    {
        // Arrange
        var movieId = Guid.NewGuid();

        _movieRepositoryMock
            .Setup(x => x.GetByIdAsync(movieId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Movie?)null);

        // Act
        var result = await _movieService.DeleteMovieAsync(movieId);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be("Movie not found");
    }
}

// Fake TimeProvider for testing
public class FakeTimeProvider : TimeProvider
{
    private readonly DateTimeOffset _fixedTime;

    public FakeTimeProvider(DateTime fixedTime)
    {
        _fixedTime = new DateTimeOffset(fixedTime);
    }

    public override DateTimeOffset GetUtcNow() => _fixedTime;
}

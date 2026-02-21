using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Data
{
    public static class TestDataSeeder
    {
        // Fixed Guid so migration data and seeder stay in sync
        public static readonly Guid DefaultCinemaId = new("10000000-0000-0000-0000-000000000001");

        private static readonly (string Title, string Genre, int Duration, string Rating, string Description, string PosterUrl, int MonthsAgo)[] MovieData =
        [
            ("Inception", "Sci-Fi", 148, "PG-13", "A thief who enters the dreams of others to steal secrets from their subconscious.", "https://image.tmdb.org/t/p/w500/9gk7adHYeDvHkCSEqAvQNLV5Uge.jpg", 6),
            ("The Dark Knight", "Action", 152, "PG-13", "Batman faces the Joker, a criminal mastermind who plunges Gotham into anarchy.", "https://image.tmdb.org/t/p/w500/qJ2tW6WMUDux911r6m7haRef0WH.jpg", 12),
            ("Interstellar", "Sci-Fi", 169, "PG-13", "A team of explorers travel through a wormhole in space in an attempt to ensure humanity's survival.", "https://image.tmdb.org/t/p/w500/gEU2QniE6E77NI6lCU6MxlNBvIx.jpg", 3),
            ("The Matrix", "Action", 136, "R", "A computer hacker learns the truth about his reality and joins a rebellion against its controllers.", "https://image.tmdb.org/t/p/w500/f89U3ADr1oiB1s9GkdPOEpXUk5H.jpg", 24),
            ("Dune", "Sci-Fi", 155, "PG-13", "A noble family becomes embroiled in a war for control over the galaxy's most valuable asset.", "https://image.tmdb.org/t/p/w500/d5NXSklpcvweasgyencYPTNDKa.jpg", 2),
            ("Oppenheimer", "Drama", 181, "R", "The story of J. Robert Oppenheimer's role in the development of the atomic bomb.", "https://image.tmdb.org/t/p/w500/8Gxv8gSFCU0XGDykEGv7zR1n2ua.jpg", 1),
            ("Avatar", "Action", 162, "PG-13", "A paraplegic marine dispatched to the moon Pandora becomes torn between following orders and protecting its world.", "https://image.tmdb.org/t/p/w500/jRXYjXNq0Cs2TcJjLkki24MLp7u.jpg", 18),
            ("The Shawshank Redemption", "Drama", 142, "R", "Two imprisoned men bond over a number of years, finding solace and eventual redemption through acts of common decency.", "https://image.tmdb.org/t/p/w500/lyQBXzOQSuE59IsHyhrp0qIiPAz.jpg", 36),
        ];

        public static async Task SeedTestDataAsync(CinemaDbContext context)
        {
            // Backfill empty poster URLs on existing movies
            var moviesWithEmptyPosters = await context.Movies
                .Where(m => m.PosterUrl == "")
                .ToListAsync();
            if (moviesWithEmptyPosters.Count > 0)
            {
                foreach (var (m, i) in moviesWithEmptyPosters.Select((m, i) => (m, i)))
                {
                    var data = MovieData[i % MovieData.Length];
                    context.Entry(m).Property(nameof(Movie.PosterUrl)).CurrentValue = data.PosterUrl;
                }
                await context.SaveChangesAsync();
            }

            // Only seed if no movies exist
            if (await context.Movies.AnyAsync()) return;

            // Ensure we have a default Cinema
            if (!await context.Cinemas.AnyAsync(c => c.Id == DefaultCinemaId))
            {
                var cinema = new Cinema
                {
                    Id = DefaultCinemaId,
                    Name = "Default Cinema",
                    Address = "123 Main Street",
                    City = "Cityville",
                    Country = "Countryland",
                    PhoneNumber = "+1-000-000-0000",
                    Email = "info@defaultcinema.local",
                    LogoUrl = null,
                    OpenTime = new TimeOnly(9, 0),
                    CloseTime = new TimeOnly(23, 0),
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };
                context.Cinemas.Add(cinema);
                await context.SaveChangesAsync();
            }

            // Create a cinema hall
            var hall = new CinemaHall
            {
                Id = Guid.NewGuid(),
                CinemaId = DefaultCinemaId,
                Name = "Main Hall",
                TotalSeats = 20,
                SeatLayoutJson = "",
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };
            context.CinemaHalls.Add(hall);

            // Create movies
            var movies = MovieData.Select((data, i) => new Movie
            {
                Id = Guid.NewGuid(),
                Title = data.Title,
                Description = data.Description,
                Genre = data.Genre,
                DurationMinutes = data.Duration,
                Rating = data.Rating,
                PosterUrl = data.PosterUrl,
                ReleaseDate = DateOnly.FromDateTime(DateTime.UtcNow.AddMonths(-data.MonthsAgo)),
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            }).ToList();
            context.Movies.AddRange(movies);

            // Create a showtime for each movie
            var showtimes = movies.Select((movie, i) => new Showtime
            {
                Id = Guid.NewGuid(),
                MovieId = movie.Id,
                CinemaHallId = hall.Id,
                StartTime = DateTime.UtcNow.AddHours(2 + i * 3),
                EndTime = DateTime.UtcNow.AddHours(4 + i * 3),
                BasePrice = 10 + (i % 3) * 2,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            }).ToList();
            context.Showtimes.AddRange(showtimes);
            var showtime = showtimes[0]; // used below for seat creation

            // Create seats for the first showtime
            var seats = new List<Seat>();
            for (int i = 1; i <= hall.TotalSeats; i++)
            {
                seats.Add(new Seat
                {
                    Id = Guid.NewGuid(),
                    ShowtimeId = showtime.Id,
                    SeatNumber = $"A{i}",
                    SeatType = "Regular",
                    Status = SeatStatus.Available,
                    Price = 10,
                    ReservationId = null,
                    ReservedUntil = null,
                    RowVersion = Array.Empty<byte>(),
                    Showtime = null
                });
            }
            context.Seats.AddRange(seats);

            await context.SaveChangesAsync();
        }
    }
}

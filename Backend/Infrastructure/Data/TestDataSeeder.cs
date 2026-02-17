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
        public static async Task SeedTestDataAsync(CinemaDbContext context)
        {
            // Only seed if no movies exist
            if (await context.Movies.AnyAsync()) return;

            // Create a cinema hall
            var hall = new CinemaHall
            {
                Id = Guid.NewGuid(),
                Name = "Main Hall",
                TotalSeats = 20,
                SeatLayoutJson = "",
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };
            context.CinemaHalls.Add(hall);

            // Create a movie
            var movie = new Movie
            {
                Id = Guid.NewGuid(),
                Title = "Test Movie",
                Description = "A movie for E2E testing.",
                Genre = "Action",
                DurationMinutes = 120,
                Rating = "PG",
                PosterUrl = "",
                ReleaseDate = DateOnly.FromDateTime(DateTime.UtcNow.AddMonths(-1)),
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            context.Movies.Add(movie);

            // Create a showtime
            var showtime = new Showtime
            {
                Id = Guid.NewGuid(),
                MovieId = movie.Id,
                CinemaHallId = hall.Id,
                StartTime = DateTime.UtcNow.AddHours(2),
                EndTime = DateTime.UtcNow.AddHours(4),
                BasePrice = 10,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };
            context.Showtimes.Add(showtime);

            // Create seats
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

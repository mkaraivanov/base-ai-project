using Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Data;

public class CinemaDbContext : DbContext
{
    public CinemaDbContext(DbContextOptions<CinemaDbContext> options)
        : base(options)
    {
    }

    public DbSet<Cinema> Cinemas => Set<Cinema>();
    public DbSet<User> Users => Set<User>();
    public DbSet<Movie> Movies => Set<Movie>();
    public DbSet<CinemaHall> CinemaHalls => Set<CinemaHall>();
    public DbSet<Showtime> Showtimes => Set<Showtime>();
    public DbSet<Seat> Seats => Set<Seat>();
    public DbSet<Reservation> Reservations => Set<Reservation>();
    public DbSet<Payment> Payments => Set<Payment>();
    public DbSet<Booking> Bookings => Set<Booking>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // User configuration
        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.Id);

            entity.Property(e => e.Email)
                .IsRequired()
                .HasMaxLength(200);

            entity.Property(e => e.PasswordHash)
                .IsRequired();

            entity.Property(e => e.FirstName)
                .IsRequired()
                .HasMaxLength(100);

            entity.Property(e => e.LastName)
                .IsRequired()
                .HasMaxLength(100);

            entity.Property(e => e.PhoneNumber)
                .IsRequired()
                .HasMaxLength(20);

            entity.Property(e => e.Role)
                .HasConversion<string>();

            entity.HasIndex(e => e.Email)
                .IsUnique();
        });

        // Movie configuration
        modelBuilder.Entity<Movie>(entity =>
        {
            entity.HasKey(e => e.Id);

            entity.Property(e => e.Title)
                .IsRequired()
                .HasMaxLength(200);

            entity.Property(e => e.Description)
                .HasMaxLength(2000);

            entity.Property(e => e.Genre)
                .IsRequired()
                .HasMaxLength(50);

            entity.Property(e => e.Rating)
                .IsRequired()
                .HasMaxLength(10);

            entity.Property(e => e.PosterUrl)
                .HasMaxLength(500);

            entity.HasIndex(e => e.IsActive);
            entity.HasIndex(e => e.Genre);
        });

        // Cinema configuration
        modelBuilder.Entity<Cinema>(entity =>
        {
            entity.HasKey(e => e.Id);

            entity.Property(e => e.Name)
                .IsRequired()
                .HasMaxLength(100);

            entity.Property(e => e.Address)
                .IsRequired()
                .HasMaxLength(200);

            entity.Property(e => e.City)
                .IsRequired()
                .HasMaxLength(100);

            entity.Property(e => e.Country)
                .IsRequired()
                .HasMaxLength(100);

            entity.Property(e => e.PhoneNumber)
                .HasMaxLength(20);

            entity.Property(e => e.Email)
                .HasMaxLength(200);

            entity.Property(e => e.LogoUrl)
                .HasMaxLength(500);

            entity.HasIndex(e => e.IsActive);
            entity.HasIndex(e => new { e.Name, e.City });
        });

        // CinemaHall configuration
        modelBuilder.Entity<CinemaHall>(entity =>
        {
            entity.HasKey(e => e.Id);

            entity.Property(e => e.Name)
                .IsRequired()
                .HasMaxLength(100);

            entity.Property(e => e.SeatLayoutJson)
                .IsRequired()
                .HasColumnType("nvarchar(max)");

            entity.HasOne(e => e.Cinema)
                .WithMany(c => c.Halls)
                .HasForeignKey(e => e.CinemaId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasIndex(e => e.IsActive);
            entity.HasIndex(e => e.CinemaId);
        });

        // Showtime configuration
        modelBuilder.Entity<Showtime>(entity =>
        {
            entity.HasKey(e => e.Id);

            entity.Property(e => e.BasePrice)
                .HasPrecision(10, 2);

            entity.HasOne(e => e.Movie)
                .WithMany()
                .HasForeignKey(e => e.MovieId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.CinemaHall)
                .WithMany()
                .HasForeignKey(e => e.CinemaHallId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasIndex(e => new { e.StartTime, e.CinemaHallId });
            entity.HasIndex(e => e.MovieId);
            entity.HasIndex(e => e.IsActive);
        });

        // Seat configuration (CRITICAL for concurrency)
        modelBuilder.Entity<Seat>(entity =>
        {
            entity.HasKey(e => e.Id);

            entity.Property(e => e.SeatNumber)
                .IsRequired()
                .HasMaxLength(10);

            entity.Property(e => e.Price)
                .HasPrecision(10, 2);

            entity.Property(e => e.SeatType)
                .IsRequired()
                .HasMaxLength(20);

            // Optimistic concurrency control
            entity.Property(e => e.RowVersion)
                .IsRowVersion();

            // Enum to string conversion
            entity.Property(e => e.Status)
                .HasConversion<string>();

            entity.HasOne(e => e.Showtime)
                .WithMany()
                .HasForeignKey(e => e.ShowtimeId)
                .OnDelete(DeleteBehavior.Cascade);

            // Unique constraint for seat per showtime
            entity.HasIndex(e => new { e.ShowtimeId, e.SeatNumber })
                .IsUnique();

            // Index for fast availability queries
            entity.HasIndex(e => new { e.ShowtimeId, e.Status });

            // Index for reservation-based seat lookups
            entity.HasIndex(e => e.ReservationId);
        });

        // Reservation configuration
        modelBuilder.Entity<Reservation>(entity =>
        {
            entity.HasKey(e => e.Id);

            entity.Property(e => e.TotalAmount)
                .HasPrecision(10, 2);

            entity.Property(e => e.SeatNumbers)
                .HasConversion(
                    v => string.Join(',', v),
                    v => v.Split(',', StringSplitOptions.RemoveEmptyEntries).ToList()
                )
                .HasMaxLength(500);

            entity.Property(e => e.Status)
                .HasConversion<string>();

            entity.HasOne(e => e.User)
                .WithMany()
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.Showtime)
                .WithMany()
                .HasForeignKey(e => e.ShowtimeId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasIndex(e => new { e.UserId, e.Status });
            entity.HasIndex(e => e.ExpiresAt); // Critical for cleanup job
            entity.HasIndex(e => new { e.ShowtimeId, e.Status });
        });

        // Payment configuration
        modelBuilder.Entity<Payment>(entity =>
        {
            entity.HasKey(e => e.Id);

            entity.Property(e => e.Amount)
                .HasPrecision(10, 2);

            entity.Property(e => e.PaymentMethod)
                .IsRequired()
                .HasMaxLength(50);

            entity.Property(e => e.TransactionId)
                .IsRequired()
                .HasMaxLength(100);

            entity.HasOne(e => e.Booking)
                .WithOne(b => b.Payment)
                .HasForeignKey<Payment>(e => e.BookingId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasIndex(e => e.TransactionId).IsUnique();
            entity.HasIndex(e => e.Status);
        });

        // Booking configuration
        modelBuilder.Entity<Booking>(entity =>
        {
            entity.HasKey(e => e.Id);

            entity.Property(e => e.BookingNumber)
                .IsRequired()
                .HasMaxLength(20);

            entity.Property(e => e.TotalAmount)
                .HasPrecision(10, 2);

            entity.Property(e => e.SeatNumbers)
                .HasConversion(
                    v => string.Join(',', v),
                    v => v.Split(',', StringSplitOptions.RemoveEmptyEntries).ToList()
                )
                .HasMaxLength(500);

            entity.HasOne(e => e.User)
                .WithMany()
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.Showtime)
                .WithMany()
                .HasForeignKey(e => e.ShowtimeId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasIndex(e => e.BookingNumber).IsUnique();
            entity.HasIndex(e => new { e.UserId, e.Status });
            entity.HasIndex(e => e.ShowtimeId);

            entity.Property(e => e.CarLicensePlate)
                .HasMaxLength(10);
        });
    }
}

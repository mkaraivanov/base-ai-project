using Domain.Entities;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

public class BookingRepository : IBookingRepository
{
    private readonly CinemaDbContext _context;

    public BookingRepository(CinemaDbContext context)
    {
        _context = context;
    }

    public async Task<Booking?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        return await _context.Bookings
            .AsNoTracking()
            .Include(b => b.Showtime)
                .ThenInclude(s => s!.Movie)
            .Include(b => b.Showtime)
                .ThenInclude(s => s!.CinemaHall)
            .FirstOrDefaultAsync(b => b.Id == id, ct);
    }

    public async Task<Booking?> GetByBookingNumberAsync(string bookingNumber, CancellationToken ct = default)
    {
        return await _context.Bookings
            .AsNoTracking()
            .Include(b => b.Showtime)
                .ThenInclude(s => s!.Movie)
            .Include(b => b.Showtime)
                .ThenInclude(s => s!.CinemaHall)
            .FirstOrDefaultAsync(b => b.BookingNumber == bookingNumber, ct);
    }

    public async Task<List<Booking>> GetByUserIdAsync(Guid userId, CancellationToken ct = default)
    {
        return await _context.Bookings
            .AsNoTracking()
            .Include(b => b.Showtime)
                .ThenInclude(s => s!.Movie)
            .Include(b => b.Showtime)
                .ThenInclude(s => s!.CinemaHall)
            .Where(b => b.UserId == userId)
            .OrderByDescending(b => b.BookedAt)
            .ToListAsync(ct);
    }

    public async Task<int> GetConfirmedCountByUserIdAsync(Guid userId, CancellationToken ct = default)
    {
        // Count all non-cancelled confirmed bookings for this user.
        // This is used by the loyalty service to detect and correct missing stamps.
        return await _context.Bookings
            .AsNoTracking()
            .CountAsync(b => b.UserId == userId && b.Status == BookingStatus.Confirmed, ct);
    }

    public async Task<Booking> CreateAsync(Booking booking, CancellationToken ct = default)
    {
        _context.Bookings.Add(booking);
        await _context.SaveChangesAsync(ct);
        return booking;
    }

    public async Task<Booking> UpdateAsync(Booking booking, CancellationToken ct = default)
    {
        _context.Bookings.Update(booking);
        await _context.SaveChangesAsync(ct);
        return booking;
    }
}

using Application.DTOs.Reporting;
using Domain.Entities;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System.Text;

namespace Infrastructure.Repositories;

public class ReportingRepository : IReportingRepository
{
    private readonly CinemaDbContext _context;

    public ReportingRepository(CinemaDbContext context)
    {
        _context = context;
    }

    // ── Sales by Date ─────────────────────────────────────────────────────────

    public async Task<List<SalesByDateDto>> GetSalesByDateAsync(
        ReportQueryDto query, CancellationToken ct = default)
    {
        var current = await QuerySalesByDate(query.From, query.To, query.Granularity, query.CinemaId, ct);

        if (!query.Compare)
            return current.Select(r => new SalesByDateDto(r.Period, r.TicketsSold, r.Revenue)).ToList();

        // Year-over-year: shift from/to back one year
        var compareFrom = query.From.AddYears(-1);
        var compareTo = query.To.AddYears(-1);
        var prior = await QuerySalesByDate(compareFrom, compareTo, query.Granularity, query.CinemaId, ct);

        var priorLookup = prior.ToDictionary(r => r.Period);
        return current.Select(r =>
        {
            var hasPrior = priorLookup.TryGetValue(r.Period, out var cmp);
            return new SalesByDateDto(
                r.Period,
                r.TicketsSold,
                r.Revenue,
                hasPrior ? cmp.TicketsSold : (int?)null,
                hasPrior ? cmp.Revenue : (decimal?)null);
        }).ToList();
    }

    public async Task<List<SalesByDateDto>> GetSalesByDateCompareAsync(
        ReportQueryDto query, CancellationToken ct = default)
        => await GetSalesByDateAsync(query with { Compare = true }, ct);

    // ── Sales by Movie ────────────────────────────────────────────────────────

    public async Task<List<SalesByMovieDto>> GetSalesByMovieAsync(
        ReportQueryDto query, CancellationToken ct = default)
    {
        var joined = ConfirmedTickets(query.From, query.To);

        if (query.CinemaId.HasValue)
            joined = joined.Where(x => x.Cinema.Id == query.CinemaId.Value);

        var rows = await joined
            .GroupBy(x => new { x.Showtime.MovieId, MovieTitle = x.Movie.Title })
            .Select(g => new
            {
                g.Key.MovieId,
                g.Key.MovieTitle,
                TicketsSold = g.Count(),
                Revenue = g.Sum(x => x.Ticket.UnitPrice)
            })
            .ToListAsync(ct);

        // Capacity: sum of hall seats for all showtimes of each movie in range
        var movieIds = rows.Select(r => r.MovieId).ToList();

        var capacities = await _context.Showtimes
            .AsNoTracking()
            .Where(s => movieIds.Contains(s.MovieId)
                     && s.StartTime >= query.From
                     && s.StartTime <= query.To)
            .GroupBy(s => s.MovieId)
            .Select(g => new { MovieId = g.Key, Total = g.Sum(s => s.CinemaHall!.TotalSeats) })
            .ToListAsync(ct);

        var capacityLookup = capacities.ToDictionary(c => c.MovieId, c => c.Total);

        return rows.Select(r =>
        {
            var cap = capacityLookup.GetValueOrDefault(r.MovieId, 0);
            var pct = cap > 0 ? Math.Round((double)r.TicketsSold / cap * 100, 2) : 0;
            return new SalesByMovieDto(r.MovieId, r.MovieTitle, r.TicketsSold, r.Revenue, cap, pct);
        })
        .OrderByDescending(r => r.Revenue)
        .ToList();
    }

    // ── Sales by Showtime ─────────────────────────────────────────────────────

    public async Task<List<SalesByShowtimeDto>> GetSalesByShowtimeAsync(
        ReportQueryDto query, CancellationToken ct = default)
    {
        var joined = ConfirmedTickets(query.From, query.To);

        if (query.CinemaId.HasValue)
            joined = joined.Where(x => x.Cinema.Id == query.CinemaId.Value);

        if (query.MovieId.HasValue)
            joined = joined.Where(x => x.Showtime.MovieId == query.MovieId.Value);

        return await joined
            .GroupBy(x => new
            {
                x.Booking.ShowtimeId,
                StartTime  = x.Showtime.StartTime,
                MovieTitle = x.Movie.Title,
                HallName   = x.Hall.Name,
                CinemaName = x.Cinema.Name,
                Capacity   = x.Hall.TotalSeats
            })
            .Select(g => new SalesByShowtimeDto(
                g.Key.ShowtimeId,
                g.Key.StartTime,
                g.Key.MovieTitle,
                g.Key.HallName,
                g.Key.CinemaName,
                g.Count(),
                g.Key.Capacity,
                g.Key.Capacity > 0
                    ? Math.Round((double)g.Count() / g.Key.Capacity * 100, 2)
                    : 0.0,
                g.Sum(x => x.Ticket.UnitPrice)))
            .OrderByDescending(r => r.StartTime)
            .ToListAsync(ct);
    }

    // ── Sales by Location ─────────────────────────────────────────────────────

    public async Task<List<SalesByLocationDto>> GetSalesByLocationAsync(
        ReportQueryDto query, CancellationToken ct = default)
    {
        return await ConfirmedTickets(query.From, query.To)
            .GroupBy(x => new
            {
                CinemaId   = x.Cinema.Id,
                CinemaName = x.Cinema.Name,
                City       = x.Cinema.City,
                Country    = x.Cinema.Country
            })
            .Select(g => new SalesByLocationDto(
                g.Key.CinemaId,
                g.Key.CinemaName,
                g.Key.City,
                g.Key.Country,
                g.Count(),
                g.Sum(x => x.Ticket.UnitPrice)))
            .OrderByDescending(r => r.Revenue)
            .ToListAsync(ct);
    }

    // ── CSV Exports ───────────────────────────────────────────────────────────

    public async Task<byte[]> ExportSalesByDateCsvAsync(ReportQueryDto query, CancellationToken ct = default)
    {
        var data = await GetSalesByDateAsync(query, ct);
        var sb = new StringBuilder();
        sb.AppendLine("Period,TicketsSold,Revenue,CompareTicketsSold,CompareRevenue");
        foreach (var r in data)
            sb.AppendLine($"{r.Period},{r.TicketsSold},{r.Revenue},{r.CompareTicketsSold},{r.CompareRevenue}");
        return Encoding.UTF8.GetBytes(sb.ToString());
    }

    public async Task<byte[]> ExportSalesByMovieCsvAsync(ReportQueryDto query, CancellationToken ct = default)
    {
        var data = await GetSalesByMovieAsync(query, ct);
        var sb = new StringBuilder();
        sb.AppendLine("MovieId,MovieTitle,TicketsSold,Revenue,TotalCapacity,CapacityUsedPercent");
        foreach (var r in data)
            sb.AppendLine($"{r.MovieId},{EscapeCsv(r.MovieTitle)},{r.TicketsSold},{r.Revenue},{r.TotalCapacity},{r.CapacityUsedPercent}");
        return Encoding.UTF8.GetBytes(sb.ToString());
    }

    public async Task<byte[]> ExportSalesByLocationCsvAsync(ReportQueryDto query, CancellationToken ct = default)
    {
        var data = await GetSalesByLocationAsync(query, ct);
        var sb = new StringBuilder();
        sb.AppendLine("CinemaId,CinemaName,City,Country,TicketsSold,Revenue");
        foreach (var r in data)
            sb.AppendLine($"{r.CinemaId},{EscapeCsv(r.CinemaName)},{EscapeCsv(r.City)},{EscapeCsv(r.Country)},{r.TicketsSold},{r.Revenue}");
        return Encoding.UTF8.GetBytes(sb.ToString());
    }

    // ── Private helpers ───────────────────────────────────────────────────────

    /// <summary>Returns a flat join of confirmed BookingTickets with all related data.</summary>
    private IQueryable<BookingTicketFlat> ConfirmedTickets(DateTime startDate, DateTime endDate)
        => from b  in _context.Bookings.AsNoTracking()
           join bt in _context.BookingTickets.AsNoTracking() on b.Id equals bt.BookingId
           join s  in _context.Showtimes.AsNoTracking() on b.ShowtimeId equals s.Id
           join m  in _context.Movies.AsNoTracking() on s.MovieId equals m.Id
           join ch in _context.CinemaHalls.AsNoTracking() on s.CinemaHallId equals ch.Id
           join c  in _context.Cinemas.AsNoTracking() on ch.CinemaId equals c.Id
           where b.Status == BookingStatus.Confirmed
              && b.BookedAt >= startDate
              && b.BookedAt <= endDate
           select new BookingTicketFlat
           {
               Booking  = b,
               Ticket   = bt,
               Showtime = s,
               Movie    = m,
               Hall     = ch,
               Cinema   = c
           };

    private async Task<List<(string Period, int TicketsSold, decimal Revenue)>> QuerySalesByDate(
        DateTime startDate, DateTime endDate, string granularity, Guid? cinemaId, CancellationToken ct)
    {
        var joined = ConfirmedTickets(startDate, endDate);

        if (cinemaId.HasValue)
            joined = joined.Where(x => x.Cinema.Id == cinemaId.Value);

        if (granularity == "Monthly")
        {
            var rows = await joined
                .GroupBy(x => new { x.Booking.BookedAt.Year, x.Booking.BookedAt.Month })
                .Select(g => new
                {
                    Period      = g.Key.Year.ToString() + "-" + g.Key.Month.ToString("D2"),
                    TicketsSold = g.Count(),
                    Revenue     = g.Sum(x => x.Ticket.UnitPrice)
                })
                .OrderBy(r => r.Period)
                .ToListAsync(ct);
            return rows.Select(r => (r.Period, r.TicketsSold, r.Revenue)).ToList();
        }
        else if (granularity == "Weekly")
        {
            var rows = await joined
                .GroupBy(x => new
                {
                    x.Booking.BookedAt.Year,
                    Week = (x.Booking.BookedAt.DayOfYear - 1) / 7 + 1
                })
                .Select(g => new
                {
                    Period      = g.Key.Year.ToString() + "-W" + g.Key.Week.ToString("D2"),
                    TicketsSold = g.Count(),
                    Revenue     = g.Sum(x => x.Ticket.UnitPrice)
                })
                .OrderBy(r => r.Period)
                .ToListAsync(ct);
            return rows.Select(r => (r.Period, r.TicketsSold, r.Revenue)).ToList();
        }
        else // Daily
        {
            var rows = await joined
                .GroupBy(x => x.Booking.BookedAt.Date)
                .Select(g => new
                {
                    Period      = g.Key.ToString("yyyy-MM-dd"),
                    TicketsSold = g.Count(),
                    Revenue     = g.Sum(x => x.Ticket.UnitPrice)
                })
                .OrderBy(r => r.Period)
                .ToListAsync(ct);
            return rows.Select(r => (r.Period, r.TicketsSold, r.Revenue)).ToList();
        }
    }

    private static string EscapeCsv(string value)
    {
        if (value.Contains(',') || value.Contains('"') || value.Contains('\n'))
            return $"\"{value.Replace("\"", "\"\"")}\"";
        return value;
    }
    // ── Projection type ───────────────────────────────────────────────────────

    private sealed class BookingTicketFlat
    {
        public Booking Booking { get; init; } = null!;
        public BookingTicket Ticket { get; init; } = null!;
        public Showtime Showtime { get; init; } = null!;
        public Movie Movie { get; init; } = null!;
        public CinemaHall Hall { get; init; } = null!;
        public Cinema Cinema { get; init; } = null!;
    }}

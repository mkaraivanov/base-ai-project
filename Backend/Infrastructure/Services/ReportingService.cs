using Application.DTOs.Reporting;
using Application.Services;
using Backend.Infrastructure.Caching;
using Domain.Common;
using Infrastructure.Repositories;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Services;

public class ReportingService : IReportingService
{
    private readonly IReportingRepository _repository;
    private readonly ICacheService _cache;
    private readonly ILogger<ReportingService> _logger;
    private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(5);

    public ReportingService(
        IReportingRepository repository,
        ICacheService cache,
        ILogger<ReportingService> logger)
    {
        _repository = repository;
        _cache = cache;
        _logger = logger;
    }

    public async Task<Result<List<SalesByDateDto>>> GetSalesByDateAsync(
        ReportQueryDto query, CancellationToken ct = default)
    {
        try
        {
            ValidateQuery(query);
            var key = BuildCacheKey("sales-by-date", query);
            var cached = await _cache.GetAsync<List<SalesByDateDto>>(key, ct);
            if (cached is not null)
                return Result<List<SalesByDateDto>>.Success(cached);

            var data = await _repository.GetSalesByDateAsync(query, ct);
            await _cache.SetAsync(key, data, CacheDuration, ct);
            return Result<List<SalesByDateDto>>.Success(data);
        }
        catch (ArgumentException ex)
        {
            return Result<List<SalesByDateDto>>.Failure(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving sales-by-date report");
            return Result<List<SalesByDateDto>>.Failure("Failed to retrieve sales by date report");
        }
    }

    public async Task<Result<List<SalesByMovieDto>>> GetSalesByMovieAsync(
        ReportQueryDto query, CancellationToken ct = default)
    {
        try
        {
            ValidateQuery(query);
            var key = BuildCacheKey("sales-by-movie", query);
            var cached = await _cache.GetAsync<List<SalesByMovieDto>>(key, ct);
            if (cached is not null)
                return Result<List<SalesByMovieDto>>.Success(cached);

            var data = await _repository.GetSalesByMovieAsync(query, ct);
            await _cache.SetAsync(key, data, CacheDuration, ct);
            return Result<List<SalesByMovieDto>>.Success(data);
        }
        catch (ArgumentException ex)
        {
            return Result<List<SalesByMovieDto>>.Failure(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving sales-by-movie report");
            return Result<List<SalesByMovieDto>>.Failure("Failed to retrieve sales by movie report");
        }
    }

    public async Task<Result<List<SalesByShowtimeDto>>> GetSalesByShowtimeAsync(
        ReportQueryDto query, CancellationToken ct = default)
    {
        try
        {
            ValidateQuery(query);
            var key = BuildCacheKey("sales-by-showtime", query);
            var cached = await _cache.GetAsync<List<SalesByShowtimeDto>>(key, ct);
            if (cached is not null)
                return Result<List<SalesByShowtimeDto>>.Success(cached);

            var data = await _repository.GetSalesByShowtimeAsync(query, ct);
            await _cache.SetAsync(key, data, CacheDuration, ct);
            return Result<List<SalesByShowtimeDto>>.Success(data);
        }
        catch (ArgumentException ex)
        {
            return Result<List<SalesByShowtimeDto>>.Failure(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving sales-by-showtime report");
            return Result<List<SalesByShowtimeDto>>.Failure("Failed to retrieve sales by showtime report");
        }
    }

    public async Task<Result<List<SalesByLocationDto>>> GetSalesByLocationAsync(
        ReportQueryDto query, CancellationToken ct = default)
    {
        try
        {
            ValidateQuery(query);
            var key = BuildCacheKey("sales-by-location", query);
            var cached = await _cache.GetAsync<List<SalesByLocationDto>>(key, ct);
            if (cached is not null)
                return Result<List<SalesByLocationDto>>.Success(cached);

            var data = await _repository.GetSalesByLocationAsync(query, ct);
            await _cache.SetAsync(key, data, CacheDuration, ct);
            return Result<List<SalesByLocationDto>>.Success(data);
        }
        catch (ArgumentException ex)
        {
            return Result<List<SalesByLocationDto>>.Failure(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving sales-by-location report");
            return Result<List<SalesByLocationDto>>.Failure("Failed to retrieve sales by location report");
        }
    }

    public async Task<Result<byte[]>> ExportCsvAsync(
        string reportType, ReportQueryDto query, CancellationToken ct = default)
    {
        try
        {
            ValidateQuery(query);
            var bytes = reportType switch
            {
                "date"     => await _repository.ExportSalesByDateCsvAsync(query, ct),
                "movie"    => await _repository.ExportSalesByMovieCsvAsync(query, ct),
                "location" => await _repository.ExportSalesByLocationCsvAsync(query, ct),
                _ => throw new ArgumentException($"Unknown report type: {reportType}")
            };
            return Result<byte[]>.Success(bytes);
        }
        catch (ArgumentException ex)
        {
            return Result<byte[]>.Failure(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error exporting CSV for report type {ReportType}", reportType);
            return Result<byte[]>.Failure("Failed to export CSV");
        }
    }

    private static void ValidateQuery(ReportQueryDto query)
    {
        if (query.From > query.To)
            throw new ArgumentException("'from' date must be before 'to' date");

        if ((query.To - query.From).TotalDays > 366)
            throw new ArgumentException("Date range cannot exceed 366 days");

        if (!new[] { "Daily", "Weekly", "Monthly" }.Contains(query.Granularity))
            throw new ArgumentException($"Invalid granularity '{query.Granularity}'. Must be Daily, Weekly, or Monthly");
    }

    private static string BuildCacheKey(string type, ReportQueryDto query)
        => $"report:{type}:{query.From:yyyyMMdd}:{query.To:yyyyMMdd}:{query.Granularity}:{query.Compare}:{query.CinemaId}:{query.MovieId}";
}

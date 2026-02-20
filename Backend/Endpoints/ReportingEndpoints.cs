using Application.DTOs.Reporting;
using Application.Services;
using Backend.Models;

namespace Backend.Endpoints;

public static class ReportingEndpoints
{
    public static RouteGroupBuilder MapReportingEndpoints(this RouteGroupBuilder group)
    {
        group.MapGet("/by-date", GetSalesByDateAsync)
            .WithName("GetSalesByDate")
            .RequireAuthorization("Admin");

        group.MapGet("/by-movie", GetSalesByMovieAsync)
            .WithName("GetSalesByMovie")
            .RequireAuthorization("Admin");

        group.MapGet("/by-showtime", GetSalesByShowtimeAsync)
            .WithName("GetSalesByShowtime")
            .RequireAuthorization("Admin");

        group.MapGet("/by-location", GetSalesByLocationAsync)
            .WithName("GetSalesByLocation")
            .RequireAuthorization("Admin");

        group.MapGet("/export/{reportType}", ExportCsvAsync)
            .WithName("ExportReportCsv")
            .RequireAuthorization("Admin");

        return group;
    }

    private static async Task<IResult> GetSalesByDateAsync(
        DateTime from,
        DateTime to,
        string granularity = "Daily",
        bool compare = false,
        Guid? cinemaId = null,
        IReportingService reportingService = null!,
        CancellationToken ct = default)
    {
        var query = new ReportQueryDto(from, to, granularity, compare, cinemaId);
        var result = await reportingService.GetSalesByDateAsync(query, ct);
        return result.IsSuccess
            ? Results.Ok(new ApiResponse<List<SalesByDateDto>>(true, result.Value, null))
            : Results.BadRequest(new ApiResponse<List<SalesByDateDto>>(false, null, result.Error));
    }

    private static async Task<IResult> GetSalesByMovieAsync(
        DateTime from,
        DateTime to,
        Guid? cinemaId = null,
        IReportingService reportingService = null!,
        CancellationToken ct = default)
    {
        var query = new ReportQueryDto(from, to, CinemaId: cinemaId);
        var result = await reportingService.GetSalesByMovieAsync(query, ct);
        return result.IsSuccess
            ? Results.Ok(new ApiResponse<List<SalesByMovieDto>>(true, result.Value, null))
            : Results.BadRequest(new ApiResponse<List<SalesByMovieDto>>(false, null, result.Error));
    }

    private static async Task<IResult> GetSalesByShowtimeAsync(
        DateTime from,
        DateTime to,
        Guid? cinemaId = null,
        Guid? movieId = null,
        IReportingService reportingService = null!,
        CancellationToken ct = default)
    {
        var query = new ReportQueryDto(from, to, CinemaId: cinemaId, MovieId: movieId);
        var result = await reportingService.GetSalesByShowtimeAsync(query, ct);
        return result.IsSuccess
            ? Results.Ok(new ApiResponse<List<SalesByShowtimeDto>>(true, result.Value, null))
            : Results.BadRequest(new ApiResponse<List<SalesByShowtimeDto>>(false, null, result.Error));
    }

    private static async Task<IResult> GetSalesByLocationAsync(
        DateTime from,
        DateTime to,
        IReportingService reportingService = null!,
        CancellationToken ct = default)
    {
        var query = new ReportQueryDto(from, to);
        var result = await reportingService.GetSalesByLocationAsync(query, ct);
        return result.IsSuccess
            ? Results.Ok(new ApiResponse<List<SalesByLocationDto>>(true, result.Value, null))
            : Results.BadRequest(new ApiResponse<List<SalesByLocationDto>>(false, null, result.Error));
    }

    private static async Task<IResult> ExportCsvAsync(
        string reportType,
        DateTime from,
        DateTime to,
        string granularity = "Daily",
        bool compare = false,
        Guid? cinemaId = null,
        Guid? movieId = null,
        IReportingService reportingService = null!,
        CancellationToken ct = default)
    {
        var query = new ReportQueryDto(from, to, granularity, compare, cinemaId, movieId);
        var result = await reportingService.ExportCsvAsync(reportType, query, ct);
        if (!result.IsSuccess)
            return Results.BadRequest(new ApiResponse<object>(false, null, result.Error));

        var filename = $"report-{reportType}-{from:yyyyMMdd}-{to:yyyyMMdd}.csv";
        return Results.File(result.Value!, "text/csv", filename);
    }
}

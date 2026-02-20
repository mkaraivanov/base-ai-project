using Application.DTOs.Reporting;
using Domain.Common;

namespace Application.Services;

public interface IReportingService
{
    Task<Result<List<SalesByDateDto>>> GetSalesByDateAsync(ReportQueryDto query, CancellationToken ct = default);
    Task<Result<List<SalesByMovieDto>>> GetSalesByMovieAsync(ReportQueryDto query, CancellationToken ct = default);
    Task<Result<List<SalesByShowtimeDto>>> GetSalesByShowtimeAsync(ReportQueryDto query, CancellationToken ct = default);
    Task<Result<List<SalesByLocationDto>>> GetSalesByLocationAsync(ReportQueryDto query, CancellationToken ct = default);
    Task<Result<byte[]>> ExportCsvAsync(string reportType, ReportQueryDto query, CancellationToken ct = default);
}

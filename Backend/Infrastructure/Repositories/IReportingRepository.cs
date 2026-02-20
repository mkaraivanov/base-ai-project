using Application.DTOs.Reporting;

namespace Infrastructure.Repositories;

public interface IReportingRepository
{
    Task<List<SalesByDateDto>> GetSalesByDateAsync(ReportQueryDto query, CancellationToken ct = default);
    Task<List<SalesByDateDto>> GetSalesByDateCompareAsync(ReportQueryDto query, CancellationToken ct = default);
    Task<List<SalesByMovieDto>> GetSalesByMovieAsync(ReportQueryDto query, CancellationToken ct = default);
    Task<List<SalesByShowtimeDto>> GetSalesByShowtimeAsync(ReportQueryDto query, CancellationToken ct = default);
    Task<List<SalesByLocationDto>> GetSalesByLocationAsync(ReportQueryDto query, CancellationToken ct = default);
    Task<byte[]> ExportSalesByDateCsvAsync(ReportQueryDto query, CancellationToken ct = default);
    Task<byte[]> ExportSalesByMovieCsvAsync(ReportQueryDto query, CancellationToken ct = default);
    Task<byte[]> ExportSalesByLocationCsvAsync(ReportQueryDto query, CancellationToken ct = default);
}

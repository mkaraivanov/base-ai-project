namespace Application.DTOs.Reporting;

public record ReportQueryDto(
    DateTime From,
    DateTime To,
    string Granularity = "Daily",   // Daily | Weekly | Monthly
    bool Compare = false,
    Guid? CinemaId = null,
    Guid? MovieId = null
);

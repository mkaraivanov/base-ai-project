# Phase 7: Ticket Sales Reporting Module — Implementation Plan

## Overview

This phase introduces a comprehensive Ticket Sales Reporting module, covering all key business dimensions: by date/time, by movie, by showtime/screen, and by location. The solution spans backend (C# ASP.NET Core, EF Core) and frontend (React, TypeScript, Recharts), with admin-only access, CSV export, and optional year-over-year (YoY) comparison.

---

## 1. Backend

### 1.1 DTOs
- `ReportQueryDto.cs`: Query params (date range, granularity, compare flag, optional cinema/movie).
- `SalesByDateDto.cs`: Period, tickets sold, revenue, comparison fields.
- `SalesByMovieDto.cs`: Movie, tickets sold, revenue, capacity used.
- `SalesByShowtimeDto.cs`: Showtime, movie, hall, tickets, capacity, revenue.
- `SalesByLocationDto.cs`: Cinema, city, country, tickets, revenue.

### 1.2 Repository
- `IReportingRepository.cs`: Five methods for each report dimension and CSV export.
- `ReportingRepository.cs`: Implements all queries using EF Core LINQ projections, `.AsNoTracking()`, and in-memory YoY merge.

### 1.3 Service
- `IReportingService.cs` / `ReportingService.cs`: Wraps repository, validates input, applies 5-min cache via `ICacheService`.

### 1.4 Endpoints
- `ReportingEndpoints.cs`: Five endpoints under `/api/reports`, all `RequireAuthorization("Admin")`, returns `ApiResponse<T>`.
- Query params mapped to DTOs, CSV export returns `File(bytes, "text/csv", "report.csv")`.

### 1.5 DI Registration
- Register repository/service in `Program.cs`.
- Map endpoints under `/api/reports`.

---

## 2. Frontend

### 2.1 Setup & Types
- Add `recharts` and `@types/recharts`.
- `types/reporting.ts`: Readonly interfaces for all DTOs and query params.

### 2.2 API Layer
- `api/reportingApi.ts`: Five functions for endpoints, CSV export as blob download.

### 2.3 Data Hook
- `hooks/useReportingData.ts`: Handles loading, error, data, refetch, and export.

### 2.4 Components
- `DateRangePicker.tsx`: Presets + custom range.
- `GranularitySelector.tsx`: Daily/Weekly/Monthly.
- `ExportButton.tsx`: Triggers CSV download.

### 2.5 Tab Views
- `SalesByDateTab.tsx`: Date picker, granularity, YoY toggle, Recharts, table.
- `SalesByMovieTab.tsx`: Date picker, movie chart/table.
- `SalesByShowtimeTab.tsx`: Date picker, cinema/movie filter, table.
- `SalesByLocationTab.tsx`: Date picker, location chart/table.

### 2.6 Page & Routing
- `ReportsPage.tsx`: Tab navigation, shared controls, renders tab views.
- Register route in `App.tsx` under admin.
- Add "Reports" card to `DashboardPage.tsx`.

---

## 3. Unit Tests

> **TDD note:** Unit tests were not written ahead of implementation (RED→GREEN→REFACTOR was not followed). This was a deviation from the project's TDD policy. Tests were added retroactively as a corrective step. Future features must follow the TDD workflow.

### 3.1 `ReportingServiceTests.cs` — `Tests.Unit/Services/`

Covers `ReportingService` in full isolation (all dependencies mocked):

| Test | Scenario |
|---|---|
| `GetSalesByDateAsync_WhenQueryIsValid_ReturnsDataFromRepository` | Happy path — data is fetched from repo and returned |
| `GetSalesByDateAsync_WhenCacheHit_ReturnsCachedDataWithoutCallingRepository` | Cache hit — repo is never called |
| `GetSalesByDateAsync_WhenCacheMiss_StoresResultInCache` | Cache miss — data is stored after fetch |
| `GetSalesByDateAsync_WhenFromIsAfterTo_ReturnsFailure` | Validation: reversed date range |
| `GetSalesByDateAsync_WhenDateRangeExceeds366Days_ReturnsFailure` | Validation: > 366 days |
| `GetSalesByDateAsync_WhenGranularityIsInvalid_ReturnsFailure` | Validation: bad granularity string |
| `GetSalesByMovieAsync_WhenQueryIsValid_ReturnsData` | Happy path for movie dimension |
| `GetSalesByShowtimeAsync_WhenQueryIsValid_ReturnsData` | Happy path for showtime dimension |
| `GetSalesByLocationAsync_WhenQueryIsValid_ReturnsData` | Happy path for location dimension |
| `ExportCsvAsync_WhenReportTypeIsDate_ReturnsCsvBytes` | CSV export — date |
| `ExportCsvAsync_WhenReportTypeIsMovie_ReturnsCsvBytes` | CSV export — movie |
| `ExportCsvAsync_WhenReportTypeIsLocation_ReturnsCsvBytes` | CSV export — location |
| `ExportCsvAsync_WhenReportTypeIsUnknown_ReturnsFailure` | CSV export — unknown type returns failure |
| `ExportCsvAsync_WhenFromIsAfterTo_ReturnsFailure` | CSV export — validation propagated |

---

## 4. E2E Tests
- `e2e/admin-reports.spec.ts`: Playwright spec for navigation, filters, YoY toggle, export.

---

## 5. Key Decisions
- **Recharts** for charts.
- **EF Core LINQ projections** only (no raw SQL).
- **YoY**: In-memory merge of two queries.
- **CSV**: Built-in C# string builder.
- **5-min cache** for aggregates.

---

## 6. Verification
- `dotnet build && dotnet test` (backend)
- `npm install && npm run build` (frontend)
- `npx playwright test e2e/admin-reports.spec.ts --ui` (E2E)
- Manual: check endpoints (admin/403), CSV headers, UI loads.

---

## Constraints & Gotchas
- EF Core only, no raw SQL.
- Use `BookingTicket.UnitPrice` for revenue.
- Filter to `Confirmed` bookings.
- All new async methods accept `CancellationToken`.
- All reporting reads use `.AsNoTracking()`.
- All new TS types are `readonly`.
- All endpoints are admin-only.
- No global state; use hooks and local state.
- Add charting library to `package.json`.

---

*See also: business requirements, codebase research summary, and referenced file paths for implementation details.*

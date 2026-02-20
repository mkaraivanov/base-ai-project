// ── Reporting types ──────────────────────────────────────────────────────────

export type ReportGranularity = 'Daily' | 'Weekly' | 'Monthly';

export interface ReportQueryParams {
  readonly from: string;  // ISO string e.g. "2025-01-01"
  readonly to: string;
  readonly granularity?: ReportGranularity;
  readonly compare?: boolean;
  readonly cinemaId?: string;
  readonly movieId?: string;
}

export interface SalesByDateDto {
  readonly period: string;
  readonly ticketsSold: number;
  readonly revenue: number;
  readonly compareTicketsSold: number | null;
  readonly compareRevenue: number | null;
}

export interface SalesByMovieDto {
  readonly movieId: string;
  readonly movieTitle: string;
  readonly ticketsSold: number;
  readonly revenue: number;
  readonly totalCapacity: number;
  readonly capacityUsedPercent: number;
}

export interface SalesByShowtimeDto {
  readonly showtimeId: string;
  readonly startTime: string;
  readonly movieTitle: string;
  readonly hallName: string;
  readonly cinemaName: string;
  readonly ticketsSold: number;
  readonly capacity: number;
  readonly occupancyPercent: number;
  readonly revenue: number;
}

export interface SalesByLocationDto {
  readonly cinemaId: string;
  readonly cinemaName: string;
  readonly city: string;
  readonly country: string;
  readonly ticketsSold: number;
  readonly revenue: number;
}

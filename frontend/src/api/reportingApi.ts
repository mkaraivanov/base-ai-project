import apiClient from './apiClient';
import type { ApiResponse } from '../types';
import type {
  ReportQueryParams,
  SalesByDateDto,
  SalesByMovieDto,
  SalesByShowtimeDto,
  SalesByLocationDto,
} from '../types/reporting';

const buildParams = (query: ReportQueryParams) => ({
  from: query.from,
  to: query.to,
  ...(query.granularity && { granularity: query.granularity }),
  ...(query.compare !== undefined && { compare: query.compare }),
  ...(query.cinemaId && { cinemaId: query.cinemaId }),
  ...(query.movieId && { movieId: query.movieId }),
});

export const reportingApi = {
  getSalesByDate: async (query: ReportQueryParams): Promise<readonly SalesByDateDto[]> => {
    const response = await apiClient.get<ApiResponse<SalesByDateDto[]>>('/reports/by-date', {
      params: buildParams(query),
    });
    return response.data.data ?? [];
  },

  getSalesByMovie: async (query: ReportQueryParams): Promise<readonly SalesByMovieDto[]> => {
    const response = await apiClient.get<ApiResponse<SalesByMovieDto[]>>('/reports/by-movie', {
      params: buildParams(query),
    });
    return response.data.data ?? [];
  },

  getSalesByShowtime: async (query: ReportQueryParams): Promise<readonly SalesByShowtimeDto[]> => {
    const response = await apiClient.get<ApiResponse<SalesByShowtimeDto[]>>('/reports/by-showtime', {
      params: buildParams(query),
    });
    return response.data.data ?? [];
  },

  getSalesByLocation: async (query: ReportQueryParams): Promise<readonly SalesByLocationDto[]> => {
    const response = await apiClient.get<ApiResponse<SalesByLocationDto[]>>('/reports/by-location', {
      params: buildParams(query),
    });
    return response.data.data ?? [];
  },

  exportCsv: async (reportType: string, query: ReportQueryParams): Promise<void> => {
    const response = await apiClient.get(`/reports/export/${reportType}`, {
      params: buildParams(query),
      responseType: 'blob',
    });
    const blob = new Blob([response.data as BlobPart], { type: 'text/csv' });
    const url = URL.createObjectURL(blob);
    const link = document.createElement('a');
    link.href = url;
    link.download = `report-${reportType}-${query.from}-${query.to}.csv`;
    document.body.appendChild(link);
    link.click();
    document.body.removeChild(link);
    URL.revokeObjectURL(url);
  },
};

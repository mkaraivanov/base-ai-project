import apiClient from './apiClient';
import type {
  ApiResponse,
  ShowtimeDto,
  CreateShowtimeDto,
} from '../types';

export const showtimeApi = {
  getAll: async (fromDate?: string, toDate?: string): Promise<readonly ShowtimeDto[]> => {
    const response = await apiClient.get<ApiResponse<ShowtimeDto[]>>('/showtimes', {
      params: { fromDate, toDate },
    });
    return response.data.data ?? [];
  },

  getById: async (id: string): Promise<ShowtimeDto> => {
    const response = await apiClient.get<ApiResponse<ShowtimeDto>>(`/showtimes/${id}`);
    return response.data.data!;
  },

  getByMovie: async (movieId: string): Promise<readonly ShowtimeDto[]> => {
    const response = await apiClient.get<ApiResponse<ShowtimeDto[]>>(
      `/showtimes/movie/${movieId}`,
    );
    return response.data.data ?? [];
  },

  create: async (data: CreateShowtimeDto): Promise<ShowtimeDto> => {
    const response = await apiClient.post<ApiResponse<ShowtimeDto>>('/showtimes', data);
    return response.data.data!;
  },

  delete: async (id: string): Promise<void> => {
    await apiClient.delete(`/showtimes/${id}`);
  },
};

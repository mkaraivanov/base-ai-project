import apiClient from './apiClient';
import type {
  ApiResponse,
  CinemaDto,
  CreateCinemaDto,
  UpdateCinemaDto,
  CinemaHallDto,
} from '../types';

export const cinemaApi = {
  getAll: async (activeOnly = true): Promise<readonly CinemaDto[]> => {
    const response = await apiClient.get<ApiResponse<CinemaDto[]>>('/cinemas', {
      params: { activeOnly },
    });
    return response.data.data ?? [];
  },

  getById: async (id: string): Promise<CinemaDto> => {
    const response = await apiClient.get<ApiResponse<CinemaDto>>(`/cinemas/${id}`);
    return response.data.data!;
  },

  getHalls: async (cinemaId: string): Promise<readonly CinemaHallDto[]> => {
    const response = await apiClient.get<ApiResponse<CinemaHallDto[]>>(
      `/cinemas/${cinemaId}/halls`,
    );
    return response.data.data ?? [];
  },

  create: async (data: CreateCinemaDto): Promise<CinemaDto> => {
    const response = await apiClient.post<ApiResponse<CinemaDto>>('/cinemas', data);
    return response.data.data!;
  },

  update: async (id: string, data: UpdateCinemaDto): Promise<CinemaDto> => {
    const response = await apiClient.put<ApiResponse<CinemaDto>>(`/cinemas/${id}`, data);
    return response.data.data!;
  },

  delete: async (id: string): Promise<void> => {
    await apiClient.delete(`/cinemas/${id}`);
  },
};

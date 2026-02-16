import apiClient from './apiClient';
import type {
  ApiResponse,
  CinemaHallDto,
  CreateCinemaHallDto,
  UpdateCinemaHallDto,
} from '../types';

export const hallApi = {
  getAll: async (activeOnly = true): Promise<readonly CinemaHallDto[]> => {
    const response = await apiClient.get<ApiResponse<CinemaHallDto[]>>('/halls', {
      params: { activeOnly },
    });
    return response.data.data ?? [];
  },

  getById: async (id: string): Promise<CinemaHallDto> => {
    const response = await apiClient.get<ApiResponse<CinemaHallDto>>(`/halls/${id}`);
    return response.data.data!;
  },

  create: async (data: CreateCinemaHallDto): Promise<CinemaHallDto> => {
    const response = await apiClient.post<ApiResponse<CinemaHallDto>>('/halls', data);
    return response.data.data!;
  },

  update: async (id: string, data: UpdateCinemaHallDto): Promise<CinemaHallDto> => {
    const response = await apiClient.put<ApiResponse<CinemaHallDto>>(`/halls/${id}`, data);
    return response.data.data!;
  },

  delete: async (id: string): Promise<void> => {
    await apiClient.delete(`/halls/${id}`);
  },
};

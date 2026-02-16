import apiClient from './apiClient';
import type {
  ApiResponse,
  MovieDto,
  CreateMovieDto,
  UpdateMovieDto,
} from '../types';

export const movieApi = {
  getAll: async (activeOnly = true): Promise<readonly MovieDto[]> => {
    const response = await apiClient.get<ApiResponse<MovieDto[]>>('/movies', {
      params: { activeOnly },
    });
    return response.data.data ?? [];
  },

  getById: async (id: string): Promise<MovieDto> => {
    const response = await apiClient.get<ApiResponse<MovieDto>>(`/movies/${id}`);
    return response.data.data!;
  },

  create: async (data: CreateMovieDto): Promise<MovieDto> => {
    const response = await apiClient.post<ApiResponse<MovieDto>>('/movies', data);
    return response.data.data!;
  },

  update: async (id: string, data: UpdateMovieDto): Promise<MovieDto> => {
    const response = await apiClient.put<ApiResponse<MovieDto>>(`/movies/${id}`, data);
    return response.data.data!;
  },

  delete: async (id: string): Promise<void> => {
    await apiClient.delete(`/movies/${id}`);
  },
};

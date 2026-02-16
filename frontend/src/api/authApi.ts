import apiClient from './apiClient';
import type { ApiResponse, AuthResponseDto, LoginDto, RegisterDto } from '../types';

export const authApi = {
  login: async (email: string, password: string): Promise<AuthResponseDto> => {
    const body: LoginDto = { email, password };
    const response = await apiClient.post<ApiResponse<AuthResponseDto>>('/auth/login', body);
    return response.data.data!;
  },

  register: async (data: RegisterDto): Promise<AuthResponseDto> => {
    const response = await apiClient.post<ApiResponse<AuthResponseDto>>('/auth/register', data);
    return response.data.data!;
  },
};

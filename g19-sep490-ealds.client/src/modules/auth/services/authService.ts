import { apiClient } from '../../../shared/services/apiClient';
import type { LoginFormData, LoginResponse } from '../types/auth.types';

const authApi = apiClient;

export const authService = {
  login: async (credentials: LoginFormData): Promise<LoginResponse> => {
    const response = await authApi.post<LoginResponse>('/api/auth/login', credentials);
    return response.data;
  },

  logout: async (): Promise<void> => {
    await authApi.post('/api/auth/logout');
  },

  refreshToken: async (refreshToken: string): Promise<LoginResponse> => {
    const response = await authApi.post<LoginResponse>('/api/auth/refresh', {
      refreshToken,
    });
    return response.data;
  },

  forgotPassword: async (email: string): Promise<{ message: string }> => {
    const response = await authApi.post<{ message: string }>('/api/auth/forgot-password', { email });
    return response.data;
  },

  verifyOTP: async (email: string, otpCode: string): Promise<{ token: string }> => {
    const response = await authApi.post<{ token: string }>('/api/auth/verify-otp', { email, otpCode });
    return response.data;
  },

  resetPassword: async (
    token: string,
    newPassword: string,
    confirmNewPassword: string
  ): Promise<{ message: string }> => {
    const response = await authApi.post<{ message: string }>('/api/auth/reset-password', {
      token,
      newPassword,
      confirmNewPassword,
    });
    return response.data;
  },
};

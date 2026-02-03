import axios from 'axios';
import type { LoginFormData, LoginResponse } from '../types/auth.types';

// TODO: Replace with actual API endpoint
const API_BASE_URL = import.meta.env.VITE_API_BASE_URL || 'http://localhost:3000/api';

const authApi = axios.create({
  baseURL: API_BASE_URL,
  headers: {
    'Content-Type': 'application/json',
  },
});

export const authService = {
  login: async (credentials: LoginFormData): Promise<LoginResponse> => {
    const response = await authApi.post<LoginResponse>('/auth/login', credentials);
    return response.data;
  },

  logout: async (): Promise<void> => {
    await authApi.post('/auth/logout');
  },

  refreshToken: async (refreshToken: string): Promise<LoginResponse> => {
    const response = await authApi.post<LoginResponse>('/auth/refresh', {
      refreshToken,
    });
    return response.data;
  },

  forgotPassword: async (email: string): Promise<void> => {
    await authApi.post('/auth/forgot-password', { email });
  },

  verifyOTP: async (email: string, otp: string): Promise<void> => {
    await authApi.post('/auth/verify-otp', { email, otp });
  },

  resetPassword: async (email: string, otp: string, newPassword: string): Promise<void> => {
    await authApi.post('/auth/reset-password', { email, otp, newPassword });
  },
};

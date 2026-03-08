import axios from 'axios';

const API_BASE_URL = import.meta.env.VITE_API_BASE_URL || '';

const profileApi = axios.create({
  baseURL: API_BASE_URL,
  headers: {
    'Content-Type': 'application/json',
  },
});

profileApi.interceptors.request.use((config) => {
  const token = localStorage.getItem('accessToken');
  if (token) {
    config.headers = config.headers ?? {};
    config.headers.Authorization = `Bearer ${token}`;
  }
  return config;
});

export interface UserProfile {
  id: number;
  email: string;
  name: string;
  employeeCode?: string | null;
  phone?: string | null;
  address?: string | null;
  dob?: string | null;
  gender?: number | null;
  imageUrl?: string | null;
  departmentName?: string | null;
  role: string;
}

export interface UpdateProfilePayload {
  name: string;
  phone?: string | null;
  address?: string | null;
  dob?: string | null;
  gender?: number | null;
  imageUrl?: string | null;
}

export interface ChangePasswordPayload {
  currentPassword: string;
  newPassword: string;
  confirmNewPassword: string;
}

export const profileService = {
  async getProfile(): Promise<UserProfile> {
    const response = await profileApi.get<UserProfile>('/api/profile');
    return response.data;
  },

  async updateProfile(payload: UpdateProfilePayload): Promise<UserProfile> {
    const response = await profileApi.put<UserProfile>('/api/profile', payload);
    return response.data;
  },

  async changePassword(payload: ChangePasswordPayload): Promise<{ message: string }> {
    const response = await profileApi.put<{ message: string }>('/api/profile/change-password', payload);
    return response.data;
  },
};


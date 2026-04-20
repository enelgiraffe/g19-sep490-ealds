import { apiClient } from '../../../shared/services/apiClient';

const profileApi = apiClient;

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
  departmentId?: number | null;
  role: string;
  /** From API: UserRoles includes department-head role (not inferred from `role` string alone). */
  isDepartmentHead?: boolean;
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


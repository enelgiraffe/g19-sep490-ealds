import axios from 'axios';

const API_BASE_URL = import.meta.env.VITE_API_BASE_URL || '';

const userApi = axios.create({
  baseURL: API_BASE_URL,
  headers: {
    'Content-Type': 'application/json',
  },
});

userApi.interceptors.request.use((config) => {
  const token = localStorage.getItem('accessToken');
  if (token) {
    config.headers = config.headers ?? {};
    config.headers.Authorization = `Bearer ${token}`;
  }
  return config;
});

export interface UserItem {
  userId: number;
  email: string;
  status: number;
  employeeCode?: string | null;
  fullName?: string | null;
  departmentId?: number | null;
  departmentName?: string | null;
  phone?: string | null;
  imageUrl?: string | null;
  roleIds: number[];
  roles: string[];
}

export interface CreateUserPayload {
  fullName: string;
  employeeCode: string;
  email: string;
  password: string;
  phone: string;
  departmentId: number;
  status: number;
  roleIds: number[];
}

export interface UpdateUserPayload {
  fullName: string;
  email: string;
  phone: string;
  departmentId: number;
  status: number;
  roleIds: number[];
}

export interface AdminChangePasswordPayload {
  newPassword: string;
  confirmNewPassword: string;
}

export interface RoleOption {
  roleId: number;
  name: string;
}

export interface DepartmentOption {
  departmentId: number;
  name: string;
}

export interface UserMetadata {
  roles: RoleOption[];
  departments: DepartmentOption[];
}

export const userService = {
  async getAll(): Promise<UserItem[]> {
    const response = await userApi.get<UserItem[]>('/api/users');
    return response.data;
  },

  async getById(id: number): Promise<UserItem> {
    const response = await userApi.get<UserItem>(`/api/users/${id}`);
    return response.data;
  },

  async getMetadata(): Promise<UserMetadata> {
    const response = await userApi.get<UserMetadata>('/api/users/metadata');
    return response.data;
  },

  async getRoles(): Promise<RoleOption[]> {
    const response = await userApi.get<RoleOption[]>('/api/users/roles');
    return response.data;
  },

  async getDepartments(): Promise<DepartmentOption[]> {
    const response = await userApi.get<DepartmentOption[]>('/api/users/departments');
    return response.data;
  },

  async create(payload: CreateUserPayload): Promise<UserItem> {
    const response = await userApi.post<UserItem>('/api/users', payload);
    return response.data;
  },

  async update(id: number, payload: UpdateUserPayload): Promise<void> {
    await userApi.put<void>(`/api/users/${id}`, payload);
  },

  async changePassword(id: number, payload: AdminChangePasswordPayload): Promise<void> {
    await userApi.put<void>(`/api/users/${id}/password`, payload);
  },

  async delete(id: number): Promise<void> {
    await userApi.delete<void>(`/api/users/${id}`);
  },

  async deactivate(id: number): Promise<void> {
    await userApi.put<void>(`/api/users/${id}/deactivate`);
  },
};


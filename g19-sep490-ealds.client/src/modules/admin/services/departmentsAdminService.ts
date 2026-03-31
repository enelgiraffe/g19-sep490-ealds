import axios from 'axios';

const API_BASE_URL = import.meta.env.VITE_API_BASE_URL || '';

const api = axios.create({
  baseURL: API_BASE_URL,
  headers: {
    'Content-Type': 'application/json',
  },
});

api.interceptors.request.use((config) => {
  const token = localStorage.getItem('accessToken');
  if (token) {
    config.headers = config.headers ?? {};
    config.headers.Authorization = `Bearer ${token}`;
  }
  return config;
});

export interface DepartmentAdminItem {
  departmentId: number;
  code: string;
  name: string;
  status: number;
  createDate: string;
  updateDate?: string | null;
}

export interface CreateDepartmentPayload {
  code: string;
  name: string;
  status: number;
}

export type UpdateDepartmentPayload = CreateDepartmentPayload;

export const departmentsAdminService = {
  async getAll(keyword?: string): Promise<DepartmentAdminItem[]> {
    const response = await api.get<DepartmentAdminItem[]>('/api/departments', {
      params: keyword ? { keyword } : undefined,
    });
    return response.data;
  },

  async getById(id: number): Promise<DepartmentAdminItem> {
    const response = await api.get<DepartmentAdminItem>(`/api/departments/${id}`);
    return response.data;
  },

  async create(payload: CreateDepartmentPayload): Promise<DepartmentAdminItem> {
    const response = await api.post<DepartmentAdminItem>('/api/departments', payload);
    return response.data;
  },

  async update(id: number, payload: UpdateDepartmentPayload): Promise<void> {
    await api.put<void>(`/api/departments/${id}`, payload);
  },

  async delete(id: number): Promise<{ message?: string } | void> {
    const response = await api.delete<{ message?: string } | void>(`/api/departments/${id}`);
    return response.data;
  },
};

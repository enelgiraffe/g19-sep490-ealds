import axios from 'axios';

const API_BASE_URL = import.meta.env.VITE_API_BASE_URL || '';

const maintenanceScheduleApi = axios.create({
  baseURL: API_BASE_URL,
  headers: {
    'Content-Type': 'application/json',
  },
});

maintenanceScheduleApi.interceptors.request.use((config) => {
  const token = localStorage.getItem('accessToken');
  if (token) {
    config.headers = config.headers ?? {};
    config.headers.Authorization = `Bearer ${token}`;
  }
  return config;
});

export interface MaintenanceScheduleResponse {
  scheduleId: number;
  assetId: number;
  assetInstanceId?: number | null;
  instanceCode?: string | null;
  templateId?: number | null;
  content?: string | null;
  scheduleType: number | string;
  intervalUnit?: number | string | null;
  intervalValue?: number | null;
  startDate: string;
  nextDueDate?: string | null;
  endDate?: string | null;
  isActive?: boolean | null;
  createBy: number;
  createDate: string;
}

export interface MaintenanceScheduleCreatePayload {
  assetId: number;
  templateId?: number | null;
  content?: string | null;
  scheduleType: number;
  intervalUnit?: number | null;
  intervalValue?: number | null;
  startDate: string;
  endDate?: string | null;
  isActive?: boolean | null;
  createBy: number;
  createDate?: string;
}

export const maintenanceScheduleService = {
  async addSchedule(
    payload: MaintenanceScheduleCreatePayload
  ): Promise<MaintenanceScheduleResponse> {
    const response = await maintenanceScheduleApi.post<MaintenanceScheduleResponse>(
      '/api/MaintenanceSchedule/add-schedule',
      payload
    );
    return response.data;
  },

  async findByAssetId(assetId: number): Promise<MaintenanceScheduleResponse[]> {
    const response = await maintenanceScheduleApi.get<MaintenanceScheduleResponse[]>(
      `/api/MaintenanceSchedule/find-by/${assetId}`
    );
    return response.data;
  },

  async findByInstanceId(instanceId: number): Promise<MaintenanceScheduleResponse[]> {
    const response = await maintenanceScheduleApi.get<MaintenanceScheduleResponse[]>(
      `/api/MaintenanceSchedule/find-by-instance/${instanceId}`
    );
    return response.data;
  },

  async changeStatus(id: number): Promise<boolean> {
    const response = await maintenanceScheduleApi.put<boolean>(
      `/api/MaintenanceSchedule/change-status/${id}`
    );
    return response.data;
  },
};


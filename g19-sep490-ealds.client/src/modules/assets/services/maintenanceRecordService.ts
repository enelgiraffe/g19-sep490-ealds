import axios from 'axios';

const API_BASE_URL = import.meta.env.VITE_API_BASE_URL || '';

const maintenanceRecordApi = axios.create({
  baseURL: API_BASE_URL,
  headers: {
    'Content-Type': 'application/json',
  },
});

maintenanceRecordApi.interceptors.request.use((config) => {
  const token = localStorage.getItem('accessToken');
  if (token) {
    config.headers = config.headers ?? {};
    config.headers.Authorization = `Bearer ${token}`;
  }
  return config;
});

export interface MaintenanceRecordResponse {
  recordId: number;
  taskId: number;
  executionDate: string;
  totalCost: number;
  workPerformed: string;
  conditionBefore: string;
  conditionAfter: string;
  technicalNote?: string | null;
  status: number;
}

export function getMaintenanceRecordStatusLabel(status: number): string {
  switch (status) {
    case 1:
      return 'Hoàn thành';
    case 2:
      return 'Thất bại';
    case 3:
      return 'Một phần';
    default:
      return '—';
  }
}

export const maintenanceRecordService = {
  async getByAssetId(assetId: number): Promise<MaintenanceRecordResponse[]> {
    const response = await maintenanceRecordApi.get<MaintenanceRecordResponse[]>(
      `/api/MaintenanceRecord/asset/${assetId}`
    );
    return response.data;
  },
};

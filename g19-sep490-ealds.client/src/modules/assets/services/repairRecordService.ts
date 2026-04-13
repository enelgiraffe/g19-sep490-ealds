import axios from 'axios';
import type { MaintenanceRecordResponse } from './maintenanceRecordService';

const API_BASE_URL = import.meta.env.VITE_API_BASE_URL || '';

const repairRecordApi = axios.create({
  baseURL: API_BASE_URL,
  headers: {
    'Content-Type': 'application/json',
  },
});

repairRecordApi.interceptors.request.use((config) => {
  const token = localStorage.getItem('accessToken');
  if (token) {
    config.headers = config.headers ?? {};
    config.headers.Authorization = `Bearer ${token}`;
  }
  return config;
});

/** Cùng dạng với một dòng lịch sử bảo dưỡng trên UI; nguồn là bảng RepairRecord. */
export type RepairHistoryRecordResponse = Omit<MaintenanceRecordResponse, 'recordSource'>;

export const repairRecordService = {
  async getByAssetId(assetId: number): Promise<RepairHistoryRecordResponse[]> {
    const response = await repairRecordApi.get<RepairHistoryRecordResponse[]>(
      `/api/RepairRecord/asset/${assetId}`
    );
    return response.data;
  },

  async getByInstanceId(instanceId: number): Promise<RepairHistoryRecordResponse[]> {
    const response = await repairRecordApi.get<RepairHistoryRecordResponse[]>(
      `/api/RepairRecord/instance/${instanceId}`
    );
    return response.data;
  },
};

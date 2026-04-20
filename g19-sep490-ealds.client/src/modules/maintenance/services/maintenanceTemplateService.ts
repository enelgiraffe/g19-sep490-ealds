import { apiClient } from '../../../shared/services/apiClient';

const maintenanceTemplateApi = apiClient;

export type MaintenanceFrequencyType = 1 | 2; // 1: OneTime, 2: Periodic
export type MaintenanceRepeatIntervalUnit = 0 | 1 | 2 | 3 | 4; // 0 for OneTime on backend

export interface MaintenanceTemplatePayload {
  assetTypeId: number;
  name: string;
  content: string;
  frequencyType: MaintenanceFrequencyType;
  repeatIntervalValue: number;
  repeatIntervalUnit: MaintenanceRepeatIntervalUnit;
  isActive?: boolean;
}

export interface MaintenanceTemplateItem {
  templateId: number;
  assetTypeId: number;
  name: string;
  content: string;
  frequencyType: number | string;
  repeatIntervalValue: number;
  repeatIntervalUnit: number | string;
  isActive: boolean;
}

export const maintenanceTemplateService = {
  async getAll(): Promise<MaintenanceTemplateItem[]> {
    const response = await maintenanceTemplateApi.get<MaintenanceTemplateItem[]>(
      '/api/MaintenanceTemplate/get-all'
    );
    return response.data;
  },

  async getById(id: number): Promise<MaintenanceTemplateItem> {
    const response = await maintenanceTemplateApi.get<MaintenanceTemplateItem>(
      `/api/MaintenanceTemplate/find-id/${id}`
    );
    return response.data;
  },

  async create(payload: MaintenanceTemplatePayload): Promise<MaintenanceTemplateItem> {
    const response = await maintenanceTemplateApi.post<MaintenanceTemplateItem>(
      '/api/MaintenanceTemplate/add-template',
      payload
    );
    return response.data;
  },

  async update(id: number, payload: MaintenanceTemplatePayload): Promise<MaintenanceTemplateItem> {
    const response = await maintenanceTemplateApi.put<MaintenanceTemplateItem>(
      `/api/MaintenanceTemplate/update/${id}`,
      payload
    );
    return response.data;
  },

  async changeStatus(id: number): Promise<MaintenanceTemplateItem> {
    const response = await maintenanceTemplateApi.put<MaintenanceTemplateItem>(
      `/api/MaintenanceTemplate/change-status/${id}`
    );
    return response.data;
  },

  async deletePermanent(id: number): Promise<boolean> {
    const response = await maintenanceTemplateApi.delete<boolean>(
      `/api/MaintenanceTemplate/delete-permanent/${id}`
    );
    return response.data;
  },
};


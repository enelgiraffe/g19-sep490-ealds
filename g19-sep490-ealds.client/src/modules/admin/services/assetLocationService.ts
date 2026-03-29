import axios from 'axios';

const API_BASE_URL = import.meta.env.VITE_API_BASE_URL || '';

const assetLocationApi = axios.create({
  baseURL: API_BASE_URL,
  headers: {
    'Content-Type': 'application/json',
  },
});

assetLocationApi.interceptors.request.use((config) => {
  const token = localStorage.getItem('accessToken');
  if (token) {
    config.headers = config.headers ?? {};
    config.headers.Authorization = `Bearer ${token}`;
  }
  return config;
});

export interface AssetLocationItem {
  locationId: number;
  assetInstanceId: number;
  assetId: number;
  instanceCode: string;
  assetName: string;
  assetCode: string;
  departmentId: number;
  departmentName: string;
  startDate: string;
  endDate?: string | null;
  isCurrent: boolean;
  note?: string | null;
}

export interface GetAssetLocationsParams {
  assetInstanceId?: number;
  assetId?: number;
  departmentId?: number;
  isCurrent?: boolean;
}

export interface DepartmentLocationOption {
  locationId: number;
  displayName: string;
}

export interface CreateAssetLocationPayload {
  assetInstanceId: number;
  departmentId: number;
  startDate: string;
  endDate?: string | null;
  isCurrent: boolean;
  note?: string | null;
}

export interface UpdateAssetLocationPayload {
  departmentId: number;
  startDate: string;
  endDate?: string | null;
  isCurrent: boolean;
  note?: string | null;
}

export const assetLocationService = {
  async getAll(params?: GetAssetLocationsParams): Promise<AssetLocationItem[]> {
    const response = await assetLocationApi.get<AssetLocationItem[]>('/api/AssetLocations', {
      params,
    });
    return response.data;
  },

  async getDepartments(): Promise<DepartmentLocationOption[]> {
    const response = await assetLocationApi.get<DepartmentLocationOption[]>(
      '/api/AssetLocations/departments',
    );
    return response.data;
  },

  async create(payload: CreateAssetLocationPayload): Promise<AssetLocationItem> {
    const response = await assetLocationApi.post<AssetLocationItem>('/api/AssetLocations', {
      assetInstanceId: payload.assetInstanceId,
      departmentId: payload.departmentId,
      startDate: payload.startDate,
      endDate: payload.endDate ?? undefined,
      isCurrent: payload.isCurrent,
      note: payload.note ?? undefined,
    });
    return response.data;
  },

  async update(locationId: number, payload: UpdateAssetLocationPayload): Promise<AssetLocationItem> {
    const response = await assetLocationApi.put<AssetLocationItem>(
      `/api/AssetLocations/${locationId}`,
      {
        departmentId: payload.departmentId,
        startDate: payload.startDate,
        endDate: payload.endDate ?? undefined,
        isCurrent: payload.isCurrent,
        note: payload.note ?? undefined,
      },
    );
    return response.data;
  },

  async delete(locationId: number): Promise<void> {
    await assetLocationApi.delete(`/api/AssetLocations/${locationId}`);
  },
};

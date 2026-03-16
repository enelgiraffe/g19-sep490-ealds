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
  assetId: number;
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
  assetId?: number;
  departmentId?: number;
  isCurrent?: boolean;
}

export const assetLocationService = {
  async getAll(params?: GetAssetLocationsParams): Promise<AssetLocationItem[]> {
    const response = await assetLocationApi.get<AssetLocationItem[]>('/api/assetlocations', {
      params,
    });
    return response.data;
  },
};


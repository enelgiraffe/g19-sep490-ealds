import axios from 'axios';

const API_BASE_URL = import.meta.env.VITE_API_BASE_URL || '';

const transferApi = axios.create({
  baseURL: API_BASE_URL,
  headers: {
    'Content-Type': 'application/json',
  },
});

transferApi.interceptors.request.use((config) => {
  const token = localStorage.getItem('accessToken');
  if (token) {
    config.headers = config.headers ?? {};
    config.headers.Authorization = `Bearer ${token}`;
  }
  return config;
});

export interface AssetLocationOption {
  locationId: number;
  displayName: string;
}

export interface TransferRequestPayload {
  assetId: number;
  requestTypeId: number;
  fromLocationId: number;
  toLocationId: number;
  fromUserId?: number | null;
  toUserId?: number | null;
  transferDate?: string | null;
  executeBy: number;
  createdBy: number;
  title?: string | null;
  description?: string | null;
}

export interface TransferRequestListItem {
  recordId: number;
  assetRequestId: number;
  code: string;
  transferDate: string;
  assetCode: string;
  assetName: string;
  fromDepartment: string;
  toDepartment: string;
  quantity: number;
  status: number;
  statusName: string;
  reason?: string | null;
}

export const transferRequestService = {
  async getList(): Promise<TransferRequestListItem[]> {
    const response = await transferApi.get<TransferRequestListItem[]>('/api/Assets/Requests/transfer');
    return response.data;
  },

  async getAssetLocations(): Promise<AssetLocationOption[]> {
    const response = await transferApi.get<AssetLocationOption[]>('/api/AssetLocations');
    return response.data;
  },

  async create(payload: TransferRequestPayload): Promise<{ assetRequestId: number; recordId: number }> {
    const response = await transferApi.post<{ assetRequestId: number; recordId: number }>(
      '/api/Assets/Requests/transfer',
      payload
    );
    return response.data;
  },
};


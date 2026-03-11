import axios from 'axios';

const API_BASE_URL = import.meta.env.VITE_API_BASE_URL || '';

const assetApi = axios.create({
  baseURL: API_BASE_URL,
  headers: {
    'Content-Type': 'application/json',
  },
});

assetApi.interceptors.request.use((config) => {
  const token = localStorage.getItem('accessToken');
  if (token) {
    config.headers = config.headers ?? {};
    config.headers.Authorization = `Bearer ${token}`;
  }
  return config;
});

/** Asset status enum - matches backend AssetStatus */
export type AssetStatus =
  | 'Available'
  | 'InUse'
  | 'InMaintenance'
  | 'Reserved'
  | 'Disposed'
  | 'Lost'
  | 'Liquidated';

export interface AssetResponse {
  assetId: number;
  code: string;
  name: string;
  assetTypeId: number;
  assetTypeName?: string | null;
  purchaseDate: string;
  originalPrice: number;
  currentValue: number;
  status: number;
  statusName: string;
  warrantyEndDate?: string | null;
  inUseDate?: string | null;
  unit: string;
  quantity: number;
  warehouseId: number;
  warehouseName?: string | null;
  createdBy: number;
  currentDepartmentId?: number | null;
  currentDepartmentName?: string | null;
}

export interface AssetTypeItem {
  assetTypeId: number;
  name: string;
}

export interface GetAssetsParams {
  keyword?: string;
  status?: number;
  assetTypeId?: number;
  warehouseId?: number;
  minPrice?: number;
  maxPrice?: number;
  fromDate?: string;
  toDate?: string;
}

export interface CreateAssetPayload {
  code: string;
  name: string;
  assetTypeId: number;
  purchaseDate: string;
  originalPrice: number;
  currentValue: number;
  warrantyEndDate?: string | null;
  inUseDate?: string | null;
  unit: string;
  quantity: number;
  warehouseId: number;
  createdBy: number;
  depreciationPolicyId?: number | null;
}

export interface UpdateAssetPayload {
  code?: string;
  name?: string;
  assetTypeId?: number;
  purchaseDate?: string;
  originalPrice?: number;
  currentValue?: number;
  status?: number;
  warrantyEndDate?: string | null;
  inUseDate?: string | null;
  unit?: string;
  quantity?: number;
  warehouseId?: number;
}

export interface DeleteAssetPayload {
  status: number; // Disposed=4, Lost=5, Liquidated=6
  reason?: string | null;
}

/** Format number as VND */
export function formatVnd(value: number): string {
  return (
    new Intl.NumberFormat('vi-VN', {
      style: 'decimal',
      minimumFractionDigits: 0,
    }).format(value) + ' đ'
  );
}

/** Map status to Vietnamese label */
export function getStatusLabel(statusName: string): string {
  const map: Record<string, string> = {
    Available: 'Sẵn có',
    InUse: 'Đang sử dụng',
    InMaintenance: 'Đang bảo trì',
    Reserved: 'Đã đặt trước',
    Disposed: 'Đã thanh lý',
    Lost: 'Mất',
    Liquidated: 'Đã thanh lý',
  };
  return map[statusName] ?? statusName;
}

export const assetService = {
  async getAll(params?: GetAssetsParams): Promise<AssetResponse[]> {
    const response = await assetApi.get<AssetResponse[]>('/api/assets', {
      params,
    });
    return response.data;
  },

  async getById(id: number): Promise<AssetResponse> {
    const response = await assetApi.get<AssetResponse>(`/api/assets/${id}`);
    return response.data;
  },

  async create(payload: CreateAssetPayload): Promise<AssetResponse> {
    const response = await assetApi.post<AssetResponse>('/api/assets', payload);
    return response.data;
  },

  async update(id: number, payload: UpdateAssetPayload): Promise<AssetResponse> {
    const response = await assetApi.put<AssetResponse>(
      `/api/assets/${id}`,
      payload
    );
    return response.data;
  },

  async softDelete(
    id: number,
    payload: DeleteAssetPayload
  ): Promise<AssetResponse> {
    const response = await assetApi.delete<AssetResponse>(
      `/api/assets/${id}`,
      { data: payload }
    );
    return response.data;
  },

  async getAssetTypes(): Promise<AssetTypeItem[]> {
    const response = await assetApi.get<AssetTypeItem[]>('/api/assettypes');
    return response.data;
  },
};

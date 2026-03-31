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
  | 'InRepair'
  | 'Reserved'
  | 'Disposed'
  | 'Lost'
  | 'Liquidated'
  | 'Capitalized'
  | 'Damaged';

export interface MaintenanceSchedule {
  scheduleId: number;
  /** Có giá trị khi quy định chỉ áp dụng cho một cá thể. */
  assetInstanceId?: number | null;
  instanceCode?: string | null;
  templateId?: number | null;
  content?: string | null;
  templateName?: string | null;
  scheduleType: number;
  intervalMonths?: number | null;
  intervalHours?: number | null;
  intervalValue?: number | null;
  intervalUnit?: number | null;
  startDate: string;
  nextDueDate?: string | null;
  endDate?: string | null;
  isActive?: boolean | null;
}

export interface AssetDocumentItem {
  documentId: number;
  documentType: number;
  fileUrl: string;
  uploadedDate: string;
}

export interface GuaranteeItem {
  guaranteeId: number;
  warrantyPeriodValue: number;
  warrantyPeriodUnit: string;
  warrantyConditions?: string | null;
  startDate: string;
  warrantyEndDate: string;
}

/** Catalog row from GET /api/assets */
export interface AssetCatalogResponse {
  assetId: number;
  code: string;
  name: string;
  assetTypeId: number;
  assetTypeName?: string | null;
  status: number;
  statusName: string;
  unit: string;
  quantity?: number | null;
  createdBy: number;
  inUseDate?: string | null;
  specification?: string | null;
  note?: string | null;
}

/** Physical instance from GET /api/asset-instances */
export interface AssetInstanceResponse {
  assetInstanceId: number;
  assetId: number;
  assetTypeId: number;
  assetCode?: string | null;
  assetName?: string | null;
  instanceCode: string;
  serialNumber?: string | null;
  warehouseId: number;
  warehouseName?: string | null;
  purchaseDate: string;
  originalPrice: number;
  currentValue: number;
  status: number;
  statusName: string;
  inUseDate?: string | null;
  supplierId?: number | null;
  contractNo?: string | null;
  condition?: string | null;
  note?: string | null;
  currentLocationId?: number | null;
  currentDepartmentId?: number | null;
  currentDepartmentName?: string | null;
  /** Ghi chú trên bản ghi AssetLocation hiện tại (IsCurrent). */
  currentLocationNote?: string | null;
  currentResponsibleEmployeeId?: number | null;
  currentResponsibleEmployeeName?: string | null;
  currentResponsibleUserId?: number | null;
  depreciationPolicyId?: number | null;
  depreciationPolicyName?: string | null;
  depreciationUsefulLifeMonths?: number | null;
  depreciationSalvageValue?: number | null;
  depreciationPeriod?: string | null;
  depreciationAmount?: number | null;
  accumulatedDepreciation?: number | null;
  remainingValue?: number | null;
  guaranteeId?: number | null;
  warrantyPeriodValue?: number | null;
  warrantyPeriodUnit?: string | null;
  warrantyConditions?: string | null;
  warrantyStartDate?: string | null;
  warrantyEndDate?: string | null;
  guarantees?: GuaranteeItem[] | null;
}

/** GET /api/assets/{id} — catalog + instances + maintenance + documents */
export interface AssetDetailResponse extends AssetCatalogResponse {
  maintenanceSchedules?: MaintenanceSchedule[] | null;
  documents?: AssetDocumentItem[] | null;
  instances?: AssetInstanceResponse[] | null;
}

/** @deprecated Use AssetCatalogResponse / AssetDetailResponse / AssetInstanceResponse as appropriate */
export type AssetResponse = AssetDetailResponse;

export interface AssetTypeItem {
  assetTypeId: number;
  name: string;
}

export interface WarehouseItem {
  warehouseId: number;
  name: string;
  description?: string | null;
}

/** Query params for GET /api/assets (catalog only) */
export interface GetAssetCatalogParams {
  keyword?: string;
  status?: number;
  assetTypeId?: number;
}

/** Query params for GET /api/asset-instances (physical rows; supports former asset list filters) */
export interface GetAssetInstancesParams {
  keyword?: string;
  status?: number;
  assetTypeId?: number;
  warehouseId?: number;
  minPrice?: number;
  maxPrice?: number;
  fromDate?: string;
  toDate?: string;
}

/** @deprecated Use GetAssetCatalogParams or GetAssetInstancesParams */
export type GetAssetsParams = GetAssetInstancesParams;

export interface CreateAssetInstancePayload {
  assetId?: number;
  instanceCode: string;
  serialNumber?: string | null;
  warehouseId: number;
  purchaseDate: string;
  originalPrice: number;
  currentValue: number;
  inUseDate?: string | null;
  depreciationPolicyId?: number | null;
  supplierId?: number | null;
  contractNo?: string | null;
  condition?: string | null;
  note?: string | null;
  assignedDepartmentId?: number | null;
  responsibleEmployeeId?: number | null;
  assignmentEffectiveDate?: string | null;
}

export interface CreateAssetPayload {
  code: string;
  name: string;
  assetTypeId: number;
  unit: string;
  quantity?: number | null;
  createdBy: number;
  inUseDate?: string | null;
  specification?: string | null;
  note?: string | null;
  /** Optional first physical row (same request as catalog create) */
  initialInstance?: CreateAssetInstancePayload;
}

export interface UpdateAssetPayload {
  code?: string;
  name?: string;
  assetTypeId?: number;
  status?: number;
  unit?: string;
  quantity?: number | null;
  inUseDate?: string | null;
  specification?: string | null;
  note?: string | null;
}

export interface UpdateAssetInstancePayload {
  instanceCode?: string;
  serialNumber?: string | null;
  warehouseId?: number;
  purchaseDate?: string;
  originalPrice?: number;
  currentValue?: number;
  status?: number;
  inUseDate?: string | null;
  depreciationPolicyId?: number | null;
  supplierId?: number | null;
  contractNo?: string | null;
  condition?: string | null;
  note?: string | null;
  assignedDepartmentId?: number | null;
  responsibleEmployeeId?: number | null;
  assignmentEffectiveDate?: string | null;
  clearDepartmentAssignment?: boolean;
  clearResponsibleEmployee?: boolean;
}

export interface DeleteAssetPayload {
  status: number; // Disposed=4, Lost=5, Liquidated=6
  reason?: string | null;
}

export interface DeleteAssetInstancePayload {
  status: number;
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
    InRepair: 'Đang sửa chữa',
    Reserved: 'Đã đặt trước',
    Disposed: 'Đã thanh lý',
    Lost: 'Mất',
    Liquidated: 'Đã thanh lý',
    Capitalized: 'Đã vốn hoá',
    Damaged: 'Đã hỏng',
  };
  return map[statusName] ?? statusName;
}

export const assetService = {
  async getAll(params?: GetAssetCatalogParams): Promise<AssetCatalogResponse[]> {
    const response = await assetApi.get<AssetCatalogResponse[]>('/api/assets', {
      params,
    });
    return response.data;
  },

  async getById(id: number): Promise<AssetDetailResponse> {
    const response = await assetApi.get<AssetDetailResponse>(`/api/assets/${id}`);
    return response.data;
  },

  async create(payload: CreateAssetPayload): Promise<AssetDetailResponse> {
    const response = await assetApi.post<AssetDetailResponse>('/api/assets', payload);
    return response.data;
  },

  async update(id: number, payload: UpdateAssetPayload): Promise<AssetDetailResponse> {
    const response = await assetApi.put<AssetDetailResponse>(
      `/api/assets/${id}`,
      payload
    );
    return response.data;
  },

  async softDelete(
    id: number,
    payload: DeleteAssetPayload
  ): Promise<AssetCatalogResponse> {
    const response = await assetApi.delete<AssetCatalogResponse>(
      `/api/assets/${id}`,
      { data: payload }
    );
    return response.data;
  },

  async getAssetTypes(): Promise<AssetTypeItem[]> {
    const response = await assetApi.get<AssetTypeItem[]>('/api/assettypes');
    return response.data;
  },

  async getWarehouses(): Promise<WarehouseItem[]> {
    const response = await assetApi.get<WarehouseItem[]>('/api/warehouseassets');
    return response.data;
  },
};

export const assetInstanceService = {
  async getAll(params?: GetAssetInstancesParams): Promise<AssetInstanceResponse[]> {
    const response = await assetApi.get<AssetInstanceResponse[]>('/api/assetinstances', {
      params,
    });
    return response.data;
  },

  async getById(id: number): Promise<AssetInstanceResponse> {
    const response = await assetApi.get<AssetInstanceResponse>(`/api/assetinstances/${id}`);
    return response.data;
  },

  async create(payload: CreateAssetInstancePayload): Promise<AssetInstanceResponse> {
    const response = await assetApi.post<AssetInstanceResponse>(
      '/api/assetinstances',
      payload
    );
    return response.data;
  },

  async update(id: number, payload: UpdateAssetInstancePayload): Promise<AssetInstanceResponse> {
    const response = await assetApi.put<AssetInstanceResponse>(
      `/api/assetinstances/${id}`,
      payload
    );
    return response.data;
  },

  async softDelete(
    id: number,
    payload: DeleteAssetInstancePayload
  ): Promise<AssetInstanceResponse> {
    const response = await assetApi.delete<AssetInstanceResponse>(
      `/api/assetinstances/${id}`,
      { data: payload }
    );
    return response.data;
  },
};

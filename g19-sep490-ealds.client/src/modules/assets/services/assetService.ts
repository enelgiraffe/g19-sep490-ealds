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

/** Physical instance from GET /api/assetinstances */
export interface AssetInstanceResponse {
  assetInstanceId: number;
  assetId: number;
  assetTypeId: number;
  /** Tên loại tài sản (catalog), từ Asset.AssetType */
  assetTypeName?: string | null;
  assetCode?: string | null;
  assetName?: string | null;
  specification?: string | null;
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

/** Query params for GET /api/assetinstances (physical rows; supports former asset list filters) */
export interface GetAssetInstancesParams {
  keyword?: string;
  status?: number;
  assetTypeId?: number;
  warehouseId?: number;
  /** Filter to instances whose current AssetLocation is in this department (e.g. transfer “from” department). */
  currentDepartmentId?: number;
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

/** Backend default document type for catalog attachments (CreateAssetDocumentDTO.DocumentType). */
export const ASSET_CATALOG_DOCUMENT_TYPE = 20;

export interface AssetDocumentPayloadItem {
  fileUrl: string;
  documentType?: number;
}

export interface CreateAssetPayload {
  /** Catalog mã tài sản; ignored when assetCodePrefix is set (server generates). */
  code?: string;
  /** Prefix for generated catalog code (mã tài sản), same numbering rules as instance prefix. */
  assetCodePrefix?: string | null;
  name: string;
  assetTypeId: number;
  unit: string;
  quantity?: number | null;
  createdBy: number;
  inUseDate?: string | null;
  specification?: string | null;
  note?: string | null;
  /** Prefix for generated instance codes (required when quantity is greater than 1). */
  instanceCodePrefix?: string | null;
  /** Optional first physical row (same request as catalog create) */
  initialInstance?: CreateAssetInstancePayload;

  /** URLs from POST /api/files/upload, persisted after the asset row is created. */
  documents?: AssetDocumentPayloadItem[];
}

/** Đơn vị tính options for asset create/edit forms. */
export const ASSET_MEASUREMENT_UNITS = [
  'Bộ',
  'Cái',
  'Chiếc',
  'Máy',
  'Đôi',
  'Bình',
  'Chai',
  'Cuốn',
  'Tập',
  'Mét',
  'Kiện',
  'Thùng',
  'Quyển',
  'Hộp',
  'Gói',
] as const;

export interface SupplierListItem {
  supplierId: number;
  code: string;
  name: string;
  taxCode?: string | null;
  address?: string | null;
  phone?: string | null;
  email?: string | null;
  status: number;
  createDate: string;
}

export interface DepartmentEmployeeOption {
  employeeId: number;
  name: string;
  code: string;
  userId?: number | null;
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
  warrantyPeriodValue?: number | null;
  warrantyPeriodUnit?: string | null;
  warrantyConditions?: string | null;
  warrantyStartDate?: string | null;
  warrantyEndDate?: string | null;
  depreciationPeriod?: string | null;
  depreciationAmount?: number | null;
  accumulatedDepreciation?: number | null;
  remainingValue?: number | null;
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
    Active: 'Đang sử dụng',
    InMaintenance: 'Đang bảo trì',
    UnderMaintenance: 'Đang bảo trì',
    InRepair: 'Đang sửa chữa',
    Reserved: 'Đã đặt trước',
    Disposed: 'Đã loại bỏ',
    Lost: 'Mất',
    Liquidated: 'Đã thanh lý',
    Capitalized: 'Đã vốn hoá',
    Damaged: 'Đã hỏng',
  };
  return map[statusName] ?? statusName;
}

/** Backend `AssetStatus` int → enum member name (see `Status.cs`). */
const ASSET_STATUS_INT_TO_NAME: Record<number, string> = {
  0: 'Available',
  1: 'InUse',
  2: 'InMaintenance',
  3: 'Reserved',
  4: 'Disposed',
  5: 'Lost',
  6: 'Liquidated',
  7: 'Capitalized',
  8: 'Damaged',
  9: 'InRepair',
};

export function assetStatusNameFromValue(status: number): string {
  return ASSET_STATUS_INT_TO_NAME[status] ?? String(status);
}

export function formatAssetStatusVi(status: number): string {
  return getStatusLabel(assetStatusNameFromValue(status));
}

/** Select options for inventory execution (all statuses). */
export function getInventoryExecutionStatusSelectOptions(): { value: number; label: string }[] {
  return (
    [
      [0, 'Available'],
      [1, 'InUse'],
      [2, 'InMaintenance'],
      [3, 'Reserved'],
      [4, 'Disposed'],
      [5, 'Lost'],
      [6, 'Liquidated'],
      [7, 'Capitalized'],
      [8, 'Damaged'],
      [9, 'InRepair'],
    ] as const
  ).map(([value, name]) => ({
    value,
    label: getStatusLabel(name),
  }));
}

export const assetService = {
  async getAll(params?: GetAssetCatalogParams): Promise<AssetCatalogResponse[]> {
    const response = await assetApi.get<AssetCatalogResponse[]>('/api/assets', {
      params,
    });
    return response.data;
  },

  async getAssetCodePrefixes(): Promise<string[]> {
    const response = await assetApi.get<string[]>('/api/assets/code-prefixes');
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

  async addDocument(
    assetId: number,
    payload: AssetDocumentPayloadItem
  ): Promise<AssetDocumentItem> {
    const response = await assetApi.post<AssetDocumentItem>(
      `/api/assets/${assetId}/documents`,
      {
        fileUrl: payload.fileUrl,
        documentType: payload.documentType ?? ASSET_CATALOG_DOCUMENT_TYPE,
      }
    );
    return response.data;
  },

  async removeDocument(assetId: number, documentId: number): Promise<void> {
    await assetApi.delete(`/api/assets/${assetId}/documents/${documentId}`);
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

  async getInstanceCodePrefixes(): Promise<string[]> {
    const response = await assetApi.get<string[]>('/api/assetinstances/instance-code-prefixes');
    return response.data;
  },

  async getEmployeesByDepartment(departmentId: number): Promise<DepartmentEmployeeOption[]> {
    const response = await assetApi.get<DepartmentEmployeeOption[]>(
      `/api/AssetLocations/departments/${departmentId}/employees`
    );
    return response.data;
  },

  async getSuppliers(keyword?: string): Promise<SupplierListItem[]> {
    const response = await assetApi.get<SupplierListItem[]>('/api/Suppliers', {
      params: keyword ? { keyword } : undefined,
    });
    return response.data;
  },

  async createAssetType(payload: { name: string; description?: string | null; categoryId?: number }): Promise<AssetTypeItem> {
    const categoryId = payload.categoryId ?? 1;
    const response = await assetApi.post<AssetTypeItem>('/api/AssetTypes', {
      categoryId,
      name: payload.name,
    });
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

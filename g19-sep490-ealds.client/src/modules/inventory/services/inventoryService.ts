import axios from 'axios';

const API_BASE_URL = import.meta.env.VITE_API_BASE_URL || '';

const inventoryApi = axios.create({
  baseURL: API_BASE_URL,
  headers: { 'Content-Type': 'application/json' },
});

inventoryApi.interceptors.request.use((config) => {
  const token = localStorage.getItem('accessToken');
  if (token) {
    config.headers = config.headers ?? {};
    config.headers.Authorization = `Bearer ${token}`;
  }
  return config;
});

export interface DropdownItem {
  id: number;
  name: string;
}

export interface AssetDropdownItem {
  assetId: number;
  code: string;
  name: string;
  assetTypeId: number;
  currentDepartmentId: number | null;
}

export interface SessionDetail {
  sessionId: number;
  code: string;
  purpose: string;
  startDate: string;
  endDate: string;
  departmentName: string;
  assetCategoryName: string;
  assetTypeName: string;
  status: number;
  statusName: string;
  quantityDiffCount: number;
  locationChangeCount: number;
  departmentChangeCount: number;
  conditionChangeCount: number;
}

export interface SessionAssetCheckItem {
  assetId: number;
  assetCode: string;
  assetName: string;
  departmentName: string;
  bookQty: number;
  actualQty: number | null;
  difference: number | null;
  checkStatus: number; // 0=Chưa kiểm kê, 1=Đang kiểm kê, 2=Hoàn tất
}

export interface AssetStatusEntry {
  statusKey: string;
  statusLabel: string;
  bookQty: number;
  actualQty: number | null;
}

export interface AssetInventoryDetail {
  assetId: number;
  assetCode: string;
  assetName: string;
  categoryName: string;
  typeName: string;
  statusEntries: AssetStatusEntry[];
  bookLocationId: number | null;
  bookLocationName: string;
  actualLocationId: number | null;
  bookManagerId: number | null;
  bookManagerName: string;
  actualManagerId: number | null;
  locations: DropdownItem[];
  managers: DropdownItem[];
}

export interface SaveAssetInventoryPayload {
  assetId: number;
  statusEntries: { statusKey: string; actualQty: number }[];
  actualLocationId: number | null;
  actualManagerId: number | null;
  checkedBy: number;
}

export interface CompleteSessionResult {
  quantityDiffCount: number;
  locationChangeCount: number;
  departmentChangeCount: number;
  conditionChangeCount: number;
}

export interface InventorySessionListItem {
  sessionId: number;
  code: string;
  purpose: string;
  startDate: string;
  endDate: string;
  departmentId: number;
  departmentName: string;
  assetCategoryName: string;
  assetTypeName: string;
  status: number;
  statusName: string;
  progressPercent: number | null;
  totalTasks: number;
  completedTasks: number;
  createDate: string | null;
}

export interface CreateInventorySessionPayload {
  purpose: string;
  startDate: string;
  endDate: string;
  departmentId: number;
  assetCategoryId: number;
  assetTypeId: number;
  createdBy: number;
}

export function getCurrentUserId(): number {
  try {
    const userStr = localStorage.getItem('user');
    if (!userStr) return 0;
    const user = JSON.parse(userStr);
    return parseInt(user.id, 10) || 0;
  } catch {
    return 0;
  }
}

export const inventoryService = {
  async getSessions(params?: {
    departmentId?: number;
    status?: number;
    keyword?: string;
  }): Promise<InventorySessionListItem[]> {
    const res = await inventoryApi.get<InventorySessionListItem[]>('/api/inventory/sessions', { params });
    return res.data;
  },

  async createSession(payload: CreateInventorySessionPayload): Promise<unknown> {
    const res = await inventoryApi.post('/api/inventory/sessions', payload);
    return res.data;
  },

  async getDepartments(): Promise<DropdownItem[]> {
    const res = await inventoryApi.get<DropdownItem[]>('/api/inventory/meta/departments');
    return res.data;
  },

  async getAssetCategories(): Promise<DropdownItem[]> {
    const res = await inventoryApi.get<DropdownItem[]>('/api/inventory/meta/asset-categories');
    return res.data;
  },

  async getAssetTypes(categoryId?: number): Promise<DropdownItem[]> {
    const res = await inventoryApi.get<DropdownItem[]>('/api/inventory/meta/asset-types', {
      params: categoryId ? { categoryId } : undefined,
    });
    return res.data;
  },

  async getSessionDetail(sessionId: number): Promise<SessionDetail> {
    const res = await inventoryApi.get<SessionDetail>(`/api/inventory/sessions/${sessionId}`);
    return res.data;
  },

  async getSessionAssets(
    sessionId: number,
    params?: { keyword?: string; checkStatus?: number },
  ): Promise<SessionAssetCheckItem[]> {
    const res = await inventoryApi.get<SessionAssetCheckItem[]>(
      `/api/inventory/sessions/${sessionId}/assets`,
      { params },
    );
    return res.data;
  },

  async getAssetInventoryDetail(
    sessionId: number,
    assetId: number,
  ): Promise<AssetInventoryDetail> {
    const res = await inventoryApi.get<AssetInventoryDetail>(
      `/api/inventory/sessions/${sessionId}/assets/${assetId}`,
    );
    return res.data;
  },

  async saveAssetInventory(
    sessionId: number,
    payload: SaveAssetInventoryPayload,
  ): Promise<unknown> {
    const res = await inventoryApi.put(
      `/api/inventory/sessions/${sessionId}/assets/${payload.assetId}`,
      payload,
    );
    return res.data;
  },

  async completeSession(sessionId: number): Promise<CompleteSessionResult> {
    const res = await inventoryApi.post<CompleteSessionResult>(
      `/api/inventory/sessions/${sessionId}/complete`,
    );
    return res.data;
  },

  async getAssets(params?: { keyword?: string }): Promise<AssetDropdownItem[]> {
    const res = await inventoryApi.get<{
      assetId: number;
      code: string;
      name: string;
      assetTypeId: number;
      currentDepartmentId?: number | null;
    }[]>('/api/assets', { params });
    return res.data.map((a) => ({
      assetId: a.assetId,
      code: a.code,
      name: a.name,
      assetTypeId: a.assetTypeId,
      currentDepartmentId: a.currentDepartmentId ?? null,
    }));
  },
};

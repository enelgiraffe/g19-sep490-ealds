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

inventoryApi.interceptors.response.use((response) => {
  const url = response.config.url ?? '';
  if (
    url.includes('/complete') ||
    url.includes('/director-approve') ||
    url.includes('/reject') ||
    url.includes('/cancel')
  ) {
    window.dispatchEvent(new Event('ealds-notifications-changed'));
  }
  return response;
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
  progressPercent: number | null;
  totalTasks: number;
  completedTasks: number;
  unresolvedDiscrepancyCount?: number;
  quantityDiffCount: number;
  locationChangeCount: number;
  departmentChangeCount: number;
  conditionChangeCount: number;
}

export interface SessionAssetCheckItem {
  /** Catalog (master) asset id */
  assetId: number;
  /** Physical row being counted */
  assetInstanceId: number;
  assetCode: string;
  instanceCode: string;
  assetName: string;
  departmentName: string;
  /** Book-side AssetStatus int */
  bookStatus: number;
  /** Reported status after check; null if not yet submitted */
  actualStatus: number | null;
  checkStatus: number; // 0=Chưa kiểm kê, 1=Đang kiểm kê, 2=Hoàn tất
}

export interface AssetInventoryDetail {
  assetId: number;
  assetInstanceId: number;
  assetCode: string;
  instanceCode: string;
  assetName: string;
  categoryName: string;
  typeName: string;
  /** Book-side AssetStatus int */
  bookStatus: number;
  /** Book-side status name */
  bookAssetStatus?: string;
  /** Last reported status (int); null if never saved */
  actualStatus: number | null;
  /** Stored enum name on record */
  actualCondition: string;
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
  assetInstanceId: number;
  /** Reported AssetStatus int */
  actualStatus: number;
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

export interface ReviewSessionPayload {
  reviewedBy: number;
  reviewerRoleId: number;
  reviewNotes?: string;
  applyCorrections?: boolean;
}

export interface InventoryDiscrepancyDetail {
  discrepancyId: number;
  taskId: number;
  assetId: number;
  assetInstanceId: number;
  assetCode: string;
  instanceCode: string;
  assetName: string;
  discrepancyType: number;
  discrepancyTypeName: string;
  bookValue: number;
  bookQuantity: number;
  actualQuantity: number | null;
  bookDepartmentName?: string;
  bookUserId?: number;
  bookUserName?: string;
  bookCondition: string;
  actualValue: number;
  actualDepartmentName?: string;
  actualUserId?: number;
  actualUserName?: string;
  actualCondition: string;
  /** Set when accountant applied actuals to the book (UTC ISO string from API). */
  resolvedAt?: string | null;
}

export interface InventoryReviewSummary {
  sessionId: number;
  code: string;
  purpose: string;
  startDate: string;
  endDate: string;
  departmentName: string;
  assetCategoryName?: string;
  assetTypeName?: string;
  status: number;
  statusName: string;
  totalTasks: number;
  completedTasks: number;
  progressPercent: number | null;
  totalDiscrepancies: number;
  assetNotFoundCount: number;
  quantityMismatchCount: number;
  locationMismatchCount: number;
  userMismatchCount: number;
  valueMismatchCount: number;
  conditionMismatchCount: number;
  discrepancies: InventoryDiscrepancyDetail[];
}

export const SESSION_STATUS = {
  Scheduled: 0,
  InProgress: 1,
  Completed: 2,
  Cancelled: 3,
  Confirmed: 4,
  Due: 5,
  PendingAccountant: 6,
} as const;

export const SESSION_STATUS_LABEL: Record<number, string> = {
  0: 'Đã lên lịch',
  1: 'Đang thực hiện',
  2: 'Chờ xác nhận',
  3: 'Đã hủy',
  4: 'Đã xử lý',
  5: 'Đến lịch',
  6: 'Chờ xử lý',
};

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
  isPeriodic: boolean;
  periodDays: number | null;
  /** Rows in InventoryDiscrepancy with ResolvedAt null (eligible tasks only). */
  unresolvedDiscrepancyCount?: number;
}

export interface CreateInventorySessionPayload {
  purpose: string;
  startDate: string;
  endDate: string;
  departmentId: number;
  assetCategoryId?: number;
  assetTypeId?: number;
  createdBy: number;
  isPeriodic?: boolean;
  periodDays?: number;
}

export function getCurrentUserId(): number {
  try {
    const userStr = localStorage.getItem('user');
    if (!userStr) return 0;
    const user = JSON.parse(userStr);
    return Number.parseInt(user.id, 10) || 0;
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
    assetInstanceId: number,
  ): Promise<AssetInventoryDetail> {
    const res = await inventoryApi.get<AssetInventoryDetail>(
      `/api/inventory/sessions/${sessionId}/instances/${assetInstanceId}`,
    );
    return res.data;
  },

  async saveAssetInventory(
    sessionId: number,
    payload: SaveAssetInventoryPayload,
  ): Promise<unknown> {
    const res = await inventoryApi.put(
      `/api/inventory/sessions/${sessionId}/instances/${payload.assetInstanceId}`,
      payload,
    );
    return res.data;
  },

  /** Same rows as getSessionAssets, filtered to one catalog (master) asset. */
  async getSessionAssetsForCatalogAsset(
    sessionId: number,
    catalogAssetId: number,
    params?: { keyword?: string; checkStatus?: number },
  ): Promise<SessionAssetCheckItem[]> {
    const res = await inventoryApi.get<SessionAssetCheckItem[]>(
      `/api/inventory/sessions/${sessionId}/assets/${catalogAssetId}/items`,
      { params },
    );
    return res.data;
  },

  async completeSession(sessionId: number): Promise<CompleteSessionResult> {
    const res = await inventoryApi.post<CompleteSessionResult>(
      `/api/inventory/sessions/${sessionId}/complete`,
    );
    return res.data;
  },

  async getReviewSummary(sessionId: number): Promise<InventoryReviewSummary> {
    const res = await inventoryApi.get<InventoryReviewSummary>(
      `/api/inventory/sessions/${sessionId}/review-summary`,
    );
    return res.data;
  },

  async applyDiscrepancyActual(
    sessionId: number,
    discrepancyId: number,
  ): Promise<{ message?: string }> {
    const res = await inventoryApi.post<{ message?: string }>(
      `/api/inventory/sessions/${sessionId}/discrepancies/${discrepancyId}/apply-actual`,
    );
    return res.data;
  },

  async directorApproveSession(
    sessionId: number,
    payload: ReviewSessionPayload,
  ): Promise<{
    message?: string;
    newStatus?: number;
    statusName?: string;
    hasQuantityOrUserDiscrepancy?: boolean;
  }> {
    const res = await inventoryApi.post(
      `/api/inventory/sessions/${sessionId}/director-approve`,
      payload,
    );
    return res.data;
  },

  async confirmSession(
    sessionId: number,
    payload: ReviewSessionPayload,
  ): Promise<{ message?: string; sessionId?: number }> {
    const res = await inventoryApi.post(`/api/inventory/sessions/${sessionId}/confirm`, payload);
    return res.data;
  },

  async rejectSession(
    sessionId: number,
    payload: ReviewSessionPayload,
  ): Promise<{ message?: string; sessionId?: number }> {
    const res = await inventoryApi.post(`/api/inventory/sessions/${sessionId}/reject`, payload);
    return res.data;
  },

  async cancelSession(sessionId: number, payload: ReviewSessionPayload): Promise<unknown> {
    const res = await inventoryApi.post(
      `/api/inventory/sessions/${sessionId}/cancel`,
      payload,
    );
    return res.data;
  },

  async activateSession(sessionId: number): Promise<unknown> {
    const res = await inventoryApi.post(`/api/inventory/sessions/${sessionId}/activate`);
    return res.data;
  },

  async updateSession(
    sessionId: number,
    payload: { purpose: string; startDate: string; endDate: string; periodDays?: number },
  ): Promise<unknown> {
    const res = await inventoryApi.put(`/api/inventory/sessions/${sessionId}`, payload);
    return res.data;
  },

  async getAssets(params?: { keyword?: string }): Promise<AssetDropdownItem[]> {
    const res = await inventoryApi.get<
      {
        assetInstanceId: number;
        assetId: number;
        assetTypeId: number;
        instanceCode: string;
        assetCode?: string | null;
        assetName?: string | null;
        currentDepartmentId?: number | null;
      }[]
    >('/api/assetinstances', { params });
    return res.data.map((a) => ({
      assetId: a.assetId,
      code: a.instanceCode,
      name: a.assetName ?? a.assetCode ?? a.instanceCode,
      assetTypeId: a.assetTypeId,
      currentDepartmentId: a.currentDepartmentId ?? null,
    }));
  },
};

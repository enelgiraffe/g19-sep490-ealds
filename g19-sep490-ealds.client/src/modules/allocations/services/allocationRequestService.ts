import axios from 'axios';

const API_BASE_URL = import.meta.env.VITE_API_BASE_URL || '';

/** Parses ASP.NET BadRequest `{ message }` or validation ProblemDetails `{ errors }`. */
export function allocationRequestApiErrorMessage(data: unknown): string | null {
  if (!data || typeof data !== 'object') return null;
  const o = data as Record<string, unknown>;
  if (typeof o.message === 'string' && o.message.trim()) return o.message.trim();
  if (typeof o.detail === 'string' && o.detail.trim()) return o.detail.trim();
  const errs = o.errors;
  if (errs && typeof errs === 'object' && !Array.isArray(errs)) {
    const parts = Object.values(errs as Record<string, unknown>).flatMap((v) =>
      Array.isArray(v) ? v.filter((x): x is string => typeof x === 'string') : [],
    );
    if (parts.length) return parts.join(' ');
  }
  if (typeof o.title === 'string' && o.title.trim()) return o.title.trim();
  return null;
}

const api = axios.create({
  baseURL: API_BASE_URL,
  headers: { 'Content-Type': 'application/json' },
});

api.interceptors.request.use((config) => {
  const token = localStorage.getItem('accessToken');
  if (token) {
    config.headers = config.headers ?? {};
    config.headers.Authorization = `Bearer ${token}`;
  }
  return config;
});

export const ALLOCATION_REQUEST_TYPE_ID = 6;

export interface AllocationLineInput {
  assetTypeId: number;
  assetId: number;
  quantity: number;
  reason?: string | null;
}

export interface AllocationRequestListItem {
  assetRequestId: number;
  assetAllocationOrderId: number | null;
  title: string;
  status: number;
  departmentId: number;
  departmentName: string;
  createDate: string;
  requestedByUserId: number;
  requestedByName: string;
  receiptConfirmedAt?: string | null;
  receiptConfirmedByUserId?: number | null;
  receiptConfirmedByName?: string | null;
}

export interface AllocationOrderLineDetail {
  assetTypeId: number;
  assetTypeName: string;
  assetId: number;
  assetCode: string;
  assetName: string;
  quantity: number;
  reason?: string | null;
}

export interface AllocationOrderSummaryRow {
  assetAllocationOrderId: number;
  assetRequestId: number;
  title: string;
  departmentName: string;
  orderStatus: 'awaiting_confirm' | 'confirmed';
  requestStatus: number;
  createdAt: string;
  confirmedAt?: string | null;
}

export interface AllocationOrderDetail {
  assetAllocationOrderId: number;
  assetRequestId: number;
  /** allocation = cấp phát; return = hoàn trả về kho */
  orderKind?: 'allocation' | 'return';
  title: string;
  departmentId: number;
  departmentName: string;
  orderStatus: 'awaiting_confirm' | 'confirmed';
  requestStatus: number;
  requestedByUserId: number;
  requestedByName: string;
  requestSubmittedAt: string;
  createdAt: string;
  confirmedAt?: string | null;
  confirmedByUserId?: number | null;
  confirmedByName?: string | null;
  lines: AllocationOrderLineDetail[];
}

export interface CatalogAssetOption {
  assetId: number;
  code: string;
  name: string;
  assetTypeId: number;
}

export const allocationRequestService = {
  /** When `warehouseStockOnly`, only assets with ≥1 instance not assigned to any department (kho). */
  async catalogByType(
    assetTypeId: number,
    keyword?: string,
    warehouseStockOnly?: boolean,
  ): Promise<CatalogAssetOption[]> {
    const res = await api.get<CatalogAssetOption[]>('/api/Assets', {
      params: {
        assetTypeId,
        keyword: keyword?.trim() || undefined,
        ...(warehouseStockOnly ? { warehouseStockOnly: true } : {}),
      },
    });
    return Array.isArray(res.data) ? res.data : [];
  },

  async list(): Promise<AllocationRequestListItem[]> {
    const res = await api.get<AllocationRequestListItem[]>('/api/Assets/Requests/allocation');
    return res.data;
  },

  async listOrdersSummary(): Promise<AllocationOrderSummaryRow[]> {
    const res = await api.get<AllocationOrderSummaryRow[]>('/api/Assets/Requests/allocation/orders-summary');
    return Array.isArray(res.data) ? res.data : [];
  },

  async create(payload: { title: string; lines: AllocationLineInput[] }): Promise<{ assetRequestId: number }> {
    try {
      const res = await api.post<{ assetRequestId: number }>('/api/Assets/Requests/allocation', payload);
      return res.data;
    } catch (e) {
      if (axios.isAxiosError(e) && e.response?.data) {
        const msg = allocationRequestApiErrorMessage(e.response.data);
        if (msg) throw new Error(msg);
      }
      throw e;
    }
  },

  async warehouseAvailable(assetId: number): Promise<number> {
    const res = await api.get<number>('/api/Assets/Requests/allocation/warehouse-available', {
      params: { assetId },
    });
    return res.data;
  },

  async getOrder(orderId: number): Promise<AllocationOrderDetail> {
    const res = await api.get<AllocationOrderDetail>(`/api/Assets/Requests/allocation/orders/${orderId}`);
    return res.data;
  },

  async confirmOrder(orderId: number): Promise<{ assetAllocationOrderId: number; status: string }> {
    const res = await api.post<{ assetAllocationOrderId: number; status: string }>(
      `/api/Assets/Requests/allocation/orders/${orderId}/confirm`,
    );
    return res.data;
  },
};

import axios from 'axios';

const API_BASE_URL = import.meta.env.VITE_API_BASE_URL || '';

const poApi = axios.create({
  baseURL: API_BASE_URL,
  headers: { 'Content-Type': 'application/json' },
});

poApi.interceptors.request.use((config) => {
  const token = localStorage.getItem('accessToken');
  if (token) {
    config.headers = config.headers ?? {};
    config.headers.Authorization = `Bearer ${token}`;
  }
  return config;
});

/** Matches backend PurchaseOrdersController */
export const PO_STATUS = {
  created: 0,
  partiallyReceived: 1,
  cancelled: 2,
  completed: 3,
} as const;

export interface PurchaseOrderLineWrite {
  description?: string | null;
  assetId?: number | null;
  quantity: number;
  unit?: string | null;
  unitPrice: number;
  expectedDeliveryDate?: string | null;
}

export interface PurchaseOrderCreateBody {
  assetRequestId?: number | null;
  supplierId: number;
  currency?: string;
  lines: PurchaseOrderLineWrite[];
}

export interface PurchaseOrderLineItem {
  lineId: number;
  lineIndex: number;
  description: string | null;
  assetId: number | null;
  assetCode: string | null;
  assetName: string | null;
  quantity: number;
  unit: string | null;
  unitPrice: number;
  expectedDeliveryDate: string | null;
  lineTotal: number;
  receivedQuantity: number;
  openQuantity: number;
}

export interface PurchaseOrderListItem {
  procurementId: number;
  assetRequestId: number | null;
  supplierId: number;
  supplierName: string | null;
  contractNo: string;
  title: string;
  currency: string;
  totalAmount: number;
  status: number;
  createDate: string;
}

export interface PurchaseOrderDetail extends PurchaseOrderListItem {
  lines: PurchaseOrderLineItem[];
}

export interface PurchaseOrderListResponse {
  items: PurchaseOrderListItem[];
  total: number;
  page: number;
  pageSize: number;
  totalPages: number;
}

export const procurementPoService = {
  async getList(params: {
    procurementId?: number;
    supplierId?: number;
    status?: number;
    receivingEligible?: boolean;
    page?: number;
    pageSize?: number;
  }): Promise<PurchaseOrderListResponse> {
    const response = await poApi.get<PurchaseOrderListResponse>('/api/purchase-orders', { params });
    return response.data;
  },

  async getById(id: number): Promise<PurchaseOrderDetail> {
    const response = await poApi.get<PurchaseOrderDetail>(`/api/purchase-orders/${id}`);
    return response.data;
  },

  async create(body: PurchaseOrderCreateBody): Promise<{ procurementId: number }> {
    const response = await poApi.post<{ procurementId: number }>('/api/purchase-orders', body);
    return response.data;
  },

  async update(id: number, body: PurchaseOrderCreateBody): Promise<{ procurementId: number }> {
    const response = await poApi.put<{ procurementId: number }>(`/api/purchase-orders/${id}`, body);
    return response.data;
  },

  async cancel(id: number): Promise<{ procurementId: number; status: number }> {
    const response = await poApi.post<{ procurementId: number; status: number }>(
      `/api/purchase-orders/${id}/cancel`,
      {},
    );
    return response.data;
  },
};

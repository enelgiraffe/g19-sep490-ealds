import axios from 'axios';

const API_BASE_URL = import.meta.env.VITE_API_BASE_URL || '';

const purchaseApi = axios.create({
  baseURL: API_BASE_URL,
  headers: {
    'Content-Type': 'application/json',
  },
});

purchaseApi.interceptors.request.use((config) => {
  const token = localStorage.getItem('accessToken');
  if (token) {
    config.headers = config.headers ?? {};
    config.headers.Authorization = `Bearer ${token}`;
  }
  return config;
});

/** Backend Status: -1=Nháp, 0=Đã gửi (kế toán), 1=Chờ duyệt (giám đốc), 2=Duyệt, 3=Từ chối, 4=Chờ ngân sách */
export interface PurchaseOrderListItem {
  assetRequestId: number;
  assetId?: number | null;
  title: string;
  description: string | null;
  proposedData: string | null;
  status: number;
  createDate: string;
  createdBy: number;
  creatorName: string | null;
}

export interface PurchaseOrderDetail extends PurchaseOrderListItem {}

export interface CreatePurchaseOrderPayload {
  userId: number;
  assetId?: number | null;
  title: string;
  description?: string | null;
  proposedData?: string | null;
  createdBy: number;
  /** -1: Draft, 0: Submitted/Pending approval (default) */
  status?: number | null;
}

export const purchaseOrderService = {
  async getList(requestTypeId?: number): Promise<PurchaseOrderListItem[]> {
    const params = requestTypeId != null ? { requestTypeId } : {};
    const response = await purchaseApi.get<PurchaseOrderListItem[]>(
      '/api/Assets/Requests/purchase',
      { params }
    );
    return response.data;
  },

  async getById(id: number): Promise<PurchaseOrderDetail> {
    const response = await purchaseApi.get<PurchaseOrderDetail>(
      `/api/Assets/Requests/purchase/${id}`
    );
    return response.data;
  },

  async create(payload: CreatePurchaseOrderPayload): Promise<{ assetRequestId: number }> {
    const response = await purchaseApi.post<{ assetRequestId: number }>(
      '/api/Assets/Requests/purchase',
      payload
    );
    return response.data;
  },

  async update(id: number, payload: CreatePurchaseOrderPayload): Promise<{ assetRequestId: number }> {
    const response = await purchaseApi.put<{ assetRequestId: number }>(
      `/api/Assets/Requests/purchase/${id}`,
      payload
    );
    return response.data;
  },

  async approveAsAccountant(
    id: number,
    payload: { approvedBy: number; comment?: string | null },
  ): Promise<{ assetRequestId: number; status: number }> {
    const response = await purchaseApi.post<{ assetRequestId: number; status: number }>(
      `/api/Assets/Requests/accountant/${id}/approve`,
      {
        approvedBy: payload.approvedBy,
        comment: payload.comment ?? null,
      },
    );
    return response.data;
  },

  async rejectAsAccountant(
    id: number,
    payload: { approvedBy: number; comment?: string | null },
  ): Promise<{ assetRequestId: number; status: number }> {
    const response = await purchaseApi.post<{ assetRequestId: number; status: number }>(
      `/api/Assets/Requests/accountant/${id}/reject`,
      {
        approvedBy: payload.approvedBy,
        comment: payload.comment ?? null,
      },
    );
    return response.data;
  },
};

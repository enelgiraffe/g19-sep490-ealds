import axios from 'axios';

const API_BASE_URL = import.meta.env.VITE_API_BASE_URL || '';

/** Khớp App:TransferRequestTypeId trên backend (mặc định 3). */
export const TRANSFER_REQUEST_TYPE_ID = 3;

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
  assetInstanceId: number;
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
  /** Danh mục: từ API thanh lý / điều chuyển khi backend gửi kèm */
  assetTypeName?: string | null;
  assetInstanceId?: number | null;
  instanceCode?: string | null;
  fromDepartment: string;
  toDepartment: string;
  fromDepartmentId: number;
  toDepartmentId: number;
  createdBy: number;
  createdByName?: string | null;
  quantity: number;
  status: number;
  statusName: string;
  reason?: string | null;
  /** Chỉ API thanh lý: nguyên giá cá thể */
  originalPrice?: number | null;
  /** Chỉ API thanh lý: giá trị còn lại trên sổ */
  currentValue?: number | null;
  /** Chỉ API thanh lý: giá trị khai báo trên đơn */
  disposalDeclaredValue?: number | null;
  isSenderConfirmed: boolean;
  isReceiverConfirmed: boolean;
  /** Ý kiến kế toán (từ bước phê duyệt). */
  accountantComment?: string | null;
  /** Ý kiến giám đốc (từ bước phê duyệt). */
  directorComment?: string | null;
}

export interface TransferHandoverDetails {
  side: string;
  protocolCode: string;
  assetRequestId: number;
  fromDepartment: string;
  toDepartment: string;
  instanceCode: string;
  assetCode: string;
  assetName: string;
  summary: string;
}

export interface TransferHandoverRecordItem {
  transferHandoverRecordId: number;
  side: string;
  actionByUserId: number;
  actionByUserName?: string | null;
  occurredAt: string;
  details: TransferHandoverDetails;
  userNote?: string | null;
}

export const transferRequestService = {
  async getList(): Promise<TransferRequestListItem[]> {
    const response = await transferApi.get<TransferRequestListItem[]>('/api/Assets/Requests/transfer');
    return response.data;
  },

  async getAssetLocations(): Promise<AssetLocationOption[]> {
    // Backend exposes department dropdown options at /api/AssetLocations/departments
    const response = await transferApi.get<AssetLocationOption[]>('/api/AssetLocations/departments');
    return response.data;
  },

  async create(payload: TransferRequestPayload): Promise<{ assetRequestId: number; recordId: number }> {
    const response = await transferApi.post<{ assetRequestId: number; recordId: number }>(
      '/api/Assets/Requests/transfer',
      payload
    );
    return response.data;
  },

  async delete(assetRequestId: number): Promise<void> {
    await transferApi.delete(`/api/Assets/Requests/transfer/${assetRequestId}`);
  },

  async getHandoverRecords(assetRequestId: number): Promise<TransferHandoverRecordItem[]> {
    const response = await transferApi.get<TransferHandoverRecordItem[]>(
      `/api/Assets/Requests/transfer/${assetRequestId}/handover-records`,
    );
    return response.data;
  },

  async confirmSend(
    assetRequestId: number,
    payload?: { note?: string | null },
  ): Promise<{ message: string; isReady: boolean }> {
    const response = await transferApi.post<{ message: string; isReady: boolean }>(
      `/api/Assets/Requests/transfer/${assetRequestId}/confirm-send`,
      payload ?? {},
    );
    return response.data;
  },

  async confirmReceive(
    assetRequestId: number,
    payload?: { note?: string | null },
  ): Promise<{ message: string; isReady: boolean }> {
    const response = await transferApi.post<{ message: string; isReady: boolean }>(
      `/api/Assets/Requests/transfer/${assetRequestId}/confirm-receive`,
      payload ?? {},
    );
    return response.data;
  },

  async approveAsAccountant(
    id: number,
    payload: { approvedBy: number; comment?: string | null },
  ): Promise<{ assetRequestId: number; status: number }> {
    const response = await transferApi.post<{ assetRequestId: number; status: number }>(
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
    const response = await transferApi.post<{ assetRequestId: number; status: number }>(
      `/api/Assets/Requests/accountant/${id}/reject`,
      {
        approvedBy: payload.approvedBy,
        comment: payload.comment ?? null,
      },
    );
    return response.data;
  },
};


import axios from 'axios';

const API_BASE_URL = import.meta.env.VITE_API_BASE_URL || '';

const damageReportApi = axios.create({
  baseURL: API_BASE_URL,
  headers: {
    'Content-Type': 'application/json',
  },
});

damageReportApi.interceptors.request.use((config) => {
  const token = localStorage.getItem('accessToken');
  if (token) {
    config.headers = config.headers ?? {};
    config.headers.Authorization = `Bearer ${token}`;
  }
  return config;
});

export interface ReportDamagePayload {
  assetInstanceId: number;
  reportedBy: number;
  /** ISO date string or value parsable by backend DateTime (ngày hỏng/ghi nhận) */
  reportDate: string;
  description?: string | null;
  severity?: number | null;
  documentId?: number | null;
}

export interface ReportDamageResponse {
  assetInstanceId: number;
}

export interface AssetRequestListItem {
  id: number;
  title: string | null;
  description: string | null;
  status: number;
  createDate: string;
  userId: number;
  userEmail?: string | null;
  assetId: number;
  assetInstanceId?: number | null;
  assetCode?: string | null;
  assetInstanceCode?: string | null;
  assetName?: string | null;
  assetQuantity?: number | null;
  currentDepartmentName?: string | null;
  currentLocation?: string | null;
  requestTypeId: number;
}

export interface PagedAssetRequestResult {
  items: AssetRequestListItem[];
  total: number;
  page: number;
  pageSize: number;
  totalPages: number;
}

export const damageReportService = {
  async report(payload: ReportDamagePayload): Promise<ReportDamageResponse> {
    const body = {
      assetInstanceId: payload.assetInstanceId,
      reportedBy: payload.reportedBy,
      description: payload.description ?? null,
      severity: payload.severity ?? null,
      reportDate: payload.reportDate,
      documentId: payload.documentId ?? null,
    };

    const response = await damageReportApi.post<ReportDamageResponse>(
      '/api/Assets/Requests/report-damage',
      body
    );
    return response.data;
  },

  async delete(assetRequestId: number): Promise<{ assetRequestId: number; deleted: boolean }> {
    const response = await damageReportApi.delete<{ assetRequestId: number; deleted: boolean }>(
      `/api/Assets/Requests/report-damage/${assetRequestId}`
    );
    return response.data;
  },

  async list(params?: {
    status?: number;
    requestTypeId?: number;
    userId?: number;
    page?: number;
    pageSize?: number;
  }): Promise<PagedAssetRequestResult> {
    const response = await damageReportApi.get<PagedAssetRequestResult>('/api/Assets/Requests', {
      params,
    });
    return response.data;
  },
};


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
  assetId: number;
  reportedBy: number;
  requestTypeId?: number | null;
  /** ISO date string or value parsable by backend DateTime (ngày hỏng/ghi nhận) */
  reportDate: string;
  description?: string | null;
  severity?: number | null;
  documentId?: number | null;
}

export interface ReportDamageResponse {
  assetRequestId: number;
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
  assetCode?: string | null;
  assetName?: string | null;
  assetQuantity?: number | null;
  currentDepartmentName?: string | null;
  requestTypeId: number;
}

export interface PagedAssetRequestResult {
  items: AssetRequestListItem[];
  total: number;
  page: number;
  pageSize: number;
  totalPages: number;
}

/**
 * RequestTypeId theo config hiện có: 1=Mua, 2=Bảo dưỡng, 3=Điều chuyển, 4=Sửa chữa.
 * Báo hỏng hiện được dùng để dẫn tới luồng sửa chữa, nên default = 4 nếu không truyền.
 */
const DEFAULT_DAMAGE_REQUEST_TYPE_ID = 4;

export const damageReportService = {
  async report(payload: ReportDamagePayload): Promise<ReportDamageResponse> {
    const body = {
      assetId: payload.assetId,
      reportedBy: payload.reportedBy,
      requestTypeId: payload.requestTypeId ?? DEFAULT_DAMAGE_REQUEST_TYPE_ID,
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


import axios from 'axios';

const API_BASE_URL = import.meta.env.VITE_API_BASE_URL || '';

const directorApi = axios.create({
  baseURL: API_BASE_URL,
  headers: {
    'Content-Type': 'application/json',
  },
});

directorApi.interceptors.request.use((config) => {
  const token = localStorage.getItem('accessToken');
  if (token) {
    config.headers = config.headers ?? {};
    config.headers.Authorization = `Bearer ${token}`;
  }
  return config;
});

/** RequestTypeId trùng backend: 1=Mua, 2=Bảo dưỡng, 3=Điều chuyển, 4=Sửa chữa, 5=Thanh lý (nếu có) */
export const REQUEST_TYPE_IDS = {
  purchase: 1,
  maintenance: 2,
  transfer: 3,
  repair: 4,
  liquidation: 5,
} as const;

export interface DirectorRequestListItem {
  assetRequestId: number;
  title: string;
  status: number;
  requestTypeId: number;
  userId: number;
  createDate: string;
  description?: string | null;
  proposedData?: string | null;
  assetId?: number | null;
  assetCode?: string | null;
  assetName?: string | null;
  assetQuantity?: number | null;
  currentDepartmentName?: string | null;
  creatorEmail?: string | null;
}

export interface DirectorViewParams {
  status?: number | null;
  requestTypeId?: number | null;
  userId?: number | null;
  page?: number;
  pageSize?: number;
}

export interface DirectorViewResponse {
  items: DirectorRequestListItem[];
  total: number;
  page: number;
  pageSize: number;
  totalPages: number;
}

export const directorRequestService = {
  async getView(params: DirectorViewParams = {}): Promise<DirectorViewResponse> {
    const { status, requestTypeId, userId, page = 1, pageSize = 50 } = params;
    const queryParams: Record<string, number> = {
      page,
      pageSize,
    };
    if (status != null && status !== undefined) queryParams.status = status;
    if (requestTypeId != null && requestTypeId !== undefined) queryParams.requestTypeId = requestTypeId;
    if (userId != null && userId !== undefined) queryParams.userId = userId;
    const response = await directorApi.get<DirectorViewResponse>(
      '/api/Assets/Requests/director/view',
      { params: queryParams },
    );
    return response.data;
  },
};

import axios from 'axios';

const API_BASE_URL = import.meta.env.VITE_API_BASE_URL || '';

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

/** Response GET /api/Assets/Requests/{id} (subset used by FE) */
export interface AssetRequestDetailsResponse {
  id: number;
  title?: string | null;
  description?: string | null;
  proposedData?: string | null;
  status: number;
  createDate?: string;
  approveDate?: string | null;
  asset?: {
    assetId: number;
    name: string;
    code: string;
  } | null;
  requestType?: { requestTypeId: number; workflowId: number } | null;
  maintenanceTasks?: Array<{
    taskId: number;
    plannedDate: string;
    status: number;
    assignTo: number;
  }>;
  repairTasks?: Array<{
    taskId: number;
    estimatedCost: number;
    reason: string;
    status: number;
  }>;
}

export const assetRequestService = {
  async getById(assetRequestId: number): Promise<AssetRequestDetailsResponse> {
    const res = await api.get<AssetRequestDetailsResponse>(
      `/api/Assets/Requests/${assetRequestId}`
    );
    return res.data;
  },
};

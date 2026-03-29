import axios from 'axios';

const API_BASE_URL = import.meta.env.VITE_API_BASE_URL || '';

const disposalApi = axios.create({
  baseURL: API_BASE_URL,
  headers: {
    'Content-Type': 'application/json',
  },
});

disposalApi.interceptors.request.use((config) => {
  const token = localStorage.getItem('accessToken');
  if (token) {
    config.headers = config.headers ?? {};
    config.headers.Authorization = `Bearer ${token}`;
  }
  return config;
});

const DEFAULT_DISPOSAL_REQUEST_TYPE_ID = 5;

export interface DisposalRequestPayload {
  userId: number;
  assetInstanceId: number;
  createdBy: number;
  title: string;
  description?: string | null;
  requestTypeId?: number | null;
  diposalMethod?: number;
  diposalValue?: number;
  diposalDate: string;
  reason?: string | null;
}

export interface DisposalRequestResponse {
  assetRequestId: number;
  diposalId: number;
}

export const disposalRequestService = {
  async create(payload: DisposalRequestPayload): Promise<DisposalRequestResponse> {
    const body = {
      userId: payload.userId,
      assetInstanceId: payload.assetInstanceId,
      requestTypeId: payload.requestTypeId ?? DEFAULT_DISPOSAL_REQUEST_TYPE_ID,
      title: payload.title,
      description: payload.description ?? null,
      createdBy: payload.createdBy,
      diposalMethod: payload.diposalMethod ?? 0,
      diposalValue: payload.diposalValue ?? 0,
      diposalDate: payload.diposalDate,
      reason: payload.reason ?? null,
    };

    const response = await disposalApi.post<DisposalRequestResponse>(
      '/api/Assets/Requests/disposal',
      body
    );
    return response.data;
  },
};

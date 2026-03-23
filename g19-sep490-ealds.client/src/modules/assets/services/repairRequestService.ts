import axios from 'axios';

const API_BASE_URL = import.meta.env.VITE_API_BASE_URL || '';

const repairApi = axios.create({
  baseURL: API_BASE_URL,
  headers: { 'Content-Type': 'application/json' },
});

repairApi.interceptors.request.use((config) => {
  const token = localStorage.getItem('accessToken');
  if (token) {
    config.headers = config.headers ?? {};
    config.headers.Authorization = `Bearer ${token}`;
  }
  return config;
});

/** Khớp backend RepairStartDto */
export interface RepairStartPayload {
  startedBy: number;
  comment?: string | null;
  reportNumber?: string | null;
  damageDate?: string | null;
  damageCondition?: string | null;
  attachmentDocumentIds?: number[] | null;
  attachmentUrls?: string[] | null;
  repairDate?: string | null;
  expectedCompletionDate?: string | null;
  expectedCompletionFrom?: string | null;
  expectedCompletionTo?: string | null;
  estimatedCost?: number | null;
  repairProgressStatus?: string | null;
}

export const repairRequestService = {
  async start(
    assetRequestId: number,
    payload: RepairStartPayload
  ): Promise<{ assetRequestId: number; status: number; taskId?: number }> {
    const response = await repairApi.post<{
      assetRequestId: number;
      status: number;
      taskId?: number;
    }>(`/api/Assets/Requests/repair/${assetRequestId}/start`, payload);
    return response.data;
  },

  async complete(
    taskId: number,
    payload: RepairCompletePayload
  ): Promise<{ recordId: number; taskId: number }> {
    const response = await repairApi.post<{ recordId: number; taskId: number }>(
      `/api/Assets/Requests/repair/tasks/${taskId}/complete`,
      payload
    );
    return response.data;
  },
};

/** Khớp backend RepairCompleteDto */
export interface RepairCompletePayload {
  completedBy: number;
  reportNumber?: string | null;
  completionDate?: string | null;
  repairDate?: string | null;
  returnToUseDate?: string | null;
  actualCost: number;
  result?: string;
  detailedDescription?: string | null;
  supplierId?: number | null;
  attachmentDocumentIds?: number[] | null;
  attachmentUrls?: string[] | null;
}

import axios from 'axios';

const API_BASE_URL = import.meta.env.VITE_API_BASE_URL || '';

const executionApi = axios.create({
  baseURL: API_BASE_URL,
  headers: { 'Content-Type': 'application/json' },
});

executionApi.interceptors.request.use((config) => {
  const token = localStorage.getItem('accessToken');
  if (token) {
    config.headers = config.headers ?? {};
    config.headers.Authorization = `Bearer ${token}`;
  }
  return config;
});

export interface DisposalExecutionDto {
  disposalExecutionId?: number | null;
  assetRequestId: number;
  disposalRecordId?: number | null;
  plannedExecutionDate?: string | null;
  executedDate?: string | null;
  executionMethod?: number | null;
  buyerName?: string | null;
  buyerContact?: string | null;
  contractNo?: string | null;
  invoiceNo?: string | null;
  minutesNo?: string | null;
  actualDisposalValue?: number | null;
  expenseValue?: number | null;
  attachmentUrls?: string | null;
  executionNote?: string | null;
  status: number;
  assetRequestStatus: number;
  canEdit: boolean;
  canFinalize: boolean;
  blockFinalizeReason?: string | null;
}

export interface RecordDisposalAppraisalPayload {
  userId: number;
  appraisalDate?: string | null;
  appraisalMinutesNo?: string | null;
  appraisalConclusion?: string | null;
}

export interface SaveDisposalExecutionPayload {
  userId: number;
  plannedExecutionDate?: string | null;
  executedDate?: string | null;
  executionMethod?: number | null;
  buyerName?: string | null;
  buyerContact?: string | null;
  contractNo?: string | null;
  invoiceNo?: string | null;
  minutesNo?: string | null;
  actualDisposalValue?: number | null;
  expenseValue?: number | null;
  attachmentUrls?: string | null;
  executionNote?: string | null;
}

export const disposalExecutionService = {
  async getByAssetRequest(assetRequestId: number): Promise<DisposalExecutionDto> {
    const response = await executionApi.get<DisposalExecutionDto>(
      `/api/Assets/Requests/disposal/execution/by-request/${assetRequestId}`,
    );
    return response.data;
  },

  async save(assetRequestId: number, payload: SaveDisposalExecutionPayload): Promise<DisposalExecutionDto> {
    const response = await executionApi.put<DisposalExecutionDto>(
      `/api/Assets/Requests/disposal/execution/by-request/${assetRequestId}`,
      payload,
    );
    return response.data;
  },

  async finalize(assetRequestId: number, userId: number): Promise<void> {
    await executionApi.post(`/api/Assets/Requests/disposal/execution/by-request/${assetRequestId}/finalize`, {
      userId,
    });
  },

  async recordAppraisal(
    assetRequestId: number,
    payload: RecordDisposalAppraisalPayload,
  ): Promise<DisposalExecutionDto> {
    const response = await executionApi.post<DisposalExecutionDto>(
      `/api/Assets/Requests/disposal/execution/by-request/${assetRequestId}/record-appraisal`,
      payload,
    );
    return response.data;
  },
};

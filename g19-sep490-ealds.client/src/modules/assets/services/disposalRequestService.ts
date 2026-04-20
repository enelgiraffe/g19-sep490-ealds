import type { TransferRequestListItem } from './transferRequestService';
import { apiClient } from '../../../shared/services/apiClient';

const disposalApi = apiClient;

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
  /** Danh sách yêu cầu thanh lý (cùng DTO với điều chuyển). */
  async getList(): Promise<TransferRequestListItem[]> {
    const response = await disposalApi.get<TransferRequestListItem[]>('/api/Assets/Requests/disposal');
    return response.data;
  },

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

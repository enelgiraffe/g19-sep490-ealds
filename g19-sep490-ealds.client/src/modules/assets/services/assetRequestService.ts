import { apiClient } from '../../../shared/services/apiClient';

const api = apiClient;

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
    damageCondition: string;
    status: number;
    /** Ngày bắt đầu sửa chữa (RepairTask.RepairDate). */
    repairDate?: string | null;
    supplierId?: number | null;
    supplierName?: string | null;
    assetInstanceId?: number;
    instanceCode?: string | null;
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

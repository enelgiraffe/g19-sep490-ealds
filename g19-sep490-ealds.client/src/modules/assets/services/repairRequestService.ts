import { apiClient } from '../../../shared/services/apiClient';

const repairApi = apiClient;

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
  supplierId?: number | null;
  newSupplier?: { code: string; name: string } | null;
}

/** Khớp backend TransferRequestListItemDTO (một phần trường dùng cho sửa chữa). */
export interface RepairRequestListItem {
  recordId: number;
  assetRequestId: number;
  code: string;
  transferDate: string;
  assetCode: string;
  assetName: string;
  assetInstanceId?: number | null;
  instanceCode?: string | null;
  fromDepartment: string;
  status: number;
  statusName: string;
  /** Sửa chữa: tình trạng hỏng hóc (RepairTask). */
  damageCondition?: string | null;
  requestDescription?: string | null;
  fromDepartmentId: number;
  createdBy: number;
}

export interface DamagedInstancePendingItem {
  assetInstanceId: number;
  assetId: number;
  instanceCode: string;
  assetCode: string;
  assetName: string;
  damageNote?: string | null;
  fromDepartment: string;
  fromDepartmentId: number;
  location: string;
}

export interface CreateRepairRequestPayload {
  assetInstanceId: number;
  createdBy: number;
  damageCondition: string;
  repairKind: string;
  estimatedCost?: number;
  requestTypeId?: number;
  title?: string | null;
  damageDate?: string | null;
}

export const repairRequestService = {
  async list(): Promise<RepairRequestListItem[]> {
    const response = await repairApi.get<RepairRequestListItem[]>('/api/Assets/Requests/repair');
    return response.data;
  },

  async listDamagedPending(): Promise<DamagedInstancePendingItem[]> {
    const response = await repairApi.get<DamagedInstancePendingItem[]>(
      '/api/Assets/Requests/repair/damaged-pending'
    );
    return response.data;
  },

  async create(
    payload: CreateRepairRequestPayload
  ): Promise<{ assetRequestId: number; taskId: number }> {
    const body = {
      assetInstanceId: payload.assetInstanceId,
      createdBy: payload.createdBy,
      damageCondition: payload.damageCondition,
      repairKind: payload.repairKind,
      estimatedCost: payload.estimatedCost ?? 0,
      requestTypeId: payload.requestTypeId ?? 4,
      title: payload.title ?? null,
      damageDate: payload.damageDate ?? null,
      supplierId: null as number | null,
      description: null as string | null,
    };
    const response = await repairApi.post<{ assetRequestId: number; taskId: number }>(
      '/api/Assets/Requests/repair',
      body
    );
    return response.data;
  },

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
  newSupplier?: { code: string; name: string } | null;
  attachmentDocumentIds?: number[] | null;
  attachmentUrls?: string[] | null;
  /** Bảo hành gắn biên bản sửa chữa; không cập nhật bảo hành tài sản. */
  repairWarrantyStartDate?: string | null;
  repairWarrantyEndDate?: string | null;
  repairWarrantyPeriodValue?: number | null;
  repairWarrantyPeriodUnit?: string | null;
  repairWarrantyConditions?: string | null;
  repairWarrantyNote?: string | null;
}

import { apiClient } from '../../../shared/services/apiClient';

const purchaseApi = apiClient;

/** Backend Status: -1=Nháp, 0=Đã gửi (kế toán), 1=Chờ duyệt (giám đốc), 2=Duyệt, 3=Từ chối, 4=Chờ ngân sách, 5=Đã ghi tăng */
export interface PurchaseOrderListItem {
  assetRequestId: number;
  assetId?: number | null;
  title: string;
  description: string | null;
  proposedData: string | null;
  status: number;
  createDate: string;
  userId: number;
  createdBy: number;
  creatorName: string | null;
  creatorDepartmentName?: string | null;
  assetCode?: string | null;
  assetName?: string | null;
  accountantComment?: string | null;
  directorComment?: string | null;
}

export interface PurchaseOrderApprovalItem {
  approvalId: number;
  decisionDate: string;
  comment: string | null;
  roleCode: string | null;
}

export interface PurchaseOrderDetail extends PurchaseOrderListItem {
  approvals?: PurchaseOrderApprovalItem[];
}

export interface PurchaseOrderLineItem {
  lineId: number;
  lineIndex: number;
  itemName: string | null;
  quantity: number;
  unit: string | null;
  modelCode: string | null;
  estimatedPrice: string | null;
  assetId: number | null;
  assetCode: string | null;
  assetName: string | null;
  capitalizedAt: string | null;
}

export interface CreatePurchaseOrderPayload {
  userId: number;
  assetId?: number | null;
  title: string;
  description?: string | null;
  proposedData?: string | null;
  createdBy: number;
  /** -1: Draft, 0: Submitted/Pending approval (default) */
  status?: number | null;
}

export const purchaseOrderService = {
  async getList(requestTypeId?: number): Promise<PurchaseOrderListItem[]> {
    const params = requestTypeId != null ? { requestTypeId } : {};
    const response = await purchaseApi.get<PurchaseOrderListItem[]>(
      '/api/Assets/Requests/purchase',
      { params }
    );
    return response.data;
  },

  async getById(id: number): Promise<PurchaseOrderDetail> {
    const response = await purchaseApi.get<PurchaseOrderDetail>(
      `/api/Assets/Requests/purchase/${id}`
    );
    return response.data;
  },

  async getPurchaseLines(id: number): Promise<PurchaseOrderLineItem[]> {
    const response = await purchaseApi.get<PurchaseOrderLineItem[]>(
      `/api/Assets/Requests/purchase/${id}/lines`
    );
    return response.data;
  },

  async create(payload: CreatePurchaseOrderPayload): Promise<{ assetRequestId: number }> {
    const response = await purchaseApi.post<{ assetRequestId: number }>(
      '/api/Assets/Requests/purchase',
      payload
    );
    return response.data;
  },

  async update(id: number, payload: CreatePurchaseOrderPayload): Promise<{ assetRequestId: number }> {
    const response = await purchaseApi.put<{ assetRequestId: number }>(
      `/api/Assets/Requests/purchase/${id}`,
      payload
    );
    return response.data;
  },

  async approveAsAccountant(
    id: number,
    payload: { approvedBy: number; comment?: string | null },
  ): Promise<{ assetRequestId: number; status: number }> {
    const response = await purchaseApi.post<{ assetRequestId: number; status: number }>(
      `/api/Assets/Requests/accountant/${id}/approve`,
      {
        approvedBy: payload.approvedBy,
        comment: payload.comment ?? null,
      },
    );
    return response.data;
  },

  async rejectAsAccountant(
    id: number,
    payload: { approvedBy: number; comment?: string | null },
  ): Promise<{ assetRequestId: number; status: number }> {
    const response = await purchaseApi.post<{ assetRequestId: number; status: number }>(
      `/api/Assets/Requests/accountant/${id}/reject`,
      {
        approvedBy: payload.approvedBy,
        comment: payload.comment ?? null,
      },
    );
    return response.data;
  },

  async revertToDraft(
    id: number,
    userId: number,
  ): Promise<{ assetRequestId: number; status: number }> {
    const response = await purchaseApi.post<{ assetRequestId: number; status: number }>(
      `/api/Assets/Requests/purchase/${id}/revert-to-draft`,
      { userId },
    );
    return response.data;
  },

  async deleteDraft(id: number, userId: number): Promise<void> {
    await purchaseApi.delete(`/api/Assets/Requests/purchase/${id}`, {
      data: { userId },
    });
  },
};

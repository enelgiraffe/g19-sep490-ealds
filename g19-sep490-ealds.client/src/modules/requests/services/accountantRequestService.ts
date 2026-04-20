import { apiClient } from '../../../shared/services/apiClient';

const accountantApi = apiClient;

// These IDs must match App:PurchaseRequestTypeId and App:TransferRequestTypeId on the backend
export const PURCHASE_REQUEST_TYPE_ID = 1;
export const LIQUIDATION_REQUEST_TYPE_ID = 5;
/** Cấp phát — App:AllocationRequestTypeId */
export const ALLOCATION_ACCOUNTANT_REQUEST_TYPE_ID = 6;
/** Thu hồi — App:HandoverRequestTypeId */
export const HANDOVER_ACCOUNTANT_REQUEST_TYPE_ID = 7;

export interface AccountantRequestListItem {
  assetRequestId: number;
  title: string;
  status: number;
  requestTypeId: number;
  userId: number;
  createDate: string;
  proposedData?: string | null;
  allocationTargetDepartmentId?: number | null;
  targetDepartmentName?: string | null;
  assetAllocationOrderId?: number | null;
}

interface ApprovalActionPayload {
  approvedBy: number;
  comment?: string | null;
}

interface AccountantRequestListResponse {
  items: AccountantRequestListItem[];
  total: number;
  page: number;
  pageSize: number;
  totalPages: number;
}

export const accountantRequestService = {
  async getPurchaseRequests(): Promise<AccountantRequestListItem[]> {
    const response = await accountantApi.get<AccountantRequestListResponse>(
      '/api/Assets/Requests/accountant/view',
      {
        params: {
          requestTypeIds: PURCHASE_REQUEST_TYPE_ID,
          page: 1,
          pageSize: 200,
        },
      },
    );
    return response.data.items;
  },

  async getLiquidationRequests(): Promise<AccountantRequestListItem[]> {
    const response = await accountantApi.get<AccountantRequestListResponse>(
      '/api/Assets/Requests/accountant/view',
      {
        params: {
          requestTypeIds: LIQUIDATION_REQUEST_TYPE_ID,
          page: 1,
          pageSize: 200,
        },
      },
    );
    return response.data.items;
  },

  async getAllocationRequests(): Promise<AccountantRequestListItem[]> {
    const response = await accountantApi.get<AccountantRequestListResponse>(
      '/api/Assets/Requests/accountant/view',
      {
        params: {
          requestTypeIds: ALLOCATION_ACCOUNTANT_REQUEST_TYPE_ID,
          page: 1,
          pageSize: 200,
        },
      },
    );
    return response.data.items;
  },

  async getHandoverRequests(): Promise<AccountantRequestListItem[]> {
    const response = await accountantApi.get<AccountantRequestListResponse>(
      '/api/Assets/Requests/accountant/view',
      {
        params: {
          requestTypeIds: HANDOVER_ACCOUNTANT_REQUEST_TYPE_ID,
          page: 1,
          pageSize: 200,
        },
      },
    );
    return response.data.items;
  },

  async approve(
    id: number,
    payload: ApprovalActionPayload,
  ): Promise<{ assetRequestId: number; status: number }> {
    const response = await accountantApi.post<{ assetRequestId: number; status: number }>(
      `/api/Assets/Requests/accountant/${id}/approve`,
      {
        approvedBy: payload.approvedBy,
        comment: payload.comment ?? null,
      },
    );
    return response.data;
  },

  async reject(
    id: number,
    payload: ApprovalActionPayload,
  ): Promise<{ assetRequestId: number; status: number }> {
    const response = await accountantApi.post<{ assetRequestId: number; status: number }>(
      `/api/Assets/Requests/accountant/${id}/reject`,
      {
        approvedBy: payload.approvedBy,
        comment: payload.comment ?? null,
      },
    );
    return response.data;
  },
};


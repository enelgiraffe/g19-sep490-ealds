import { isAxiosError } from 'axios';
import {
  allocationRequestApiErrorMessage,
  type AllocationLineInput,
  type AllocationOrderDetail,
  type AllocationOrderSummaryRow,
  type AllocationRequestListItem,
} from './allocationRequestService';
import { apiClient } from '../../../shared/services/apiClient';

const api = apiClient;

export const HANDOVER_REQUEST_TYPE_ID = 7;

export const handoverRequestService = {
  async list(): Promise<AllocationRequestListItem[]> {
    const res = await api.get<AllocationRequestListItem[]>('/api/Assets/Requests/handover');
    return res.data;
  },

  async listOrdersSummary(): Promise<AllocationOrderSummaryRow[]> {
    const res = await api.get<AllocationOrderSummaryRow[]>('/api/Assets/Requests/handover/orders-summary');
    return Array.isArray(res.data) ? res.data : [];
  },

  async create(payload: { title: string; lines: AllocationLineInput[] }): Promise<{ assetRequestId: number }> {
    try {
      const res = await api.post<{ assetRequestId: number }>('/api/Assets/Requests/handover', payload);
      return res.data;
    } catch (e) {
      if (isAxiosError(e) && e.response?.data) {
        const msg = allocationRequestApiErrorMessage(e.response.data);
        if (msg) throw new Error(msg);
      }
      throw e;
    }
  },

  async departmentAssigned(assetId: number): Promise<number> {
    const res = await api.get<number>('/api/Assets/Requests/handover/department-assigned', {
      params: { assetId },
    });
    return res.data;
  },

  async getOrder(orderId: number): Promise<AllocationOrderDetail> {
    const res = await api.get<AllocationOrderDetail>(`/api/Assets/Requests/handover/orders/${orderId}`);
    return res.data;
  },

  async confirmOrder(orderId: number): Promise<{ assetAllocationOrderId: number; status: string }> {
    const res = await api.post<{ assetAllocationOrderId: number; status: string }>(
      `/api/Assets/Requests/handover/orders/${orderId}/confirm`,
    );
    return res.data;
  },
};

import { apiClient } from '../../../shared/services/apiClient';

const api = apiClient;

export type BudgetAllocationStatus = 'pending' | 'allocated' | 'recalled';

export interface BudgetAllocationListItem {
  id: number;
  assetInstanceId: number;
  name: string;
  category: string;
  departmentId: number;
  departmentName?: string | null;
  date: string;
  status: BudgetAllocationStatus;
  submittedBy: string;
  note?: string | null;
}

export interface CreateBudgetAllocationPayload {
  departmentId: number;
  assetCategoryId: number;
  assetInstanceId: number;
  transactionDate?: string | null;
  note?: string | null;
  isRecall: boolean;
}

export interface AssetInstanceOption {
  assetInstanceId: number;
  label: string;
}

export const budgetAllocationService = {
  async list(params?: { departmentId?: number; status?: string }): Promise<BudgetAllocationListItem[]> {
    const response = await api.get<BudgetAllocationListItem[]>('/api/BudgetAllocations', { params });
    return response.data;
  },

  async getAssetInstanceOptions(params: {
    categoryId: number;
    departmentId: number;
    mode: 'assign' | 'recall';
    search?: string;
  }): Promise<AssetInstanceOption[]> {
    const response = await api.get<AssetInstanceOption[]>('/api/BudgetAllocations/asset-instance-options', {
      params: {
        categoryId: params.categoryId,
        departmentId: params.departmentId,
        mode: params.mode,
        search: params.search?.trim() || undefined,
      },
    });
    return response.data;
  },

  async create(payload: CreateBudgetAllocationPayload): Promise<BudgetAllocationListItem> {
    const response = await api.post<BudgetAllocationListItem>('/api/BudgetAllocations', payload);
    return response.data;
  },

  async remove(id: number): Promise<void> {
    await api.delete(`/api/BudgetAllocations/${id}`);
  },
};

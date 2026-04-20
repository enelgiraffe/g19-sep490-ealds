import { apiClient } from '../../../shared/services/apiClient';

const supplierApi = apiClient;

export interface SupplierItem {
  supplierId: number;
  code: string;
  name: string;
  taxCode?: string | null;
  address?: string | null;
  phone?: string | null;
  email?: string | null;
  status: number;
  createDate: string;
}

export interface CreateSupplierPayload {
  code: string;
  name: string;
  taxCode?: string;
  address?: string;
  phone?: string;
  email?: string;
  status: number;
}

export interface UpdateSupplierPayload extends CreateSupplierPayload {}

export const supplierService = {
  async getAll(keyword?: string): Promise<SupplierItem[]> {
    const response = await supplierApi.get<SupplierItem[]>('/api/suppliers', {
      params: keyword ? { keyword } : undefined,
    });
    return response.data;
  },

  async create(payload: CreateSupplierPayload): Promise<SupplierItem> {
    const response = await supplierApi.post<SupplierItem>('/api/suppliers', payload);
    return response.data;
  },

  async update(id: number, payload: UpdateSupplierPayload): Promise<void> {
    await supplierApi.put<void>(`/api/suppliers/${id}`, payload);
  },

  async delete(id: number): Promise<void> {
    await supplierApi.delete(`/api/suppliers/${id}`);
  },
};


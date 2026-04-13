import axios from 'axios';

const API_BASE_URL = import.meta.env.VITE_API_BASE_URL || '';

const siApi = axios.create({
  baseURL: API_BASE_URL,
  headers: { 'Content-Type': 'application/json' },
});

siApi.interceptors.request.use((config) => {
  const token = localStorage.getItem('accessToken');
  if (token) {
    config.headers = config.headers ?? {};
    config.headers.Authorization = `Bearer ${token}`;
  }
  return config;
});

/** Matches SupplierInvoicesController */
export const SUPPLIER_INVOICE_STATUS = {
  active: 0,
  cancelled: 1,
} as const;

export interface SupplierInvoiceListItem {
  supplierInvoiceId: number;
  invoiceNumber: string;
  supplierId: number;
  supplierName: string | null;
  totalAmount: number;
  invoiceDate: string;
  status: number;
  procurementId: number;
  goodsReceiptId: number | null;
}

export interface SupplierInvoiceListResponse {
  items: SupplierInvoiceListItem[];
  total: number;
  page: number;
  pageSize: number;
  totalPages: number;
}

export interface SupplierInvoiceDetailLine {
  supplierInvoiceLineId: number;
  procurementLineId: number | null;
  goodsReceiptLineId: number | null;
  chargeDescription: string | null;
  assetId: number | null;
  assetCode: string | null;
  assetName: string | null;
  quantity: number;
  unitPrice: number;
  lineTotal: number;
}

export interface SupplierInvoiceDetail {
  supplierInvoiceId: number;
  invoiceNumber: string;
  supplierId: number;
  supplierName: string | null;
  invoiceDate: string;
  currency: string;
  totalAmount: number;
  note: string | null;
  status: number;
  procurementId: number;
  goodsReceiptId: number | null;
  createdDate: string;
  lines: SupplierInvoiceDetailLine[];
}

export interface SupplierInvoiceCreateLine {
  procurementLineId: number | null;
  goodsReceiptLineId?: number | null;
  chargeDescription?: string | null;
  quantity: number;
  unitPrice: number;
}

export interface SupplierInvoiceCreateBody {
  procurementId: number;
  goodsReceiptId?: number | null;
  supplierId: number;
  invoiceNumber: string;
  invoiceDate: string;
  note?: string | null;
  lines: SupplierInvoiceCreateLine[];
}

export const supplierInvoiceService = {
  async getList(params: {
    invoiceNumber?: string;
    supplierId?: number;
    dateFrom?: string;
    dateTo?: string;
    page?: number;
    pageSize?: number;
  }): Promise<SupplierInvoiceListResponse> {
    const response = await siApi.get<SupplierInvoiceListResponse>('/api/supplier-invoices', { params });
    return response.data;
  },

  async getById(id: number): Promise<SupplierInvoiceDetail> {
    const response = await siApi.get<SupplierInvoiceDetail>(`/api/supplier-invoices/${id}`);
    return response.data;
  },

  async create(body: SupplierInvoiceCreateBody): Promise<{ supplierInvoiceId: number }> {
    const response = await siApi.post<{ supplierInvoiceId: number }>('/api/supplier-invoices', body);
    return response.data;
  },

  async cancel(id: number): Promise<void> {
    await siApi.post(`/api/supplier-invoices/${id}/cancel`);
  },
};

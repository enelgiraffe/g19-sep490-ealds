import axios from 'axios';

const API_BASE_URL = import.meta.env.VITE_API_BASE_URL || '';

const grApi = axios.create({
  baseURL: API_BASE_URL,
  headers: { 'Content-Type': 'application/json' },
});

grApi.interceptors.request.use((config) => {
  const token = localStorage.getItem('accessToken');
  if (token) {
    config.headers = config.headers ?? {};
    config.headers.Authorization = `Bearer ${token}`;
  }
  return config;
});

export const GR_STATUS = {
  posted: 1,
} as const;

export interface GoodsReceiptListItem {
  goodsReceiptId: number;
  procurementId: number;
  contractNo: string | null;
  supplierName: string | null;
  totalReceivedQuantity: number;
  status: number;
  createdDate: string;
}

export interface GoodsReceiptListResponse {
  items: GoodsReceiptListItem[];
  total: number;
  page: number;
  pageSize: number;
  totalPages: number;
}

export interface GoodsReceiptInstance {
  assetInstanceId: number;
  instanceCode: string;
  serialNumber: string | null;
}

export interface GoodsReceiptDetailLine {
  goodsReceiptLineId: number;
  procurementLineId: number;
  assetId: number | null;
  assetCode: string | null;
  assetName: string | null;
  orderedQuantity: number;
  quantityReceivedOnThisReceipt: number;
  cumulativeReceivedQuantity: number;
  openQuantity: number;
  instances: GoodsReceiptInstance[];
}

export interface GoodsReceiptDetail {
  goodsReceiptId: number;
  procurementId: number;
  contractNo: string | null;
  supplierName: string | null;
  createdDate: string;
  status: number;
  note: string | null;
  lines: GoodsReceiptDetailLine[];
}

export interface GoodsReceiptCreateLine {
  procurementLineId: number;
  quantityReceived: number;
  assetId?: number | null;
  instanceSerialNumbers?: (string | null)[] | null;
}

export interface GoodsReceiptCreateBody {
  procurementId: number;
  warehouseId: number;
  note?: string | null;
  lines: GoodsReceiptCreateLine[];
}

export const goodsReceiptService = {
  async getList(params: {
    goodsReceiptId?: number;
    procurementId?: number;
    supplierId?: number;
    dateFrom?: string;
    dateTo?: string;
    page?: number;
    pageSize?: number;
  }): Promise<GoodsReceiptListResponse> {
    const response = await grApi.get<GoodsReceiptListResponse>('/api/goods-receipts', { params });
    return response.data;
  },

  async getById(id: number): Promise<GoodsReceiptDetail> {
    const response = await grApi.get<GoodsReceiptDetail>(`/api/goods-receipts/${id}`);
    return response.data;
  },

  async create(body: GoodsReceiptCreateBody): Promise<{ goodsReceiptId: number }> {
    const response = await grApi.post<{ goodsReceiptId: number }>('/api/goods-receipts', body);
    return response.data;
  },
};

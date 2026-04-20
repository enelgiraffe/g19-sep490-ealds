import { apiClient } from '../../../shared/services/apiClient';

const grApi = apiClient;

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
  /** Present when API returns attachment list (may be empty). */
  attachments?: { documentId: number; fileUrl: string }[];
  lines: GoodsReceiptDetailLine[];
}

export interface GoodsReceiptCreateLine {
  procurementLineId: number;
  quantityReceived: number;
  assetId?: number | null;
  instanceSerialNumbers?: (string | null)[] | null;
  instanceCodes?: (string | null)[] | null;
}

export interface GoodsReceiptCreateBody {
  procurementId: number;
  warehouseId: number;
  postingDate?: string;
  note?: string | null;
  attachmentFileUrls?: string[];
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

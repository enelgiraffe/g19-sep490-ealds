import axios from 'axios';

const API_BASE_URL = import.meta.env.VITE_API_BASE_URL || '';

const capitalizationApi = axios.create({
  baseURL: API_BASE_URL,
  headers: {
    'Content-Type': 'application/json',
  },
});

capitalizationApi.interceptors.request.use((config) => {
  const token = localStorage.getItem('accessToken');
  if (token) {
    config.headers = config.headers ?? {};
    config.headers.Authorization = `Bearer ${token}`;
  }
  return config;
});

export interface AssetCapitalizationPayload {
  assetId: number;
  assetRequestId?: number;
  note?: string | null;
  documents?: { name?: string; url: string }[] | null;
}

export interface CapitalizePurchaseRequestPayload {
  assetRequestId: number;
  note?: string | null;
  documents?: { name?: string; url: string }[] | null;
  code: string;
  name: string;
  assetTypeId: number;
  purchaseDate: string; // yyyy-mm-dd
  originalPrice: number;
  currentValue: number;
  unit: string;
  quantity: number;
  warehouseId: number;
}

export const assetCapitalizationService = {
  async changeStatus(payload: AssetCapitalizationPayload) {
    const response = await capitalizationApi.put('/api/AssetCapitalization/change-status', {
      assetId: payload.assetId,
      assetRequestId: payload.assetRequestId ?? null,
      note: payload.note ?? null,
      documents: payload.documents ?? null,
    });
    return response.data;
  },

  async capitalizePurchaseRequest(payload: CapitalizePurchaseRequestPayload) {
    const response = await capitalizationApi.put('/api/AssetCapitalization/capitalize-purchase-request', {
      assetRequestId: payload.assetRequestId,
      note: payload.note ?? null,
      documents: payload.documents ?? null,
      code: payload.code,
      name: payload.name,
      assetTypeId: payload.assetTypeId,
      purchaseDate: payload.purchaseDate,
      originalPrice: payload.originalPrice,
      currentValue: payload.currentValue,
      unit: payload.unit,
      quantity: payload.quantity,
      warehouseId: payload.warehouseId,
    });
    return response.data;
  },
};


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
  note?: string | null;
}

export const assetCapitalizationService = {
  async changeStatus(payload: AssetCapitalizationPayload) {
    const response = await capitalizationApi.put('/api/AssetCapitalization/change-status', {
      assetId: payload.assetId,
      note: payload.note ?? null,
    });
    return response.data;
  },
};


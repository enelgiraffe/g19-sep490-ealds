import axios from 'axios';

const API_BASE_URL = import.meta.env.VITE_API_BASE_URL || '';

const api = axios.create({
  baseURL: API_BASE_URL,
  headers: { 'Content-Type': 'application/json' },
});

api.interceptors.request.use((config) => {
  const token = localStorage.getItem('accessToken');
  if (token) {
    config.headers = config.headers ?? {};
    config.headers.Authorization = `Bearer ${token}`;
  }
  return config;
});

export interface AdminAssetPickerItem {
  assetId: number;
  code: string;
  name: string;
}

/** Danh sách tài sản cho dropdown (admin). */
export const adminAssetsService = {
  async listForPicker(keyword?: string): Promise<AdminAssetPickerItem[]> {
    const response = await api.get<AdminAssetPickerItem[]>('/api/assets', {
      params: keyword?.trim() ? { keyword: keyword.trim() } : undefined,
    });
    return response.data;
  },
};

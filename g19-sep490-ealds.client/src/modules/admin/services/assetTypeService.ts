import axios from 'axios';

const API_BASE_URL = import.meta.env.VITE_API_BASE_URL || '';

const assetTypeApi = axios.create({
  baseURL: API_BASE_URL,
  headers: {
    'Content-Type': 'application/json',
  },
});

assetTypeApi.interceptors.request.use((config) => {
  const token = localStorage.getItem('accessToken');
  if (token) {
    config.headers = config.headers ?? {};
    config.headers.Authorization = `Bearer ${token}`;
  }
  return config;
});

export interface AssetTypeListItem {
  assetTypeId: number;
  name: string;
  categoryId: number;
  categoryName: string;
  assetCount: number;
}

export const assetTypeService = {
  async getAll(keyword?: string, categoryId?: number): Promise<AssetTypeListItem[]> {
    const response = await assetTypeApi.get<AssetTypeListItem[]>('/api/assettypes', {
      params: {
        keyword: keyword || undefined,
        categoryId: categoryId ?? undefined,
      },
    });
    return response.data;
  },
};


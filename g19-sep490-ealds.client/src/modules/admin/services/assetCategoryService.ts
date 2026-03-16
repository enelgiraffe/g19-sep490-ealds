import axios from 'axios';

const API_BASE_URL = import.meta.env.VITE_API_BASE_URL || '';

const assetCategoryApi = axios.create({
  baseURL: API_BASE_URL,
  headers: {
    'Content-Type': 'application/json',
  },
});

assetCategoryApi.interceptors.request.use((config) => {
  const token = localStorage.getItem('accessToken');
  if (token) {
    config.headers = config.headers ?? {};
    config.headers.Authorization = `Bearer ${token}`;
  }
  return config;
});

export interface AssetCategoryItem {
  categoryId: number;
  name: string;
  assetTypeCount: number;
}

export interface CreateAssetCategoryPayload {
  name: string;
}

export interface UpdateAssetCategoryPayload {
  name: string;
}

export const assetCategoryService = {
  async getAll(keyword?: string): Promise<AssetCategoryItem[]> {
    const response = await assetCategoryApi.get<AssetCategoryItem[]>('/api/assetcategories', {
      params: keyword ? { keyword } : undefined,
    });
    return response.data;
  },

  async create(payload: CreateAssetCategoryPayload): Promise<AssetCategoryItem> {
    const response = await assetCategoryApi.post<AssetCategoryItem>('/api/assetcategories', payload);
    return response.data;
  },

  async update(id: number, payload: UpdateAssetCategoryPayload): Promise<AssetCategoryItem> {
    const response = await assetCategoryApi.put<AssetCategoryItem>(`/api/assetcategories/${id}`, payload);
    return response.data;
  },

  async delete(id: number): Promise<void> {
    await assetCategoryApi.delete(`/api/assetcategories/${id}`);
  },
};


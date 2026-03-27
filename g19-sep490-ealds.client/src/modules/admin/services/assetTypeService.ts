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

export interface CreateAssetTypePayload {
  categoryId: number;
  name: string;
}

export interface UpdateAssetTypePayload {
  categoryId: number;
  name: string;
}

export const assetTypeService = {
  async getAll(keyword?: string, categoryId?: number): Promise<AssetTypeListItem[]> {
    const response = await assetTypeApi.get<AssetTypeListItem[]>('/api/AssetTypes', {
      params: {
        keyword: keyword || undefined,
        categoryId: categoryId ?? undefined,
      },
    });
    return response.data;
  },

  async create(payload: CreateAssetTypePayload): Promise<AssetTypeListItem> {
    const response = await assetTypeApi.post<AssetTypeListItem>('/api/AssetTypes', {
      categoryId: payload.categoryId,
      name: payload.name,
    });
    return response.data;
  },

  async update(assetTypeId: number, payload: UpdateAssetTypePayload): Promise<AssetTypeListItem> {
    const response = await assetTypeApi.put<AssetTypeListItem>(`/api/AssetTypes/${assetTypeId}`, {
      categoryId: payload.categoryId,
      name: payload.name,
    });
    return response.data;
  },

  async delete(assetTypeId: number): Promise<void> {
    await assetTypeApi.delete(`/api/AssetTypes/${assetTypeId}`);
  },
};


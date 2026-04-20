import { apiClient } from '../../../shared/services/apiClient';

const api = apiClient;

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

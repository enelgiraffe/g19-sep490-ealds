import { apiClient } from '../../../shared/services/apiClient';

const fileApi = apiClient;

export const purchaseOrderFileService = {
  async upload(file: File): Promise<{ fileName: string; url: string }> {
    const formData = new FormData();
    formData.append('file', file);
    const response = await fileApi.post<{ fileName: string; url: string }>(
      '/api/files/upload',
      formData,
      { headers: { 'Content-Type': 'multipart/form-data' } },
    );
    return response.data;
  },
};


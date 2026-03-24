import axios from 'axios';

const API_BASE_URL = import.meta.env.VITE_API_BASE_URL || '';

const fileApi = axios.create({
  baseURL: API_BASE_URL,
});

fileApi.interceptors.request.use((config) => {
  const token = localStorage.getItem('accessToken');
  if (token) {
    config.headers = config.headers ?? {};
    config.headers.Authorization = `Bearer ${token}`;
  }
  return config;
});

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


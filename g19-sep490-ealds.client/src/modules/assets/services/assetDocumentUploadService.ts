import axios from 'axios';

const API_BASE_URL = import.meta.env.VITE_API_BASE_URL || '';

const uploadApi = axios.create({
  baseURL: API_BASE_URL,
});

uploadApi.interceptors.request.use((config) => {
  const token = localStorage.getItem('accessToken');
  if (token) {
    config.headers = config.headers ?? {};
    config.headers.Authorization = `Bearer ${token}`;
  }
  return config;
});

/** POST /api/files/upload — same endpoint as purchase orders; stores file and returns a public URL. */
export async function uploadAssetFile(
  file: File
): Promise<{ fileName: string; url: string }> {
  const formData = new FormData();
  formData.append('file', file);
  const response = await uploadApi.post<{ fileName: string; url: string }>(
    '/api/files/upload',
    formData,
    { headers: { 'Content-Type': 'multipart/form-data' } }
  );
  return response.data;
}

import { apiClient } from '../../../shared/services/apiClient';

const uploadApi = apiClient;

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

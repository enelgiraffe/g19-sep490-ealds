import { apiClient } from '../../../shared/services/apiClient';
import {
  ALLOWED_DOCUMENT_FILE_ACCEPT,
  isAllowedDocumentFile,
} from '../../../shared/utils/allowedDocumentFiles';

const uploadApi = apiClient;

export const ASSET_DOCUMENT_FILE_ACCEPT = ALLOWED_DOCUMENT_FILE_ACCEPT;
export const isAllowedAssetDocumentFile = isAllowedDocumentFile;

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

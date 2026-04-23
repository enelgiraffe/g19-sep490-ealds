import { apiClient } from '../../../shared/services/apiClient';

const uploadApi = apiClient;

/** HTML `accept` value for asset catalog documents (images and PDF only). */
export const ASSET_DOCUMENT_FILE_ACCEPT = 'image/*,application/pdf';

const imageExtPattern = /\.(jpe?g|png|gif|webp|bmp|svg|ico|tiff?|heic|heif)$/i;

/** Client-side check; `accept` is a hint only. */
export function isAllowedAssetDocumentFile(file: File): boolean {
  if (file.type === 'application/pdf' || file.type.startsWith('image/')) return true;
  if (file.type) return false;
  const n = file.name.toLowerCase();
  if (n.endsWith('.pdf')) return true;
  return imageExtPattern.test(n);
}

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

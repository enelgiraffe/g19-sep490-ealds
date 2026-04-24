const DOCX_MIME = 'application/vnd.openxmlformats-officedocument.wordprocessingml.document';

/** HTML `accept` for document uploads: images, PDF, Word (.docx). */
export const ALLOWED_DOCUMENT_FILE_ACCEPT = `image/*,application/pdf,${DOCX_MIME},.docx`;

const imageExtPattern = /\.(jpe?g|png|gif|webp|bmp|svg|ico|tiff?|heic|heif)$/i;

/** Client-side check; `accept` is a hint only. */
export function isAllowedDocumentFile(file: File): boolean {
  const t = file.type;
  const n = file.name.toLowerCase();

  if (t === 'application/pdf' || t === DOCX_MIME || t.startsWith('image/')) {
    return true;
  }
  if (t === 'application/zip' && n.endsWith('.docx')) {
    return true;
  }
  if (t && t !== 'application/octet-stream' && t !== 'application/zip') {
    return false;
  }
  if (n.endsWith('.pdf') || n.endsWith('.docx')) return true;
  return imageExtPattern.test(n);
}

export const DISALLOWED_DOCUMENT_TYPE_MESSAGE = 'Chỉ chấp nhận file ảnh, PDF hoặc Word (.docx).';

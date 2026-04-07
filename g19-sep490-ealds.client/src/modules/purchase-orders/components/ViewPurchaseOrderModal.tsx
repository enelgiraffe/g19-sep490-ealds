import { useEffect, useMemo, useState } from 'react';
import { message } from 'antd';
import {
  purchaseOrderService,
  type PurchaseOrderDetail,
} from '../services/purchaseOrderService';
import './ViewPurchaseOrderModal.css';

const STATUS_MAP: Record<number, { label: string; color: string }> = {
  [-1]: { label: 'Nháp', color: 'default' },
  0: { label: 'Đã gửi', color: 'processing' },
  1: { label: 'Chờ duyệt', color: 'warning' },
  2: { label: 'Duyệt', color: 'success' },
  3: { label: 'Từ chối', color: 'error' },
  4: { label: 'Chờ ngân sách', color: 'warning' },
  5: { label: 'Đã ghi tăng', color: 'success' },
};

function formatDateOnly(value: string): string {
  try {
    const date = new Date(value);
    if (Number.isNaN(date.getTime())) return value;
    return date.toLocaleDateString('vi-VN');
  } catch {
    return value;
  }
}

interface ViewPurchaseOrderModalProps {
  open: boolean;
  onClose: () => void;
  data: PurchaseOrderDetail | null;
  currentUserId?: number | null;
  currentUserRole?: string | null;
  onActionCompleted?: (assetRequestId: number, nextStatus?: number) => void | Promise<void>;
}

export function ViewPurchaseOrderModal({
  open,
  onClose,
  data,
  currentUserId,
  currentUserRole,
  onActionCompleted,
}: ViewPurchaseOrderModalProps) {
  const [isApproveOpen, setIsApproveOpen] = useState(false);
  const [decision, setDecision] = useState<'approved' | 'rejected'>('approved');
  const [comment, setComment] = useState('');
  const [submitting, setSubmitting] = useState(false);

  const normalizedRole = String(currentUserRole ?? '').toUpperCase();
  const isAccountantRole = normalizedRole === 'ACCOUNTANT';
  const canAccountantApprove = isAccountantRole && !!currentUserId && data?.status === 0;

  const parsedProposedData = useMemo(() => {
    try {
      if (!data?.proposedData) return null;
      return JSON.parse(data.proposedData) as Record<string, unknown>;
    } catch {
      return null;
    }
  }, [data?.proposedData]);

  const attachmentDocs = useMemo(() => {
    const raw = parsedProposedData && Array.isArray((parsedProposedData as { documents?: unknown }).documents)
      ? (parsedProposedData as { documents: unknown[] }).documents
      : [];
    return raw
      .filter((d): d is Record<string, unknown> => d != null && typeof d === 'object')
      .map((d, idx) => ({
        name: String(d.name ?? `Tài liệu ${idx + 1}`),
        url: String((d as { url?: string; fileUrl?: string }).url ?? (d as { fileUrl?: string }).fileUrl ?? ''),
      }))
      .filter((d) => d.url.length > 0);
  }, [parsedProposedData]);

  useEffect(() => {
    if (!open) return;
    setIsApproveOpen(false);
    setDecision('approved');
    setComment('');
  }, [open, data?.assetRequestId]);

  const handleSubmitApproval = async () => {
    if (!currentUserId || !data) {
      message.error('Không lấy được thông tin người dùng.');
      return;
    }
    setSubmitting(true);
    try {
      if (decision === 'approved') {
        const res = await purchaseOrderService.approveAsAccountant(data.assetRequestId, {
          approvedBy: currentUserId,
          comment: comment.trim() || null,
        });
        message.success('Đã chuyển yêu cầu sang giám đốc (Chờ duyệt).');
        await onActionCompleted?.(data.assetRequestId, res.status);
      } else {
        const res = await purchaseOrderService.rejectAsAccountant(data.assetRequestId, {
          approvedBy: currentUserId,
          comment: comment.trim() || null,
        });
        message.success('Đã trả yêu cầu về Nháp.');
        await onActionCompleted?.(data.assetRequestId, res.status);
      }
      setIsApproveOpen(false);
    } catch (e: unknown) {
      const err = e as { response?: { data?: string } };
      message.error(err?.response?.data ?? 'Thao tác phê duyệt thất bại.');
    } finally {
      setSubmitting(false);
    }
  };

  if (!open || !data) return null;

  const statusConfig = STATUS_MAP[data.status] ?? STATUS_MAP[0];
  let equipment: {
    stt: number;
    name: string;
    quantity: number;
    modelCode?: string;
    unit?: string;
    estimatedPrice?: string;
  }[] = [];
  let totalPrice = '—';
  try {
    if (data.proposedData) {
      const parsed = JSON.parse(data.proposedData) as {
        equipment?: { name?: string; quantity?: number; modelCode?: string; machineCode?: string; unit?: string; estimatedPrice?: string }[];
        totalPrice?: string;
      };
      if (Array.isArray(parsed.equipment)) {
        equipment = parsed.equipment.map((e, i) => ({
          stt: i + 1,
          name: e.name ?? '—',
          quantity: e.quantity ?? 1,
          modelCode: e.modelCode ?? e.machineCode,
          unit: e.unit,
          estimatedPrice: e.estimatedPrice,
        }));
      }
      if (parsed.totalPrice) totalPrice = parsed.totalPrice;
    }
  } catch {
    // keep empty
  }

  const inferredAssetTypeName = (() => {
    const fromProposed =
      parsedProposedData && typeof parsedProposedData === 'object'
        ? String((parsedProposedData as { assetTypeName?: string | null }).assetTypeName ?? '').trim()
        : '';
    if (fromProposed) return fromProposed;
    const descriptionText = String(data.description ?? '');
    const marker = 'Loại tài sản:';
    const idx = descriptionText.indexOf(marker);
    if (idx < 0) return null;
    const line = descriptionText.slice(idx + marker.length).split('\n')[0].trim();
    return line || null;
  })();
  const extractedSupplierName = (() => {
    const descriptionText = String(data.description ?? '');
    const marker = 'Nhà cung cấp đề xuất:';
    const idx = descriptionText.indexOf(marker);
    if (idx < 0) return null;
    const line = descriptionText.slice(idx + marker.length).split('\n')[0].trim();
    return line || null;
  })();
  const extractedPurpose = (() => {
    const descriptionText = String(data.description ?? '');
    const marker = 'Mục đích:';
    const idx = descriptionText.indexOf(marker);
    if (idx < 0) return null;
    const line = descriptionText.slice(idx + marker.length).split('\n')[0].trim();
    return line || null;
  })();
  const extractedNeedDate = (() => {
    const descriptionText = String(data.description ?? '');
    const marker = 'Thời gian cần:';
    const idx = descriptionText.indexOf(marker);
    if (idx < 0) return null;
    const line = descriptionText.slice(idx + marker.length).split('\n')[0].trim();
    return line || null;
  })();
  const displayCreatorName = (() => {
    const raw = String(data.creatorName ?? '').trim();
    if (!raw) return '—';
    if (!raw.includes('@')) return raw;
    const localPart = raw.split('@')[0]?.trim() ?? '';
    if (!localPart) return raw;
    const normalized = localPart.replace(/[._-]+/g, ' ').trim();
    if (!normalized) return raw;
    return normalized
      .split(/\s+/)
      .map((word) => word.charAt(0).toUpperCase() + word.slice(1))
      .join(' ');
  })();
  const requestAssetDisplay = (() => {
    if (data.assetCode || data.assetName) {
      return [data.assetCode, data.assetName].filter(Boolean).join(' - ');
    }
    if (equipment.length > 0) {
      const names = equipment.map((e) => e.name).filter(Boolean);
      if (names.length === 0) return null;
      return names.length === 1 ? names[0] : `${names.length} vật tư (${names[0]}...)`;
    }
    return null;
  })();

  const latestAccountantCommentFromApproval =
    data.approvals
      ?.find((a) => String(a.roleCode ?? '').toUpperCase() === 'ACCOUNTANT')
      ?.comment?.trim() ?? '';
  const latestDirectorCommentFromApproval =
    data.approvals
      ?.find((a) => String(a.roleCode ?? '').toUpperCase() === 'DIRECTOR')
      ?.comment?.trim() ?? '';
  const accountantCommentDisplay = latestAccountantCommentFromApproval || data.accountantComment?.trim() || '—';
  const directorCommentDisplay = latestDirectorCommentFromApproval || data.directorComment?.trim() || '—';

  const statusClassName =
    statusConfig.color === 'success'
      ? 'view-purchase-status-tag view-purchase-status-tag--success'
      : statusConfig.color === 'warning'
        ? 'view-purchase-status-tag view-purchase-status-tag--warning'
        : 'view-purchase-status-tag';

  return (
    <div className="view-purchase-modal-overlay" role="dialog" aria-modal="true">
      <div className="view-purchase-modal">
        <div className="view-purchase-modal__header">
          <div className="view-purchase-modal__header-left">
            <h2 className="view-purchase-modal__title">Chi tiết đơn mua</h2>
            <span className={statusClassName}>{statusConfig.label}</span>
          </div>
        </div>

        <div className="view-purchase-modal__body">
          <div className="view-purchase-modal__content">
            <div className="view-purchase-form">
              <div className="view-purchase-form__row view-purchase-form__row--meta">
                <div className="view-purchase-form__field">
                  <label>Mã yêu cầu</label>
                  <div className="view-purchase-form__value">YC-{data.assetRequestId}</div>
                </div>
                <div className="view-purchase-form__field">
                  <label>Người gửi</label>
                  <div className="view-purchase-form__value">{displayCreatorName}</div>
                </div>
                <div className="view-purchase-form__field">
                  <label>Phòng ban</label>
                  <div className="view-purchase-form__value">{data.creatorDepartmentName ?? '—'}</div>
                </div>
              </div>

              <div className="view-purchase-form__row">
                <div className="view-purchase-form__field">
                  <label>Lý do đề nghị</label>
                  <div className="view-purchase-form__value">{data.title}</div>
                </div>
                <div className="view-purchase-form__field">
                  <label>Thời gian cần vật tư</label>
                  <div className="view-purchase-form__value">
                    {extractedNeedDate ?? formatDateOnly(data.createDate)}
                  </div>
                </div>
              </div>

              <div className="view-purchase-form__row">
                <div className="view-purchase-form__field">
                  <label>Nhà cung cấp đề xuất</label>
                  <div className="view-purchase-form__value">{extractedSupplierName ?? '—'}</div>
                </div>
                <div className="view-purchase-form__field">
                  <label>Loại tài sản</label>
                  <div className="view-purchase-form__value">{inferredAssetTypeName ?? '—'}</div>
                </div>
              </div>

              <div className="view-purchase-form__row">
                <div className="view-purchase-form__field">
                  <label>Tài sản / Vật tư</label>
                  <div className="view-purchase-form__value">{requestAssetDisplay ?? '—'}</div>
                </div>
                <div className="view-purchase-form__field view-purchase-form__field--empty" aria-hidden="true" />
              </div>

              {equipment.length > 0 && (
                <div className="view-purchase-form__section">
                  <h3 className="view-purchase-form__section-title">Danh mục vật tư</h3>
                  <table className="view-purchase-equipment-table">
                    <thead>
                      <tr>
                        <th>STT</th>
                        <th>Tên vật tư</th>
                        <th>Số lượng</th>
                        <th>Mã model</th>
                        <th>Đơn vị tính</th>
                        <th>Đơn giá dự tính</th>
                      </tr>
                    </thead>
                    <tbody>
                      {equipment.map((item) => (
                        <tr key={item.stt}>
                          <td>{item.stt}</td>
                          <td>{item.name}</td>
                          <td>{item.quantity}</td>
                          <td>{item.modelCode ?? '—'}</td>
                          <td>{item.unit ?? '—'}</td>
                          <td className="view-purchase-equipment-price">{item.estimatedPrice ?? '—'}</td>
                        </tr>
                      ))}
                      <tr className="view-purchase-equipment-total">
                        <td colSpan={5}>Thành tiền</td>
                        <td className="view-purchase-equipment-price">{totalPrice}</td>
                      </tr>
                    </tbody>
                  </table>
                </div>
              )}

              {data.proposedData && equipment.length === 0 && (
                <div className="view-purchase-form__section">
                  <label>Dữ liệu đề xuất</label>
                  <pre
                    className="view-purchase-form__value"
                    style={{ whiteSpace: 'pre-wrap', wordBreak: 'break-all' }}
                  >
                    {data.proposedData}
                  </pre>
                </div>
              )}

              {data.description && (
                <div className="view-purchase-form__section">
                  <label>Mục đích sử dụng</label>
                  <div className="view-purchase-form__value">{extractedPurpose ?? '—'}</div>
                </div>
              )}

              <div className="view-purchase-form__row">
                <div className="view-purchase-form__field">
                  <label>Ý kiến kế toán</label>
                  <div className="view-purchase-form__value">{accountantCommentDisplay}</div>
                </div>
                <div className="view-purchase-form__field">
                  <label>Ý kiến giám đốc</label>
                  <div className="view-purchase-form__value">{directorCommentDisplay}</div>
                </div>
              </div>

              <div className="view-purchase-form__section">
                <h3 className="view-purchase-form__section-title">Tài liệu đính kèm</h3>
                <div className="view-purchase-attachments">
                  {attachmentDocs.length === 0 ? (
                    <div className="view-purchase-form__value">—</div>
                  ) : (
                    attachmentDocs.map((doc, idx) => (
                      <div key={`${doc.url}-${idx}`} className="view-purchase-attachment-item">
                        <span className="view-purchase-attachment-number">#{idx + 1}</span>
                        <span className="view-purchase-attachment-name">{doc.name || `Tài liệu ${idx + 1}`}</span>
                        <button
                          type="button"
                          className="view-purchase-attachment-download"
                          onClick={() => window.open(doc.url, '_blank')}
                        >
                          Mở
                        </button>
                      </div>
                    ))
                  )}
                </div>
              </div>
            </div>
          </div>
        </div>

        <div className="view-purchase-modal__footer">
          <button type="button" onClick={onClose} className="view-purchase-btn-close">
            Quay lại
          </button>
          {canAccountantApprove && (
            <button type="button" className="view-purchase-btn-approve" onClick={() => setIsApproveOpen(true)}>
              <span className="view-purchase-btn-approve-icon">📋</span>
              <span>Phê duyệt</span>
            </button>
          )}
        </div>
      </div>

      {canAccountantApprove && isApproveOpen && (
        <div className="approve-purchase-modal-overlay" role="dialog" aria-modal="true">
          <div className="approve-purchase-modal">
            <div className="approve-purchase-modal__header">
              <h3 className="approve-purchase-modal__title">Phê duyệt đơn</h3>
            </div>

            <div className="approve-purchase-modal__body">
              <div className="approve-purchase-form">
                <div className="approve-purchase-form__row">
                  <div className="approve-purchase-form__field">
                    <label>Phê duyệt</label>
                    <select
                      className="approve-purchase-select"
                      value={decision}
                      onChange={(e) => setDecision(e.target.value === 'rejected' ? 'rejected' : 'approved')}
                    >
                      <option value="approved">Phê duyệt</option>
                      <option value="rejected">Từ chối</option>
                    </select>
                  </div>
                  <div className="approve-purchase-form__field">
                    <label>Ghi chú</label>
                    <textarea
                      className="approve-purchase-textarea"
                      placeholder="Không cần thiết"
                      value={comment}
                      onChange={(e) => setComment(e.target.value)}
                    />
                  </div>
                </div>
              </div>
            </div>

            <div className="approve-purchase-modal__footer">
              <button type="button" className="approve-purchase-btn-back" onClick={() => setIsApproveOpen(false)}>
                ← Quay lại
              </button>
              <button
                type="button"
                className="approve-purchase-btn-approve"
                disabled={submitting}
                onClick={handleSubmitApproval}
              >
                <span className="approve-purchase-btn-approve-icon">📋</span>
                <span>Phê duyệt</span>
              </button>
            </div>
          </div>
        </div>
      )}
    </div>
  );
}

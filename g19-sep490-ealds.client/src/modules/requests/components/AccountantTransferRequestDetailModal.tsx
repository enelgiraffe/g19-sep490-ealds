import { useEffect, useMemo, useState } from 'react';
import { message } from 'antd';
import type { TransferRequestListItem } from '../../assets/services/transferRequestService';
import { transferRequestService } from '../../assets/services/transferRequestService';
import './AccountantTransferRequestDetailModal.css';

const STATUS_MAP: Record<
  number,
  { label: string; color: 'default' | 'processing' | 'warning' | 'success' | 'error' }
> = {
  0: { label: 'Nháp', color: 'default' },
  1: { label: 'Đã gửi', color: 'processing' },
  2: { label: 'Chờ phê duyệt', color: 'warning' },
  3: { label: 'Từ chối', color: 'error' },
  4: { label: 'Phê duyệt', color: 'success' },
};

function formatDate(iso?: string | null): string {
  if (!iso) return '—';
  try {
    return new Date(iso).toLocaleDateString('vi-VN');
  } catch {
    return iso ?? '—';
  }
}

interface AccountantTransferRequestDetailModalProps {
  open: boolean;
  onClose: () => void;
  data: TransferRequestListItem | null;
  currentUserId?: number | null;
  onActionCompleted?: (assetRequestId: number) => void | Promise<void>;
}

export function AccountantTransferRequestDetailModal({
  open,
  onClose,
  data,
  currentUserId,
  onActionCompleted,
}: AccountantTransferRequestDetailModalProps) {
  const [isApproveOpen, setIsApproveOpen] = useState(false);
  const [decision, setDecision] = useState<'approved' | 'rejected'>('approved');
  const [comment, setComment] = useState('');
  const [submitting, setSubmitting] = useState(false);
  const [handled, setHandled] = useState(false);

  const requestStatus = data?.status ?? null;

  useEffect(() => {
    if (!open || !data) {
      setHandled(false);
      return;
    }
    setHandled(false);
  }, [open, data?.assetRequestId, data?.status]);

  const statusConfig = useMemo(() => {
    if (!data) return null;
    return STATUS_MAP[data.status] ?? STATUS_MAP[1];
  }, [data]);

  const statusClassName = useMemo(() => {
    if (!statusConfig) return 'acct-transfer-status-tag';
    if (statusConfig.color === 'success') return 'acct-transfer-status-tag acct-transfer-status-tag--success';
    if (statusConfig.color === 'warning') return 'acct-transfer-status-tag acct-transfer-status-tag--warning';
    if (statusConfig.color === 'processing') return 'acct-transfer-status-tag acct-transfer-status-tag--processing';
    if (statusConfig.color === 'error') return 'acct-transfer-status-tag acct-transfer-status-tag--error';
    return 'acct-transfer-status-tag';
  }, [statusConfig]);

  const canApprove = !!currentUserId && !!data && requestStatus === 1 && !handled; // Đã gửi (kế toán xử lý)

  const handleSubmitApproval = async () => {
    if (!data) return;
    if (!currentUserId) {
      message.error('Không lấy được thông tin người dùng.');
      return;
    }
    setSubmitting(true);
    try {
      if (decision === 'approved') {
        await transferRequestService.approveAsAccountant(data.assetRequestId, {
          approvedBy: currentUserId,
          comment: comment.trim() || null,
        });
        message.success('Đã phê duyệt yêu cầu điều chuyển.');
      } else {
        await transferRequestService.rejectAsAccountant(data.assetRequestId, {
          approvedBy: currentUserId,
          comment: comment.trim() || null,
        });
        message.success('Đã từ chối yêu cầu điều chuyển.');
      }
      await onActionCompleted?.(data.assetRequestId);
      setIsApproveOpen(false);
      setHandled(true);
    } catch (e: unknown) {
      const err = e as { response?: { data?: string } };
      message.error(err?.response?.data ?? 'Thao tác phê duyệt thất bại.');
    } finally {
      setSubmitting(false);
    }
  };

  if (!open) return null;

  return (
    <div className="acct-transfer-modal-overlay" role="dialog" aria-modal="true">
      <div className="acct-transfer-modal">
        <div className="acct-transfer-modal__header">
          <div className="acct-transfer-modal__header-left">
            <h2 className="acct-transfer-modal__title">Chi tiết yêu cầu điều chuyển</h2>
            {statusConfig && <span className={statusClassName}>{statusConfig.label}</span>}
          </div>
        </div>

        <div className="acct-transfer-modal__body">
          <div className="acct-transfer-modal__content">
            {!data ? (
              <div className="acct-transfer-form__section">
                <div className="acct-transfer-form__value">Không tìm thấy dữ liệu yêu cầu điều chuyển.</div>
              </div>
            ) : (
              <>
                <div className="acct-transfer-form__row">
                  <div className="acct-transfer-form__field">
                    <label>Mã yêu cầu</label>
                    <div className="acct-transfer-form__value">{data.code || `YC-${data.assetRequestId}`}</div>
                  </div>
                  <div className="acct-transfer-form__field">
                    <label>Ngày điều chuyển</label>
                    <div className="acct-transfer-form__value">{formatDate(data.transferDate)}</div>
                  </div>
                </div>

                <div className="acct-transfer-form__row">
                  <div className="acct-transfer-form__field">
                    <label>Điều chuyển từ</label>
                    <div className="acct-transfer-form__value">{data.fromDepartment || '—'}</div>
                  </div>
                  <div className="acct-transfer-form__field">
                    <label>Điều chuyển đến</label>
                    <div className="acct-transfer-form__value">{data.toDepartment || '—'}</div>
                  </div>
                </div>

                <div className="acct-transfer-form__row">
                  <div className="acct-transfer-form__field">
                    <label>Mã cá thể</label>
                    <div className="acct-transfer-form__value">
                      {data.instanceCode || '—'}
                    </div>
                  </div>
                  <div className="acct-transfer-form__field">
                    <label>Trạng thái yêu cầu</label>
                    <div className="acct-transfer-form__value">{statusConfig?.label || data.statusName || '—'}</div>
                  </div>
                </div>

                <div className="acct-transfer-form__row">
                  <div className="acct-transfer-form__field">
                    <label>Mã tài sản</label>
                    <div className="acct-transfer-form__value">{data.assetCode || '—'}</div>
                  </div>
                  <div className="acct-transfer-form__field">
                    <label>Tên tài sản</label>
                    <div className="acct-transfer-form__value">{data.assetName || '—'}</div>
                  </div>
                </div>

                <div className="acct-transfer-form__row">
                  <div className="acct-transfer-form__field">
                    <label>Người gửi</label>
                    <div className="acct-transfer-form__value">
                      {data.createdByName?.trim() || `User #${data.createdBy}`}
                    </div>
                  </div>
                  <div className="acct-transfer-form__field">
                    <label>Số lượng</label>
                    <div className="acct-transfer-form__value">{Number.isFinite(data.quantity) ? data.quantity : '—'}</div>
                  </div>
                </div>

                <div className="acct-transfer-form__section">
                  <h3 className="acct-transfer-form__section-title">Lý do điều chuyển</h3>
                  <div className="acct-transfer-form__value">{data.reason || '—'}</div>
                </div>
              </>
            )}
          </div>
        </div>

        <div className="acct-transfer-modal__footer">
          <button type="button" onClick={onClose} className="acct-transfer-btn-close">
            Quay lại
          </button>

          {canApprove && (
            <button
              type="button"
              className="acct-transfer-btn-approve"
              onClick={() => setIsApproveOpen(true)}
            >
              <span className="acct-transfer-btn-approve-icon">📋</span>
              <span>Phê duyệt</span>
            </button>
          )}
        </div>
      </div>

      {canApprove && isApproveOpen && (
        <div className="acct-transfer-approve-overlay" role="dialog" aria-modal="true">
          <div className="acct-transfer-approve-modal">
            <div className="acct-transfer-approve-modal__header">
              <h3 className="acct-transfer-approve-modal__title">Phê duyệt yêu cầu điều chuyển</h3>
            </div>

            <div className="acct-transfer-approve-modal__body">
              <div className="acct-transfer-approve-form">
                <div className="acct-transfer-approve-form__row">
                  <div className="acct-transfer-approve-form__field">
                    <label>Phê duyệt</label>
                    <select
                      className="acct-transfer-approve-select"
                      value={decision}
                      onChange={(e) =>
                        setDecision(e.target.value === 'rejected' ? 'rejected' : 'approved')
                      }
                    >
                      <option value="approved">Phê duyệt</option>
                      <option value="rejected">Từ chối</option>
                    </select>
                  </div>
                  <div className="acct-transfer-approve-form__field">
                    <label>Ghi chú</label>
                    <textarea
                      className="acct-transfer-approve-textarea"
                      placeholder="Không cần thiết"
                      value={comment}
                      onChange={(e) => setComment(e.target.value)}
                    />
                  </div>
                </div>
              </div>
            </div>

            <div className="acct-transfer-approve-modal__footer">
              <button
                type="button"
                className="acct-transfer-approve-btn-back"
                onClick={() => setIsApproveOpen(false)}
              >
                ← Quay lại
              </button>
              <button
                type="button"
                className="acct-transfer-approve-btn-submit"
                disabled={submitting}
                onClick={handleSubmitApproval}
              >
                <span className="acct-transfer-btn-approve-icon">📋</span>
                <span>Phê duyệt</span>
              </button>
            </div>
          </div>
        </div>
      )}
    </div>
  );
}


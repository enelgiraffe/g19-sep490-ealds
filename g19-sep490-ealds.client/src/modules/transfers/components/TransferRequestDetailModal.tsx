import './TransferRequestDetailModal.css';
import type { TransferRequestListItem } from '../../assets/services/transferRequestService';

const STATUS_MAP: Record<
  number,
  {
    label: string;
    color: string;
  }
> = {
  0: { label: 'Nháp', color: 'default' },
  1: { label: 'Đã nộp', color: 'processing' },
  2: { label: 'Chờ phê duyệt', color: 'warning' },
  3: { label: 'Từ chối', color: 'error' },
  4: { label: 'Phê duyệt', color: 'success' },
};

interface TransferRequestDetailModalProps {
  open: boolean;
  onClose: () => void;
  request: TransferRequestListItem | null;
}

function formatDate(iso?: string | null): string {
  if (!iso) return '—';
  try {
    const d = new Date(iso);
    return d.toLocaleDateString('vi-VN');
  } catch {
    return iso ?? '—';
  }
}

export function TransferRequestDetailModal({
  open,
  onClose,
  request,
}: TransferRequestDetailModalProps) {
  if (!open || !request) return null;

  const statusConfig =
    request && STATUS_MAP[request.status] ? STATUS_MAP[request.status] : undefined;
  const statusClass =
    statusConfig?.color === 'success'
      ? 'transfer-detail-status transfer-detail-status--success'
      : statusConfig?.color === 'processing'
        ? 'transfer-detail-status transfer-detail-status--processing'
        : statusConfig?.color === 'warning'
          ? 'transfer-detail-status transfer-detail-status--warning'
          : statusConfig?.color === 'error'
            ? 'transfer-detail-status transfer-detail-status--danger'
            : 'transfer-detail-status transfer-detail-status--default';
  const displayInstanceCode = request.instanceCode || request.assetCode || '—';

  return (
    <div className="transfer-detail-modal-overlay" role="dialog" aria-modal="true">
      <div className="transfer-detail-modal">
        <button
          type="button"
          className="transfer-detail-modal__close-btn"
          onClick={onClose}
          aria-label="Đóng"
        >
          <span className="transfer-detail-modal__close">×</span>
        </button>

        <div className="transfer-detail-modal__header">
          <h2 className="transfer-detail-modal__title">Chi tiết yêu cầu điều chuyển</h2>
        </div>

        <div className="transfer-detail-modal__body">
          <div className="transfer-detail-modal__content">
            <div className="transfer-detail-form__section">
              <h3 className="transfer-detail-section-title">Thông tin chung</h3>
              <div className="transfer-detail-form__row">
                <div className="transfer-detail-form__item">
                  <label>Số biên bản</label>
                  <div className="transfer-detail-info-value">{request.code || '—'}</div>
                </div>
                <div className="transfer-detail-form__item">
                  <label>Ngày điều chuyển</label>
                  <div className="transfer-detail-info-value">{formatDate(request.transferDate)}</div>
                </div>
              </div>
              <div className="transfer-detail-form__row">
                <div className="transfer-detail-form__item">
                  <label>Trạng thái</label>
                  <div className="transfer-detail-info-value">
                    <span className={statusClass}>{statusConfig?.label ?? request.statusName ?? '—'}</span>
                  </div>
                </div>
                <div className="transfer-detail-form__item">
                  <label>Số lượng</label>
                  <div className="transfer-detail-info-value">{request.quantity}</div>
                </div>
              </div>
            </div>

            <div className="transfer-detail-form__section">
              <h3 className="transfer-detail-section-title">Thông tin cá thể và điều chuyển</h3>
              <div className="transfer-detail-form__row">
                <div className="transfer-detail-form__item">
                  <label>Mã cá thể</label>
                  <div className="transfer-detail-info-value">{displayInstanceCode}</div>
                </div>
                <div className="transfer-detail-form__item">
                  <label>Tên tài sản</label>
                  <div className="transfer-detail-info-value">{request.assetName || '—'}</div>
                </div>
              </div>
              <div className="transfer-detail-form__row">
                <div className="transfer-detail-form__item">
                  <label>Điều chuyển từ</label>
                  <div className="transfer-detail-info-value">{request.fromDepartment || '—'}</div>
                </div>
                <div className="transfer-detail-form__item">
                  <label>Điều chuyển đến</label>
                  <div className="transfer-detail-info-value">{request.toDepartment || '—'}</div>
                </div>
              </div>
              <div className="transfer-detail-form__item transfer-detail-form__item--full">
                <label>Lý do điều chuyển</label>
                <div className="transfer-detail-info-value transfer-detail-info-value--multiline">
                  {request.reason || '—'}
                </div>
              </div>
              <div className="transfer-detail-form__row">
                <div className="transfer-detail-form__item">
                  <label>Xác nhận bên gửi</label>
                  <div className="transfer-detail-info-value">
                    {request.isSenderConfirmed ? 'Đã xác nhận đã gửi' : 'Chưa xác nhận'}
                  </div>
                </div>
                <div className="transfer-detail-form__item">
                  <label>Xác nhận bên nhận</label>
                  <div className="transfer-detail-info-value">
                    {request.isReceiverConfirmed ? 'Đã xác nhận đã nhận' : 'Chưa xác nhận'}
                  </div>
                </div>
              </div>
            </div>
          </div>
        </div>

        <div className="transfer-detail-modal__footer">
          <button type="button" className="transfer-detail-btn-close" onClick={onClose}>
            Đóng
          </button>
        </div>
      </div>
    </div>
  );
}


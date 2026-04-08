import { useNavigate } from 'react-router-dom';
import { formatVnd } from '../../assets/services/assetService';
import type { TransferRequestListItem } from '../../assets/services/transferRequestService';
import '../../assets/components/MarkDamagedAssetModal.css';

function formatDate(iso: string): string {
  try {
    return new Date(iso).toLocaleDateString('vi-VN');
  } catch {
    return iso;
  }
}

function formatMoneyVnd(value: number | null | undefined): string {
  if (value == null || Number.isNaN(Number(value))) return '—';
  return formatVnd(Number(value));
}

export interface LiquidationDisposalDetailModalProps {
  open: boolean;
  onClose: () => void;
  row: TransferRequestListItem | null;
  /** Thông tin tài chính + nút xem cá thể — chỉ dùng cho kế toán */
  showAccountantExtras?: boolean;
  /**
   * Trang chi tiết cá thể đọc state này làm nút “Quay lại” (tránh mặc định về /assets/:id).
   * Ví dụ: /liquidation hoặc /requests?tab=liquidation
   */
  returnPathAfterInstance?: string;
  returnLabelAfterInstance?: string;
  /** Ghép thêm class lên overlay (vd. z-index khi mở lồng modal biên bản thẩm định). */
  overlayClassName?: string;
}

export function LiquidationDisposalDetailModal({
  open,
  onClose,
  row,
  showAccountantExtras = false,
  returnPathAfterInstance = '/liquidation',
  returnLabelAfterInstance = '← Quay lại Thanh lý',
  overlayClassName = '',
}: LiquidationDisposalDetailModalProps) {
  const navigate = useNavigate();

  if (!open || !row) return null;

  const instanceId = row.assetInstanceId;
  const canOpenInstance = instanceId != null && instanceId > 0;
  const overlayCn = ['mark-damaged-modal-overlay', overlayClassName].filter(Boolean).join(' ');

  return (
    <div className={overlayCn} role="dialog" aria-modal="true">
      <div className="mark-damaged-modal">
        <button type="button" className="mark-damaged-modal__close-btn" onClick={onClose} aria-label="Đóng">
          <span className="mark-damaged-modal__close">×</span>
        </button>

        <div className="mark-damaged-modal__header">
          <h2 className="mark-damaged-modal__title">Chi tiết yêu cầu thanh lý — {row.code}</h2>
        </div>

        <div className="mark-damaged-modal__body">
          <div className="mark-damaged-modal__content">
            <div className="mark-damaged-info-section">
              <h3 className="mark-damaged-section-title">Thông tin yêu cầu</h3>
              <div className="mark-damaged-info-grid">
                <div className="mark-damaged-info-row">
                  <div className="mark-damaged-info-item">
                    <label>Mã yêu cầu</label>
                    <div className="mark-damaged-info-value">YC-{row.assetRequestId}</div>
                  </div>
                  <div className="mark-damaged-info-item">
                    <label>Ngày gửi</label>
                    <div className="mark-damaged-info-value">{formatDate(row.transferDate)}</div>
                  </div>
                </div>
                <div className="mark-damaged-info-row">
                  <div className="mark-damaged-info-item">
                    <label>Mã tài sản gốc</label>
                    <div className="mark-damaged-info-value">{row.assetCode ?? '—'}</div>
                  </div>
                  <div className="mark-damaged-info-item">
                    <label>Mã cá thể</label>
                    <div className="mark-damaged-info-value">{row.instanceCode?.trim() || '—'}</div>
                  </div>
                </div>
                <div className="mark-damaged-info-row">
                  <div className="mark-damaged-info-item">
                    <label>Tên tài sản</label>
                    <div className="mark-damaged-info-value">{row.assetName ?? '—'}</div>
                  </div>
                  <div className="mark-damaged-info-item">
                    <label>Phòng ban đề xuất</label>
                    <div className="mark-damaged-info-value">{row.fromDepartment ?? '—'}</div>
                  </div>
                </div>
                <div className="mark-damaged-info-row">
                  <div className="mark-damaged-info-item">
                    <label>Trạng thái</label>
                    <div className="mark-damaged-info-value">{row.statusName ?? '—'}</div>
                  </div>
                  <div className="mark-damaged-info-item">
                    <label>Người tạo</label>
                    <div className="mark-damaged-info-value">{row.createdByName?.trim() || `#${row.createdBy}`}</div>
                  </div>
                </div>
                <div className="mark-damaged-info-row">
                  <div className="mark-damaged-info-item" style={{ gridColumn: '1 / -1' }}>
                    <label>Nội dung / lý do</label>
                    <div className="mark-damaged-info-value">{row.reason?.trim() || '—'}</div>
                  </div>
                </div>
              </div>
            </div>

            {showAccountantExtras && (
              <div className="mark-damaged-info-section" style={{ marginTop: 16 }}>
                <h3 className="mark-damaged-section-title">Thông tin phục vụ thẩm định (kế toán)</h3>
                <div className="mark-damaged-info-grid">
                  <div className="mark-damaged-info-row">
                    <div className="mark-damaged-info-item">
                      <label>Loại tài sản</label>
                      <div className="mark-damaged-info-value">{row.assetTypeName?.trim() || '—'}</div>
                    </div>
                    <div className="mark-damaged-info-item">
                      <label>Giá trị khai báo trên đơn</label>
                      <div className="mark-damaged-info-value">
                        {formatMoneyVnd(row.disposalDeclaredValue)}
                      </div>
                    </div>
                  </div>
                  <div className="mark-damaged-info-row">
                    <div className="mark-damaged-info-item">
                      <label>Nguyên giá (cá thể)</label>
                      <div className="mark-damaged-info-value">{formatMoneyVnd(row.originalPrice)}</div>
                    </div>
                    <div className="mark-damaged-info-item">
                      <label>Giá trị còn lại trên sổ</label>
                      <div className="mark-damaged-info-value">{formatMoneyVnd(row.currentValue)}</div>
                    </div>
                  </div>
                  <div className="mark-damaged-info-row">
                    <div className="mark-damaged-info-item" style={{ gridColumn: '1 / -1' }}>
                      <button
                        type="button"
                        className="mark-damaged-btn-submit"
                        style={{ marginTop: 4, width: 'auto', alignSelf: 'flex-start' }}
                        disabled={!canOpenInstance}
                        onClick={() => {
                          if (!canOpenInstance) return;
                          onClose();
                          navigate(`/asset-instances/${instanceId}`, {
                            state: {
                              backToPath: returnPathAfterInstance,
                              backLabel: returnLabelAfterInstance,
                            },
                          });
                        }}
                      >
                        Xem chi tiết cá thể
                      </button>
                      {!canOpenInstance && (
                        <div className="mark-damaged-info-value" style={{ marginTop: 8, fontSize: 13 }}>
                          Không có mã cá thể để mở trang chi tiết.
                        </div>
                      )}
                    </div>
                  </div>
                </div>
              </div>
            )}
          </div>
        </div>

        <div className="mark-damaged-modal__footer">
          <button type="button" className="mark-damaged-btn-draft" onClick={onClose}>
            Đóng
          </button>
        </div>
      </div>
    </div>
  );
}

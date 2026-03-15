import { useState } from 'react';
import { message } from 'antd';
import { purchaseOrderService, type PurchaseOrderDetail } from '../services/purchaseOrderService';
import './ViewPurchaseOrderModal.css';

const STATUS_MAP: Record<number, { label: string; color: string }> = {
  0: { label: 'Nhập', color: 'default' },
  1: { label: 'Duyệt', color: 'success' },
  2: { label: 'Từ chối', color: 'error' },
  3: { label: 'Chờ ngân sách', color: 'warning' },
};

function formatDate(iso: string): string {
  try {
    return new Date(iso).toLocaleString('vi-VN');
  } catch {
    return iso;
  }
}

interface ViewPurchaseOrderModalProps {
  open: boolean;
  onClose: () => void;
  data: PurchaseOrderDetail | null;
  currentUserId?: number | null;
}

export function ViewPurchaseOrderModal({
  open,
  onClose,
  data,
  currentUserId,
}: ViewPurchaseOrderModalProps) {
  if (!open || !data) return null;

  const [isApproveOpen, setIsApproveOpen] = useState(false);
  const [decision, setDecision] = useState<'approved' | 'rejected'>('approved');
  const [comment, setComment] = useState('');
  const [submitting, setSubmitting] = useState(false);
  const handleSubmitApproval = async () => {
    if (!currentUserId) {
      message.error('Không lấy được thông tin người dùng.');
      return;
    }
    setSubmitting(true);
    try {
      if (decision === 'approved') {
        await purchaseOrderService.approveAsAccountant(data.assetRequestId, {
          approvedBy: currentUserId,
          comment: comment.trim() || null,
        });
        message.success('Phê duyệt đơn thành công.');
      } else {
        await purchaseOrderService.rejectAsAccountant(data.assetRequestId, {
          approvedBy: currentUserId,
          comment: comment.trim() || null,
        });
        message.success('Từ chối đơn thành công.');
      }
      setIsApproveOpen(false);
    } catch (e: unknown) {
      const err = e as { response?: { data?: string } };
      message.error(err?.response?.data ?? 'Thao tác phê duyệt thất bại.');
    } finally {
      setSubmitting(false);
    }
  };

  const statusConfig = STATUS_MAP[data.status] ?? STATUS_MAP[0];
  let equipment: { stt: number; name: string; quantity: number; machineCode?: string; unit?: string; estimatedPrice?: string }[] = [];
  let totalPrice = '—';
  try {
    if (data.proposedData) {
      const parsed = JSON.parse(data.proposedData) as {
        equipment?: { name?: string; quantity?: number; machineCode?: string; unit?: string; estimatedPrice?: string }[];
        totalPrice?: string;
      };
      if (Array.isArray(parsed.equipment)) {
        equipment = parsed.equipment.map((e, i) => ({
          stt: i + 1,
          name: e.name ?? '—',
          quantity: e.quantity ?? 1,
          machineCode: e.machineCode,
          unit: e.unit,
          estimatedPrice: e.estimatedPrice,
        }));
      }
      if (parsed.totalPrice) totalPrice = parsed.totalPrice;
    }
  } catch {
    // keep empty
  }

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
              {/* Thông tin chung */}
              <div className="view-purchase-form__row">
                <div className="view-purchase-form__field">
                  <label>Người gửi</label>
                  <div className="view-purchase-form__value">
                    {data.creatorName ?? data.createdBy}
                  </div>
                </div>
                <div className="view-purchase-form__field">
                  <label>Phòng ban</label>
                  <div className="view-purchase-form__value">—</div>
                </div>
              </div>

              <div className="view-purchase-form__row">
                <div className="view-purchase-form__field">
                  <label>Lý do đề nghị</label>
                  <div className="view-purchase-form__value">{data.title}</div>
                </div>
                <div className="view-purchase-form__field">
                  <label>Thời gian cần vật tư</label>
                  <div className="view-purchase-form__value">{formatDate(data.createDate)}</div>
                </div>
              </div>

              <div className="view-purchase-form__row">
                <div className="view-purchase-form__field">
                  <label>Nhà cung cấp đề xuất</label>
                  <div className="view-purchase-form__value">—</div>
                </div>
                <div className="view-purchase-form__field">
                  <label>Loại tài sản</label>
                  <div className="view-purchase-form__value">—</div>
                </div>
              </div>

              {/* Mã yêu cầu & tiêu đề */}
              <div className="view-purchase-form__row">
                <div className="view-purchase-form__field">
                  <label>Mã yêu cầu</label>
                  <div className="view-purchase-form__value">YC-{data.assetRequestId}</div>
                </div>
              </div>

              {/* Danh mục vật tư */}
              {equipment.length > 0 && (
                <div className="view-purchase-form__section">
                  <h3 className="view-purchase-form__section-title">Danh mục vật tư</h3>
                  <table className="view-purchase-equipment-table">
                    <thead>
                      <tr>
                        <th>STT</th>
                        <th>Tên vật tư</th>
                        <th>Số lượng</th>
                        <th>Mã máy</th>
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
                          <td>{item.machineCode ?? '—'}</td>
                          <td>{item.unit ?? '—'}</td>
                          <td className="view-purchase-equipment-price">
                            {item.estimatedPrice ?? '—'}
                          </td>
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

              {/* Mục đích sử dụng */}
              {data.description && (
                <div className="view-purchase-form__section">
                  <label>Mục đích sử dụng</label>
                  <div className="view-purchase-form__value">{data.description}</div>
                </div>
              )}

              {/* Tài liệu đính kèm (demo giống design) */}
              <div className="view-purchase-form__section">
                <h3 className="view-purchase-form__section-title">Tài liệu đính kèm</h3>
                <div className="view-purchase-attachments">
                  <div className="view-purchase-attachment-item">
                    <span className="view-purchase-attachment-number">#1</span>
                    <span className="view-purchase-attachment-name">Thông tin máy</span>
                    <button
                      type="button"
                      className="view-purchase-attachment-download"
                    >
                      Tải xuống
                    </button>
                  </div>
                  <div className="view-purchase-attachment-item">
                    <span className="view-purchase-attachment-number">#2</span>
                    <span className="view-purchase-attachment-name">Thông tin nhà cung cấp</span>
                    <button
                      type="button"
                      className="view-purchase-attachment-download"
                    >
                      Tải xuống
                    </button>
                  </div>
                </div>
              </div>

              {/* Ghi chú của người gửi */}
              {data.description && (
                <div className="view-purchase-form__section">
                  <div className="view-purchase-feedback-box">
                    <label>Ghi chú của người gửi</label>
                    <div className="view-purchase-feedback-content">{data.description}</div>
                  </div>
                </div>
              )}
            </div>
          </div>
        </div>

        <div className="view-purchase-modal__footer">
          <button
            type="button"
            onClick={onClose}
            className="view-purchase-btn-close"
          >
            Quay lại
          </button>
          {currentUserId && (
            <button
              type="button"
              className="view-purchase-btn-approve"
              onClick={() => setIsApproveOpen(true)}
            >
              <span className="view-purchase-btn-approve-icon">📋</span>
              <span>Phê duyệt</span>
            </button>
          )}
        </div>
      </div>

      {currentUserId && isApproveOpen && (
        <div
          className="approve-purchase-modal-overlay"
          role="dialog"
          aria-modal="true"
        >
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
                      onChange={(e) =>
                        setDecision(e.target.value === 'rejected' ? 'rejected' : 'approved')
                      }
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
              <button
                type="button"
                className="approve-purchase-btn-back"
                onClick={() => setIsApproveOpen(false)}
              >
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

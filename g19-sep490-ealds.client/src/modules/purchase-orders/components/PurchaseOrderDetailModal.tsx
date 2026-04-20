import { Modal, message } from 'antd';
import type { PurchaseOrderDetail, PurchaseOrderLineItem } from '../services/procurementPoService';
import { PO_STATUS } from '../services/procurementPoService';
import './PurchaseOrderDetailModal.css';

function statusLabel(status: number): string {
  if (status === PO_STATUS.draft) return 'Nháp';
  if (status === PO_STATUS.cancelled) return 'Đã hủy';
  if (status === PO_STATUS.partiallyReceived) return 'Nhận một phần';
  if (status === PO_STATUS.completed) return 'Đã nhận đủ';
  return 'Đã tạo';
}

/** Gộp tiêu đề đơn mua với mục đích mua (tiêu đề yêu cầu) khi có liên kết. */
function formatDetailTitle(poTitle: string, purchasePurpose: string | null | undefined): string {
  const t = (poTitle ?? '').trim();
  const p = (purchasePurpose ?? '').trim();
  if (!p) return t || '—';
  if (!t || t === p) return p;
  return `${t} — ${p}`;
}

function formatMoney(n: number, currency: string): string {
  try {
    return `${n.toLocaleString('vi-VN')} ${currency}`;
  } catch {
    return `${n} ${currency}`;
  }
}

/** Ngày tạo chỉ hiển thị phần ngày (tránh lệch múi giờ với chuỗi ISO). */
function formatCreateDateOnly(iso: string): string {
  const m = /^(\d{4})-(\d{2})-(\d{2})/.exec(iso);
  if (m) {
    const [, y, mo, d] = m;
    return `${d}/${mo}/${y}`;
  }
  const dt = new Date(iso);
  if (Number.isNaN(dt.getTime())) return '—';
  return dt.toLocaleDateString('vi-VN');
}

function lineAssetTypeLabel(row: PurchaseOrderLineItem): string {
  const t = (row.assetTypeName ?? '').trim();
  if (t) return t;
  const d = (row.description ?? '').trim();
  return d || '—';
}

function lineAssetLabel(row: PurchaseOrderLineItem): string {
  const parts = [row.assetCode, row.assetName].filter(Boolean);
  return parts.length ? parts.join(' ') : '—';
}

export interface PurchaseOrderDetailModalProps {
  open: boolean;
  data: PurchaseOrderDetail | null;
  onClose: () => void;
  onEdit: () => void;
  onCancelOrder: () => Promise<void>;
}

export function PurchaseOrderDetailModal({
  open,
  data,
  onClose,
  onEdit,
  onCancelOrder,
}: PurchaseOrderDetailModalProps) {
  const hasReceipt = data?.lines.some((l) => Number(l.receivedQuantity ?? 0) > 0) ?? false;
  const canEdit =
    (data?.status === PO_STATUS.created || data?.status === PO_STATUS.draft) && !hasReceipt;
  const canCancel =
    data != null &&
    data.status !== PO_STATUS.cancelled &&
    data.status !== PO_STATUS.completed &&
    !hasReceipt;

  const handleCancel = async () => {
    Modal.confirm({
      title: 'Hủy đơn mua?',
      content: 'Trạng thái sẽ chuyển sang Đã hủy.',
      okText: 'Hủy đơn',
      okType: 'danger',
      cancelText: 'Đóng',
      onOk: async () => {
        try {
          await onCancelOrder();
          message.success('Đã hủy đơn mua.');
        } catch {
          message.error('Không hủy được đơn.');
        }
      },
    });
  };

  if (!open) return null;

  const titleText = data ? `Đơn mua ${data.contractNo}` : 'Chi tiết đơn mua';
  const statusText = data ? statusLabel(data.status) : '';

  return (
    <div className="po-detail-modal-overlay" role="dialog" aria-modal="true">
      <div className="po-detail-modal">
        <button type="button" className="po-detail-modal__close-btn" onClick={onClose} aria-label="Đóng">
          <span className="po-detail-modal__close">×</span>
        </button>

        <div className="po-detail-modal__header">
          <h2 className="po-detail-modal__title">{titleText}</h2>
        </div>

        <div className="po-detail-modal__body">
          <div className="po-detail-modal__content">
            {data && (
              <>
                <div className="po-detail-modal-info-section">
                  <h3 className="po-detail-modal-section-title">Thông tin đơn mua</h3>
                  <div className="po-detail-modal-info-grid">
                    <div className="po-detail-modal-info-row">
                      <div className="po-detail-modal-info-item" style={{ gridColumn: '1 / -1' }}>
                        <label>Số chứng từ</label>
                        <div className="po-detail-modal-info-value">{data.contractNo}</div>
                      </div>
                    </div>
                    <div className="po-detail-modal-info-row">
                      <div className="po-detail-modal-info-item" style={{ gridColumn: '1 / -1' }}>
                        <label>Tiêu đề</label>
                        <div className="po-detail-modal-info-value">
                          {formatDetailTitle(data.title, data.assetRequestTitle)}
                        </div>
                      </div>
                    </div>
                    <div className="po-detail-modal-info-row">
                      <div className="po-detail-modal-info-item">
                        <label>Nhà cung cấp</label>
                        <div className="po-detail-modal-info-value">
                          {data.supplierName ?? `ID ${data.supplierId}`}
                        </div>
                      </div>
                      <div className="po-detail-modal-info-item">
                        <label>Tiền tệ</label>
                        <div className="po-detail-modal-info-value">{data.currency}</div>
                      </div>
                    </div>
                    <div className="po-detail-modal-info-row">
                      <div className="po-detail-modal-info-item">
                        <label>Tổng tiền</label>
                        <div className="po-detail-modal-info-value">
                          {formatMoney(Number(data.totalAmount), data.currency)}
                        </div>
                      </div>
                      <div className="po-detail-modal-info-item">
                        <label>Trạng thái</label>
                        <div className="po-detail-modal-info-value">{statusText}</div>
                      </div>
                    </div>
                    <div className="po-detail-modal-info-row">
                      <div className="po-detail-modal-info-item">
                        <label>Ngày tạo</label>
                        <div className="po-detail-modal-info-value">{formatCreateDateOnly(data.createDate)}</div>
                      </div>
                      {data.assetRequestId != null && (
                        <div className="po-detail-modal-info-item">
                          <label>Yêu cầu liên kết</label>
                          <div className="po-detail-modal-info-value">#{data.assetRequestId}</div>
                        </div>
                      )}
                    </div>
                  </div>
                </div>

                <div className="po-detail-modal-info-section">
                  <h3 className="po-detail-modal-section-title">Chi tiết dòng hàng</h3>
                  <div className="po-detail-modal-table-wrap">
                    <table className="po-detail-modal-table">
                      <thead>
                        <tr>
                          <th style={{ width: 40 }}>#</th>
                          <th style={{ minWidth: 140 }}>Loại tài sản</th>
                          <th style={{ minWidth: 180 }}>Tài sản</th>
                          <th className="po-detail-modal-table-num">SL đặt</th>
                          <th className="po-detail-modal-table-num">Đã nhận</th>
                          <th className="po-detail-modal-table-num">Còn lại</th>
                          <th>ĐVT</th>
                          <th className="po-detail-modal-table-num">Đơn giá</th>
                          <th>Ngày giao dự kiến</th>
                          <th className="po-detail-modal-table-num">Thành tiền</th>
                        </tr>
                      </thead>
                      <tbody>
                        {data.lines.map((r, i) => (
                          <tr key={r.lineId}>
                            <td>{i + 1}</td>
                            <td>{lineAssetTypeLabel(r)}</td>
                            <td>{lineAssetLabel(r)}</td>
                            <td className="po-detail-modal-table-num">
                              {Number(r.quantity).toLocaleString('vi-VN')}
                            </td>
                            <td className="po-detail-modal-table-num">
                              {Number(r.receivedQuantity ?? 0).toLocaleString('vi-VN')}
                            </td>
                            <td className="po-detail-modal-table-num">
                              {Number(r.openQuantity ?? 0).toLocaleString('vi-VN')}
                            </td>
                            <td>{r.unit || '—'}</td>
                            <td className="po-detail-modal-table-num">
                              {Number(r.unitPrice).toLocaleString('vi-VN')}
                            </td>
                            <td>
                              {r.expectedDeliveryDate
                                ? (() => {
                                    const m = /^(\d{4})-(\d{2})-(\d{2})/.exec(r.expectedDeliveryDate!);
                                    if (m) return `${m[3]}/${m[2]}/${m[1]}`;
                                    return new Date(r.expectedDeliveryDate!).toLocaleDateString('vi-VN');
                                  })()
                                : '—'}
                            </td>
                            <td className="po-detail-modal-table-num">
                              {Number(r.lineTotal).toLocaleString('vi-VN')}
                            </td>
                          </tr>
                        ))}
                      </tbody>
                    </table>
                  </div>
                </div>
              </>
            )}
          </div>
        </div>

        <div className="po-detail-modal-modal__footer">
          {canEdit && (
            <button type="button" className="po-detail-modal__btn po-detail-modal__btn--secondary" onClick={onEdit}>
              Chỉnh sửa
            </button>
          )}
          {canCancel && (
            <button
              type="button"
              className="po-detail-modal__btn po-detail-modal__btn--danger"
              onClick={handleCancel}
            >
              Hủy đơn
            </button>
          )}
          <button type="button" className="po-detail-modal__btn po-detail-modal__btn--primary" onClick={onClose}>
            Đóng
          </button>
        </div>
      </div>
    </div>
  );
}

import { Button } from 'antd';
import dayjs from 'dayjs';
import { SUPPLIER_INVOICE_STATUS, type SupplierInvoiceDetail } from '../services/supplierInvoiceService';
import './SupplierInvoiceDetailModal.css';

interface SupplierInvoiceDetailModalProps {
  open: boolean;
  loading: boolean;
  detail: SupplierInvoiceDetail | null;
  onClose: () => void;
  onCancel: (id: number) => void;
}

function statusLabel(status: number): string {
  if (status === SUPPLIER_INVOICE_STATUS.cancelled) return 'Đã hủy';
  return 'Hiệu lực';
}

function formatDate(iso: string): string {
  try {
    return dayjs(iso).format('DD/MM/YYYY');
  } catch {
    return iso;
  }
}

export function SupplierInvoiceDetailModal({
  open,
  loading,
  detail,
  onClose,
  onCancel,
}: SupplierInvoiceDetailModalProps) {
  if (!open) return null;

  return (
    <div className="supplier-invoice-detail-overlay" role="dialog" aria-modal="true">
      <div className="supplier-invoice-detail-modal">
        <button
          type="button"
          className="supplier-invoice-detail-modal__close-btn"
          onClick={onClose}
          aria-label="Đóng"
        >
          <span className="supplier-invoice-detail-modal__close">×</span>
        </button>

        <div className="supplier-invoice-detail-modal__header">
          <h2 className="supplier-invoice-detail-modal__title">
            Chi tiết hóa đơn #{detail?.supplierInvoiceId ?? ''}
          </h2>
        </div>

        <div className="supplier-invoice-detail-modal__body">
          {loading && (
            <div className="supplier-invoice-detail-modal__loading">Đang tải...</div>
          )}
          
          {detail && !loading && (
            <div className="supplier-invoice-detail-modal__content">
              {/* Thông tin hóa đơn */}
              <div className="supplier-invoice-detail-info-section">
                <h3 className="supplier-invoice-detail-section-title">Thông tin hóa đơn</h3>
                
                <div className="supplier-invoice-detail-info-grid">
                  <div className="supplier-invoice-detail-info-row">
                    <div className="supplier-invoice-detail-info-item">
                      <label>Số hóa đơn</label>
                      <div className="supplier-invoice-detail-info-value">{detail.invoiceNumber}</div>
                    </div>
                    <div className="supplier-invoice-detail-info-item">
                      <label>Ngày hóa đơn</label>
                      <div className="supplier-invoice-detail-info-value">
                        {formatDate(detail.invoiceDate)}
                      </div>
                    </div>
                  </div>

                  <div className="supplier-invoice-detail-info-row">
                    <div className="supplier-invoice-detail-info-item">
                      <label>Nhà cung cấp</label>
                      <div className="supplier-invoice-detail-info-value">
                        {detail.supplierName || '—'}
                      </div>
                    </div>
                    <div className="supplier-invoice-detail-info-item">
                      <label>Trạng thái</label>
                      <div className="supplier-invoice-detail-info-value">
                        <span
                          className={
                            detail.status === SUPPLIER_INVOICE_STATUS.cancelled
                              ? 'supplier-invoice-detail-status-pill supplier-invoice-detail-status-pill--inactive'
                              : 'supplier-invoice-detail-status-pill supplier-invoice-detail-status-pill--active'
                          }
                        >
                          {statusLabel(detail.status)}
                        </span>
                      </div>
                    </div>
                  </div>

                  <div className="supplier-invoice-detail-info-row">
                    <div className="supplier-invoice-detail-info-item">
                      <label>Đơn mua</label>
                      <div className="supplier-invoice-detail-info-value">
                        #{detail.procurementId}
                      </div>
                    </div>
                    <div className="supplier-invoice-detail-info-item">
                      <label>Biên nhận</label>
                      <div className="supplier-invoice-detail-info-value">
                        {detail.goodsReceiptId ? `#${detail.goodsReceiptId}` : '—'}
                      </div>
                    </div>
                  </div>

                  <div className="supplier-invoice-detail-info-row">
                    <div className="supplier-invoice-detail-info-item">
                      <label>Tổng tiền</label>
                      <div className="supplier-invoice-detail-info-value supplier-invoice-detail-info-value--highlight">
                        {Number(detail.totalAmount).toLocaleString('en-US')} {detail.currency}
                      </div>
                    </div>
                    <div className="supplier-invoice-detail-info-item">
                      <label>Ngày tạo</label>
                      <div className="supplier-invoice-detail-info-value">
                        {formatDate(detail.createdDate)}
                      </div>
                    </div>
                  </div>

                  {detail.note && (
                    <div className="supplier-invoice-detail-info-row">
                      <div className="supplier-invoice-detail-info-item" style={{ gridColumn: '1 / -1' }}>
                        <label>Ghi chú</label>
                        <div className="supplier-invoice-detail-info-value">{detail.note}</div>
                      </div>
                    </div>
                  )}
                </div>
              </div>

              {/* Chi tiết dòng hóa đơn */}
              <div className="supplier-invoice-detail-lines-section">
                <h3 className="supplier-invoice-detail-section-title">Chi tiết dòng hóa đơn</h3>
                
                <div className="supplier-invoice-detail-table-wrapper">
                  <table className="supplier-invoice-detail-table">
                    <thead>
                      <tr>
                        <th style={{ width: '40px' }}>#</th>
                        <th>Tài sản</th>
                        <th style={{ width: '100px', textAlign: 'right' }}>Số lượng</th>
                        <th style={{ width: '130px', textAlign: 'right' }}>Đơn giá (đ)</th>
                        <th style={{ width: '150px', textAlign: 'right' }}>Thành tiền (đ)</th>
                      </tr>
                    </thead>
                    <tbody>
                      {detail.lines.map((line, idx) => (
                        <tr key={line.supplierInvoiceLineId}>
                          <td>{idx + 1}</td>
                          <td>
                            {[line.assetCode, line.assetName].filter(Boolean).join(' ') || '—'}
                          </td>
                          <td style={{ textAlign: 'right' }}>
                            {Number(line.quantity).toLocaleString('en-US')}
                          </td>
                          <td style={{ textAlign: 'right' }}>
                            {Number(line.unitPrice).toLocaleString('en-US')}
                          </td>
                          <td style={{ textAlign: 'right', fontWeight: 500 }}>
                            {Number(line.lineTotal).toLocaleString('en-US')}
                          </td>
                        </tr>
                      ))}
                    </tbody>
                    <tfoot>
                      <tr className="supplier-invoice-detail-table-total">
                        <td colSpan={4} style={{ textAlign: 'right', fontWeight: 600 }}>
                          Tổng cộng:
                        </td>
                        <td style={{ textAlign: 'right', fontWeight: 600 }}>
                          {Number(detail.totalAmount).toLocaleString('en-US')}
                        </td>
                      </tr>
                    </tfoot>
                  </table>
                </div>
              </div>
            </div>
          )}
        </div>

        <div className="supplier-invoice-detail-modal__footer">
          {detail && detail.status === SUPPLIER_INVOICE_STATUS.active && (
            <button
              type="button"
              onClick={() => onCancel(detail.supplierInvoiceId)}
              className="supplier-invoice-detail-btn-cancel-invoice"
            >
              Hủy hóa đơn
            </button>
          )}
          <button
            type="button"
            onClick={onClose}
            className="supplier-invoice-detail-btn-close"
          >
            Đóng
          </button>
        </div>
      </div>
    </div>
  );
}

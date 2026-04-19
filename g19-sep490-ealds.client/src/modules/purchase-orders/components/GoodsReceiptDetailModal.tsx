import dayjs from 'dayjs';
import type { GoodsReceiptDetail } from '../services/goodsReceiptService';
import './GoodsReceiptDetailModal.css';

function fileLabelFromUrl(url: string): string {
  try {
    const u = new URL(url);
    const seg = u.pathname.split('/').filter(Boolean);
    return decodeURIComponent(seg[seg.length - 1] || url);
  } catch {
    return url;
  }
}

interface GoodsReceiptDetailModalProps {
  open: boolean;
  onClose: () => void;
  detail: GoodsReceiptDetail | null;
  onPrintLabels?: (goodsReceiptId: number) => void;
}

export function GoodsReceiptDetailModal({
  open,
  onClose,
  detail,
  onPrintLabels,
}: GoodsReceiptDetailModalProps) {
  if (!open || !detail) return null;

  return (
    <div className="gr-detail-modal-overlay" role="dialog" aria-modal="true">
      <div className="gr-detail-modal">
        <button
          type="button"
          className="gr-detail-modal__close-btn"
          onClick={onClose}
          aria-label="Đóng"
        >
          <span className="gr-detail-modal__close">×</span>
        </button>

        <div className="gr-detail-modal__header">
          <h2 className="gr-detail-modal__title">Chi tiết biên nhận #{detail.goodsReceiptId}</h2>
        </div>

        <div className="gr-detail-modal__body">
          <div className="gr-detail-modal__content">
            <div className="gr-detail-info-section">
              <h3 className="gr-detail-section-title">Thông tin biên nhận</h3>

              <div className="gr-detail-info-grid">
                <div className="gr-detail-info-row">
                  <div className="gr-detail-info-item">
                    <label>Mã biên nhận</label>
                    <div className="gr-detail-info-value">#{detail.goodsReceiptId}</div>
                  </div>
                  <div className="gr-detail-info-item">
                    <label>Đơn mua</label>
                    <div className="gr-detail-info-value">
                      {detail.contractNo} (#{detail.procurementId})
                    </div>
                  </div>
                </div>

                <div className="gr-detail-info-row">
                  <div className="gr-detail-info-item">
                    <label>Nhà cung cấp</label>
                    <div className="gr-detail-info-value">{detail.supplierName || '—'}</div>
                  </div>
                  <div className="gr-detail-info-item">
                    <label>Ngày tạo</label>
                    <div className="gr-detail-info-value">
                      {dayjs(detail.createdDate).format('DD/MM/YYYY HH:mm')}
                    </div>
                  </div>
                </div>

                {detail.note && (
                  <div className="gr-detail-info-row">
                    <div className="gr-detail-info-item" style={{ gridColumn: '1 / -1' }}>
                      <label>Ghi chú</label>
                      <div className="gr-detail-info-value">{detail.note}</div>
                    </div>
                  </div>
                )}
              </div>
            </div>

            <div className="gr-detail-attachments-section">
              <h3 className="gr-detail-section-title">Tài liệu đính kèm</h3>
              {(detail.attachments ?? []).length === 0 ? (
                <p className="gr-detail-attachments-empty">Không có tài liệu đính kèm.</p>
              ) : (
                <ul className="gr-detail-attachments-list">
                  {(detail.attachments ?? []).map((a, idx) => (
                    <li key={a.documentId ?? `${a.fileUrl}-${idx}`}>
                      <a
                        href={a.fileUrl}
                        target="_blank"
                        rel="noopener noreferrer"
                        className="gr-detail-attachments-link"
                      >
                        {fileLabelFromUrl(a.fileUrl)}
                      </a>
                    </li>
                  ))}
                </ul>
              )}
            </div>

            <div className="gr-detail-lines-section">
              <h3 className="gr-detail-section-title">Chi tiết hàng hóa</h3>

              <div className="gr-detail-table-wrapper">
                <table className="gr-detail-table">
                  <thead>
                    <tr>
                      <th style={{ width: '40px' }}>#</th>
                      <th>Tài sản</th>
                      <th style={{ width: '80px', textAlign: 'right' }}>Đặt</th>
                      <th style={{ width: '100px', textAlign: 'right' }}>Nhận (BN này)</th>
                      <th style={{ width: '100px', textAlign: 'right' }}>Đã nhận (lũy kế)</th>
                      <th style={{ width: '80px', textAlign: 'right' }}>Còn lại</th>
                    </tr>
                  </thead>
                  <tbody>
                    {detail.lines.map((line, idx) => (
                      <>
                        <tr key={line.goodsReceiptLineId}>
                          <td>{idx + 1}</td>
                          <td>
                            <div className="gr-detail-asset-cell">
                              <div className="gr-detail-asset-name">
                                {line.assetName || '—'}
                              </div>
                              {line.assetCode && (
                                <div className="gr-detail-asset-code">{line.assetCode}</div>
                              )}
                            </div>
                          </td>
                          <td style={{ textAlign: 'right' }}>
                            {line.orderedQuantity.toLocaleString('vi-VN')}
                          </td>
                          <td style={{ textAlign: 'right' }}>
                            <strong>
                              {line.quantityReceivedOnThisReceipt.toLocaleString('vi-VN')}
                            </strong>
                          </td>
                          <td style={{ textAlign: 'right' }}>
                            {line.cumulativeReceivedQuantity.toLocaleString('vi-VN')}
                          </td>
                          <td style={{ textAlign: 'right' }}>
                            {line.openQuantity.toLocaleString('vi-VN')}
                          </td>
                        </tr>
                        {line.instances.length > 0 && (
                          <tr key={`${line.goodsReceiptLineId}-instances`}>
                            <td colSpan={6}>
                              <div className="gr-detail-instances-section">
                                <div className="gr-detail-instances-title">
                                  Danh sách {line.instances.length} cá thể tài sản
                                </div>
                                <div className="gr-detail-instances-list">
                                  {line.instances.map((inst, instIdx) => (
                                    <div key={inst.assetInstanceId} className="gr-detail-instance-item">
                                      <span className="gr-detail-instance-number">{instIdx + 1}.</span>
                                      <span className="gr-detail-instance-code">{inst.instanceCode}</span>
                                      {inst.serialNumber && (
                                        <span className="gr-detail-instance-serial">
                                          SN: {inst.serialNumber}
                                        </span>
                                      )}
                                    </div>
                                  ))}
                                </div>
                              </div>
                            </td>
                          </tr>
                        )}
                      </>
                    ))}
                  </tbody>
                </table>
              </div>
            </div>
          </div>
        </div>

        <div className="gr-detail-modal__footer">
          {onPrintLabels && (
            <button
              type="button"
              onClick={() => onPrintLabels(detail.goodsReceiptId)}
              className="gr-detail-btn-print"
            >
              🖨️ In nhãn QR
            </button>
          )}
          <button type="button" onClick={onClose} className="gr-detail-btn-close">
            Đóng
          </button>
        </div>
      </div>
    </div>
  );
}

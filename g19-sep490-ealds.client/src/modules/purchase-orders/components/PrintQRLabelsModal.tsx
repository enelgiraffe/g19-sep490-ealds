import { useState, useEffect } from 'react';
import { message } from 'antd';
import QRCode from 'qrcode';
import { goodsReceiptService, type GoodsReceiptDetail } from '../services/goodsReceiptService';
import './PrintQRLabelsModal.css';

interface PrintQRLabelsModalProps {
  open: boolean;
  onClose: () => void;
  goodsReceiptId: number | null;
}

interface InstanceLabel {
  assetInstanceId: number;
  instanceCode: string;
  serialNumber: string | null;
  assetName: string | null;
  qrDataUrl: string;
}

export function PrintQRLabelsModal({ open, onClose, goodsReceiptId }: PrintQRLabelsModalProps) {
  const [loading, setLoading] = useState(false);
  const [labels, setLabels] = useState<InstanceLabel[]>([]);

  useEffect(() => {
    if (open && goodsReceiptId) {
      loadLabels();
    } else {
      setLabels([]);
    }
  }, [open, goodsReceiptId]);

  const loadLabels = async () => {
    if (!goodsReceiptId) return;
    
    setLoading(true);
    try {
      const detail: GoodsReceiptDetail = await goodsReceiptService.getById(goodsReceiptId);
      const instanceLabels: InstanceLabel[] = [];

      for (const line of detail.lines) {
        for (const inst of line.instances) {
          // Tạo QR code data
          const qrData = JSON.stringify({
            instanceId: inst.assetInstanceId,
            code: inst.instanceCode,
            serial: inst.serialNumber,
            asset: line.assetName,
          });

          // Generate QR code
          const qrDataUrl = await QRCode.toDataURL(qrData, {
            width: 200,
            margin: 1,
            color: {
              dark: '#000000',
              light: '#FFFFFF',
            },
          });

          instanceLabels.push({
            assetInstanceId: inst.assetInstanceId,
            instanceCode: inst.instanceCode,
            serialNumber: inst.serialNumber,
            assetName: line.assetName,
            qrDataUrl,
          });
        }
      }

      setLabels(instanceLabels);
    } catch (error) {
      message.error('Không tải được dữ liệu biên nhận.');
      setLabels([]);
    } finally {
      setLoading(false);
    }
  };

  const handlePrint = () => {
    window.print();
  };

  if (!open) return null;

  return (
    <div className="print-qr-modal-overlay" role="dialog" aria-modal="true">
      <div className="print-qr-modal">
        <button
          type="button"
          className="print-qr-modal__close-btn"
          onClick={onClose}
          aria-label="Đóng"
        >
          <span className="print-qr-modal__close">×</span>
        </button>

        <div className="print-qr-modal__header">
          <h2 className="print-qr-modal__title">In nhãn QR cho cá thể tài sản</h2>
        </div>

        <div className="print-qr-modal__body">
          {loading && (
            <div className="print-qr-loading">Đang tạo mã QR...</div>
          )}

          {!loading && labels.length === 0 && (
            <div className="print-qr-empty">Không có cá thể tài sản nào để in.</div>
          )}

          {!loading && labels.length > 0 && (
            <>
              <div className="print-qr-info">
                <p style={{ fontSize: '14px', color: '#6b7280', marginBottom: '12px' }}>
                  Sẵn sàng in {labels.length} nhãn QR. Nhấn "In nhãn" để bắt đầu in.
                </p>
                <div style={{ padding: '12px', background: '#f9fafb', borderRadius: '8px', border: '1px solid #e5e7eb' }}>
                  <div style={{ fontSize: '13px', color: '#6b7280', marginBottom: '8px' }}>
                    <strong>Lưu ý:</strong>
                  </div>
                  <ul style={{ margin: 0, paddingLeft: '20px', fontSize: '13px', color: '#6b7280' }}>
                    <li>Mỗi nhãn chứa: Mã cá thể, Serial number, Tên tài sản, QR code</li>
                    <li>Định dạng: 4 nhãn/trang A4 hoặc giấy nhãn dán</li>
                    <li>Khuyến nghị: In trên giấy nhãn dán để dễ dán lên tài sản</li>
                  </ul>
                </div>
              </div>

              <div className="print-qr-labels-grid" id="print-area">
                {labels.map((label) => (
                  <div key={label.assetInstanceId} className="print-qr-label">
                    <div className="print-qr-label-header">
                      <div className="print-qr-label-title">{label.assetName || 'Tài sản'}</div>
                    </div>
                    <div className="print-qr-label-body">
                      <img
                        src={label.qrDataUrl}
                        alt={`QR ${label.instanceCode}`}
                        className="print-qr-image"
                      />
                      <div className="print-qr-label-info">
                        <div className="print-qr-label-row">
                          <span className="print-qr-label-key">Mã cá thể:</span>
                          <span className="print-qr-label-value">{label.instanceCode}</span>
                        </div>
                        {label.serialNumber && (
                          <div className="print-qr-label-row">
                            <span className="print-qr-label-key">Serial:</span>
                            <span className="print-qr-label-value">{label.serialNumber}</span>
                          </div>
                        )}
                      </div>
                    </div>
                  </div>
                ))}
              </div>
            </>
          )}
        </div>

        <div className="print-qr-modal__footer">
          <button
            type="button"
            onClick={handlePrint}
            className="print-qr-btn-print"
            disabled={loading || labels.length === 0}
          >
            🖨️ In nhãn
          </button>
          <button type="button" onClick={onClose} className="print-qr-btn-close">
            Đóng
          </button>
        </div>
      </div>
    </div>
  );
}

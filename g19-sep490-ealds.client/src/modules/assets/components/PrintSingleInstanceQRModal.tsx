import { useState, useEffect } from 'react';
import { message } from 'antd';
import QRCode from 'qrcode';
import { assetInstanceService } from '../services/assetService';
// Tái sử dụng CSS từ module purchase-orders để đảm bảo giao diện nhất quán
import '../../purchase-orders/components/PrintQRLabelsModal.css';

interface PrintSingleInstanceQRModalProps {
  open: boolean;
  onClose: () => void;
  /** ID của cá thể tài sản cần in QR */
  assetInstanceId: number | null;
}

interface InstanceLabel {
  assetInstanceId: number;
  instanceCode: string;
  serialNumber: string | null;
  assetName: string | null;
  qrDataUrl: string;
}

/** Tạo URL tuyệt đối để khi quét QR sẽ mở trang chi tiết cá thể tài sản */
function buildQrUrl(instanceId: number): string {
  return new URL(`/asset-instances/${instanceId}`, window.location.origin).href;
}

export function PrintSingleInstanceQRModal({
  open,
  onClose,
  assetInstanceId,
}: PrintSingleInstanceQRModalProps) {
  const [loading, setLoading] = useState(false);
  const [label, setLabel] = useState<InstanceLabel | null>(null);

  useEffect(() => {
    if (!open || !assetInstanceId) {
      setLabel(null);
      return;
    }

    const loadLabel = async () => {
      setLoading(true);
      try {
        const instance = await assetInstanceService.getById(assetInstanceId);
        const qrData = buildQrUrl(assetInstanceId);
        const qrDataUrl = await QRCode.toDataURL(qrData, {
          width: 200,
          margin: 1,
          color: { dark: '#000000', light: '#FFFFFF' },
        });

        setLabel({
          assetInstanceId,
          instanceCode: instance.instanceCode ?? String(assetInstanceId),
          serialNumber: instance.serialNumber ?? null,
          assetName: instance.assetName ?? instance.assetCode ?? null,
          qrDataUrl,
        });
      } catch {
        message.error('Không tải được thông tin cá thể tài sản để in QR.');
        setLabel(null);
      } finally {
        setLoading(false);
      }
    };

    loadLabel();
  }, [open, assetInstanceId]);

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
          <h2 className="print-qr-modal__title">In nhãn QR — Cá thể tài sản</h2>
        </div>

        <div className="print-qr-modal__body">
          {loading && (
            <div className="print-qr-loading">Đang tạo mã QR...</div>
          )}

          {!loading && !label && (
            <div className="print-qr-empty">Không tải được thông tin cá thể tài sản.</div>
          )}

          {!loading && label && (
            <>
              <div className="print-qr-info">
                <p style={{ fontSize: '14px', color: '#6b7280', marginBottom: '12px' }}>
                  Sẵn sàng in nhãn QR. Khi quét mã, trình duyệt sẽ mở trang chi tiết cá thể này.
                </p>
              </div>

              {/* Khu vực in — chỉ in phần này khi gọi window.print() */}
              <div className="print-qr-labels-grid" id="print-area">
                <div className="print-qr-label">
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
              </div>
            </>
          )}
        </div>

        <div className="print-qr-modal__footer">
          <button
            type="button"
            onClick={() => window.print()}
            className="print-qr-btn-print"
            disabled={loading || !label}
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

import { useEffect, useState } from 'react';
import './MaintenanceProposalModal.css';

export interface AssetInfo {
  code: string;
  name: string;
  type: string;
  specification: string;
  purchaseDate: string;
  warrantyExpiry: string;
  currentValue: string;
  remainingValue: string;
  location: string;
  status: string;
  admissionDate: string;
  department: string;
}

interface MaintenanceProposalModalProps {
  open: boolean;
  onClose: () => void;
  onSubmit: (values: {
    assetInstanceId: number;
    recordNumber?: string;
    maintenanceContent: string;
  }) => void;
  assetInfo: AssetInfo | null;
  assetInstanceId: number | null;
}

export function MaintenanceProposalModal({
  open,
  onClose,
  onSubmit,
  assetInfo,
  assetInstanceId,
}: MaintenanceProposalModalProps) {
  const [recordNumber, setRecordNumber] = useState<string>('');
  const [maintenanceContent, setMaintenanceContent] = useState<string>('Hỏng nhẹ');
  const [maintenanceError, setMaintenanceError] = useState<string | null>(null);

  useEffect(() => {
    if (!open) return;
    const datePart = new Date().toISOString().slice(0, 10).replace(/-/g, '');
    const randomPart = Math.floor(Math.random() * 900 + 100);
    setRecordNumber(`BB-BD-${datePart}-${randomPart}`);
  }, [open]);

  if (!open || !assetInfo || assetInstanceId == null) return null;

  const handleSubmit = () => {
    if (!maintenanceContent.trim()) {
      setMaintenanceError('Vui lòng nhập nội dung bảo dưỡng');
      return;
    }

    onSubmit({
      assetInstanceId,
      recordNumber: recordNumber.trim() || undefined,
      maintenanceContent: maintenanceContent.trim(),
    });
  };

  return (
    <div className="maintenance-proposal-overlay" role="dialog" aria-modal="true">
      <div className="maintenance-proposal-modal">
        <button
          type="button"
          className="maintenance-proposal-modal__close-btn"
          onClick={onClose}
          aria-label="Đóng"
        >
          <span className="maintenance-proposal-modal__close">×</span>
        </button>

        <div className="maintenance-proposal-modal__header">
          <h2 className="maintenance-proposal-modal__title">
            Gửi đề xuất bảo dưỡng máy móc
          </h2>
        </div>

        <div className="maintenance-proposal-modal__body">
          <div className="maintenance-proposal-modal__content">
            <div className="maintenance-proposal-form__item">
              <label htmlFor="maintenance-record-number">Số biên bản</label>
              <input
                id="maintenance-record-number"
                type="text"
                className="maintenance-proposal-input"
                value={recordNumber}
                readOnly
              />
            </div>

          <div className="maintenance-proposal-info-section">
            <h3 className="maintenance-proposal-section-title">Thông tin tài sản</h3>
            <div className="maintenance-proposal-info-grid">
              <div className="maintenance-proposal-info-row">
                <div className="maintenance-proposal-info-item">
                  <label>Mã cá thể</label>
                  <div className="maintenance-proposal-info-value">{assetInfo.code}</div>
                </div>
                <div className="maintenance-proposal-info-item">
                  <label>Giá trị tài sản</label>
                  <div className="maintenance-proposal-info-value">{assetInfo.currentValue}</div>
                </div>
              </div>
              <div className="maintenance-proposal-info-row">
                <div className="maintenance-proposal-info-item">
                  <label>Tên tài sản</label>
                  <div className="maintenance-proposal-info-value">{assetInfo.name}</div>
                </div>
                <div className="maintenance-proposal-info-item">
                  <label>Giá trị còn lại</label>
                  <div className="maintenance-proposal-info-value">{assetInfo.remainingValue}</div>
                </div>
              </div>
              <div className="maintenance-proposal-info-row">
                <div className="maintenance-proposal-info-item">
                  <label>Loại tài sản</label>
                  <div className="maintenance-proposal-info-value">{assetInfo.type}</div>
                </div>
                <div className="maintenance-proposal-info-item">
                  <label>Vị trí tài sản</label>
                  <div className="maintenance-proposal-info-value">{assetInfo.location}</div>
                </div>
              </div>
              <div className="maintenance-proposal-info-row">
                <div className="maintenance-proposal-info-item">
                  <label>Quy cách tài sản</label>
                  <div className="maintenance-proposal-info-value">{assetInfo.specification}</div>
                </div>
                <div className="maintenance-proposal-info-item">
                  <label>Tình trạng</label>
                  <div className="maintenance-proposal-info-value">{assetInfo.status}</div>
                </div>
              </div>
              <div className="maintenance-proposal-info-row">
                <div className="maintenance-proposal-info-item">
                  <label>Ngày mua</label>
                  <div className="maintenance-proposal-info-value">{assetInfo.purchaseDate}</div>
                </div>
                <div className="maintenance-proposal-info-item">
                  <label>Ngày đưa vào SD</label>
                  <div className="maintenance-proposal-info-value">{assetInfo.admissionDate}</div>
                </div>
              </div>
              <div className="maintenance-proposal-info-row">
                <div className="maintenance-proposal-info-item">
                  <label>Hạn bảo hành</label>
                  <div className="maintenance-proposal-info-value">{assetInfo.warrantyExpiry}</div>
                </div>
                <div className="maintenance-proposal-info-item">
                  <label>Phòng ban SD</label>
                  <div className="maintenance-proposal-info-value">{assetInfo.department}</div>
                </div>
              </div>
            </div>
          </div>

          <div className="maintenance-proposal-form-section">
            <h3 className="maintenance-proposal-section-title">Thông tin bảo dưỡng</h3>
            <div className="maintenance-proposal-form__item">
              <label htmlFor="maintenance-content">
                Nội dung bảo dưỡng<span className="maintenance-proposal-required">*</span>
              </label>
              <textarea
                id="maintenance-content"
                className="maintenance-proposal-textarea"
                rows={4}
                placeholder="Mô tả nội dung cần bảo dưỡng..."
                value={maintenanceContent}
                onChange={(e) => {
                  setMaintenanceContent(e.target.value);
                  setMaintenanceError(null);
                }}
              />
              {maintenanceError && (
                <div className="maintenance-proposal-error-text">{maintenanceError}</div>
              )}
            </div>
          </div>
        </div>

        
      </div>
      <div className="maintenance-proposal-modal__footer">
          <button
            type="button"
            onClick={handleSubmit}
            className="maintenance-proposal-btn-submit"
          >
            Gửi yêu cầu
          </button>
          <button
            type="button"
            onClick={onClose}
            className="maintenance-proposal-btn-cancel"
          >
            Hủy
          </button>
        </div>
    </div>
  </div>
  );
}

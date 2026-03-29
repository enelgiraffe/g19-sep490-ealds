import { useState } from 'react';
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

interface AttachmentItem {
  id: string;
  name: string;
}

export function MaintenanceProposalModal({
  open,
  onClose,
  onSubmit,
  assetInfo,
  assetInstanceId,
}: MaintenanceProposalModalProps) {
  const [attachments, setAttachments] = useState<AttachmentItem[]>([
    { id: '1', name: 'Thông tin máy' },
    { id: '2', name: 'Thông tin nhà cung cấp' },
  ]);
  const [recordNumber, setRecordNumber] = useState<string>('BA001');
  const [maintenanceContent, setMaintenanceContent] = useState<string>('Hỏng nhẹ');
  const [maintenanceError, setMaintenanceError] = useState<string | null>(null);

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

  const handleAddAttachment = () => {
    setAttachments((prev) => [
      ...prev,
      { id: String(Date.now()), name: `File đính kèm #${prev.length + 1}` },
    ]);
  };

  const handleRemoveAttachment = (id: string) => {
    setAttachments((prev) => prev.filter((a) => a.id !== id));
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
                placeholder="BA001"
                value={recordNumber}
                onChange={(e) => setRecordNumber(e.target.value)}
              />
            </div>

          <div className="maintenance-proposal-info-section">
            <h3 className="maintenance-proposal-section-title">Thông tin tài sản</h3>
            <div className="maintenance-proposal-info-grid">
              <div className="maintenance-proposal-info-row">
                <div className="maintenance-proposal-info-item">
                  <label>Mã tài sản</label>
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

            <div className="maintenance-proposal-attachments-section">
              <h4 className="maintenance-proposal-attachments-title">Tài liệu đính kèm</h4>
              <div className="maintenance-proposal-attachments-list">
                {attachments.map((att) => (
                  <div key={att.id} className="maintenance-proposal-attachment-item">
                    <span className="maintenance-proposal-attachment-number">
                      #{attachments.indexOf(att) + 1}
                    </span>
                    <span className="maintenance-proposal-attachment-name">{att.name}</span>
                    <div className="maintenance-proposal-attachment-actions">
                      <button type="button" className="maintenance-proposal-attachment-btn">
                        ✏
                      </button>
                      <button
                        type="button"
                        className="maintenance-proposal-attachment-btn maintenance-proposal-attachment-btn--danger"
                        onClick={() => handleRemoveAttachment(att.id)}
                      >
                        🗑
                      </button>
                    </div>
                  </div>
                ))}
              </div>
              <button
                type="button"
                className="maintenance-proposal-btn-upload"
                onClick={handleAddAttachment}
              >
                <span className="maintenance-proposal-btn-upload-icon">+</span>
                <span>Thêm file đính kèm</span>
              </button>
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

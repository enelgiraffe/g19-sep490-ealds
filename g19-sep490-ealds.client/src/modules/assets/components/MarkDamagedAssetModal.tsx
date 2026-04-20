import { useEffect, useState } from 'react';
import './MarkDamagedAssetModal.css';

interface AssetInfo {
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

interface MarkDamagedAssetModalProps {
  open: boolean;
  onClose: () => void;
  onSubmit: (values: { damageDate: string; condition: string }) => void;
  assetInfo: AssetInfo | null;
}

export function MarkDamagedAssetModal({
  open,
  onClose,
  onSubmit,
  assetInfo,
}: MarkDamagedAssetModalProps) {
  const isMeaningfulCondition = (value: string): boolean => {
    const normalized = value.trim();
    if (!normalized) return false;
    // Reject placeholder-like inputs such as "-", "--", "..."
    if (/^[-.\s]+$/.test(normalized)) return false;
    return true;
  };

  const [damageDate, setDamageDate] = useState<string>('');
  const [recordNumber, setRecordNumber] = useState<string>('');
  const [condition, setCondition] = useState<string>('');
  const [dateError, setDateError] = useState<string | null>(null);
  const [conditionError, setConditionError] = useState<string | null>(null);

  useEffect(() => {
    if (open) {
      const today = new Date().toISOString().slice(0, 10);
      setDamageDate(today);
      const datePart = today.replace(/-/g, '');
      const randomPart = Math.floor(Math.random() * 900 + 100);
      setRecordNumber(`BB-HONG-${datePart}-${randomPart}`);
      setCondition('');
      setDateError(null);
      setConditionError(null);
    }
  }, [open]);

  if (!open || !assetInfo) return null;

  const handleClose = () => {
    onClose();
  };

  const handleSubmit = () => {
    if (!damageDate) {
      setDateError('Vui lòng chọn ngày hỏng');
      return;
    }
    if (!isMeaningfulCondition(condition)) {
      setConditionError('Vui lòng nhập tình trạng hỏng hợp lệ (không để "-", "...")');
      return;
    }

    onSubmit({
      damageDate,
      condition: condition.trim(),
    });
  };

  return (
    <div className="mark-damaged-modal-overlay" role="dialog" aria-modal="true">
      <div className="mark-damaged-modal">
        <button
          type="button"
          className="mark-damaged-modal__close-btn"
          onClick={handleClose}
          aria-label="Đóng"
        >
          <span className="mark-damaged-modal__close">×</span>
        </button>

        <div className="mark-damaged-modal__header">
          <h2 className="mark-damaged-modal__title">Đánh dấu hỏng tài sản</h2>
        </div>

        <div className="mark-damaged-modal__body">
          <div className="mark-damaged-modal__content">
            <div className="mark-damaged-form__item">
              <label htmlFor="mark-damaged-record-number">Số biên bản</label>
              <input
                id="mark-damaged-record-number"
                type="text"
                value={recordNumber}
                readOnly
                className="mark-damaged-input--disabled"
              />
            </div>

            <div className="mark-damaged-info-section">
              <h3 className="mark-damaged-section-title">Thông tin tài sản</h3>
              
              <div className="mark-damaged-info-grid">
                <div className="mark-damaged-info-row">
                  <div className="mark-damaged-info-item">
                    <label>Mã cá thể</label>
                    <div className="mark-damaged-info-value">{assetInfo.code}</div>
                  </div>
                  <div className="mark-damaged-info-item">
                    <label>Giá trị tài sản</label>
                    <div className="mark-damaged-info-value">{assetInfo.currentValue}</div>
                  </div>
                </div>

                <div className="mark-damaged-info-row">
                  <div className="mark-damaged-info-item">
                    <label>Tên tài sản</label>
                    <div className="mark-damaged-info-value">{assetInfo.name}</div>
                  </div>
                  <div className="mark-damaged-info-item">
                    <label>Giá trị còn lại</label>
                    <div className="mark-damaged-info-value">{assetInfo.remainingValue}</div>
                  </div>
                </div>

                <div className="mark-damaged-info-row">
                  <div className="mark-damaged-info-item">
                    <label>Loại tài sản</label>
                    <div className="mark-damaged-info-value">{assetInfo.type}</div>
                  </div>
                  <div className="mark-damaged-info-item">
                    <label>Vị trí tài sản</label>
                    <div className="mark-damaged-info-value">{assetInfo.location}</div>
                  </div>
                </div>

                <div className="mark-damaged-info-row">
                  <div className="mark-damaged-info-item">
                    <label>Quy cách tài sản</label>
                    <div className="mark-damaged-info-value">{assetInfo.specification}</div>
                  </div>
                  <div className="mark-damaged-info-item">
                    <label>Tình trạng</label>
                    <div className="mark-damaged-info-value">{assetInfo.status}</div>
                  </div>
                </div>

                <div className="mark-damaged-info-row">
                  <div className="mark-damaged-info-item">
                    <label>Ngày mua</label>
                    <div className="mark-damaged-info-value">{assetInfo.purchaseDate}</div>
                  </div>
                  <div className="mark-damaged-info-item">
                    <label>Ngày đưa vào SD</label>
                    <div className="mark-damaged-info-value">{assetInfo.admissionDate}</div>
                  </div>
                </div>

                <div className="mark-damaged-info-row">
                  <div className="mark-damaged-info-item">
                    <label>Hạn bảo hành</label>
                    <div className="mark-damaged-info-value">{assetInfo.warrantyExpiry}</div>
                  </div>
                  <div className="mark-damaged-info-item">
                    <label>Phòng ban SD</label>
                    <div className="mark-damaged-info-value">{assetInfo.department}</div>
                  </div>
                </div>
              </div>
            </div>

            <div className="mark-damaged-form-section">
              <h3 className="mark-damaged-section-title">Thông tin ghi nhận tài sản hỏng</h3>
              
              <div className="mark-damaged-form__item">
                <label htmlFor="mark-damaged-date">
                  Ngày hỏng<span style={{ color: '#ef4444' }}>*</span>
                </label>
                <input
                  id="mark-damaged-date"
                  type="date"
                  className="mark-damaged-input"
                  value={damageDate}
                  onChange={(e) => {
                    setDamageDate(e.target.value);
                    setDateError(null);
                  }}
                />
                {dateError && <div className="mark-damaged-error-text">{dateError}</div>}
              </div>

              <div className="mark-damaged-form__item">
                <label htmlFor="mark-damaged-condition">
                  Tình trạng<span style={{ color: '#ef4444' }}>*</span>
                </label>
                <textarea
                  id="mark-damaged-condition"
                  className="mark-damaged-textarea"
                  rows={4}
                  placeholder="-"
                  value={condition}
                  onChange={(e) => {
                    setCondition(e.target.value);
                    setConditionError(null);
                  }}
                />
                {conditionError && <div className="mark-damaged-error-text">{conditionError}</div>}
              </div>

            </div>
          </div>
        </div>

        <div className="mark-damaged-modal__footer">
          <button
            type="button"
            onClick={handleSubmit}
            className="mark-damaged-btn-submit"
          >
            Gửi
          </button>
          <button
            type="button"
            onClick={handleClose}
            className="mark-damaged-btn-draft"
          >
            Nháp
          </button>
        </div>
      </div>
    </div>
  );
}

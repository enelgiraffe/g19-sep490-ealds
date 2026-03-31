import { useEffect, useState } from 'react';
import './LiquidationRequestModal.css';

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

interface LiquidationRequestModalProps {
  open: boolean;
  onClose: () => void;
  onSubmit: (values: {
    liquidationDate: Date | null;
    reason?: string;
    disposalMethod?: string;
    notes?: string;
  }) => void;
  assetInfo: AssetInfo | null;
}

export function LiquidationRequestModal({ open, onClose, onSubmit, assetInfo }: LiquidationRequestModalProps) {
  const [liquidationDate, setLiquidationDate] = useState<string>('');
  const [recordNumber, setRecordNumber] = useState<string>('');
  const [reason, setReason] = useState<string>('');
  const [disposalMethod, setDisposalMethod] = useState<string>('');
  const [notes, setNotes] = useState<string>('');
  const [dateError, setDateError] = useState<string | null>(null);

  useEffect(() => {
    if (open) {
      const today = new Date().toISOString().slice(0, 10);
      setLiquidationDate(today);
      const datePart = today.replace(/-/g, '');
      const randomPart = Math.floor(Math.random() * 900 + 100);
      setRecordNumber(`BB-TL-${datePart}-${randomPart}`);
      setReason('');
      setDisposalMethod('');
      setNotes('');
      setDateError(null);
    }
  }, [open]);

  if (!open || !assetInfo) return null;

  const handleSubmit = () => {
    if (!liquidationDate) {
      setDateError('Vui lòng chọn ngày');
      return;
    }

    const dateValue = liquidationDate ? new Date(liquidationDate) : null;

    onSubmit({
      liquidationDate: dateValue,
      reason: reason.trim() || undefined,
      disposalMethod: disposalMethod.trim() || undefined,
      notes: notes.trim() || undefined,
    });
    onClose();
  };

  return (
    <div className="liquidation-modal-overlay" role="dialog" aria-modal="true">
      <div className="liquidation-modal">
        <button
          type="button"
          className="liquidation-modal__close-btn"
          onClick={onClose}
          aria-label="Đóng"
        >
          <span className="liquidation-modal__close">×</span>
        </button>

        <div className="liquidation-modal__header">
          <h2 className="liquidation-modal__title">Đơn đề nghị thanh lý</h2>
        </div>

        <div className="liquidation-modal__body">
          <div className="liquidation-modal__content">
            <div className="liquidation-form__item">
              <label htmlFor="liquidation-record-number">Số biên bản</label>
              <input
                id="liquidation-record-number"
                type="text"
                value={recordNumber}
                readOnly
                className="liquidation-input--disabled"
              />
            </div>

            <div className="liquidation-info-section">
              <h3 className="liquidation-section-title">Thông tin tài sản</h3>
              
              <div className="liquidation-info-grid">
                <div className="liquidation-info-row">
                  <div className="liquidation-info-item">
                    <label>Mã cá thể</label>
                    <div className="liquidation-info-value">{assetInfo.code}</div>
                  </div>
                  <div className="liquidation-info-item">
                    <label>Giá trị tài sản</label>
                    <div className="liquidation-info-value">{assetInfo.currentValue}</div>
                  </div>
                </div>

                <div className="liquidation-info-row">
                  <div className="liquidation-info-item">
                    <label>Tên tài sản</label>
                    <div className="liquidation-info-value">{assetInfo.name}</div>
                  </div>
                  <div className="liquidation-info-item">
                    <label>Giá trị còn lại</label>
                    <div className="liquidation-info-value">{assetInfo.remainingValue}</div>
                  </div>
                </div>

                <div className="liquidation-info-row">
                  <div className="liquidation-info-item">
                    <label>Loại tài sản</label>
                    <div className="liquidation-info-value">{assetInfo.type}</div>
                  </div>
                  <div className="liquidation-info-item">
                    <label>Vị trí tài sản</label>
                    <div className="liquidation-info-value">{assetInfo.location}</div>
                  </div>
                </div>

                <div className="liquidation-info-row">
                  <div className="liquidation-info-item">
                    <label>Quy cách tài sản</label>
                    <div className="liquidation-info-value">{assetInfo.specification}</div>
                  </div>
                  <div className="liquidation-info-item">
                    <label>Tình trạng</label>
                    <div className="liquidation-info-value">
                      {assetInfo.status === 'Đang sử dụng' ? 'Đang hỏng' : assetInfo.status}
                    </div>
                  </div>
                </div>

                <div className="liquidation-info-row">
                  <div className="liquidation-info-item">
                    <label>Ngày mua</label>
                    <div className="liquidation-info-value">{assetInfo.purchaseDate}</div>
                  </div>
                  <div className="liquidation-info-item">
                    <label>Ngày đưa vào SD</label>
                    <div className="liquidation-info-value">{assetInfo.admissionDate}</div>
                  </div>
                </div>

                <div className="liquidation-info-row">
                  <div className="liquidation-info-item">
                    <label>Hạn bảo hành</label>
                    <div className="liquidation-info-value">{assetInfo.warrantyExpiry}</div>
                  </div>
                  <div className="liquidation-info-item">
                    <label>Phòng ban SD</label>
                    <div className="liquidation-info-value">{assetInfo.department}</div>
                  </div>
                </div>
              </div>
            </div>

            <div className="liquidation-form-section">
              <h3 className="liquidation-section-title">Thông tin đề nghị thanh lý</h3>
              
              <div className="liquidation-form-row">
                <div className="liquidation-form-col">
                  <label htmlFor="liquidation-date">
                    Ngày đề nghị thanh lý<span className="liquidation-required">*</span>
                  </label>
                  <input
                    id="liquidation-date"
                    type="date"
                    className="liquidation-input"
                    value={liquidationDate}
                    onChange={(e) => {
                      setLiquidationDate(e.target.value);
                      setDateError(null);
                    }}
                  />
                  {dateError && <div className="liquidation-error-text">{dateError}</div>}
                </div>

                <div className="liquidation-form-col">
                  <label htmlFor="liquidation-reason">Lý do thanh lý</label>
                  <textarea
                    id="liquidation-reason"
                    className="liquidation-textarea"
                    rows={1}
                    placeholder="-"
                    value={reason}
                    onChange={(e) => setReason(e.target.value)}
                  />
                </div>
              </div>

              <div className="liquidation-form-row">
                <div className="liquidation-form-col">
                  <label htmlFor="liquidation-disposal-method">Phương án xử lý</label>
                  <input
                    id="liquidation-disposal-method"
                    className="liquidation-input"
                    placeholder="-"
                    value={disposalMethod}
                    onChange={(e) => setDisposalMethod(e.target.value)}
                  />
                </div>

                <div className="liquidation-form-col">
                  <label htmlFor="liquidation-notes">Ghi chú</label>
                  <input
                    id="liquidation-notes"
                    className="liquidation-input"
                    placeholder="-"
                    value={notes}
                    onChange={(e) => setNotes(e.target.value)}
                  />
                </div>
              </div>

            </div>
          </div>
        </div>

        <div className="liquidation-modal__footer">
          <button
            type="button"
            onClick={handleSubmit}
            className="liquidation-btn-submit"
          >
            Gửi yêu cầu
          </button>
          <button
            type="button"
            onClick={onClose}
            className="liquidation-btn-draft"
          >
            Nháp
          </button>
        </div>
      </div>
    </div>
  );
}

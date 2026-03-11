import { useEffect, useState } from 'react';
import { transferRequestService, type AssetLocationOption } from '../services/transferRequestService';
import './TransferAssetModal.css';

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

interface TransferAssetModalProps {
  open: boolean;
  onClose: () => void;
  onSubmit: (values: any) => void;
  assetInfo: AssetInfo | null;
  fromDepartmentId?: number | null;
}

export function TransferAssetModal({
  open,
  onClose,
  onSubmit,
  assetInfo,
  fromDepartmentId,
}: TransferAssetModalProps) {
  const [locations, setLocations] = useState<AssetLocationOption[]>([]);
  const [locationsLoading, setLocationsLoading] = useState(false);
  const [transferDate, setTransferDate] = useState<string>('');
  const [reason, setReason] = useState<string>('');
  const [fromLocationId, setFromLocationId] = useState<string>('');
  const [toLocationId, setToLocationId] = useState<string>('');
  const [dateError, setDateError] = useState<string | null>(null);
  const [fromError, setFromError] = useState<string | null>(null);
  const [toError, setToError] = useState<string | null>(null);

  useEffect(() => {
    if (open) {
      setLocationsLoading(true);
      transferRequestService
        .getAssetLocations()
        .then(setLocations)
        .catch(() => setLocations([]))
        .finally(() => setLocationsLoading(false));
    }
  }, [open]);

  useEffect(() => {
    if (open) {
      const today = new Date().toISOString().slice(0, 10);
      setTransferDate(today);
      setReason('');
      setDateError(null);
      setFromError(null);
      setToError(null);
      if (fromDepartmentId != null) {
        setFromLocationId(String(fromDepartmentId));
      } else {
        setFromLocationId('');
      }
      setToLocationId('');
    }
  }, [open, fromDepartmentId]);

  const handleSubmit = () => {
    let hasError = false;
    if (!transferDate) {
      setDateError('Vui lòng chọn ngày điều chuyển');
      hasError = true;
    }
    if (!fromLocationId) {
      setFromError('Chọn vị trí nguồn');
      hasError = true;
    }
    if (!toLocationId) {
      setToError('Chọn vị trí đích');
      hasError = true;
    }
    if (hasError) return;

    const transferDateValue = transferDate ? new Date(transferDate) : undefined;

    onSubmit({
      transferDate: transferDateValue,
      reason: reason.trim() || undefined,
      fromLocationId,
      toLocationId,
    });
    onClose();
  };

  if (!open) return null;

  return (
    <div className="transfer-modal-overlay" role="dialog" aria-modal="true">
      <div className="transfer-modal">
        <button
          type="button"
          className="transfer-modal__close-btn"
          onClick={onClose}
          aria-label="Đóng"
        >
          <span className="transfer-modal__close">×</span>
        </button>

        <div className="transfer-modal__header">
          <h2 className="transfer-modal__title">Yêu cầu điều chuyển</h2>
        </div>

        <div className="transfer-modal__body">
          <div className="transfer-form__section">
            <h3 className="transfer-section-title">Thông tin chung</h3>

            <div className="transfer-form__row">
              <div className="transfer-form__item">
                <label htmlFor="transfer-record-number">Số biên bản</label>
                <input
                  id="transfer-record-number"
                  type="text"
                  value="-"
                  disabled
                  className="transfer-input transfer-input--disabled"
                />
              </div>
              <div className="transfer-form__item">
                <label htmlFor="transfer-date">
                  Ngày điều chuyển<span className="transfer-required">*</span>
                </label>
                <input
                  id="transfer-date"
                  type="date"
                  className="transfer-input"
                  value={transferDate}
                  onChange={(e) => {
                    setTransferDate(e.target.value);
                    setDateError(null);
                  }}
                />
                {dateError && <div className="transfer-error-text">{dateError}</div>}
              </div>
            </div>

            <div className="transfer-form__item transfer-form__item--full">
              <label htmlFor="transfer-reason">Lý do điều chuyển</label>
              <textarea
                id="transfer-reason"
                className="transfer-textarea"
                rows={3}
                placeholder="Nhập lý do điều chuyển"
                value={reason}
                onChange={(e) => setReason(e.target.value)}
              />
            </div>
          </div>

          <div className="transfer-form__section transfer-form__section--locations">
            <h3 className="transfer-section-title">Điều chuyển</h3>
            <div className="transfer-form__row">
              <div className="transfer-form__item">
                <label htmlFor="transfer-from-location">
                  Từ vị trí<span className="transfer-required">*</span>
                </label>
                <select
                  id="transfer-from-location"
                  className="transfer-select"
                  value={fromLocationId}
                  disabled={locationsLoading}
                  onChange={(e) => {
                    setFromLocationId(e.target.value);
                    setFromError(null);
                  }}
                >
                  <option value="">Chọn vị trí hiện tại</option>
                  {locations.map((loc) => (
                    <option key={loc.locationId} value={loc.locationId}>
                      {loc.displayName}
                    </option>
                  ))}
                </select>
                {fromError && <div className="transfer-error-text">{fromError}</div>}
              </div>

              <div className="transfer-form__item">
                <label htmlFor="transfer-to-location">
                  Đến vị trí<span className="transfer-required">*</span>
                </label>
                <select
                  id="transfer-to-location"
                  className="transfer-select"
                  value={toLocationId}
                  disabled={locationsLoading}
                  onChange={(e) => {
                    setToLocationId(e.target.value);
                    setToError(null);
                  }}
                >
                  <option value="">Chọn vị trí chuyển đến</option>
                  {locations.map((loc) => (
                    <option key={loc.locationId} value={loc.locationId}>
                      {loc.displayName}
                    </option>
                  ))}
                </select>
                {toError && <div className="transfer-error-text">{toError}</div>}
              </div>
            </div>
          </div>

          <div className="transfer-form__section">
            <h3 className="transfer-section-title">Tài sản được chuyển</h3>
            {assetInfo ? (
              <div className="transfer-asset-table">
                <table>
                  <thead>
                    <tr>
                      <th>STT</th>
                      <th>Mã tài sản</th>
                      <th>Tài sản</th>
                      <th>Vị trí tài sản</th>
                      <th>Tình trạng</th>
                      <th>Số lượng</th>
                      <th>Phòng ban sử dụng</th>
                    </tr>
                  </thead>
                  <tbody>
                    <tr>
                      <td>1</td>
                      <td>{assetInfo.code}</td>
                      <td>{assetInfo.name}</td>
                      <td>{assetInfo.location}</td>
                      <td>{assetInfo.status}</td>
                      <td>1</td>
                      <td>{assetInfo.department}</td>
                    </tr>
                  </tbody>
                </table>
              </div>
            ) : (
              <p className="transfer-asset-placeholder">
                Vui lòng chọn tài sản từ danh sách tài sản để hiển thị thông tin chi tiết.
              </p>
            )}
          </div>

          <div className="transfer-form__section">
            <h3 className="transfer-section-title">Tài liệu đính kèm</h3>
            <div className="transfer-attachments">
              <div className="transfer-attachment-item">
                <span>#1</span>
                <span>Thông tin máy</span>
              </div>
              <div className="transfer-attachment-item">
                <span>#2</span>
                <span>Thông tin nhà cung cấp</span>
              </div>
            </div>
          </div>
        </div>

        <div className="transfer-modal__footer">
          <button
            type="button"
            onClick={handleSubmit}
            className="transfer-btn-submit"
          >
            Gửi yêu cầu
          </button>
          <button
            type="button"
            onClick={onClose}
            className="transfer-btn-cancel"
          >
            Nháp
          </button>
        </div>
      </div>
    </div>
  );
}


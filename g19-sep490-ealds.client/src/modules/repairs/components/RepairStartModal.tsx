import { memo, useEffect, useMemo, useState } from 'react';
import dayjs from 'dayjs';
import type { AssetResponse } from '../../assets/services/assetService';
import './RepairStartModal.css';

interface RepairStartRow {
  assetCode: string;
  assetName: string;
  condition: string;
  location: string;
  department: string;
}

export interface RepairStartFormValues {
  reportNumber: string;
  damageDate: string;
  damageCondition: string;
  repairDate: string;
  expectedCompletionDate?: string;
  estimatedCost?: number;
  repairProgressStatus: string;
}

interface RepairStartModalProps {
  open: boolean;
  loading: boolean;
  submitting: boolean;
  row: RepairStartRow | null;
  asset: AssetResponse | null;
  onClose: () => void;
  onSubmit: (values: RepairStartFormValues) => void;
}

function toDisplayDate(value?: string | null): string {
  if (!value) return '-';
  const d = new Date(value);
  if (Number.isNaN(d.getTime())) return value;
  return d.toLocaleDateString('vi-VN');
}

function formatVndValue(value?: number | null): string {
  if (value == null) return '-';
  return `${value.toLocaleString('vi-VN')} ₫`;
}

function toInputDate(value?: string | null): string {
  if (!value) return '';
  const d = new Date(value);
  if (Number.isNaN(d.getTime())) return '';
  const yyyy = d.getFullYear();
  const mm = String(d.getMonth() + 1).padStart(2, '0');
  const dd = String(d.getDate()).padStart(2, '0');
  return `${yyyy}-${mm}-${dd}`;
}

function parseNumberInput(value: string): number | undefined {
  const normalized = value.replace(/[^\d]/g, '');
  if (!normalized) return undefined;
  const parsed = Number(normalized);
  return Number.isFinite(parsed) ? parsed : undefined;
}

function toIsoDateOrUndefined(value: string): string | undefined {
  if (!value) return undefined;
  return new Date(`${value}T00:00:00`).toISOString();
}

function toIsoDate(value: string): string {
  return new Date(`${value}T00:00:00`).toISOString();
}

function RepairStartModalInner({
  open,
  loading,
  submitting,
  row,
  asset,
  onClose,
  onSubmit,
}: RepairStartModalProps) {
  const [reportNumber, setReportNumber] = useState('');
  const [damageDate, setDamageDate] = useState('');
  const [damageCondition, setDamageCondition] = useState('');
  const [repairDate, setRepairDate] = useState('');
  const [expectedCompletionDate, setExpectedCompletionDate] = useState('');
  const [estimatedCostInput, setEstimatedCostInput] = useState('');
  const [repairProgressStatus, setRepairProgressStatus] = useState('');

  useEffect(() => {
    if (!open || !row) return;
    const today = dayjs().format('YYYY-MM-DD');
    setReportNumber('');
    setDamageDate(today);
    setDamageCondition(row.condition || '');
    setRepairDate(today);
    setExpectedCompletionDate('');
    setEstimatedCostInput('');
    setRepairProgressStatus('');
  }, [open, row]);

  const assetInfo = useMemo(
    () => ({
      code: asset?.code ?? row?.assetCode ?? '-',
      name: asset?.name ?? row?.assetName ?? '-',
      type: asset?.assetTypeName ?? '-',
      specification: asset
        ? [asset.unit ? `Đơn vị: ${asset.unit}` : null, `SL: ${asset.quantity}`]
            .filter(Boolean)
            .join(' · ')
        : '-',
      purchaseDate: toDisplayDate(asset?.purchaseDate),
      warrantyExpiry: toDisplayDate(asset?.warrantyEndDate),
      currentValue: formatVndValue(asset?.originalPrice),
      remainingValue:
        asset?.remainingValue != null
          ? formatVndValue(asset.remainingValue)
          : asset
            ? formatVndValue(asset.currentValue)
            : '-',
      location: asset?.warehouseName || asset?.currentDepartmentName || row?.location || '-',
      status: asset?.statusName ?? '-',
      admissionDate: toDisplayDate(asset?.inUseDate),
      department: asset?.currentDepartmentName ?? row?.department ?? '-',
    }),
    [asset, row]
  );

  if (!open) return null;

  const handleSubmit = () => {
    if (!damageDate || !repairDate || !repairProgressStatus.trim()) return;
    onSubmit({
      reportNumber: reportNumber.trim(),
      damageDate: toIsoDate(damageDate),
      damageCondition: damageCondition.trim(),
      repairDate: toIsoDate(repairDate),
      expectedCompletionDate: toIsoDateOrUndefined(expectedCompletionDate),
      estimatedCost: parseNumberInput(estimatedCostInput),
      repairProgressStatus: repairProgressStatus.trim(),
    });
  };

  return (
    <div className="repair-start-modal-overlay" role="dialog" aria-modal="true">
      <div className="repair-start-modal">
        <button type="button" className="repair-start-modal__close-btn" onClick={onClose} aria-label="Đóng">
          <span className="repair-start-modal__close">×</span>
        </button>

        <div className="repair-start-modal__header">
          <h2 className="repair-start-modal__title">Sửa chữa tài sản</h2>
        </div>

        <div className="repair-start-modal__body">
          {loading || !row ? (
            <div className="repair-start-modal__loading">Đang tải...</div>
          ) : (
            <div className="repair-start-modal__content">
              <div className="repair-start-form__item">
                <label htmlFor="repair-start-report-number">Số biên bản</label>
                <input
                  id="repair-start-report-number"
                  type="text"
                  className="repair-start-input"
                  value={reportNumber}
                  onChange={(e) => setReportNumber(e.target.value)}
                  placeholder="VD: BA001"
                />
              </div>

              <div className="repair-start-info-section">
                <h3 className="repair-start-section-title">Thông tin tài sản</h3>
                <div className="repair-start-info-grid">
                  <div className="repair-start-info-row">
                    <div className="repair-start-info-item">
                      <label>Mã tài sản</label>
                      <div className="repair-start-info-value">{assetInfo.code}</div>
                    </div>
                    <div className="repair-start-info-item">
                      <label>Giá trị tài sản</label>
                      <div className="repair-start-info-value">{assetInfo.currentValue}</div>
                    </div>
                  </div>
                  <div className="repair-start-info-row">
                    <div className="repair-start-info-item">
                      <label>Tên tài sản</label>
                      <div className="repair-start-info-value">{assetInfo.name}</div>
                    </div>
                    <div className="repair-start-info-item">
                      <label>Giá trị còn lại</label>
                      <div className="repair-start-info-value">{assetInfo.remainingValue}</div>
                    </div>
                  </div>
                  <div className="repair-start-info-row">
                    <div className="repair-start-info-item">
                      <label>Loại tài sản</label>
                      <div className="repair-start-info-value">{assetInfo.type}</div>
                    </div>
                    <div className="repair-start-info-item">
                      <label>Vị trí tài sản</label>
                      <div className="repair-start-info-value">{assetInfo.location}</div>
                    </div>
                  </div>
                  <div className="repair-start-info-row">
                    <div className="repair-start-info-item">
                      <label>Quy cách tài sản</label>
                      <div className="repair-start-info-value">{assetInfo.specification}</div>
                    </div>
                    <div className="repair-start-info-item">
                      <label>Tình trạng</label>
                      <div className="repair-start-info-value">{assetInfo.status}</div>
                    </div>
                  </div>
                  <div className="repair-start-info-row">
                    <div className="repair-start-info-item">
                      <label>Ngày mua</label>
                      <div className="repair-start-info-value">{assetInfo.purchaseDate}</div>
                    </div>
                    <div className="repair-start-info-item">
                      <label>Ngày đưa vào SD</label>
                      <div className="repair-start-info-value">{assetInfo.admissionDate}</div>
                    </div>
                  </div>
                  <div className="repair-start-info-row">
                    <div className="repair-start-info-item">
                      <label>Hạn bảo hành</label>
                      <div className="repair-start-info-value">{assetInfo.warrantyExpiry}</div>
                    </div>
                    <div className="repair-start-info-item">
                      <label>Phòng ban SD</label>
                      <div className="repair-start-info-value">{assetInfo.department}</div>
                    </div>
                  </div>
                </div>
              </div>

              <div className="repair-start-form-section">
                <h3 className="repair-start-section-title">Thông tin ghi nhận tài sản hỏng</h3>
                <div className="repair-start-info-row">
                  <div className="repair-start-form__item">
                    <label htmlFor="repair-start-damage-date">
                      Ngày hỏng<span className="repair-start-required">*</span>
                    </label>
                    <input
                      id="repair-start-damage-date"
                      type="date"
                      className="repair-start-input"
                      value={damageDate}
                      onChange={(e) => setDamageDate(e.target.value)}
                    />
                  </div>
                  <div className="repair-start-form__item">
                    <label htmlFor="repair-start-damage-condition">Tình trạng</label>
                    <textarea
                      id="repair-start-damage-condition"
                      className="repair-start-textarea"
                      rows={4}
                      value={damageCondition}
                      onChange={(e) => setDamageCondition(e.target.value)}
                      placeholder="VD: Hỏng nhẹ"
                    />
                  </div>
                </div>
              </div>

              <div className="repair-start-form-section">
                <h3 className="repair-start-section-title">Thông tin sửa chữa</h3>
                <div className="repair-start-info-row">
                  <div>
                    <div className="repair-start-form__item">
                      <label htmlFor="repair-start-repair-date">
                        Ngày sửa chữa<span className="repair-start-required">*</span>
                      </label>
                      <input
                        id="repair-start-repair-date"
                        type="date"
                        className="repair-start-input"
                        value={repairDate}
                        onChange={(e) => setRepairDate(e.target.value)}
                      />
                    </div>
                    <div className="repair-start-form__item">
                      <label htmlFor="repair-start-expected-date">Ngày dự kiến hoàn thành</label>
                      <input
                        id="repair-start-expected-date"
                        type="date"
                        className="repair-start-input"
                        value={expectedCompletionDate}
                        onChange={(e) => setExpectedCompletionDate(e.target.value)}
                      />
                    </div>
                    <div className="repair-start-form__item">
                      <label htmlFor="repair-start-estimated-cost">Chi phí dự kiến</label>
                      <input
                        id="repair-start-estimated-cost"
                        type="text"
                        className="repair-start-input"
                        value={estimatedCostInput}
                        onChange={(e) => setEstimatedCostInput(e.target.value)}
                        placeholder="VD: 1000000"
                      />
                    </div>
                  </div>
                  <div className="repair-start-form__item">
                    <label htmlFor="repair-start-progress-status">
                      Tình trạng sửa chữa<span className="repair-start-required">*</span>
                    </label>
                    <textarea
                      id="repair-start-progress-status"
                      className="repair-start-textarea repair-start-textarea--tall"
                      rows={10}
                      value={repairProgressStatus}
                      onChange={(e) => setRepairProgressStatus(e.target.value)}
                      placeholder="Mô tả tình trạng / kế hoạch sửa chữa"
                    />
                  </div>
                </div>
              </div>
            </div>
          )}
        </div>

        <div className="repair-start-modal__footer">
          <button
            type="button"
            onClick={handleSubmit}
            className="repair-start-btn-submit"
            disabled={submitting || loading}
          >
            {submitting ? 'Đang gửi...' : 'Gửi yêu cầu'}
          </button>
          <button type="button" onClick={onClose} className="repair-start-btn-cancel" disabled={submitting}>
            Hủy
          </button>
        </div>
      </div>
    </div>
  );
}

export const RepairStartModal = memo(RepairStartModalInner);

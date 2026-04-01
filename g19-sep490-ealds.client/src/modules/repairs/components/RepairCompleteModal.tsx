import { memo, useEffect, useMemo, useState } from 'react';
import dayjs from 'dayjs';
import type { AssetDetailResponse } from '../../assets/services/assetService';
import './RepairCompleteModal.css';

interface RepairCompleteRow {
  condition: string;
  assetCode: string;
  assetName: string;
  location: string;
  department: string;
}

export interface RepairCompleteFormValues {
  reportNumber: string;
  completionDate: string;
  returnToUseDate: string;
  actualCost: number;
  result: string;
  detail: string;
}

interface RepairCompleteModalProps {
  open: boolean;
  loading: boolean;
  submitting: boolean;
  row: RepairCompleteRow | null;
  asset: AssetDetailResponse | null;
  defaultReportNumber?: string;
  onClose: () => void;
  onSubmit: (values: RepairCompleteFormValues) => void;
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

function parseNumberInput(value: string): number | undefined {
  const normalized = value.replace(/[^\d]/g, '');
  if (!normalized) return undefined;
  const parsed = Number(normalized);
  return Number.isFinite(parsed) ? parsed : undefined;
}

function toIsoDate(value: string): string {
  return new Date(`${value}T00:00:00`).toISOString();
}

function RepairCompleteModalInner({
  open,
  loading,
  submitting,
  row,
  asset,
  defaultReportNumber,
  onClose,
  onSubmit,
}: RepairCompleteModalProps) {
  const [reportNumber, setReportNumber] = useState('');
  const [completionDate, setCompletionDate] = useState('');
  const [returnDate, setReturnDate] = useState('');
  const [actualCost, setActualCost] = useState<number | null>(null);
  const [result, setResult] = useState('');
  const [detail, setDetail] = useState('');

  useEffect(() => {
    if (!open || !row) return;
    const today = dayjs().format('YYYY-MM-DD');
    setReportNumber(defaultReportNumber ?? '');
    setCompletionDate(today);
    setReturnDate(today);
    setActualCost(null);
    setResult('');
    setDetail(row.condition || '');
  }, [open, row, defaultReportNumber]);

  const assetInfo = useMemo(() => {
    const instances = asset?.instances ?? [];
    const primary =
      instances.find((i) => i.instanceCode === row?.assetCode || i.assetCode === row?.assetCode) ??
      instances[0];
    const fallbackWithWarranty = instances.find(
      (i) => i.warrantyEndDate || i.guarantees?.some((g) => !!g.warrantyEndDate),
    );
    const warrantyEndDate =
      primary?.warrantyEndDate ??
      primary?.guarantees?.[0]?.warrantyEndDate ??
      fallbackWithWarranty?.warrantyEndDate ??
      fallbackWithWarranty?.guarantees?.[0]?.warrantyEndDate;
    return {
      code: row?.assetCode ?? asset?.code ?? '-',
      name: asset?.name ?? row?.assetName ?? '-',
      type: asset?.assetTypeName ?? '-',
      specification:
        primary?.specification?.trim() ||
        asset?.specification?.trim() ||
        [asset?.unit ? `Đơn vị: ${asset.unit}` : null, asset?.quantity != null ? `SL: ${asset.quantity}` : null]
          .filter(Boolean)
          .join(' · ') ||
        '-',
      warrantyExpiry: toDisplayDate(warrantyEndDate),
      currentValue: formatVndValue(primary?.originalPrice),
      remainingValue:
        primary?.remainingValue != null
          ? formatVndValue(primary.remainingValue)
          : primary
            ? formatVndValue(primary.currentValue)
            : '-',
      location:
        primary?.warehouseName || primary?.currentDepartmentName || row?.location || '-',
      status: 'Đang sửa chữa',
      department: primary?.currentDepartmentName ?? row?.department ?? '-',
    };
  }, [asset, row]);

  if (!open) return null;

  const handleSubmit = () => {
    if (!completionDate || !returnDate || actualCost == null) return;
    onSubmit({
      reportNumber: reportNumber.trim(),
      completionDate: toIsoDate(completionDate),
      returnToUseDate: toIsoDate(returnDate),
      actualCost,
      result: result.trim(),
      detail: detail.trim(),
    });
  };

  return (
    <div className="repair-complete-modal-overlay" role="dialog" aria-modal="true">
      <div className="repair-complete-modal">
        <button type="button" className="repair-complete-modal__close-btn" onClick={onClose} aria-label="Đóng">
          <span className="repair-complete-modal__close">×</span>
        </button>

        <div className="repair-complete-modal__header">
          <h2 className="repair-complete-modal__title">Hoàn thành sửa chữa tài sản</h2>
        </div>

        <div className="repair-complete-modal__body">
          {loading || !row ? (
            <div className="repair-complete-modal__loading">Đang tải...</div>
          ) : (
            <div className="repair-complete-modal__content">
              <div className="repair-complete-form__item">
                <label htmlFor="repair-complete-report-number">Số biên bản</label>
                <input
                  id="repair-complete-report-number"
                  type="text"
                  className="repair-complete-input"
                  value={reportNumber}
                  readOnly
                />
              </div>

              <div className="repair-complete-info-section">
                <h3 className="repair-complete-section-title">Thông tin tài sản</h3>
                <div className="repair-complete-info-grid">
                  <div className="repair-complete-info-row">
                    <div className="repair-complete-info-item">
                      <label>Mã cá thể</label>
                      <div className="repair-complete-info-value">{assetInfo.code}</div>
                    </div>
                    <div className="repair-complete-info-item">
                      <label>Giá trị tài sản</label>
                      <div className="repair-complete-info-value">{assetInfo.currentValue}</div>
                    </div>
                  </div>
                  <div className="repair-complete-info-row">
                    <div className="repair-complete-info-item">
                      <label>Tên tài sản</label>
                      <div className="repair-complete-info-value">{assetInfo.name}</div>
                    </div>
                    <div className="repair-complete-info-item">
                      <label>Giá trị còn lại</label>
                      <div className="repair-complete-info-value">{assetInfo.remainingValue}</div>
                    </div>
                  </div>
                  <div className="repair-complete-info-row">
                    <div className="repair-complete-info-item">
                      <label>Loại tài sản</label>
                      <div className="repair-complete-info-value">{assetInfo.type}</div>
                    </div>
                    <div className="repair-complete-info-item">
                      <label>Vị trí tài sản</label>
                      <div className="repair-complete-info-value">{assetInfo.location}</div>
                    </div>
                  </div>
                  <div className="repair-complete-info-row">
                    <div className="repair-complete-info-item">
                      <label>Quy cách tài sản</label>
                      <div className="repair-complete-info-value">{assetInfo.specification}</div>
                    </div>
                    <div className="repair-complete-info-item">
                      <label>Tình trạng</label>
                      <div className="repair-complete-info-value">{assetInfo.status}</div>
                    </div>
                  </div>
                  <div className="repair-complete-info-row">
                    <div className="repair-complete-info-item">
                      <label>Hạn bảo hành</label>
                      <div className="repair-complete-info-value">{assetInfo.warrantyExpiry}</div>
                    </div>
                    <div className="repair-complete-info-item">
                      <label>Phòng ban SD</label>
                      <div className="repair-complete-info-value">{assetInfo.department}</div>
                    </div>
                  </div>
                </div>
              </div>

              <div className="repair-complete-form-section">
                <h3 className="repair-complete-section-title">Thông tin hoàn thành sửa chữa</h3>
                <div className="repair-complete-info-row">
                  <div>
                    <div className="repair-complete-form__item">
                      <label htmlFor="repair-complete-date">
                        Ngày hoàn thành sửa chữa<span className="repair-complete-required">*</span>
                      </label>
                      <input
                        id="repair-complete-date"
                        type="date"
                        className="repair-complete-input"
                        value={completionDate}
                        onChange={(e) => setCompletionDate(e.target.value)}
                      />
                    </div>
                    <div className="repair-complete-form__item">
                      <label htmlFor="repair-complete-return-date">
                        Ngày đưa vào sử dụng lại<span className="repair-complete-required">*</span>
                      </label>
                      <input
                        id="repair-complete-return-date"
                        type="date"
                        className="repair-complete-input"
                        value={returnDate}
                        onChange={(e) => setReturnDate(e.target.value)}
                      />
                    </div>
                    <div className="repair-complete-form__item">
                      <label htmlFor="repair-complete-actual-cost">
                        Chi phí thực tế<span className="repair-complete-required">*</span>
                      </label>
                      <div className="repair-complete-money-input">
                        <input
                          id="repair-complete-actual-cost"
                          type="text"
                          className="repair-complete-input"
                          inputMode="numeric"
                          value={actualCost != null ? actualCost.toLocaleString('en-US') : ''}
                          onChange={(e) => setActualCost(parseNumberInput(e.target.value) ?? null)}
                          placeholder="Nhập chi phí"
                        />
                        <span className="repair-complete-money-suffix">đ</span>
                      </div>
                    </div>
                  </div>
                  <div>
                    <div className="repair-complete-form__item">
                      <label htmlFor="repair-complete-result">Kết quả sửa chữa</label>
                      <input
                        id="repair-complete-result"
                        type="text"
                        className="repair-complete-input"
                        value={result}
                        onChange={(e) => setResult(e.target.value)}
                        placeholder="Tóm tắt kết quả"
                      />
                    </div>
                    <div className="repair-complete-form__item">
                      <label htmlFor="repair-complete-detail">Mô tả chi tiết</label>
                      <textarea
                        id="repair-complete-detail"
                        className="repair-complete-textarea"
                        rows={6}
                        value={detail}
                        onChange={(e) => setDetail(e.target.value)}
                      />
                    </div>
                  </div>
                </div>

              </div>
            </div>
          )}
        </div>

        <div className="repair-complete-modal__footer">
          <button
            type="button"
            onClick={handleSubmit}
            className="repair-complete-btn-submit"
            disabled={submitting || loading}
          >
            {submitting ? 'Đang lưu...' : 'Lưu'}
          </button>
          <button
            type="button"
            onClick={onClose}
            className="repair-complete-btn-cancel"
            disabled={submitting}
          >
            Hủy
          </button>
        </div>
      </div>
    </div>
  );
}

export const RepairCompleteModal = memo(RepairCompleteModalInner);

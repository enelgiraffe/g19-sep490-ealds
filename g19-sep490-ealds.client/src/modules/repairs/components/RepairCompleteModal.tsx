import { memo, useEffect, useMemo, useState } from 'react';
import { message } from 'antd';
import dayjs from 'dayjs';
import type { AssetDetailResponse } from '../../assets/services/assetService';
import { parseIntegerMoneyInput } from '../../../shared/utils/moneyInput';
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
  supplierId: number | null;
  newSupplier: { code: string; name: string } | null;
  /** Bảo hành theo lần sửa chữa (cấu trúc giống bảo hành cá thể); không cập nhật bảo hành tài sản */
  repairWarrantyStartDate: string | null;
  repairWarrantyEndDate: string | null;
  repairWarrantyPeriodValue: number | null;
  repairWarrantyPeriodUnit: string | null;
  repairWarrantyConditions: string;
  repairWarrantyNote: string;
}

interface RepairCompleteModalProps {
  open: boolean;
  loading: boolean;
  submitting: boolean;
  row: RepairCompleteRow | null;
  asset: AssetDetailResponse | null;
  defaultReportNumber?: string;
  /** Đơn vị sửa chữa đã chọn khi bắt đầu sửa chữa */
  defaultRepairSupplierId?: number | null;
  defaultRepairSupplierName?: string | null;
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

function toIsoDate(value: string): string {
  return new Date(`${value}T00:00:00`).toISOString();
}

/** Giống AssetInstanceEditPage: tính ngày hết hạn từ ngày bắt đầu + thời hạn. */
function computeWarrantyEndDate(startDate: string, periodValue: string, periodUnit: string): string {
  if (!startDate.trim() || !periodValue.trim() || !periodUnit.trim()) return '';
  const period = Number(periodValue);
  if (!Number.isFinite(period) || period <= 0) return '';
  const base = new Date(startDate);
  if (Number.isNaN(base.getTime())) return '';
  const result = new Date(base);
  const u = periodUnit.trim().toLowerCase();
  if (u === 'day' || u === 'days') result.setDate(result.getDate() + period);
  else if (u === 'week' || u === 'weeks') result.setDate(result.getDate() + period * 7);
  else if (u === 'month' || u === 'months') result.setMonth(result.getMonth() + period);
  else if (u === 'year' || u === 'years') result.setFullYear(result.getFullYear() + period);
  else return '';
  const pad = (n: number) => String(n).padStart(2, '0');
  return `${result.getFullYear()}-${pad(result.getMonth() + 1)}-${pad(result.getDate())}`;
}

/** Giống AssetInstanceEditPage — gộp mã BH ngoài + nội dung vào một chuỗi lưu DB. */
const EXTERNAL_WARRANTY_CODE_PREFIX = 'Mã BH ngoài:';

function buildWarrantyConditions(externalCode: string, details: string): string | null {
  const code = externalCode.trim();
  const content = details.trim();
  if (!code && !content) return null;
  if (code && content) return `${EXTERNAL_WARRANTY_CODE_PREFIX} ${code}\n${content}`;
  if (code) return `${EXTERNAL_WARRANTY_CODE_PREFIX} ${code}`;
  return content;
}

/** So sánh yyyy-MM-dd (chuỗi). */
function compareYyyyMmDd(a: string, b: string): number {
  return a.localeCompare(b, undefined, { numeric: true });
}

function RepairCompleteModalInner({
  open,
  loading,
  submitting,
  row,
  asset,
  defaultReportNumber,
  defaultRepairSupplierId,
  defaultRepairSupplierName,
  onClose,
  onSubmit,
}: RepairCompleteModalProps) {
  const [reportNumber, setReportNumber] = useState('');
  const [completionDate, setCompletionDate] = useState('');
  const [returnDate, setReturnDate] = useState('');
  /** Chỉ chữ số, không format dấu phẩy trong lúc gõ (tránh lỗi con trỏ / chèn ký tự sai). */
  const [actualCostInput, setActualCostInput] = useState('');
  const [result, setResult] = useState('');
  const [detail, setDetail] = useState('');
  const [selectedSupplierId, setSelectedSupplierId] = useState<number | ''>('');
  const [repairWarrantyEndDate, setRepairWarrantyEndDate] = useState('');
  const [repairWarrantyPeriodValue, setRepairWarrantyPeriodValue] = useState('');
  const [repairWarrantyPeriodUnit, setRepairWarrantyPeriodUnit] = useState('month');
  const [repairWarrantyExternalCode, setRepairWarrantyExternalCode] = useState('');
  const [repairWarrantyConditions, setRepairWarrantyConditions] = useState('');
  const [repairWarrantyNote, setRepairWarrantyNote] = useState('');

  useEffect(() => {
    if (!open || !row) return;
    const today = dayjs().format('YYYY-MM-DD');
    setReportNumber(defaultReportNumber ?? '');
    setCompletionDate(today);
    setReturnDate(today);
    setActualCostInput('');
    setResult('');
    setDetail(row.condition || '');
    if (defaultRepairSupplierId != null && defaultRepairSupplierId > 0) {
      setSelectedSupplierId(defaultRepairSupplierId);
    } else {
      setSelectedSupplierId('');
    }
    setRepairWarrantyEndDate('');
    setRepairWarrantyPeriodValue('');
    setRepairWarrantyPeriodUnit('month');
    setRepairWarrantyExternalCode('');
    setRepairWarrantyConditions('');
    setRepairWarrantyNote('');
  }, [open, row, defaultReportNumber, defaultRepairSupplierId]);

  useEffect(() => {
    if (!open) return;
    const period = Number(repairWarrantyPeriodValue);
    if (
      !completionDate.trim() ||
      !repairWarrantyPeriodValue.trim() ||
      !Number.isFinite(period) ||
      period <= 0
    ) {
      setRepairWarrantyEndDate('');
      return;
    }
    const computed = computeWarrantyEndDate(
      completionDate.trim(),
      repairWarrantyPeriodValue.trim(),
      repairWarrantyPeriodUnit
    );
    if (computed) setRepairWarrantyEndDate(computed);
    else setRepairWarrantyEndDate('');
  }, [open, completionDate, repairWarrantyPeriodValue, repairWarrantyPeriodUnit]);

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
    const actualCost = parseIntegerMoneyInput(actualCostInput);
    if (!completionDate || !returnDate || actualCost == null) return;
    if (compareYyyyMmDd(returnDate, completionDate) < 0) {
      message.warning('Ngày đưa vào sử dụng lại không được trước ngày hoàn thành sửa chữa.');
      return;
    }

    const pickedId =
      selectedSupplierId !== '' && selectedSupplierId !== 0
        ? Number(selectedSupplierId)
        : null;

    const periodParsed = parseIntegerMoneyInput(repairWarrantyPeriodValue);
    const periodOk = repairWarrantyPeriodValue.trim() !== '' && periodParsed != null && periodParsed > 0;

    onSubmit({
      reportNumber: reportNumber.trim(),
      completionDate: toIsoDate(completionDate),
      returnToUseDate: toIsoDate(returnDate),
      actualCost,
      result: result.trim(),
      detail: detail.trim(),
      supplierId: pickedId,
      newSupplier: null,
      repairWarrantyStartDate: completionDate.trim() ? completionDate.trim() : null,
      repairWarrantyEndDate: repairWarrantyEndDate.trim() ? repairWarrantyEndDate.trim() : null,
      repairWarrantyPeriodValue: periodOk && periodParsed != null ? periodParsed : null,
      repairWarrantyPeriodUnit: periodOk ? repairWarrantyPeriodUnit.trim().slice(0, 20) || null : null,
      repairWarrantyConditions:
        buildWarrantyConditions(repairWarrantyExternalCode, repairWarrantyConditions)?.trim() ?? '',
      repairWarrantyNote: repairWarrantyNote.trim(),
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
                <div className="repair-complete-form__item repair-complete-form__item--full">
                  <label>Đơn vị sửa chữa</label>
                  <input
                    id="repair-complete-supplier"
                    type="text"
                    className="repair-complete-input repair-complete-input--readonly"
                    value={defaultRepairSupplierName?.trim() || '—'}
                    readOnly
                    tabIndex={-1}
                  />
                  <p className="repair-complete-hint repair-complete-hint--tight">
                    Đơn vị sửa chữa được giữ theo thông tin lúc bắt đầu sửa chữa.
                  </p>
                </div>
                <div className="repair-complete-info-row">
                  <div>
                    <div className="repair-complete-form__item">
                      <label htmlFor="repair-complete-date">
                        Ngày hoàn thành sửa chữa<span className="repair-complete-required">*</span>
                      </label>
                      <input
                        id="repair-complete-date"
                        type="date"
                        className="repair-complete-input repair-complete-input--readonly"
                        value={completionDate}
                        readOnly
                        tabIndex={-1}
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
                        min={completionDate.trim() || undefined}
                        onChange={(e) => {
                          let v = e.target.value;
                          const min = completionDate.trim();
                          if (min && v && compareYyyyMmDd(v, min) < 0) v = min;
                          setReturnDate(v);
                        }}
                      />
                      <p className="repair-complete-hint repair-complete-hint--tight">
                        Tối thiểu từ ngày hoàn thành sửa chữa.
                      </p>
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
                          value={actualCostInput}
                          onChange={(e) => {
                            const n = parseIntegerMoneyInput(e.target.value);
                            setActualCostInput(n == null ? '' : Math.floor(n).toLocaleString('en-US'));
                          }}
                          placeholder="Nhập chi phí (VNĐ)"
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

              <div className="repair-complete-form-section repair-complete-warranty-section">
                <h3 className="repair-complete-section-title">Bảo hành sửa chữa</h3>
                <div className="repair-complete-info-row repair-complete-info-row--warranty-dates">
                  <div className="repair-complete-form__item">
                    <label htmlFor="repair-complete-warranty-start">Ngày bắt đầu bảo hành sửa chữa</label>
                    <input
                      id="repair-complete-warranty-start"
                      type="date"
                      className="repair-complete-input repair-complete-input--readonly"
                      value={completionDate}
                      readOnly
                      tabIndex={-1}
                      title="Luôn trùng ngày hoàn thành sửa chữa"
                      aria-label="Ngày bắt đầu bảo hành sửa chữa, trùng với ngày hoàn thành sửa chữa"
                    />
                    <p className="repair-complete-hint repair-complete-hint--tight">
                      Cố định theo ngày hoàn thành sửa chữa.
                    </p>
                  </div>
                  <div className="repair-complete-form__item">
                    <label htmlFor="repair-complete-warranty-end">Ngày hết hạn bảo hành sửa chữa</label>
                    <input
                      id="repair-complete-warranty-end"
                      type="date"
                      className="repair-complete-input repair-complete-input--readonly"
                      value={repairWarrantyEndDate}
                      readOnly
                      aria-readonly="true"
                      title="Tự tính từ ngày hoàn thành sửa chữa và thời hạn"
                    />
                  </div>
                </div>

                <div className="repair-complete-inline-fields repair-complete-inline-fields--warranty-period">
                  <div className="repair-complete-form__item repair-complete-form__item--inline">
                    <label htmlFor="repair-complete-warranty-period">Thời hạn</label>
                    <input
                      id="repair-complete-warranty-period"
                      type="text"
                      className="repair-complete-input"
                      inputMode="numeric"
                      value={repairWarrantyPeriodValue}
                      onChange={(e) => {
                        const n = parseIntegerMoneyInput(e.target.value);
                        setRepairWarrantyPeriodValue(n == null ? '' : String(Math.floor(n)));
                      }}
                    />
                  </div>
                  <div className="repair-complete-form__item repair-complete-form__item--inline">
                    <label htmlFor="repair-complete-warranty-unit">Đơn vị thời hạn</label>
                    <select
                      id="repair-complete-warranty-unit"
                      className="repair-complete-input"
                      value={repairWarrantyPeriodUnit}
                      onChange={(e) => setRepairWarrantyPeriodUnit(e.target.value)}
                    >
                      <option value="day">Ngày</option>
                      <option value="week">Tuần</option>
                      <option value="month">Tháng</option>
                      <option value="year">Năm</option>
                    </select>
                  </div>
                </div>

                <div className="repair-complete-form__item repair-complete-form__item--full">
                  <label htmlFor="repair-complete-warranty-external-code">Mã bảo hành ngoài (đơn vị sửa chữa)</label>
                  <input
                    id="repair-complete-warranty-external-code"
                    type="text"
                    className="repair-complete-input"
                    value={repairWarrantyExternalCode}
                    onChange={(e) => setRepairWarrantyExternalCode(e.target.value)}
                    placeholder="Số phiếu / mã BH từ nhà cung cấp"
                    maxLength={200}
                  />
                </div>

                <div className="repair-complete-form__item repair-complete-form__item--full">
                  <label htmlFor="repair-complete-warranty-conditions">Nội dung điều khoản bảo hành sửa chữa</label>
                  <textarea
                    id="repair-complete-warranty-conditions"
                    className="repair-complete-textarea repair-complete-textarea--warranty-conditions"
                    rows={5}
                    value={repairWarrantyConditions}
                    onChange={(e) => setRepairWarrantyConditions(e.target.value)}
                    placeholder="Phạm vi, linh kiện thay thế, điều kiện áp dụng, loại trừ trách nhiệm…"
                    maxLength={8000}
                  />
                </div>

                <div className="repair-complete-form__item repair-complete-form__item--full">
                  <label htmlFor="repair-complete-warranty-note">Ghi chú thêm</label>
                  <textarea
                    id="repair-complete-warranty-note"
                    className="repair-complete-textarea"
                    rows={3}
                    value={repairWarrantyNote}
                    onChange={(e) => setRepairWarrantyNote(e.target.value)}
                    placeholder="Ghi chú nội bộ, liên hệ xử lý sự cố…"
                    maxLength={2000}
                  />
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

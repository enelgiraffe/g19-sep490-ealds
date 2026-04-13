import { useEffect, useState } from 'react';
import { message, InputNumber, DatePicker, Button } from 'antd';
import { PlusOutlined, DeleteOutlined } from '@ant-design/icons';
import dayjs, { type Dayjs } from 'dayjs';
import { supplierService, type SupplierItem } from '../../admin/services/supplierService';
import { SupplierSelectionModal } from './SupplierSelectionModal';
import { AssetSelectionModal } from './AssetSelectionModal';
import {
  purchaseOrderService,
  type PurchaseOrderListItem,
  type PurchaseOrderLineItem as PurchaseRequestLineItem,
} from '../../purchase-orders/services/purchaseOrderService';
import type { PurchaseOrderDetail, PurchaseOrderLineWrite } from '../services/procurementPoService';
import './PurchaseOrderFormModalNew.css';

const CURRENCIES = ['VND', 'USD', 'EUR'] as const;

interface LineRow {
  key: string;
  description: string;
  assetId: number | null;
  assetName: string;
  quantity: number;
  unit: string;
  unitPrice: number;
  expectedDelivery: Dayjs | null;
}

function toRows(lines: PurchaseOrderDetail['lines']): LineRow[] {
  return lines.map((l, i) => ({
    key: `l-${l.lineId}-${i}`,
    description: l.description ?? '',
    assetId: l.assetId,
    assetName: l.assetName ?? '',
    quantity: Number(l.quantity),
    unit: l.unit ?? 'Cái',
    unitPrice: Number(l.unitPrice),
    expectedDelivery: l.expectedDeliveryDate ? dayjs(l.expectedDeliveryDate) : null,
  }));
}

function emptyRow(): LineRow {
  return {
    key: `new-${Date.now()}-${Math.random()}`,
    description: '',
    assetId: null,
    assetName: '',
    quantity: 1,
    unit: 'Cái',
    unitPrice: 0,
    expectedDelivery: null,
  };
}

function parseEstimatedPrice(s: string | null): number {
  if (s == null || s === '') return 0;
  const n = Number(String(s).replace(/,/g, '').replace(/\s/g, ''));
  return Number.isFinite(n) ? n : 0;
}

function requestLinesToRows(lines: PurchaseRequestLineItem[]): LineRow[] {
  return lines.map((l, i) => ({
    key: `req-${l.lineId}-${i}`,
    description: (l.itemName ?? l.modelCode ?? '').trim(),
    assetId: l.assetId,
    assetName: [l.assetCode, l.assetName].filter(Boolean).join(' — '),
    quantity: Number(l.quantity) > 0 ? Number(l.quantity) : 1,
    unit: (l.unit ?? 'Cái').trim() || 'Cái',
    unitPrice: parseEstimatedPrice(l.estimatedPrice),
    expectedDelivery: null,
  }));
}

export interface PurchaseOrderFormModalNewProps {
  open: boolean;
  mode: 'create' | 'edit';
  initial: PurchaseOrderDetail | null;
  onClose: () => void;
  onSubmit: (payload: {
    supplierId: number;
    currency: string;
    contractNo?: string;
    assetRequestId: number | null;
    lines: PurchaseOrderLineWrite[];
    isDraft?: boolean;
  }) => Promise<void>;
}

export function PurchaseOrderFormModalNew({
  open,
  mode,
  initial,
  onClose,
  onSubmit,
}: PurchaseOrderFormModalNewProps) {
  const [selectedSupplier, setSelectedSupplier] = useState<SupplierItem | null>(null);
  const [currency, setCurrency] = useState<string>('VND');
  const [contractNo, setContractNo] = useState<string>('');
  const [assetRequestId, setAssetRequestId] = useState<string>('');
  const [assetRequestOptions, setAssetRequestOptions] = useState<PurchaseOrderListItem[]>([]);
  const [showAssetRequestDropdown, setShowAssetRequestDropdown] = useState(false);
  const [lines, setLines] = useState<LineRow[]>([emptyRow()]);
  /** Khi có giá trị: các dòng được lấy từ đơn yêu cầu; chỉ chỉnh sửa đơn giá. */
  const [linkedRequestId, setLinkedRequestId] = useState<number | null>(null);
  const [requestLinesLoading, setRequestLinesLoading] = useState(false);
  const [loading, setLoading] = useState(false);
  const [isSupplierModalOpen, setIsSupplierModalOpen] = useState(false);
  const [isAssetModalOpen, setIsAssetModalOpen] = useState(false);
  const [currentEditingLineKey, setCurrentEditingLineKey] = useState<string | null>(null);

  useEffect(() => {
    if (!open) return;
    let cancelled = false;
    (async () => {
      try {
        const requests = await purchaseOrderService.getList();
        if (!cancelled) {
          setAssetRequestOptions(requests);
        }
      } catch {
        if (!cancelled) {
          setAssetRequestOptions([]);
        }
      }
    })();
    return () => {
      cancelled = true;
    };
  }, [open]);

  useEffect(() => {
    if (!open) return;
    setLinkedRequestId(null);
    if (mode === 'edit' && initial) {
      if (initial.supplierId) {
        supplierService.getAll().then((suppliers) => {
          const found = suppliers.find((s) => s.supplierId === initial.supplierId);
          if (found) setSelectedSupplier(found);
        });
      }
      setCurrency(initial.currency || 'VND');
      setContractNo(initial.contractNo || '');
      setAssetRequestId(initial.assetRequestId ? String(initial.assetRequestId) : '');
      setLines(initial.lines.length > 0 ? toRows(initial.lines) : [emptyRow()]);
    } else {
      setSelectedSupplier(null);
      setCurrency('VND');
      setContractNo('');
      setAssetRequestId('');
      setLines([emptyRow()]);
    }
  }, [open, mode, initial]);

  const loadLinesFromRequest = async (id: number) => {
    setRequestLinesLoading(true);
    try {
      const raw = await purchaseOrderService.getPurchaseLines(id);
      if (raw.length === 0) {
        message.warning('Đơn yêu cầu không có dòng vật tư.');
        setLinkedRequestId(null);
        return;
      }
      setLinkedRequestId(id);
      setLines(requestLinesToRows(raw));
    } catch {
      message.error('Không tải được danh sách vật tư từ đơn yêu cầu.');
      setLinkedRequestId(null);
    } finally {
      setRequestLinesLoading(false);
    }
  };

  const onAssetRequestIdChange = (value: string) => {
    setAssetRequestId(value);
    const trimmed = value.trim();
    if (!trimmed) {
      setLinkedRequestId(null);
      if (mode === 'create') {
        setLines([emptyRow()]);
      }
      return;
    }
    const parsed = parseInt(trimmed, 10);
    if (!Number.isFinite(parsed) || parsed <= 0) {
      setLinkedRequestId(null);
      return;
    }
    if (linkedRequestId !== null && linkedRequestId !== parsed) {
      setLinkedRequestId(null);
      if (mode === 'create') {
        setLines([emptyRow()]);
      }
    }
  };

  /** Tải dòng từ đơn yêu cầu theo mã đang nhập (Enter hoặc gọi sau khi chọn từ danh sách). */
  const tryLoadLinesFromRequestInput = async () => {
    const trimmed = assetRequestId.trim();
    if (!trimmed) {
      setLinkedRequestId(null);
      if (mode === 'create') {
        setLines([emptyRow()]);
      }
      return;
    }
    const id = parseInt(trimmed, 10);
    if (!Number.isFinite(id) || id <= 0) {
      message.warning('Mã yêu cầu không hợp lệ.');
      return;
    }
    if (linkedRequestId === id) return;
    await loadLinesFromRequest(id);
  };

  const updateLine = (key: string, patch: Partial<LineRow>) => {
    setLines((prev) => prev.map((r) => (r.key === key ? { ...r, ...patch } : r)));
  };

  const handleSubmit = async (isDraft: boolean = false) => {
    if (!selectedSupplier) {
      message.warning('Vui lòng chọn nhà cung cấp.');
      return;
    }
    if (!isDraft && !contractNo.trim()) {
      message.warning('Vui lòng nhập số chứng từ.');
      return;
    }
    const payloadLines: PurchaseOrderLineWrite[] = lines.map((r) => ({
      description: r.description.trim() || null,
      assetId: r.assetId,
      quantity: r.quantity,
      unit: r.unit.trim() || null,
      unitPrice: r.unitPrice,
      expectedDeliveryDate: r.expectedDelivery ? r.expectedDelivery.format('YYYY-MM-DD') : null,
    }));
    const kept = payloadLines.filter((l) => l.quantity > 0);
    if (kept.length === 0 && mode !== 'edit' && !isDraft) {
      message.warning('Cần ít nhất một dòng hàng với số lượng lớn hơn 0.');
      return;
    }
    setLoading(true);
    try {
      await onSubmit({
        supplierId: selectedSupplier.supplierId,
        currency,
        contractNo: contractNo.trim() || undefined,
        assetRequestId: assetRequestId.trim() ? parseInt(assetRequestId.trim(), 10) : null,
        lines: kept.length > 0 ? kept : [],
        isDraft,
      });
      message.success(isDraft ? 'Đã lưu nháp đơn mua.' : (mode === 'edit' ? 'Đã cập nhật đơn mua.' : 'Đã tạo đơn mua.'));
      onClose();
    } catch (e: unknown) {
      const err = e as { response?: { data?: string } };
      message.error(err?.response?.data ?? 'Thao tác thất bại.');
    } finally {
      setLoading(false);
    }
  };

  if (!open) return null;

  const totalAmount = lines.reduce((sum, line) => sum + line.quantity * line.unitPrice, 0);

  return (
    <div className="po-form-modal-overlay" role="dialog" aria-modal="true">
      <div className="po-form-modal">
        <button
          type="button"
          className="po-form-modal__close-btn"
          onClick={onClose}
          aria-label="Đóng"
        >
          <span className="po-form-modal__close">×</span>
        </button>

        <div className="po-form-modal__header">
          <h2 className="po-form-modal__title">
            {mode === 'edit' ? 'Cập nhật đơn mua' : 'Tạo đơn mua'}
          </h2>
        </div>

        <div className="po-form-modal__body">
          <div className="po-form-modal__content">
            <div className="po-form-section">
              <h3 className="po-form-section-title">Thông tin chung</h3>

              <div className="po-form-row">
                <div className="po-form-item">
                  <label>
                    Nhà cung cấp<span style={{ color: '#ef4444' }}>*</span>
                  </label>
                  <div className="po-form-selection-wrapper">
                    <div className="po-form-selection-display">
                      {selectedSupplier ? selectedSupplier.name : 'Chưa chọn'}
                    </div>
                    <button
                      type="button"
                      className="po-form-btn-select"
                      onClick={() => setIsSupplierModalOpen(true)}
                    >
                      {selectedSupplier ? 'Đổi' : 'Chọn'}
                    </button>
                  </div>
                </div>
                <div className="po-form-item">
                  <label>
                    Số chứng từ<span style={{ color: '#ef4444' }}>*</span>
                  </label>
                  <input
                    type="text"
                    className="po-form-input"
                    placeholder="VD: PO-2024-001, HD-001..."
                    value={contractNo}
                    onChange={(e) => setContractNo(e.target.value)}
                  />
                </div>
              </div>

              <div className="po-form-row">
                <div className="po-form-item">
                  <label>Tiền tệ<span style={{ color: '#ef4444' }}>*</span></label>
                  <select
                    className="po-form-select"
                    value={currency}
                    onChange={(e) => setCurrency(e.target.value)}
                  >
                    {CURRENCIES.map((c) => (
                      <option key={c} value={c}>
                        {c}
                      </option>
                    ))}
                  </select>
                </div>
                <div className="po-form-item">
                  <label>Mã yêu cầu (tuỳ chọn)</label>
                  <div style={{ position: 'relative' }}>
                    <input
                      type="text"
                      className="po-form-input"
                      placeholder="Để trống, hoặc nhập mã và nhấn Enter / chọn từ danh sách để lấy vật tư"
                      value={assetRequestId}
                      onChange={(e) => onAssetRequestIdChange(e.target.value)}
                      onKeyDown={(e) => {
                        if (e.key === 'Enter') {
                          e.preventDefault();
                          void tryLoadLinesFromRequestInput();
                        }
                      }}
                      onFocus={() => setShowAssetRequestDropdown(true)}
                      onBlur={() => setTimeout(() => setShowAssetRequestDropdown(false), 200)}
                      disabled={requestLinesLoading}
                    />
                    {requestLinesLoading && (
                      <span className="po-form-inline-hint" style={{ marginTop: 4, display: 'block' }}>
                        Đang tải vật tư từ đơn yêu cầu…
                      </span>
                    )}
                    {linkedRequestId != null && (
                      <span className="po-form-inline-hint" style={{ marginTop: 4, display: 'block' }}>
                        Đang gắn với YC-{linkedRequestId}. Chỉ có đơn giá được chỉnh sửa.
                      </span>
                    )}
                    {showAssetRequestDropdown && assetRequestOptions.length > 0 && (
                      <div className="po-form-dropdown">
                        {assetRequestOptions.slice(0, 10).map((req) => (
                          <div
                            key={req.assetRequestId}
                            className="po-form-dropdown-item"
                            onMouseDown={(e) => e.preventDefault()}
                            onClick={() => {
                              setAssetRequestId(String(req.assetRequestId));
                              setShowAssetRequestDropdown(false);
                              void loadLinesFromRequest(req.assetRequestId);
                            }}
                          >
                            YC-{req.assetRequestId} - {req.title}
                          </div>
                        ))}
                      </div>
                    )}
                  </div>
                </div>
              </div>
            </div>

            <div className="po-form-total-section">
              <div className="po-form-total-label">Tổng tiền:</div>
              <div className="po-form-total-value">
                {totalAmount.toLocaleString('vi-VN')} {currency}
              </div>
            </div>

            <div className="po-form-section">
              <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: 16 }}>
                <h3 className="po-form-section-title" style={{ marginBottom: 0 }}>
                  Danh sách hàng hóa / dịch vụ
                </h3>
                {linkedRequestId == null && (
                  <button
                    type="button"
                    className="po-form-btn-add-line"
                    onClick={() => setLines((prev) => [...prev, emptyRow()])}
                  >
                    <PlusOutlined /> Thêm dòng
                  </button>
                )}
              </div>

              <div className="po-form-table-wrapper">
                <table className="po-form-table">
                  <thead>
                    <tr>
                      <th style={{ width: '40px' }}>STT</th>
                      <th style={{ width: '200px' }}>Tài sản</th>
                      <th style={{ width: '200px' }}>Mô tả</th>
                      <th style={{ width: '80px' }}>SL</th>
                      <th style={{ width: '80px' }}>ĐVT</th>
                      <th style={{ width: '120px' }}>Đơn giá</th>
                      <th style={{ width: '140px' }}>Ngày giao DK</th>
                      <th style={{ width: '120px' }}>Thành tiền</th>
                      <th style={{ width: '60px' }}></th>
                    </tr>
                  </thead>
                  <tbody>
                    {lines.map((row, idx) => {
                      const lineLocked = linkedRequestId != null;
                      return (
                      <tr key={row.key}>
                        <td className="po-form-table-center">{idx + 1}</td>
                        <td>
                          <div className="po-form-asset-select">
                            <input
                              type="text"
                              className="po-form-input po-form-input--fixed"
                              placeholder="Chọn tài sản"
                              value={row.assetName}
                              readOnly
                              onClick={() => {
                                if (lineLocked) return;
                                setCurrentEditingLineKey(row.key);
                                setIsAssetModalOpen(true);
                              }}
                            />
                            <button
                              type="button"
                              className="po-form-btn-select-small"
                              disabled={lineLocked}
                              onClick={() => {
                                if (lineLocked) return;
                                setCurrentEditingLineKey(row.key);
                                setIsAssetModalOpen(true);
                              }}
                            >
                              Chọn
                            </button>
                          </div>
                        </td>
                        <td>
                          <textarea
                            className="po-form-textarea po-form-textarea--fixed"
                            rows={2}
                            value={row.description}
                            readOnly={lineLocked}
                            onChange={(e) => updateLine(row.key, { description: e.target.value })}
                            placeholder="Mô tả hàng hóa / dịch vụ"
                          />
                        </td>
                        <td>
                          <InputNumber
                            min={0.0001}
                            step={1}
                            className="po-form-input-number--fixed"
                            style={{ width: '100%', height: '40px' }}
                            value={row.quantity}
                            disabled={lineLocked}
                            onChange={(v) => updateLine(row.key, { quantity: Number(v) || 1 })}
                          />
                        </td>
                        <td>
                          <input
                            type="text"
                            className="po-form-input po-form-input--fixed"
                            value={row.unit}
                            readOnly={lineLocked}
                            onChange={(e) => updateLine(row.key, { unit: e.target.value })}
                          />
                        </td>
                        <td>
                          <InputNumber
                            min={0}
                            className="po-form-input-number--fixed"
                            style={{ width: '100%', height: '40px' }}
                            value={row.unitPrice}
                            onChange={(v) => updateLine(row.key, { unitPrice: Number(v) || 0 })}
                            formatter={(value) => `${value}`.replace(/\B(?=(\d{3})+(?!\d))/g, ',')}
                            parser={(value) => Number(String(value ?? '').replace(/\$\s?|(,*)/g, '')) || 0}
                          />
                        </td>
                        <td>
                          <DatePicker
                            className="po-form-datepicker--fixed"
                            style={{ width: '100%', height: '40px' }}
                            value={row.expectedDelivery}
                            disabled={lineLocked}
                            onChange={(d) => updateLine(row.key, { expectedDelivery: d })}
                            format="DD/MM/YYYY"
                          />
                        </td>
                        <td className="po-form-table-right">
                          {(row.quantity * row.unitPrice).toLocaleString('vi-VN')}
                        </td>
                        <td className="po-form-table-center">
                          <Button
                            type="text"
                            danger
                            icon={<DeleteOutlined />}
                            disabled={lineLocked || lines.length <= 1}
                            onClick={() => setLines((prev) => prev.filter((r) => r.key !== row.key))}
                          />
                        </td>
                      </tr>
                    );})}
                  </tbody>
                </table>
              </div>
            </div>
          </div>
        </div>

        <div className="po-form-modal__footer">
          {mode === 'create' && (
            <button
              type="button"
              onClick={() => handleSubmit(true)}
              className="po-form-btn-draft"
              disabled={loading}
            >
              Lưu nháp
            </button>
          )}
          <button
            type="button"
            onClick={() => handleSubmit(false)}
            className="po-form-btn-submit"
            disabled={loading}
          >
            {mode === 'edit' ? 'Cập nhật' : 'Tạo đơn'}
          </button>
          <button type="button" onClick={onClose} className="po-form-btn-cancel">
            Hủy
          </button>
        </div>
      </div>

      <SupplierSelectionModal
        open={isSupplierModalOpen}
        onClose={() => setIsSupplierModalOpen(false)}
        onSelect={(supplier) => {
          setSelectedSupplier(supplier);
          setIsSupplierModalOpen(false);
        }}
      />

      <AssetSelectionModal
        open={isAssetModalOpen}
        onClose={() => {
          setIsAssetModalOpen(false);
          setCurrentEditingLineKey(null);
        }}
        onSelect={(asset) => {
          if (currentEditingLineKey) {
            updateLine(currentEditingLineKey, {
              assetId: asset.assetId,
              assetName: `${asset.code} — ${asset.name}`,
            });
          }
          setIsAssetModalOpen(false);
          setCurrentEditingLineKey(null);
        }}
      />
    </div>
  );
}

import { useEffect, useState } from 'react';
import { message, InputNumber, DatePicker, Button } from 'antd';
import { PlusOutlined, DeleteOutlined } from '@ant-design/icons';
import dayjs, { type Dayjs } from 'dayjs';
import { supplierService, type SupplierItem } from '../../admin/services/supplierService';
import { assetService } from '../../assets/services/assetService';
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
const FIXED_UNITS = ['Cái', 'Chiếc', 'Bộ'] as const;
const OTHER_UNIT_VALUE = '__other__';

interface LineRow {
  key: string;
  description: string;
  assetId: number | null;
  assetName: string;
  requestedAssetTypeId: number | null;
  requestedAssetTypeName: string;
  quantity: number;
  unit: string;
  unitPrice: number;
  expectedDelivery: Dayjs | null;
  sourceLineId?: number;
}

function toRows(lines: PurchaseOrderDetail['lines']): LineRow[] {
  return lines.map((l, i) => {
    const typeName = (l.assetTypeName ?? '').trim() || (l.description ?? '').trim();
    const typeId = l.assetTypeId ?? null;
    return {
      key: `l-${l.lineId}-${i}`,
      description: l.description ?? '',
      assetId: l.assetId,
      assetName: [l.assetCode, l.assetName].filter(Boolean).join(' — ') || '',
      requestedAssetTypeId: typeId,
      requestedAssetTypeName: typeName,
      quantity: Number(l.quantity),
      unit: l.unit ?? 'Cái',
      unitPrice: Number(l.unitPrice),
      expectedDelivery: l.expectedDeliveryDate ? dayjs(l.expectedDeliveryDate) : null,
      sourceLineId: undefined,
    };
  });
}

function emptyRow(): LineRow {
  return {
    key: `new-${Date.now()}-${Math.random()}`,
    description: '',
    assetId: null,
    assetName: '',
    requestedAssetTypeId: null,
    requestedAssetTypeName: '',
    quantity: 1,
    unit: 'Cái',
    unitPrice: 0,
    expectedDelivery: null,
    sourceLineId: undefined,
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
    requestedAssetTypeId: null,
    requestedAssetTypeName: '',
    quantity: Number(l.quantity) > 0 ? Number(l.quantity) : 1,
    unit: (l.unit ?? 'Cái').trim() || 'Cái',
    unitPrice: parseEstimatedPrice(l.estimatedPrice),
    expectedDelivery: null,
    sourceLineId: l.lineId,
  }));
}

function parseRequestedAssetTypes(proposedData: string | null | undefined): Array<{ assetTypeId: number | null; assetTypeName: string }> {
  try {
    if (!proposedData) return [];
    const parsed = JSON.parse(proposedData) as {
      equipment?: Array<{ assetTypeId?: number | string; assetTypeName?: string }>;
    };
    if (!Array.isArray(parsed.equipment)) return [];
    return parsed.equipment.map((row) => {
      const id = Number(row.assetTypeId);
      return {
        assetTypeId: Number.isFinite(id) && id > 0 ? id : null,
        assetTypeName: String(row.assetTypeName ?? '').trim(),
      };
    });
  } catch {
    return [];
  }
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
  /** Chỉ cần thiết khi mode === 'edit' để biết đơn đang là nháp hay đã tạo */
  initialStatus?: number;
}

export function PurchaseOrderFormModalNew({
  open,
  mode,
  initial,
  onClose,
  onSubmit,
  initialStatus,
}: PurchaseOrderFormModalNewProps) {
  const [selectedSupplier, setSelectedSupplier] = useState<SupplierItem | null>(null);
  const [currency, setCurrency] = useState<string>('VND');
  const [contractNo, setContractNo] = useState<string>('');
  const [assetRequestId, setAssetRequestId] = useState<string>('');
  const [assetRequestOptions, setAssetRequestOptions] = useState<PurchaseOrderListItem[]>([]);
  const [showAssetRequestDropdown, setShowAssetRequestDropdown] = useState(false);
  const [lines, setLines] = useState<LineRow[]>([emptyRow()]);
  const [expectedDeliveryDate, setExpectedDeliveryDate] = useState<Dayjs | null>(null);
  /** Khi có giá trị: các dòng được lấy từ đơn yêu cầu; chỉ chỉnh sửa đơn giá. */
  const [linkedRequestId, setLinkedRequestId] = useState<number | null>(null);
  const [requestLinesLoading, setRequestLinesLoading] = useState(false);
  const [loading, setLoading] = useState(false);
  const [isSupplierModalOpen, setIsSupplierModalOpen] = useState(false);
  const [isAssetModalOpen, setIsAssetModalOpen] = useState(false);
  const [currentEditingLineKey, setCurrentEditingLineKey] = useState<string | null>(null);
  const [expectedRequestRows, setExpectedRequestRows] = useState<
    Array<{ sourceLineId: number; quantity: number; requestedAssetTypeId: number | null }>
  >([]);

  useEffect(() => {
    if (!open) return;
    let cancelled = false;
    (async () => {
      try {
        const requests = await purchaseOrderService.getList();
        if (!cancelled) {
          // Chỉ cho kế toán tạo PO từ các yêu cầu mua đã duyệt.
          setAssetRequestOptions(requests.filter((req) => req.status === 2));
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
      const mappedLines = initial.lines.length > 0 ? toRows(initial.lines) : [emptyRow()];
      setLines(mappedLines);
      setExpectedDeliveryDate(mappedLines[0]?.expectedDelivery ?? null);
      if (initial.assetRequestId != null && initial.assetRequestId > 0) {
        setLinkedRequestId(initial.assetRequestId);
        setExpectedRequestRows(
          mappedLines.map((r) => ({
            sourceLineId: r.sourceLineId ?? 0,
            quantity: r.quantity,
            requestedAssetTypeId: r.requestedAssetTypeId,
          })),
        );
      } else {
        setLinkedRequestId(null);
        setExpectedRequestRows([]);
      }
    } else {
      setSelectedSupplier(null);
      setCurrency('VND');
      setContractNo('');
      setAssetRequestId('');
      setLines([emptyRow()]);
      setExpectedDeliveryDate(null);
      setExpectedRequestRows([]);
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
      const req = assetRequestOptions.find((x) => x.assetRequestId === id);
      const requestedTypes = parseRequestedAssetTypes(req?.proposedData);
      const typeList = await assetService.getAssetTypes().catch(() => []);
      const mapped = requestLinesToRows(raw).map((line, idx) => {
        const t = requestedTypes[idx];
        let requestedAssetTypeId = t?.assetTypeId ?? null;
        let requestedAssetTypeName = String(t?.assetTypeName ?? '').trim();
        if (requestedAssetTypeId == null && requestedAssetTypeName && typeList.length > 0) {
          const low = requestedAssetTypeName.toLowerCase();
          const foundByName = typeList.find((x) => x.name.trim().toLowerCase() === low);
          if (foundByName) requestedAssetTypeId = foundByName.assetTypeId;
        }
        if (!requestedAssetTypeName && requestedAssetTypeId != null && typeList.length > 0) {
          const foundById = typeList.find((x) => x.assetTypeId === requestedAssetTypeId);
          if (foundById) requestedAssetTypeName = foundById.name;
        }
        return {
          ...line,
          requestedAssetTypeId,
          requestedAssetTypeName,
          expectedDelivery: expectedDeliveryDate,
        };
      });
      setLines(mapped);
      setExpectedRequestRows(
        mapped.map((line) => ({
          sourceLineId: line.sourceLineId ?? 0,
          quantity: line.quantity,
          requestedAssetTypeId: line.requestedAssetTypeId,
        }))
      );
    } catch {
      message.error('Không tải được danh sách vật tư từ đơn yêu cầu.');
      setLinkedRequestId(null);
      setExpectedRequestRows([]);
    } finally {
      setRequestLinesLoading(false);
    }
  };

  const onAssetRequestIdChange = (value: string) => {
    setAssetRequestId(value);
    const trimmed = value.trim();
    if (!trimmed) {
      setLinkedRequestId(null);
      setExpectedRequestRows([]);
      if (mode === 'create') {
        setLines([{ ...emptyRow(), expectedDelivery: expectedDeliveryDate }]);
      }
      return;
    }
    const parsed = parseInt(trimmed, 10);
    if (!Number.isFinite(parsed) || parsed <= 0) {
      setLinkedRequestId(null);
      setExpectedRequestRows([]);
      return;
    }
    if (linkedRequestId !== null && linkedRequestId !== parsed) {
      setLinkedRequestId(null);
      setExpectedRequestRows([]);
      if (mode === 'create') {
        setLines([{ ...emptyRow(), expectedDelivery: expectedDeliveryDate }]);
      }
    }
  };

  /** Tải dòng từ đơn yêu cầu theo mã đang nhập (Enter hoặc gọi sau khi chọn từ danh sách). */
  const tryLoadLinesFromRequestInput = async () => {
    const trimmed = assetRequestId.trim();
    if (!trimmed) {
      setLinkedRequestId(null);
      setExpectedRequestRows([]);
      if (mode === 'create') {
        setLines([{ ...emptyRow(), expectedDelivery: expectedDeliveryDate }]);
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
    
    // Cảnh báo nhẹ nếu link với yêu cầu mua nhưng không mua đủ
    if (!isDraft && linkedRequestId != null && expectedRequestRows.length > 0) {
      if (lines.length < expectedRequestRows.length) {
        const missing = expectedRequestRows.length - lines.length;
        message.info(`Lưu ý: Đơn mua chỉ có ${lines.length}/${expectedRequestRows.length} loại tài sản (còn thiếu ${missing} loại).`);
      }
    }
    
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
      // Xác định message phù hợp
      let successMessage = 'Đã tạo đơn mua.';
      if (isDraft) {
        successMessage = mode === 'edit' ? 'Đã cập nhật nháp đơn mua.' : 'Đã lưu nháp đơn mua.';
      } else if (mode === 'edit') {
        // Khi edit và không phải draft, kiểm tra trạng thái ban đầu
        successMessage = initialStatus === -1 ? 'Đã tạo đơn mua.' : 'Đã cập nhật đơn mua.';
      }
      message.success(successMessage);
      onClose();
    } catch (e: unknown) {
      const err = e as { response?: { data?: string } };
      message.error(err?.response?.data ?? 'Thao tác thất bại.');
    } finally {
      setLoading(false);
    }
  };

  if (!open) return null;

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
                <div className="po-form-item" />
              </div>

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

            <div className="po-form-section">
              <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: 16 }}>
                <h3 className="po-form-section-title" style={{ marginBottom: 0 }}>
                  Danh sách hàng hóa / dịch vụ
                </h3>
                {linkedRequestId == null && (
                  <button
                    type="button"
                    className="po-form-btn-add-line"
                    onClick={() =>
                      setLines((prev) => [
                        ...prev,
                        { ...emptyRow(), expectedDelivery: expectedDeliveryDate },
                      ])
                    }
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
                      <th style={{ width: '140px' }}>Loại tài sản</th>
                      <th style={{ width: '200px' }}>Tài sản</th>
                      <th style={{ width: '100px' }}>Số lượng</th>
                      <th style={{ width: '110px' }}>Đơn vị tính</th>
                      <th style={{ width: '120px' }}>Đơn giá</th>
                      <th style={{ width: '160px' }}>Ngày giao dự kiến</th>
                      <th style={{ width: '120px' }}>Thành tiền</th>
                      <th style={{ width: '60px' }}></th>
                    </tr>
                  </thead>
                  <tbody>
                    {lines.map((row, idx) => {
                      const lineFromRequest = linkedRequestId != null;
                      return (
                      <tr key={row.key}>
                        <td className="po-form-table-center">{idx + 1}</td>
                        <td className="po-form-table-muted">
                          {row.requestedAssetTypeName.trim() ? row.requestedAssetTypeName : '—'}
                        </td>
                        <td>
                          <div className="po-form-asset-select">
                            <input
                              type="text"
                              className="po-form-input po-form-input--fixed"
                              placeholder="Chọn tài sản"
                              value={row.assetName}
                              readOnly
                              onClick={() => {
                                setCurrentEditingLineKey(row.key);
                                setIsAssetModalOpen(true);
                              }}
                            />
                            <button
                              type="button"
                              className="po-form-btn-select-small"
                              onClick={() => {
                                setCurrentEditingLineKey(row.key);
                                setIsAssetModalOpen(true);
                              }}
                            >
                              Chọn
                            </button>
                          </div>
                        </td>
                        <td>
                          <InputNumber
                            min={0.0001}
                            step={1}
                            className="po-form-input-number--fixed"
                            style={{ width: '100%', height: '40px' }}
                            value={row.quantity}
                            onChange={(v) => updateLine(row.key, { quantity: Number(v) || 1 })}
                          />
                        </td>
                        <td>
                          {(() => {
                            const normalized = String(row.unit ?? '').trim();
                            const isFixedUnit = FIXED_UNITS.some((u) => u === normalized);
                            const selectValue = isFixedUnit ? normalized : OTHER_UNIT_VALUE;
                            return (
                              <>
                                <select
                                  className="po-form-select"
                                  style={{ width: '100%', height: '40px' }}
                                  value={selectValue}
                                  onChange={(e) => {
                                    const next = e.target.value;
                                    if (next === OTHER_UNIT_VALUE) {
                                      if (isFixedUnit || !normalized) {
                                        updateLine(row.key, { unit: '' });
                                      }
                                      return;
                                    }
                                    updateLine(row.key, { unit: next });
                                  }}
                                >
                                  {FIXED_UNITS.map((unit) => (
                                    <option key={unit} value={unit}>
                                      {unit}
                                    </option>
                                  ))}
                                  <option value={OTHER_UNIT_VALUE}>Khác...</option>
                                </select>
                                {selectValue === OTHER_UNIT_VALUE && (
                                  <input
                                    type="text"
                                    className="po-form-input po-form-input--fixed"
                                    style={{ marginTop: 6 }}
                                    placeholder="Nhập đơn vị tính"
                                    value={normalized}
                                    onChange={(e) => updateLine(row.key, { unit: e.target.value })}
                                  />
                                )}
                              </>
                            );
                          })()}
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
                            onChange={(d) => updateLine(row.key, { expectedDelivery: d })}
                            format="DD/MM/YYYY"
                            popupStyle={{ zIndex: 2000 }}
                            getPopupContainer={() => document.body}
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
                            disabled={lines.length <= 1}
                            onClick={() => setLines((prev) => prev.filter((r) => r.key !== row.key))}
                            title={lineFromRequest ? "Xóa dòng này (đơn mua có thể không đủ yêu cầu)" : "Xóa dòng"}
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
          {(mode === 'create' || (mode === 'edit' && initialStatus === -1)) && (
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
            {mode === 'edit' && initialStatus === -1 ? 'Tạo đơn' : mode === 'edit' ? 'Cập nhật' : 'Tạo đơn'}
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
            const row = lines.find((l) => l.key === currentEditingLineKey);
            if (linkedRequestId != null && row) {
              if (row.requestedAssetTypeId != null && row.requestedAssetTypeId > 0) {
                if (asset.assetTypeId !== row.requestedAssetTypeId) {
                  message.error('Tài sản phải cùng loại với yêu cầu mua đã chọn.');
                  return;
                }
              } else if (row.requestedAssetTypeName.trim()) {
                const at = (asset.assetTypeName ?? '').trim().toLowerCase();
                const req = row.requestedAssetTypeName.trim().toLowerCase();
                if (at !== req) {
                  message.error('Tài sản phải cùng loại với yêu cầu mua đã chọn.');
                  return;
                }
              }
            }
            updateLine(currentEditingLineKey, {
              assetId: asset.assetId,
              assetName: `${asset.code} — ${asset.name}`,
            });
          }
          setIsAssetModalOpen(false);
          setCurrentEditingLineKey(null);
        }}
        filterAssetTypeId={
          currentEditingLineKey
            ? (lines.find((l) => l.key === currentEditingLineKey)?.requestedAssetTypeId ?? null)
            : null
        }
        filterAssetTypeName={
          currentEditingLineKey
            ? (lines.find((l) => l.key === currentEditingLineKey)?.requestedAssetTypeName?.trim() || null)
            : null
        }
      />
    </div>
  );
}

import { useEffect, useRef, useState } from 'react';
import dayjs, { type Dayjs } from 'dayjs';
import { uploadAssetFile } from '../../assets/services/assetDocumentUploadService';
import '../../assets/pages/AssetCreatePage.css';
import {
  procurementPoService,
  type PurchaseOrderDetail,
  type PurchaseOrderListItem,
} from '../services/procurementPoService';
import { supplierService, type SupplierItem } from '../../admin/services/supplierService';
import { supplierInvoiceService } from '../services/supplierInvoiceService';
import './SupplierInvoiceFormModal.css';

interface PoLineEdit {
  procurementLineId: number;
  quantity: number;
  unitPrice: number;
}

interface MiscChargeRow {
  id: string;
  description: string;
  amount: number;
}

function newMiscRowId(): string {
  if (typeof crypto !== 'undefined' && 'randomUUID' in crypto) {
    return crypto.randomUUID();
  }
  return `misc-${Date.now()}-${Math.random().toString(36).slice(2, 9)}`;
}

interface ThreeWayMatchWarning {
  field: string;
  poValue: number;
  invoiceValue: number;
  tolerance: number;
  exceeded: boolean;
}

// Helper functions for money input (from RepairCompleteModal)
function parseNumberInput(value: string): number | undefined {
  const normalized = value.replace(/[^\d]/g, '');
  if (!normalized) return undefined;
  const parsed = Number(normalized);
  return Number.isFinite(parsed) ? parsed : undefined;
}

type SiFormDocRow = {
  id: string;
  fileName: string;
  url?: string;
  uploading?: boolean;
  error?: string;
};

interface SupplierInvoiceFormModalProps {
  open: boolean;
  onClose: () => void;
  onSubmit: (values: {
    procurementId: number;
    goodsReceiptId: number | null;
    supplierId: number;
    invoiceNumber: string;
    invoiceDate: string;
    note: string | null;
    attachmentFileUrls?: string[];
    lines: Array<{
      procurementLineId: number | null;
      goodsReceiptLineId?: number;
      chargeDescription?: string | null;
      quantity: number;
      unitPrice: number;
    }>;
  }) => Promise<void>;
}

export function SupplierInvoiceFormModal({
  open,
  onClose,
  onSubmit,
}: SupplierInvoiceFormModalProps) {
  const [suppliers, setSuppliers] = useState<SupplierItem[]>([]);
  const [supplierId, setSupplierId] = useState<number | null>(null);
  const [poOptions, setPoOptions] = useState<PurchaseOrderListItem[]>([]);
  const [selectedPoId, setSelectedPoId] = useState<number | null>(null);
  const [poDetail, setPoDetail] = useState<PurchaseOrderDetail | null>(null);
  const [poLineEdits, setPoLineEdits] = useState<PoLineEdit[]>([]);
  const [miscCharges, setMiscCharges] = useState<MiscChargeRow[]>([]);
  const [invoiceNumber, setInvoiceNumber] = useState('');
  const [invoiceDate, setInvoiceDate] = useState<Dayjs | null>(dayjs());
  const [note, setNote] = useState('');
  const [refLoading, setRefLoading] = useState(false);
  const [submitting, setSubmitting] = useState(false);
  const [errors, setErrors] = useState<Record<string, string>>({});
  const [threeWayWarnings, setThreeWayWarnings] = useState<ThreeWayMatchWarning[]>([]);
  const [checkingDuplicate, setCheckingDuplicate] = useState(false);
  const [duplicateWarning, setDuplicateWarning] = useState<string>('');

  const [documents, setDocuments] = useState<SiFormDocRow[]>([]);
  const docFileInputRef = useRef<HTMLInputElement | null>(null);
  const pickDocument = () => docFileInputRef.current?.click();

  const onDocFileSelected = async (e: React.ChangeEvent<HTMLInputElement>) => {
    const file = e.target.files?.[0];
    e.target.value = '';
    if (!file) return;
    const rowId = crypto.randomUUID();
    setDocuments((prev) => [...prev, { id: rowId, fileName: file.name, uploading: true }]);
    try {
      const { url, fileName } = await uploadAssetFile(file);
      setDocuments((prev) =>
        prev.map((d) =>
          d.id === rowId
            ? { ...d, url, fileName: fileName || file.name, uploading: false, error: undefined }
            : d
        )
      );
    } catch {
      setDocuments((prev) =>
        prev.map((d) =>
          d.id === rowId ? { ...d, uploading: false, error: 'Tải lên thất bại.' } : d
        )
      );
    }
  };

  const removeDocumentRow = (id: string) => {
    setDocuments((prev) => prev.filter((d) => d.id !== id));
  };

  const downloadAllDocumentUrls = () => {
    const urls = documents.filter((d) => d.url).map((d) => d.url as string);
    urls.forEach((u) => window.open(u, '_blank', 'noopener,noreferrer'));
  };

  useEffect(() => {
    if (open) {
      setSupplierId(null);
      setSelectedPoId(null);
      setDocuments([]);
      setPoDetail(null);
      setPoLineEdits([]);
      setMiscCharges([]);
      setInvoiceNumber('');
      setInvoiceDate(dayjs());
      setNote('');
      setErrors({});
      setThreeWayWarnings([]);
      setDuplicateWarning('');

      let cancelled = false;
      (async () => {
        setRefLoading(true);
        try {
          const [pos, sups] = await Promise.all([
            procurementPoService.getList({ pageSize: 200, page: 1 }),
            supplierService.getAll(),
          ]);
          if (!cancelled) {
            setPoOptions(pos.items.filter((p) => p.status !== 2));
            setSuppliers(sups);
          }
        } catch {
          if (!cancelled) {
            setPoOptions([]);
            setSuppliers([]);
          }
        } finally {
          if (!cancelled) setRefLoading(false);
        }
      })();

      return () => {
        cancelled = true;
      };
    }
  }, [open]);

  const onSelectPoForCreate = async (procurementId: number) => {
    setSelectedPoId(procurementId);
    setMiscCharges([]);
    setRefLoading(true);
    setThreeWayWarnings([]);
    try {
      const d = await procurementPoService.getById(procurementId);
      setPoDetail(d);
      setSupplierId(d.supplierId);
      setErrors((prev) => {
        const next = { ...prev };
        delete next.supplierId;
        return next;
      });
      setPoLineEdits(
        d.lines.map((l) => ({
          procurementLineId: l.lineId,
          quantity: Number(l.quantity),
          unitPrice: Number(l.unitPrice),
        })),
      );
    } catch {
      setPoDetail(null);
      setPoLineEdits([]);
    } finally {
      setRefLoading(false);
    }
  };

  const updatePoLine = (procurementLineId: number, patch: Partial<PoLineEdit>) => {
    setPoLineEdits((prev) =>
      prev.map((r) => (r.procurementLineId === procurementLineId ? { ...r, ...patch } : r)),
    );
    // Recalculate three-way matching warnings when quantity or price changes
    if (patch.quantity !== undefined || patch.unitPrice !== undefined) {
      performThreeWayMatching();
    }
  };

  // Three-Way Matching: Check discrepancies between PO and Invoice
  const performThreeWayMatching = () => {
    if (!poDetail) return;

    const QUANTITY_TOLERANCE = 0.05; // 5%
    const PRICE_TOLERANCE = 0.02; // 2%
    const warnings: ThreeWayMatchWarning[] = [];

    poLineEdits.forEach((edit) => {
      const poLine = poDetail.lines.find((l) => l.lineId === edit.procurementLineId);
      if (!poLine) return;

      const poQty = Number(poLine.quantity);
      const poPrice = Number(poLine.unitPrice);
      const invoiceQty = edit.quantity;
      const invoicePrice = edit.unitPrice;

      // Check quantity variance
      if (invoiceQty > 0) {
        const qtyDiff = Math.abs(invoiceQty - poQty);
        const qtyVariance = qtyDiff / poQty;
        if (qtyVariance > QUANTITY_TOLERANCE) {
          warnings.push({
            field: `Số lượng dòng ${poLine.lineIndex + 1}`,
            poValue: poQty,
            invoiceValue: invoiceQty,
            tolerance: QUANTITY_TOLERANCE * 100,
            exceeded: true,
          });
        }
      }

      // Check price variance
      if (invoicePrice > 0) {
        const priceDiff = Math.abs(invoicePrice - poPrice);
        const priceVariance = priceDiff / poPrice;
        if (priceVariance > PRICE_TOLERANCE) {
          warnings.push({
            field: `Đơn giá dòng ${poLine.lineIndex + 1}`,
            poValue: poPrice,
            invoiceValue: invoicePrice,
            tolerance: PRICE_TOLERANCE * 100,
            exceeded: true,
          });
        }
      }
    });

    setThreeWayWarnings(warnings);
  };

  // Duplicate Invoice Detection
  const checkDuplicateInvoice = async (invoiceNo: string, supId: number) => {
    if (!invoiceNo.trim() || !supId) return;

    setCheckingDuplicate(true);
    setDuplicateWarning('');

    try {
      // Check for duplicate invoices in the last 90 days
      const dateTo = dayjs().toISOString();
      const dateFrom = dayjs().subtract(90, 'day').toISOString();

      const result = await supplierInvoiceService.getList({
        invoiceNumber: invoiceNo.trim(),
        supplierId: supId,
        dateFrom,
        dateTo,
        page: 1,
        pageSize: 10,
      });

      if (result.items.length > 0) {
        const duplicates = result.items.map((inv) => `#${inv.supplierInvoiceId}`).join(', ');
        setDuplicateWarning(
          `⚠️ Cảnh báo: Số hóa đơn này đã tồn tại trong 90 ngày qua (${duplicates}). Vui lòng kiểm tra lại.`,
        );
      }
    } catch {
      // Ignore error, don't block user
    } finally {
      setCheckingDuplicate(false);
    }
  };

  const handleSubmit = async () => {
    const newErrors: Record<string, string> = {};

    if (!invoiceNumber.trim()) {
      newErrors.invoiceNumber = 'Vui lòng nhập số hóa đơn';
    }
    if (!invoiceDate) {
      newErrors.invoiceDate = 'Vui lòng chọn ngày hóa đơn';
    }
    if (!supplierId) {
      newErrors.supplierId = 'Vui lòng chọn nhà cung cấp';
    }

    if (!selectedPoId || !poDetail) {
      newErrors.reference = 'Vui lòng chọn đơn mua';
    } else {
      const lines = poLineEdits.filter((r) => r.quantity > 0);
      if (lines.length === 0) {
        newErrors.lines = 'Nhập số lượng > 0 cho ít nhất một dòng';
      }
    }

    for (const m of miscCharges) {
      const hasDesc = m.description.trim().length > 0;
      const hasAmt = m.amount > 0;
      if (hasDesc && !hasAmt) {
        newErrors.miscCharges = 'Nhập số tiền cho mọi dòng chi phí đã đặt tên.';
        break;
      }
      if (hasAmt && !hasDesc) {
        newErrors.miscCharges = 'Nhập tên chi phí cho mọi dòng có số tiền.';
        break;
      }
    }

    if (Object.keys(newErrors).length > 0) {
      setErrors(newErrors);
      return;
    }

    // Show warning if there are three-way matching issues
    if (threeWayWarnings.length > 0) {
      const proceed = window.confirm(
        `Phát hiện ${threeWayWarnings.length} sai lệch giữa đơn mua và hóa đơn:\n\n` +
          threeWayWarnings
            .map((w) => `• ${w.field}: ĐM=${w.poValue.toLocaleString('vi-VN')}, HĐ=${w.invoiceValue.toLocaleString('vi-VN')} (vượt ${w.tolerance}%)`)
            .join('\n') +
          '\n\nBạn có chắc chắn muốn tiếp tục?',
      );
      if (!proceed) return;
    }

    // Show warning if duplicate invoice detected
    if (duplicateWarning) {
      const proceed = window.confirm(duplicateWarning + '\n\nBạn có chắc chắn muốn tiếp tục?');
      if (!proceed) return;
    }

    if (documents.some((d) => d.uploading)) {
      window.alert('Đang tải tài liệu lên, vui lòng đợi.');
      return;
    }
    if (documents.some((d) => d.error)) {
      window.alert('Có tài liệu tải lên lỗi. Xóa dòng đó hoặc thêm tài liệu khác.');
      return;
    }

    const attachmentFileUrls = documents
      .filter((d) => d.url)
      .map((d) => d.url as string);

    setSubmitting(true);
    try {
      const poLines = poLineEdits
        .filter((r) => r.quantity > 0)
        .map((r) => ({
          procurementLineId: r.procurementLineId,
          quantity: r.quantity,
          unitPrice: r.unitPrice,
        }));
      const extraLines = miscCharges
        .filter((m) => m.description.trim() && m.amount > 0)
        .map((m) => ({
          procurementLineId: null as number | null,
          chargeDescription: m.description.trim(),
          quantity: 1,
          unitPrice: m.amount,
        }));
      const lines = [...poLines, ...extraLines];
      await onSubmit({
        procurementId: selectedPoId!,
        goodsReceiptId: null,
        supplierId: supplierId!,
        invoiceNumber: invoiceNumber.trim(),
        invoiceDate: invoiceDate!.format('YYYY-MM-DD'),
        note: note.trim() || null,
        attachmentFileUrls: attachmentFileUrls.length > 0 ? attachmentFileUrls : undefined,
        lines,
      });
    } finally {
      setSubmitting(false);
    }
  };

  if (!open) return null;

  const supplierLocked = Boolean(poDetail);
  const supplierDisplayName =
    suppliers.find((s) => s.supplierId === supplierId)?.name ??
    poDetail?.supplierName ??
    '';

  const poLineById = new Map(poDetail?.lines.map((l) => [l.lineId, l]) ?? []);
  const poSubtotal = poLineEdits.reduce((sum, r) => sum + r.quantity * r.unitPrice, 0);
  const miscSubtotal = miscCharges.reduce((sum, m) => sum + (m.amount > 0 ? m.amount : 0), 0);
  const totalAmount = poSubtotal + miscSubtotal;

  return (
    <div className="supplier-invoice-modal-overlay" role="dialog" aria-modal="true">
      <div className="supplier-invoice-modal">
        <button
          type="button"
          className="supplier-invoice-modal__close-btn"
          onClick={onClose}
          aria-label="Đóng"
          disabled={submitting}
        >
          <span className="supplier-invoice-modal__close">×</span>
        </button>

        <div className="supplier-invoice-modal__header">
          <h2 className="supplier-invoice-modal__title">Tạo hóa đơn nhà cung cấp</h2>
        </div>

        <div className="supplier-invoice-modal__body">
          <div className="supplier-invoice-modal__content">
            <div className="supplier-invoice-section">
              <h3 className="supplier-invoice-section-title">Chọn đơn mua</h3>
              <div className="supplier-invoice-form__item">
                <label htmlFor="po-select">
                  Đơn mua<span style={{ color: '#ef4444' }}>*</span>
                </label>
                <select
                  id="po-select"
                  className="supplier-invoice-select"
                  value={selectedPoId ?? ''}
                  onChange={(e) => {
                    const val = e.target.value;
                    if (val) onSelectPoForCreate(Number(val));
                  }}
                  disabled={refLoading}
                >
                  <option value="">-- Chọn đơn mua --</option>
                  {poOptions.map((p) => (
                    <option key={p.procurementId} value={p.procurementId}>
                      {p.contractNo || `ĐM-${p.procurementId}`} — {p.supplierName || 'NCC'}
                    </option>
                  ))}
                </select>
                {errors.reference && (
                  <div className="supplier-invoice-error-text">{errors.reference}</div>
                )}
              </div>
            </div>

            <div className="supplier-invoice-section">
              <h3 className="supplier-invoice-section-title">Thông tin hóa đơn</h3>

              <div className="supplier-invoice-form__row">
                <div className="supplier-invoice-form__item">
                  <label htmlFor={supplierLocked ? 'supplier-readonly' : 'supplier-select'}>
                    Nhà cung cấp<span style={{ color: '#ef4444' }}>*</span>
                  </label>
                  {supplierLocked ? (
                    <>
                      <input
                        id="supplier-readonly"
                        type="text"
                        readOnly
                        className="supplier-invoice-input supplier-invoice-input--disabled"
                        value={supplierDisplayName}
                        title="Nhà cung cấp theo đơn mua đã chọn"
                      />
                      <div style={{ marginTop: 4, fontSize: 12, color: '#6b7280' }}>
                        Theo đơn mua đã chọn, không thể thay đổi nhà cung cấp.
                      </div>
                    </>
                  ) : (
                    <select
                      id="supplier-select"
                      className="supplier-invoice-select"
                      value={supplierId ?? ''}
                      onChange={(e) => {
                        const val = e.target.value;
                        const newSupplierId = val ? Number(val) : null;
                        setSupplierId(newSupplierId);
                        if (val) {
                          setErrors((prev) => {
                            const next = { ...prev };
                            delete next.supplierId;
                            return next;
                          });
                          if (invoiceNumber.trim() && newSupplierId) {
                            checkDuplicateInvoice(invoiceNumber, newSupplierId);
                          }
                        } else {
                          setDuplicateWarning('');
                        }
                      }}
                    >
                      <option value="">-- Chọn nhà cung cấp --</option>
                      {suppliers.map((s) => (
                        <option key={s.supplierId} value={s.supplierId}>
                          {s.name}
                        </option>
                      ))}
                    </select>
                  )}
                  {errors.supplierId && (
                    <div className="supplier-invoice-error-text">{errors.supplierId}</div>
                  )}
                </div>

                <div className="supplier-invoice-form__item">
                  <label htmlFor="invoice-number">
                    Số hóa đơn<span style={{ color: '#ef4444' }}>*</span>
                  </label>
                  <input
                    id="invoice-number"
                    type="text"
                    className="supplier-invoice-input"
                    value={invoiceNumber}
                    onChange={(e) => {
                      setInvoiceNumber(e.target.value);
                      if (e.target.value.trim()) {
                        setErrors((prev) => {
                          const next = { ...prev };
                          delete next.invoiceNumber;
                          return next;
                        });
                        // Trigger duplicate check when both invoice number and supplier are set
                        if (supplierId) {
                          checkDuplicateInvoice(e.target.value, supplierId);
                        }
                      } else {
                        setDuplicateWarning('');
                      }
                    }}
                    placeholder="Nhập số hóa đơn"
                  />
                  {errors.invoiceNumber && (
                    <div className="supplier-invoice-error-text">{errors.invoiceNumber}</div>
                  )}
                  {checkingDuplicate && (
                    <div style={{ marginTop: 4, fontSize: 12, color: '#6b7280' }}>
                      Đang kiểm tra trùng lặp...
                    </div>
                  )}
                  {duplicateWarning && (
                    <div
                      style={{
                        marginTop: 4,
                        fontSize: 12,
                        color: '#f59e0b',
                        padding: 8,
                        background: '#fffbeb',
                        borderRadius: 4,
                        border: '1px solid #fcd34d',
                      }}
                    >
                      {duplicateWarning}
                    </div>
                  )}
                </div>
              </div>

              <div className="supplier-invoice-form__row">
                <div className="supplier-invoice-form__item">
                  <label htmlFor="invoice-date">
                    Ngày hóa đơn<span style={{ color: '#ef4444' }}>*</span>
                  </label>
                  <input
                    id="invoice-date"
                    type="date"
                    className="supplier-invoice-input"
                    value={invoiceDate?.format('YYYY-MM-DD') ?? ''}
                    onChange={(e) => {
                      const val = e.target.value;
                      setInvoiceDate(val ? dayjs(val) : null);
                      if (val) {
                        setErrors((prev) => {
                          const next = { ...prev };
                          delete next.invoiceDate;
                          return next;
                        });
                      }
                    }}
                  />
                  {errors.invoiceDate && (
                    <div className="supplier-invoice-error-text">{errors.invoiceDate}</div>
                  )}
                </div>
              </div>

              <div className="supplier-invoice-form__item">
                <label htmlFor="note">Ghi chú</label>
                <textarea
                  id="note"
                  className="supplier-invoice-textarea"
                  rows={3}
                  value={note}
                  onChange={(e) => setNote(e.target.value)}
                  placeholder="Ghi chú (không bắt buộc)"
                />
              </div>

              <div className="asset-create__section" style={{ marginTop: 8 }}>
                <input
                  ref={docFileInputRef}
                  type="file"
                  style={{ display: 'none' }}
                  onChange={onDocFileSelected}
                />
                <h2 className="asset-create__section-title">Tài liệu đính kèm</h2>
                <div className="asset-create__files">
                  {documents.length === 0 ? (
                    <p className="asset-create__hint">
                      Chưa có tài liệu. Chọn &quot;Thêm tài liệu&quot; để tải lên.
                    </p>
                  ) : (
                    documents.map((d, idx) => (
                      <div key={d.id} className="asset-create__file">
                        <span className="asset-create__file-index">#{idx + 1}</span>
                        <span className="asset-create__file-name asset-create__file-name--grow">
                          {d.uploading
                            ? `Đang tải: ${d.fileName}…`
                            : d.error
                              ? `${d.fileName} — ${d.error}`
                              : d.fileName}
                        </span>
                        {d.url && !d.uploading && (
                          <a
                            href={d.url}
                            target="_blank"
                            rel="noopener noreferrer"
                            className="asset-create__btn asset-create__btn--secondary"
                            style={{ padding: '4px 10px', fontSize: 12 }}
                          >
                            Mở
                          </a>
                        )}
                        <button
                          type="button"
                          className="asset-create__btn asset-create__btn--secondary"
                          style={{ padding: '4px 10px', fontSize: 12 }}
                          onClick={() => removeDocumentRow(d.id)}
                        >
                          Xóa
                        </button>
                      </div>
                    ))
                  )}
                </div>
                <div className="asset-create__file-actions">
                  <button
                    type="button"
                    className="asset-create__btn asset-create__btn--secondary"
                    onClick={downloadAllDocumentUrls}
                    disabled={!documents.some((d) => d.url)}
                  >
                    Tải toàn bộ
                  </button>
                  <button
                    type="button"
                    className="asset-create__btn asset-create__btn--primary"
                    onClick={pickDocument}
                  >
                    Thêm tài liệu
                  </button>
                </div>
              </div>
            </div>

            {poDetail && poLineEdits.length > 0 && (
              <div className="supplier-invoice-section">
                <h3 className="supplier-invoice-section-title">Chi tiết dòng hóa đơn</h3>

                {threeWayWarnings.length > 0 && (
                  <div
                    style={{
                      marginBottom: 12,
                      padding: 12,
                      background: '#fffbeb',
                      border: '1px solid #fcd34d',
                      borderRadius: 6,
                    }}
                  >
                    <div style={{ fontWeight: 600, color: '#f59e0b', marginBottom: 8 }}>
                      ⚠️ Cảnh báo Three-Way Matching ({threeWayWarnings.length} sai lệch)
                    </div>
                    {threeWayWarnings.map((w, idx) => (
                      <div key={idx} style={{ fontSize: 13, color: '#92400e', marginBottom: 4 }}>
                        • {w.field}: Đơn mua = {w.poValue.toLocaleString('en-US')}, Hóa đơn ={' '}
                        {w.invoiceValue.toLocaleString('en-US')} (vượt {w.tolerance}%)
                      </div>
                    ))}
                  </div>
                )}

                <div className="supplier-invoice-table-wrapper">
                  <table className="supplier-invoice-table">
                    <thead>
                      <tr>
                        <th style={{ width: '40px' }}>#</th>
                        <th>Tài sản</th>
                        <th style={{ width: '100px' }}>SL đơn mua</th>
                        <th style={{ width: '120px' }}>SL trên HĐ</th>
                        <th style={{ width: '150px' }}>Đơn giá (đ)</th>
                      </tr>
                    </thead>
                    <tbody>
                      {poLineEdits.map((r, idx) => {
                        const pl = poLineById.get(r.procurementLineId);
                        const label =
                          pl?.description ||
                          [pl?.assetCode, pl?.assetName].filter(Boolean).join(' ') ||
                          `Dòng ${(pl?.lineIndex ?? 0) + 1}`;
                        const max = pl ? Number(pl.quantity) : undefined;
                        return (
                          <tr key={r.procurementLineId}>
                            <td>{idx + 1}</td>
                            <td>{label}</td>
                            <td style={{ textAlign: 'right' }}>
                              {max !== undefined ? max.toLocaleString('en-US') : '—'}
                            </td>
                            <td>
                              <input
                                type="number"
                                className="supplier-invoice-input-number"
                                min={0}
                                max={max}
                                value={r.quantity}
                                onChange={(e) => {
                                  const val = Number(e.target.value);
                                  updatePoLine(r.procurementLineId, { quantity: val });
                                  setErrors((prev) => {
                                    const next = { ...prev };
                                    delete next.lines;
                                    return next;
                                  });
                                }}
                              />
                            </td>
                            <td>
                              <input
                                type="text"
                                className="supplier-invoice-input-number"
                                inputMode="numeric"
                                value={r.unitPrice > 0 ? r.unitPrice.toLocaleString('en-US') : ''}
                                onChange={(e) => {
                                  const val = parseNumberInput(e.target.value) ?? 0;
                                  updatePoLine(r.procurementLineId, { unitPrice: val });
                                }}
                                placeholder="0"
                              />
                            </td>
                          </tr>
                        );
                      })}
                    </tbody>
                  </table>
                </div>

                <div className="supplier-invoice-misc">
                  <div className="supplier-invoice-misc__head">
                    <h4 className="supplier-invoice-misc__title">Chi phí thêm (ngoài dòng đơn mua)</h4>
                    <button
                      type="button"
                      className="supplier-invoice-btn-add-misc"
                      onClick={() => {
                        setMiscCharges((prev) => [
                          ...prev,
                          { id: newMiscRowId(), description: '', amount: 0 },
                        ]);
                        setErrors((prev) => {
                          const next = { ...prev };
                          delete next.miscCharges;
                          return next;
                        });
                      }}
                    >
                      + Thêm dòng chi phí
                    </button>
                  </div>
                  <p className="supplier-invoice-misc__hint">
                    Ví dụ phí vận chuyển, lắp đặt… Các khoản này được cộng vào tổng tiền bên dưới.
                  </p>
                  {miscCharges.length > 0 && (
                    <div className="supplier-invoice-table-wrapper">
                      <table className="supplier-invoice-table supplier-invoice-table--misc">
                        <thead>
                          <tr>
                            <th style={{ width: '40px' }}>#</th>
                            <th>Tên chi phí</th>
                            <th style={{ width: '180px' }}>Số tiền (đ)</th>
                            <th style={{ width: '48px' }} />
                          </tr>
                        </thead>
                        <tbody>
                          {miscCharges.map((m, idx) => (
                            <tr key={m.id}>
                              <td>{idx + 1}</td>
                              <td>
                                <input
                                  type="text"
                                  className="supplier-invoice-input"
                                  value={m.description}
                                  onChange={(e) => {
                                    const v = e.target.value;
                                    setMiscCharges((prev) =>
                                      prev.map((row) => (row.id === m.id ? { ...row, description: v } : row)),
                                    );
                                    setErrors((p) => {
                                      const n = { ...p };
                                      delete n.miscCharges;
                                      return n;
                                    });
                                  }}
                                  placeholder="VD: Phí vận chuyển"
                                />
                              </td>
                              <td>
                                <input
                                  type="text"
                                  className="supplier-invoice-input-number"
                                  inputMode="numeric"
                                  value={m.amount > 0 ? m.amount.toLocaleString('en-US') : ''}
                                  onChange={(e) => {
                                    const val = parseNumberInput(e.target.value) ?? 0;
                                    setMiscCharges((prev) =>
                                      prev.map((row) => (row.id === m.id ? { ...row, amount: val } : row)),
                                    );
                                    setErrors((p) => {
                                      const n = { ...p };
                                      delete n.miscCharges;
                                      return n;
                                    });
                                  }}
                                  placeholder="0"
                                />
                              </td>
                              <td>
                                <button
                                  type="button"
                                  className="supplier-invoice-btn-remove-misc"
                                  onClick={() =>
                                    setMiscCharges((prev) => prev.filter((row) => row.id !== m.id))
                                  }
                                  aria-label="Xóa dòng"
                                >
                                  ×
                                </button>
                              </td>
                            </tr>
                          ))}
                        </tbody>
                      </table>
                    </div>
                  )}
                </div>

                <div className="supplier-invoice-grand-total">
                  <span className="supplier-invoice-grand-total__label">Tổng tiền</span>
                  <div className="supplier-invoice-money-display supplier-invoice-grand-total__value">
                    <input
                      id="invoice-grand-total"
                      type="text"
                      className="supplier-invoice-input--disabled"
                      value={totalAmount.toLocaleString('en-US')}
                      readOnly
                      aria-readonly="true"
                    />
                    <span className="supplier-invoice-money-suffix">đ</span>
                  </div>
                </div>

                {errors.miscCharges && (
                  <div className="supplier-invoice-error-text">{errors.miscCharges}</div>
                )}
                {errors.lines && <div className="supplier-invoice-error-text">{errors.lines}</div>}
              </div>
            )}

          </div>
        </div>

        <div className="supplier-invoice-modal__footer">
          <button
            type="button"
            onClick={handleSubmit}
            className="supplier-invoice-btn-submit"
            disabled={submitting}
          >
            {submitting ? 'Đang lưu...' : 'Tạo hóa đơn'}
          </button>
          <button
            type="button"
            onClick={onClose}
            className="supplier-invoice-btn-cancel"
            disabled={submitting}
          >
            Hủy
          </button>
        </div>
      </div>
    </div>
  );
}

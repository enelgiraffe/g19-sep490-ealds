import { useEffect, useRef, useState } from 'react';
import { message } from 'antd';
import { uploadAssetFile } from '../../assets/services/assetDocumentUploadService';
import '../../assets/pages/AssetCreatePage.css';
import {
  procurementPoService,
  PO_STATUS,
  type PurchaseOrderDetail,
  type PurchaseOrderListItem,
} from '../services/procurementPoService';
import {
  assetService,
  type AssetCatalogResponse,
  type WarehouseItem,
} from '../../assets/services/assetService';
import './GoodsReceiptFormModal.css';

interface InstanceState {
  index: number;
  code: string; // Nếu trống, backend sẽ tự sinh
  serial: string;
}

interface LineState {
  lineId: number;
  lineIndex: number;
  description: string;
  assetId: number | null;
  assetCode: string | null;
  assetName: string | null;
  orderedQuantity: number;
  receivedQuantity: number;
  openQuantity: number;
  quantityToReceive: number;
  instances: InstanceState[];
  expanded: boolean;
  showBulkInput: boolean;
  bulkSerialText: string;
  showPatternInput: boolean;
  patternPrefix: string;
  patternStart: number;
  patternPadding: number;
}

type GrFormDocRow = {
  id: string;
  fileName: string;
  url?: string;
  uploading?: boolean;
  error?: string;
};

interface GoodsReceiptFormModalProps {
  open: boolean;
  onClose: () => void;
  onSubmit: (payload: {
    procurementId: number;
    warehouseId: number;
    postingDate: string;
    note: string | null;
    attachmentFileUrls?: string[];
    lines: {
      procurementLineId: number;
      quantityReceived: number;
      assetId: number;
      instanceSerialNumbers?: (string | null)[] | null;
      instanceCodes?: (string | null)[] | null;
    }[];
  }) => Promise<void>;
}

export function GoodsReceiptFormModal({ open, onClose, onSubmit }: GoodsReceiptFormModalProps) {
  const [loading, setLoading] = useState(false);
  const [submitting, setSubmitting] = useState(false);

  const [poOptions, setPoOptions] = useState<PurchaseOrderListItem[]>([]);
  const [selectedPoId, setSelectedPoId] = useState<number | null>(null);
  const [poDetail, setPoDetail] = useState<PurchaseOrderDetail | null>(null);

  const [warehouses, setWarehouses] = useState<WarehouseItem[]>([]);
  const [warehouseId, setWarehouseId] = useState<number | null>(null);

  const [postingDate, setPostingDate] = useState<string>('');
  const [note, setNote] = useState('');

  const [assets, setAssets] = useState<AssetCatalogResponse[]>([]);
  const [lines, setLines] = useState<LineState[]>([]);

  const [showInstancePreview, setShowInstancePreview] = useState(false);
  const [previewInstances, setPreviewInstances] = useState<
    { lineIndex: number; assetName: string; serial: string | null }[]
  >([]);
  
  const [showPrintLabels, setShowPrintLabels] = useState(false);
  const [printGoodsReceiptId, setPrintGoodsReceiptId] = useState<number | null>(null);

  const [documents, setDocuments] = useState<GrFormDocRow[]>([]);
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
      const today = new Date().toISOString().slice(0, 10);
      setPostingDate(today);
      setNote('');
      setDocuments([]);
      setSelectedPoId(null);
      setPoDetail(null);
      setLines([]);
      setWarehouseId(null);
      setShowInstancePreview(false);
      setPreviewInstances([]);
      loadInitialData();
    }
  }, [open]);

  const loadInitialData = async () => {
    setLoading(true);
    try {
      const [wh, ast, pos] = await Promise.all([
        assetService.getWarehouses(),
        assetService.getAll(),
        procurementPoService.getList({ receivingEligible: true, pageSize: 200, page: 1 }),
      ]);
      setWarehouses(wh);
      setAssets(ast);
      /* receivingEligible: chưa hủy, chưa nhận đủ — vẫn cho phép lần nhận đầu (trạng thái 0). Ẩn nháp. */
      setPoOptions(pos.items.filter((p) => p.status !== PO_STATUS.draft));
      if (wh.length > 0) setWarehouseId(wh[0].warehouseId);
    } catch {
      message.error('Không tải được dữ liệu tạo biên nhận.');
      setWarehouses([]);
      setAssets([]);
      setPoOptions([]);
    } finally {
      setLoading(false);
    }
  };

  const onSelectPo = async (procurementId: number) => {
    setSelectedPoId(procurementId);
    setLoading(true);
    try {
      const d = await procurementPoService.getById(procurementId);
      setPoDetail(d);
      setLines(
        d.lines.map((l) => ({
          lineId: l.lineId,
          lineIndex: l.lineIndex,
          description: l.description || [l.assetCode, l.assetName].filter(Boolean).join(' ') || '',
          assetId: l.assetId,
          assetCode: l.assetCode,
          assetName: l.assetName,
          orderedQuantity: l.quantity,
          receivedQuantity: l.receivedQuantity ?? 0,
          openQuantity: l.openQuantity ?? l.quantity - (l.receivedQuantity ?? 0),
          quantityToReceive: 0,
          instances: [],
          expanded: false,
          showBulkInput: false,
          bulkSerialText: '',
          showPatternInput: false,
          patternPrefix: l.assetCode || 'SN',
          patternStart: 1,
          patternPadding: 3,
        })),
      );
    } catch {
      message.error('Không tải chi tiết đơn mua.');
      setPoDetail(null);
      setLines([]);
    } finally {
      setLoading(false);
    }
  };

  const generateInstanceCode = (lineId: number, index: number): string => {
    // Tạo mã tạm thời để hiển thị
    return `GRL-${lineId}-${String(index).padStart(3, '0')}`;
  };

  const updateLine = (lineId: number, patch: Partial<LineState>) => {
    setLines((prev) =>
      prev.map((r) => {
        if (r.lineId !== lineId) return r;
        const updated = { ...r, ...patch };
        
        // Khi thay đổi quantityToReceive, tự động tạo/cập nhật instances
        if ('quantityToReceive' in patch) {
          const newQty = patch.quantityToReceive ?? 0;
          const currentInstances = updated.instances || [];
          
          if (newQty > currentInstances.length) {
            // Thêm instances mới với mã tự sinh hiển thị trước
            const toAdd = newQty - currentInstances.length;
            const newInstances: InstanceState[] = [];
            for (let i = 0; i < toAdd; i++) {
              const instanceIndex = currentInstances.length + i + 1;
              newInstances.push({
                index: instanceIndex,
                code: generateInstanceCode(r.lineId, instanceIndex), // Tự sinh mã hiển thị
                serial: '',
              });
            }
            updated.instances = [...currentInstances, ...newInstances];
          } else if (newQty < currentInstances.length) {
            // Xóa instances thừa
            updated.instances = currentInstances.slice(0, newQty);
          }
        }
        
        return updated;
      }),
    );
  };

  const updateInstanceSerial = (lineId: number, instanceIndex: number, serial: string) => {
    setLines((prev) =>
      prev.map((line) => {
        if (line.lineId !== lineId) return line;
        return {
          ...line,
          instances: line.instances.map((inst) =>
            inst.index === instanceIndex ? { ...inst, serial } : inst,
          ),
        };
      }),
    );
  };

  const updateInstanceCode = (lineId: number, instanceIndex: number, code: string) => {
    setLines((prev) =>
      prev.map((line) => {
        if (line.lineId !== lineId) return line;
        return {
          ...line,
          instances: line.instances.map((inst) =>
            inst.index === instanceIndex ? { ...inst, code } : inst,
          ),
        };
      }),
    );
  };

  const toggleLineExpand = (lineId: number) => {
    setLines((prev) =>
      prev.map((line) => (line.lineId === lineId ? { ...line, expanded: !line.expanded } : line)),
    );
  };

  const applyBulkSerials = (lineId: number) => {
    setLines((prev) =>
      prev.map((line) => {
        if (line.lineId !== lineId) return line;
        const serials = line.bulkSerialText
          .split(/[\n,;]+/)
          .map((s) => s.trim())
          .filter(Boolean);
        
        if (serials.length !== line.instances.length) {
          message.warning(
            `Cần ${line.instances.length} serial numbers, nhưng có ${serials.length}. Vui lòng kiểm tra lại.`,
          );
          return line;
        }

        const updatedInstances = line.instances.map((inst, idx) => ({
          ...inst,
          serial: serials[idx] || '',
        }));

        return {
          ...line,
          instances: updatedInstances,
          showBulkInput: false,
          bulkSerialText: '',
        };
      }),
    );
  };

  const applyPatternSerials = (lineId: number) => {
    setLines((prev) =>
      prev.map((line) => {
        if (line.lineId !== lineId) return line;
        
        const updatedInstances = line.instances.map((inst, idx) => {
          const num = line.patternStart + idx;
          const paddedNum = String(num).padStart(line.patternPadding, '0');
          const serial = `${line.patternPrefix}-${paddedNum}`;
          return { ...inst, serial };
        });

        return {
          ...line,
          instances: updatedInstances,
          showPatternInput: false,
        };
      }),
    );
  };

  const applyPatternCodes = (lineId: number) => {
    setLines((prev) =>
      prev.map((line) => {
        if (line.lineId !== lineId) return line;
        
        const updatedInstances = line.instances.map((inst, idx) => {
          const num = line.patternStart + idx;
          const paddedNum = String(num).padStart(line.patternPadding, '0');
          const code = `${line.patternPrefix}-${paddedNum}`;
          return { ...inst, code };
        });

        return {
          ...line,
          instances: updatedInstances,
          showPatternInput: false,
        };
      }),
    );
  };

  const clearAllSerials = (lineId: number) => {
    setLines((prev) =>
      prev.map((line) => {
        if (line.lineId !== lineId) return line;
        return {
          ...line,
          instances: line.instances.map((inst) => ({ ...inst, serial: '', code: '' })),
        };
      }),
    );
  };

  const handlePreview = () => {
    const instances: { lineIndex: number; assetName: string; serial: string | null }[] = [];
    for (const line of lines) {
      if (line.quantityToReceive <= 0) continue;
      const assetName = line.assetName || line.description || `Dòng ${line.lineIndex + 1}`;
      for (const inst of line.instances) {
        instances.push({
          lineIndex: line.lineIndex,
          assetName,
          serial: inst.serial.trim() || null,
        });
      }
    }
    setPreviewInstances(instances);
    setShowInstancePreview(true);
  };

  const handleSubmit = async () => {
    if (!selectedPoId || !poDetail) {
      message.warning('Chọn đơn mua.');
      return;
    }
    if (!warehouseId || warehouseId <= 0) {
      message.warning('Chọn kho nhập.');
      return;
    }
    if (!postingDate) {
      message.warning('Chọn ngày ghi nhận.');
      return;
    }

    const linesPayload: {
      procurementLineId: number;
      quantityReceived: number;
      assetId: number;
      instanceSerialNumbers?: (string | null)[] | null;
      instanceCodes?: (string | null)[] | null;
    }[] = [];

    // Kiểm tra serial và code trùng lặp
    const allSerials = new Set<string>();
    const allCodes = new Set<string>();
    
    for (const row of lines) {
      if (row.quantityToReceive <= 0) continue;
      for (const inst of row.instances) {
        const serial = inst.serial.trim();
        if (serial) {
          if (allSerials.has(serial)) {
            message.error(`Serial number "${serial}" bị trùng lặp. Mỗi serial phải là duy nhất.`);
            return;
          }
          allSerials.add(serial);
        }
        
        const code = inst.code.trim();
        if (code) {
          if (allCodes.has(code)) {
            message.error(`Mã cá thể "${code}" bị trùng lặp. Mỗi mã cá thể phải là duy nhất.`);
            return;
          }
          allCodes.add(code);
        }
      }
    }

    for (const row of lines) {
      const q = row.quantityToReceive;
      if (q <= 0) continue;
      if (!Number.isInteger(q)) {
        message.error(`Dòng ${row.lineIndex + 1}: số lượng nhận phải là số nguyên.`);
        return;
      }
      if (q > row.openQuantity) {
        message.error(`Dòng ${row.lineIndex + 1}: vượt số lượng còn nhận (${row.openQuantity}).`);
        return;
      }
      const assetId = row.assetId;
      if (!assetId || assetId <= 0) {
        message.error(`Dòng ${row.lineIndex + 1}: cần chọn tài sản.`);
        return;
      }
      
      const serials = row.instances.map((inst) => {
        const s = inst.serial.trim();
        return s || null;
      });
      
      const codes = row.instances.map((inst) => {
        const c = inst.code.trim();
        return c || null;
      });
      
      linesPayload.push({
        procurementLineId: row.lineId,
        quantityReceived: q,
        assetId,
        instanceSerialNumbers: serials.some((s) => s !== null) ? serials : null,
        instanceCodes: codes.some((c) => c !== null) ? codes : null,
      });
    }

    if (linesPayload.length === 0) {
      message.warning('Nhập số lượng nhận cho ít nhất một dòng.');
      return;
    }

    if (documents.some((d) => d.uploading)) {
      message.warning('Đang tải tài liệu lên, vui lòng đợi.');
      return;
    }
    if (documents.some((d) => d.error)) {
      message.error('Có tài liệu tải lên lỗi. Xóa dòng đó hoặc thêm tài liệu khác.');
      return;
    }

    const attachmentFileUrls = documents
      .filter((d) => d.url)
      .map((d) => d.url as string);

    setSubmitting(true);
    try {
      await onSubmit({
        procurementId: selectedPoId,
        warehouseId,
        postingDate,
        note: note.trim() || null,
        attachmentFileUrls: attachmentFileUrls.length > 0 ? attachmentFileUrls : undefined,
        lines: linesPayload,
      });
      message.success('Đã tạo biên nhận và sinh thể hiện tài sản.');
      
      // Mở modal in nhãn QR
      const totalInstances = linesPayload.reduce((sum, l) => sum + l.quantityReceived, 0);
      setShowPrintLabels(true);
      setPrintGoodsReceiptId(null); // Sẽ được set từ response nếu cần
      
      onClose();
    } catch (e: unknown) {
      const msg =
        typeof e === 'object' && e !== null && 'response' in e
          ? String((e as { response?: { data?: unknown } }).response?.data)
          : 'Không tạo được biên nhận.';
      message.error(msg.length > 200 ? 'Không tạo được biên nhận.' : msg);
    } finally {
      setSubmitting(false);
    }
  };

  if (!open && !showPrintLabels) return null;

  if (showPrintLabels) {
    return (
      <div className="gr-form-modal-overlay" role="dialog" aria-modal="true">
        <div className="gr-form-modal" style={{ maxWidth: '600px' }}>
          <button
            type="button"
            className="gr-form-modal__close-btn"
            onClick={() => setShowPrintLabels(false)}
            aria-label="Đóng"
          >
            <span className="gr-form-modal__close">×</span>
          </button>

          <div className="gr-form-modal__header">
            <h2 className="gr-form-modal__title">In nhãn QR cho cá thể tài sản</h2>
          </div>

          <div className="gr-form-modal__body">
            <div className="gr-form-modal__content">
              <div className="gr-print-info">
                <p style={{ fontSize: '14px', color: '#6b7280', marginBottom: '16px' }}>
                  Biên nhận đã được tạo thành công. Bạn có thể in nhãn QR cho tất cả cá thể tài sản vừa nhận.
                </p>
                <div style={{ padding: '16px', background: '#f9fafb', borderRadius: '8px', border: '1px solid #e5e7eb' }}>
                  <div style={{ fontSize: '13px', color: '#6b7280', marginBottom: '8px' }}>
                    <strong>Lưu ý:</strong>
                  </div>
                  <ul style={{ margin: 0, paddingLeft: '20px', fontSize: '13px', color: '#6b7280' }}>
                    <li>Mỗi nhãn QR chứa mã cá thể và serial number (nếu có)</li>
                    <li>Định dạng in: A4 hoặc giấy nhãn dán</li>
                    <li>Có thể in lại sau trong danh sách cá thể tài sản</li>
                  </ul>
                </div>
              </div>
            </div>
          </div>

          <div className="gr-form-modal__footer">
            <button
              type="button"
              onClick={() => {
                message.info('Tính năng in nhãn QR đang được phát triển');
                setShowPrintLabels(false);
              }}
              className="gr-form-btn-submit"
            >
              In nhãn QR
            </button>
            <button
              type="button"
              onClick={() => setShowPrintLabels(false)}
              className="gr-form-btn-secondary"
            >
              Bỏ qua
            </button>
          </div>
        </div>
      </div>
    );
  }

  if (showInstancePreview) {
    return (
      <div className="gr-form-modal-overlay" role="dialog" aria-modal="true">
        <div className="gr-form-modal">
          <button
            type="button"
            className="gr-form-modal__close-btn"
            onClick={() => setShowInstancePreview(false)}
            aria-label="Đóng"
          >
            <span className="gr-form-modal__close">×</span>
          </button>

          <div className="gr-form-modal__header">
            <h2 className="gr-form-modal__title">Preview thể hiện tài sản sẽ được tạo</h2>
          </div>

          <div className="gr-form-modal__body">
            <div className="gr-preview-table-wrapper">
              <table className="gr-preview-table">
                <thead>
                  <tr>
                    <th>#</th>
                    <th>Tài sản</th>
                    <th>Serial</th>
                  </tr>
                </thead>
                <tbody>
                  {previewInstances.map((inst, i) => (
                    <tr key={i}>
                      <td>{i + 1}</td>
                      <td>{inst.assetName}</td>
                      <td>{inst.serial || '(tự động)'}</td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          </div>

          <div className="gr-form-modal__footer">
            <button
              type="button"
              onClick={() => setShowInstancePreview(false)}
              className="gr-form-btn-secondary"
            >
              Đóng
            </button>
          </div>
        </div>
      </div>
    );
  }

  return (
    <div className="gr-form-modal-overlay" role="dialog" aria-modal="true">
      <div className="gr-form-modal">
        <button
          type="button"
          className="gr-form-modal__close-btn"
          onClick={onClose}
          aria-label="Đóng"
        >
          <span className="gr-form-modal__close">×</span>
        </button>

        <div className="gr-form-modal__header">
          <h2 className="gr-form-modal__title">Tạo biên nhận hàng</h2>
        </div>

        <div className="gr-form-modal__body">
          <div className="gr-form-modal__content">
            <div className="gr-form-section">
              <h3 className="gr-form-section-title">Thông tin chung</h3>

              <div className="gr-form-row">
                <div className="gr-form-item">
                  <label htmlFor="gr-po-select">
                    Đơn mua<span style={{ color: '#ef4444' }}>*</span>
                  </label>
                  <select
                    id="gr-po-select"
                    className="gr-form-select"
                    value={selectedPoId ?? ''}
                    onChange={(e) => {
                      const val = e.target.value;
                      if (val) onSelectPo(parseInt(val, 10));
                      else {
                        setSelectedPoId(null);
                        setPoDetail(null);
                        setLines([]);
                      }
                    }}
                    disabled={loading}
                  >
                    <option value="">Chọn đơn mua </option>
                    {poOptions.map((p) => (
                      <option key={p.procurementId} value={p.procurementId}>
                        {p.contractNo} (#{p.procurementId}) — {p.title}
                      </option>
                    ))}
                  </select>
                </div>
              </div>

              <div className="gr-form-row">
                <div className="gr-form-item">
                  <label htmlFor="gr-warehouse-select">
                    Kho nhập<span style={{ color: '#ef4444' }}>*</span>
                  </label>
                  <select
                    id="gr-warehouse-select"
                    className="gr-form-select"
                    value={warehouseId ?? ''}
                    onChange={(e) => setWarehouseId(e.target.value ? parseInt(e.target.value, 10) : null)}
                  >
                    <option value="">Chọn kho</option>
                    {warehouses.map((w) => (
                      <option key={w.warehouseId} value={w.warehouseId}>
                        {w.name}
                      </option>
                    ))}
                  </select>
                </div>
                <div className="gr-form-item">
                  <label htmlFor="gr-posting-date">
                    Ngày ghi nhận<span style={{ color: '#ef4444' }}>*</span>
                  </label>
                  <input
                    id="gr-posting-date"
                    type="date"
                    className="gr-form-input"
                    value={postingDate}
                    onChange={(e) => setPostingDate(e.target.value)}
                  />
                </div>
              </div>

              <div className="gr-form-row">
                <div className="gr-form-item">
                  <label htmlFor="gr-note">Ghi chú</label>
                  <textarea
                    id="gr-note"
                    className="gr-form-textarea"
                    rows={3}
                    placeholder="Ghi chú về biên nhận..."
                    value={note}
                    onChange={(e) => setNote(e.target.value)}
                  />
                </div>
              </div>

              <div className="asset-create__section" style={{ marginTop: 16 }}>
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

            {poDetail && lines.length > 0 && (
              <div className="gr-form-section">
                <h3 className="gr-form-section-title">Chi tiết hàng hóa</h3>
                <p className="gr-form-section-subtitle">
                  Đơn mua: <strong>{poDetail.contractNo}</strong> — {poDetail.supplierName}
                </p>

                <div className="gr-items-table-wrapper">
                  <table className="gr-items-table">
                    <thead>
                      <tr>
                        <th style={{ width: '40px' }}>#</th>
                        <th style={{ width: '200px' }}>Tài sản</th>
                        <th style={{ width: '80px', textAlign: 'right' }}>Đặt</th>
                        <th style={{ width: '80px', textAlign: 'right' }}>Đã nhận</th>
                        <th style={{ width: '80px', textAlign: 'right' }}>Còn lại</th>
                        <th style={{ width: '120px' }}>SL nhận lần này</th>
                      </tr>
                    </thead>
                    <tbody>
                      {lines.map((line, idx) => (
                        <>
                          <tr key={line.lineId}>
                            <td>{idx + 1}</td>
                            <td>
                              <div className="gr-asset-cell">
                                <div className="gr-asset-name">{line.assetName || line.description}</div>
                                {line.assetCode && (
                                  <div className="gr-asset-code">{line.assetCode}</div>
                                )}
                              </div>
                            </td>
                            <td style={{ textAlign: 'right' }}>
                              {line.orderedQuantity.toLocaleString('vi-VN')}
                            </td>
                            <td style={{ textAlign: 'right' }}>
                              {line.receivedQuantity.toLocaleString('vi-VN')}
                            </td>
                            <td style={{ textAlign: 'right' }}>
                              <strong>{line.openQuantity.toLocaleString('vi-VN')}</strong>
                            </td>
                            <td>
                              <input
                                type="number"
                                className="gr-form-input-number"
                                min={0}
                                max={line.openQuantity}
                                value={line.quantityToReceive}
                                onChange={(e) => {
                                  const val = parseInt(e.target.value, 10);
                                  updateLine(line.lineId, {
                                    quantityToReceive: isNaN(val) ? 0 : val,
                                  });
                                }}
                              />
                            </td>
                          </tr>
                          {line.quantityToReceive > 0 && (
                            <tr key={`${line.lineId}-instances`}>
                              <td colSpan={6}>
                                <div className="gr-instances-section">
                                  <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: '12px' }}>
                                    <div className="gr-instances-title">
                                      Chi tiết {line.quantityToReceive} cá thể tài sản
                                    </div>
                                    <div style={{ display: 'flex', gap: '8px' }}>
                                      {!line.expanded && (
                                        <button
                                          type="button"
                                          className="gr-line-expand-btn"
                                          onClick={() => toggleLineExpand(line.lineId)}
                                        >
                                          ▼ Mở rộng
                                        </button>
                                      )}
                                      {line.expanded && (
                                        <button
                                          type="button"
                                          className="gr-line-expand-btn"
                                          onClick={() => toggleLineExpand(line.lineId)}
                                        >
                                          ▲ Thu gọn
                                        </button>
                                      )}
                                    </div>
                                  </div>
                                  
                                  {line.expanded && (
                                    <>
                                      <div style={{ display: 'flex', gap: '8px', marginBottom: '12px' }}>
                                        <button
                                          type="button"
                                          className="gr-tool-btn"
                                          onClick={() =>
                                            updateLine(line.lineId, { showBulkInput: !line.showBulkInput, showPatternInput: false })
                                          }
                                        >
                                          📋 Nhập hàng loạt
                                        </button>
                                        <button
                                          type="button"
                                          className="gr-tool-btn"
                                          onClick={() =>
                                            updateLine(line.lineId, { showPatternInput: !line.showPatternInput, showBulkInput: false })
                                          }
                                        >
                                          🔢 Tạo theo mẫu
                                        </button>
                                        <button
                                          type="button"
                                          className="gr-tool-btn gr-tool-btn--danger"
                                          onClick={() => clearAllSerials(line.lineId)}
                                        >
                                          🗑️ Xóa tất cả
                                        </button>
                                      </div>

                                      {line.showBulkInput && (
                                        <div className="gr-bulk-input-section">
                                          <div style={{ fontSize: '13px', color: '#6b7280', marginBottom: '8px' }}>
                                            Nhập {line.instances.length} serial numbers (mỗi dòng hoặc phân cách bằng dấu phẩy/chấm phẩy):
                                          </div>
                                          <textarea
                                            className="gr-form-textarea"
                                            rows={5}
                                            placeholder="SN-001&#10;SN-002&#10;SN-003&#10;hoặc: SN-001, SN-002, SN-003"
                                            value={line.bulkSerialText}
                                            onChange={(e) => updateLine(line.lineId, { bulkSerialText: e.target.value })}
                                          />
                                          <div style={{ display: 'flex', gap: '8px', marginTop: '8px' }}>
                                            <button
                                              type="button"
                                              className="gr-form-btn-submit"
                                              style={{ height: '32px', padding: '0 16px', fontSize: '13px' }}
                                              onClick={() => applyBulkSerials(line.lineId)}
                                            >
                                              Áp dụng
                                            </button>
                                            <button
                                              type="button"
                                              className="gr-form-btn-secondary"
                                              style={{ height: '32px', padding: '0 16px', fontSize: '13px' }}
                                              onClick={() => updateLine(line.lineId, { showBulkInput: false, bulkSerialText: '' })}
                                            >
                                              Hủy
                                            </button>
                                          </div>
                                        </div>
                                      )}

                                      {line.showPatternInput && (
                                        <div className="gr-pattern-input-section">
                                          <div style={{ fontSize: '13px', color: '#6b7280', marginBottom: '8px' }}>
                                            Tạo mã cá thể theo mẫu:
                                          </div>
                                          <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr 1fr', gap: '12px', marginBottom: '8px' }}>
                                            <div>
                                              <label style={{ fontSize: '12px', color: '#6b7280', display: 'block', marginBottom: '4px' }}>
                                                Tiền tố
                                              </label>
                                              <input
                                                type="text"
                                                className="gr-form-input"
                                                placeholder="LAPTOP"
                                                value={line.patternPrefix}
                                                onChange={(e) => updateLine(line.lineId, { patternPrefix: e.target.value })}
                                              />
                                            </div>
                                            <div>
                                              <label style={{ fontSize: '12px', color: '#6b7280', display: 'block', marginBottom: '4px' }}>
                                                Bắt đầu từ
                                              </label>
                                              <input
                                                type="number"
                                                className="gr-form-input"
                                                min={1}
                                                value={line.patternStart}
                                                onChange={(e) => updateLine(line.lineId, { patternStart: parseInt(e.target.value) || 1 })}
                                              />
                                            </div>
                                            <div>
                                              <label style={{ fontSize: '12px', color: '#6b7280', display: 'block', marginBottom: '4px' }}>
                                                Số chữ số
                                              </label>
                                              <input
                                                type="number"
                                                className="gr-form-input"
                                                min={1}
                                                max={6}
                                                value={line.patternPadding}
                                                onChange={(e) => updateLine(line.lineId, { patternPadding: parseInt(e.target.value) || 3 })}
                                              />
                                            </div>
                                          </div>
                                          <div style={{ fontSize: '12px', color: '#6b7280', marginBottom: '8px' }}>
                                            Preview: {line.patternPrefix}-{String(line.patternStart).padStart(line.patternPadding, '0')}, {line.patternPrefix}-{String(line.patternStart + 1).padStart(line.patternPadding, '0')}, ...
                                          </div>
                                          <div style={{ display: 'flex', gap: '8px' }}>
                                            <button
                                              type="button"
                                              className="gr-form-btn-submit"
                                              style={{ height: '32px', padding: '0 16px', fontSize: '13px' }}
                                              onClick={() => applyPatternCodes(line.lineId)}
                                            >
                                              Áp dụng cho Mã cá thể
                                            </button>
                                            
                                            <button
                                              type="button"
                                              className="gr-form-btn-secondary"
                                              style={{ height: '32px', padding: '0 16px', fontSize: '13px' }}
                                              onClick={() => updateLine(line.lineId, { showPatternInput: false })}
                                            >
                                              Hủy
                                            </button>
                                          </div>
                                        </div>
                                      )}

                                      <div className="gr-instances-list">
                                        <div className="gr-instance-row" style={{ fontWeight: 600, background: '#f3f4f6', gridTemplateColumns: '40px 2fr 2fr' }}>
                                          <div>#</div>
                                          <div>Mã cá thể</div>
                                          <div>Serial Number</div>
                                        </div>
                                        {line.instances.map((inst) => (
                                          <div key={inst.index} className="gr-instance-row" style={{ gridTemplateColumns: '40px 2fr 2fr' }}>
                                            <div className="gr-instance-number">{inst.index}</div>
                                            <input
                                              type="text"
                                              className="gr-instance-serial-input"
                                              placeholder="Mã cá thể"
                                              value={inst.code}
                                              onChange={(e) =>
                                                updateInstanceCode(line.lineId, inst.index, e.target.value)
                                              }
                                            />
                                            <input
                                              type="text"
                                              className="gr-instance-serial-input"
                                              placeholder="(tuỳ chọn)"
                                              value={inst.serial}
                                              onChange={(e) =>
                                                updateInstanceSerial(line.lineId, inst.index, e.target.value)
                                              }
                                            />
                                          </div>
                                        ))}
                                      </div>
                                    </>
                                  )}
                                  
                                  {!line.expanded && (
                                    <div style={{ fontSize: '13px', color: '#6b7280' }}>
                                      Nhấn "Mở rộng" để nhập serial number cho từng cá thể (không bắt buộc)
                                    </div>
                                  )}
                                </div>
                              </td>
                            </tr>
                          )}
                        </>
                      ))}
                    </tbody>
                  </table>
                </div>
              </div>
            )}
          </div>
        </div>

        <div className="gr-form-modal__footer">
          <button
            type="button"
            onClick={handleSubmit}
            className="gr-form-btn-submit"
            disabled={submitting || loading}
          >
            {submitting ? 'Đang xử lý...' : 'Ghi nhận'}
          </button>
          <button
            type="button"
            onClick={handlePreview}
            className="gr-form-btn-preview"
            disabled={!poDetail || lines.every((l) => l.quantityToReceive <= 0)}
          >
            Preview thể hiện
          </button>
          <button type="button" onClick={onClose} className="gr-form-btn-secondary">
            Hủy
          </button>
        </div>
      </div>
    </div>
  );
}

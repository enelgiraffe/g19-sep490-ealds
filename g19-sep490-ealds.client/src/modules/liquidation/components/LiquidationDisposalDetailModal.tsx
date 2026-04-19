import { useEffect, useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { formatVnd } from '../../assets/services/assetService';
import type { TransferRequestListItem } from '../../assets/services/transferRequestService';
import {
  disposalExecutionService,
  type DisposalExecutionDto,
} from '../../requests/services/disposalExecutionService';
import '../../assets/components/MarkDamagedAssetModal.css';

function formatDate(iso: string): string {
  try {
    return new Date(iso).toLocaleDateString('vi-VN');
  } catch {
    return iso;
  }
}

function formatMoneyVnd(value: number | null | undefined): string {
  if (value == null || Number.isNaN(Number(value))) return '—';
  return formatVnd(Number(value));
}

/** Payload gộp do LiquidationAppraisalModal lưu vào ExecutionNote (JSON). */
interface StoredAppraisalPayload {
  location?: string | null;
  members?: { name?: string; position?: string }[];
  assetSpecs?: string | null;
  assetCondition?: string | null;
  assetOrigin?: string | null;
  conclusion?: string | null;
}

function tryParseStoredAppraisalNote(raw: string | null | undefined): StoredAppraisalPayload | null {
  if (!raw?.trim()) return null;
  try {
    const o = JSON.parse(raw) as unknown;
    if (!o || typeof o !== 'object') return null;
    const rec = o as Record<string, unknown>;
    const hasMembers = Array.isArray(rec.members);
    const hasTextFields =
      typeof rec.conclusion === 'string' ||
      typeof rec.location === 'string' ||
      typeof rec.assetSpecs === 'string' ||
      typeof rec.assetCondition === 'string' ||
      typeof rec.assetOrigin === 'string';
    if (!hasMembers && !hasTextFields) return null;
    return o as StoredAppraisalPayload;
  } catch {
    return null;
  }
}

export interface LiquidationDisposalDetailModalProps {
  open: boolean;
  onClose: () => void;
  row: TransferRequestListItem | null;
  /** Thông tin tài chính + nút xem cá thể — chỉ dùng cho kế toán */
  showAccountantExtras?: boolean;
  /**
   * Trang chi tiết cá thể đọc state này làm nút “Quay lại” (tránh mặc định về /assets/:id).
   * Ví dụ: /liquidation hoặc /requests?tab=liquidation
   */
  returnPathAfterInstance?: string;
  returnLabelAfterInstance?: string;
  /** Ghép thêm class lên overlay (vd. z-index khi mở lồng modal biên bản thẩm định). */
  overlayClassName?: string;
}

export function LiquidationDisposalDetailModal({
  open,
  onClose,
  row,
  showAccountantExtras = false,
  returnPathAfterInstance = '/liquidation',
  returnLabelAfterInstance = '← Quay lại Thanh lý',
  overlayClassName = '',
}: LiquidationDisposalDetailModalProps) {
  const navigate = useNavigate();
  const [execLoading, setExecLoading] = useState(false);
  const [execError, setExecError] = useState<string | null>(null);
  const [execDto, setExecDto] = useState<DisposalExecutionDto | null>(null);

  useEffect(() => {
    if (!open || !row) {
      setExecDto(null);
      setExecError(null);
      setExecLoading(false);
      return;
    }
    if (row.status < 4) {
      setExecDto(null);
      setExecError(null);
      setExecLoading(false);
      return;
    }
    let cancelled = false;
    setExecLoading(true);
    setExecError(null);
    disposalExecutionService
      .getByAssetRequest(row.assetRequestId)
      .then((d) => {
        if (!cancelled) setExecDto(d);
      })
      .catch(() => {
        if (!cancelled) {
          setExecDto(null);
          setExecError('Không tải được dữ liệu biên bản / thực hiện thanh lý (kiểm tra quyền đăng nhập).');
        }
      })
      .finally(() => {
        if (!cancelled) setExecLoading(false);
      });
    return () => {
      cancelled = true;
    };
  }, [open, row?.assetRequestId, row?.status]);

  if (!open || !row) return null;

  const instanceId = row.assetInstanceId;
  const canOpenInstance = instanceId != null && instanceId > 0;
  const overlayCn = ['mark-damaged-modal-overlay', overlayClassName].filter(Boolean).join(' ');

  const appraisalPayload = tryParseStoredAppraisalNote(execDto?.executionNote ?? null);
  const showAppraisalSection = row.status >= 4;

  return (
    <div className={overlayCn} role="dialog" aria-modal="true">
      <div className="mark-damaged-modal">
        <button type="button" className="mark-damaged-modal__close-btn" onClick={onClose} aria-label="Đóng">
          <span className="mark-damaged-modal__close">×</span>
        </button>

        <div className="mark-damaged-modal__header">
          <h2 className="mark-damaged-modal__title">Chi tiết yêu cầu thanh lý — {row.code}</h2>
        </div>

        <div className="mark-damaged-modal__body">
          <div className="mark-damaged-modal__content">
            <div className="mark-damaged-info-section">
              <h3 className="mark-damaged-section-title">Thông tin yêu cầu</h3>
              <div className="mark-damaged-info-grid">
                <div className="mark-damaged-info-row">
                  <div className="mark-damaged-info-item">
                    <label>Mã yêu cầu</label>
                    <div className="mark-damaged-info-value">YC-{row.assetRequestId}</div>
                  </div>
                  <div className="mark-damaged-info-item">
                    <label>Ngày gửi</label>
                    <div className="mark-damaged-info-value">{formatDate(row.transferDate)}</div>
                  </div>
                </div>
                <div className="mark-damaged-info-row">
                  <div className="mark-damaged-info-item">
                    <label>Mã tài sản gốc</label>
                    <div className="mark-damaged-info-value">{row.assetCode ?? '—'}</div>
                  </div>
                  <div className="mark-damaged-info-item">
                    <label>Mã cá thể</label>
                    <div className="mark-damaged-info-value">{row.instanceCode?.trim() || '—'}</div>
                  </div>
                </div>
                <div className="mark-damaged-info-row">
                  <div className="mark-damaged-info-item">
                    <label>Tên tài sản</label>
                    <div className="mark-damaged-info-value">{row.assetName ?? '—'}</div>
                  </div>
                  <div className="mark-damaged-info-item">
                    <label>Phòng ban đề xuất</label>
                    <div className="mark-damaged-info-value">{row.fromDepartment ?? '—'}</div>
                  </div>
                </div>
                <div className="mark-damaged-info-row">
                  <div className="mark-damaged-info-item">
                    <label>Trạng thái</label>
                    <div className="mark-damaged-info-value">{row.statusName ?? '—'}</div>
                  </div>
                  <div className="mark-damaged-info-item">
                    <label>Người tạo</label>
                    <div className="mark-damaged-info-value">{row.createdByName?.trim() || `#${row.createdBy}`}</div>
                  </div>
                </div>
                <div className="mark-damaged-info-row">
                  <div className="mark-damaged-info-item" style={{ gridColumn: '1 / -1' }}>
                    <label>Nội dung / lý do</label>
                    <div className="mark-damaged-info-value">{row.reason?.trim() || '—'}</div>
                  </div>
                </div>
              </div>
            </div>

            {showAppraisalSection && (
              <div className="mark-damaged-info-section" style={{ marginTop: 16 }}>
                <h3 className="mark-damaged-section-title">Biên bản thẩm định (đã ghi nhận)</h3>
                {execLoading ? (
                  <p style={{ fontSize: 13, color: '#6b7280' }}>Đang tải…</p>
                ) : execError ? (
                  <p style={{ fontSize: 13, color: '#b45309' }}>{execError}</p>
                ) : (
                  <>
                <p style={{ fontSize: 12, color: '#6b7280', marginBottom: 12 }}>
                  Số biên bản trên hệ thống có thể được cập nhật ở bước &quot;biên bản giao nhận&quot; sau thẩm định.
                </p>
                <div className="mark-damaged-info-grid">
                  <div className="mark-damaged-info-row">
                    <div className="mark-damaged-info-item">
                      <label>Ngày thẩm định</label>
                      <div className="mark-damaged-info-value">
                        {execDto?.plannedExecutionDate ? formatDate(execDto.plannedExecutionDate) : '—'}
                      </div>
                    </div>
                    <div className="mark-damaged-info-item">
                      <label>Số biên bản (hồ sơ hiện tại)</label>
                      <div className="mark-damaged-info-value">{execDto?.minutesNo?.trim() || '—'}</div>
                    </div>
                  </div>
                  {appraisalPayload ? (
                    <>
                      {appraisalPayload.location ? (
                        <div className="mark-damaged-info-row">
                          <div className="mark-damaged-info-item" style={{ gridColumn: '1 / -1' }}>
                            <label>Địa điểm lập biên bản</label>
                            <div className="mark-damaged-info-value">{appraisalPayload.location}</div>
                          </div>
                        </div>
                      ) : null}
                      {Array.isArray(appraisalPayload.members) && appraisalPayload.members.length > 0 ? (
                        <div className="mark-damaged-info-row">
                          <div className="mark-damaged-info-item" style={{ gridColumn: '1 / -1' }}>
                            <label>Hội đồng / thành phần tham gia</label>
                            <div className="mark-damaged-info-value" style={{ overflowX: 'auto' }}>
                              <table style={{ width: '100%', borderCollapse: 'collapse', fontSize: 13 }}>
                                <thead>
                                  <tr>
                                    <th style={{ textAlign: 'left', borderBottom: '1px solid #e5e7eb', padding: '6px 8px' }}>
                                      Họ tên
                                    </th>
                                    <th style={{ textAlign: 'left', borderBottom: '1px solid #e5e7eb', padding: '6px 8px' }}>
                                      Chức vụ
                                    </th>
                                  </tr>
                                </thead>
                                <tbody>
                                  {appraisalPayload.members.map((m, i) => (
                                    <tr key={i}>
                                      <td style={{ borderBottom: '1px solid #f3f4f6', padding: '6px 8px' }}>
                                        {m.name?.trim() || '—'}
                                      </td>
                                      <td style={{ borderBottom: '1px solid #f3f4f6', padding: '6px 8px' }}>
                                        {m.position?.trim() || '—'}
                                      </td>
                                    </tr>
                                  ))}
                                </tbody>
                              </table>
                            </div>
                          </div>
                        </div>
                      ) : null}
                      {appraisalPayload.assetSpecs ? (
                        <div className="mark-damaged-info-row">
                          <div className="mark-damaged-info-item" style={{ gridColumn: '1 / -1' }}>
                            <label>Đặc điểm kỹ thuật</label>
                            <div className="mark-damaged-info-value">{appraisalPayload.assetSpecs}</div>
                          </div>
                        </div>
                      ) : null}
                      {appraisalPayload.assetCondition ? (
                        <div className="mark-damaged-info-row">
                          <div className="mark-damaged-info-item" style={{ gridColumn: '1 / -1' }}>
                            <label>Tình trạng chất lượng</label>
                            <div className="mark-damaged-info-value">{appraisalPayload.assetCondition}</div>
                          </div>
                        </div>
                      ) : null}
                      {appraisalPayload.assetOrigin ? (
                        <div className="mark-damaged-info-row">
                          <div className="mark-damaged-info-item" style={{ gridColumn: '1 / -1' }}>
                            <label>Nguồn gốc tài sản</label>
                            <div className="mark-damaged-info-value">{appraisalPayload.assetOrigin}</div>
                          </div>
                        </div>
                      ) : null}
                      {appraisalPayload.conclusion ? (
                        <div className="mark-damaged-info-row">
                          <div className="mark-damaged-info-item" style={{ gridColumn: '1 / -1' }}>
                            <label>Kết luận thẩm định</label>
                            <div className="mark-damaged-info-value">{appraisalPayload.conclusion}</div>
                          </div>
                        </div>
                      ) : null}
                    </>
                  ) : execDto?.executionNote?.trim() ? (
                    <div className="mark-damaged-info-row">
                      <div className="mark-damaged-info-item" style={{ gridColumn: '1 / -1' }}>
                        <label>Ghi chú hồ sơ (không đọc được định dạng biên bản)</label>
                        <div className="mark-damaged-info-value" style={{ whiteSpace: 'pre-wrap' }}>
                          {execDto.executionNote}
                        </div>
                      </div>
                    </div>
                  ) : null}
                </div>
                  </>
                )}
              </div>
            )}

            {showAccountantExtras && (
              <div className="mark-damaged-info-section" style={{ marginTop: 16 }}>
                <h3 className="mark-damaged-section-title">Thông tin phục vụ thẩm định (kế toán)</h3>
                <div className="mark-damaged-info-grid">
                  <div className="mark-damaged-info-row">
                    <div className="mark-damaged-info-item">
                      <label>Loại tài sản</label>
                      <div className="mark-damaged-info-value">{row.assetTypeName?.trim() || '—'}</div>
                    </div>
                    <div className="mark-damaged-info-item">
                      <label>Giá trị khai báo trên đơn</label>
                      <div className="mark-damaged-info-value">{formatMoneyVnd(row.disposalDeclaredValue)}</div>
                    </div>
                  </div>
                  <div className="mark-damaged-info-row">
                    <div className="mark-damaged-info-item">
                      <label>Nguyên giá (cá thể)</label>
                      <div className="mark-damaged-info-value">{formatMoneyVnd(row.originalPrice)}</div>
                    </div>
                    <div className="mark-damaged-info-item">
                      <label>Giá trị còn lại trên sổ</label>
                      <div className="mark-damaged-info-value">{formatMoneyVnd(row.currentValue)}</div>
                    </div>
                  </div>
                  <div className="mark-damaged-info-row">
                    <div className="mark-damaged-info-item" style={{ gridColumn: '1 / -1' }}>
                      <button
                        type="button"
                        className="mark-damaged-btn-submit"
                        style={{ marginTop: 4, width: 'auto', alignSelf: 'flex-start' }}
                        disabled={!canOpenInstance}
                        onClick={() => {
                          if (!canOpenInstance) return;
                          onClose();
                          navigate(`/asset-instances/${instanceId}`, {
                            state: {
                              backToPath: returnPathAfterInstance,
                              backLabel: returnLabelAfterInstance,
                            },
                          });
                        }}
                      >
                        Xem chi tiết cá thể
                      </button>
                      {!canOpenInstance && (
                        <div className="mark-damaged-info-value" style={{ marginTop: 8, fontSize: 13 }}>
                          Không có mã cá thể để mở trang chi tiết.
                        </div>
                      )}
                    </div>
                  </div>
                </div>
              </div>
            )}
          </div>
        </div>

        <div className="mark-damaged-modal__footer">
          <button type="button" className="mark-damaged-btn-draft" onClick={onClose}>
            Đóng
          </button>
        </div>
      </div>
    </div>
  );
}

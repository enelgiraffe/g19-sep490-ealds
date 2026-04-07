import { useEffect, useState } from 'react';
import { message, Modal } from 'antd';
import dayjs, { type Dayjs } from 'dayjs';
import {
  disposalExecutionService,
  type DisposalExecutionDto,
} from '../../requests/services/disposalExecutionService';
import { parseIntegerMoneyInput } from '../../../shared/utils/moneyInput';
import './LiquidationExecutionModal.css';
import '../../assets/components/MarkDamagedAssetModal.css';

function axiosErrorDetail(e: unknown): string | null {
  const err = e as { response?: { data?: unknown } };
  const d = err?.response?.data;
  if (typeof d === 'string') return d;
  if (d && typeof d === 'object') {
    const o = d as Record<string, unknown>;
    if (typeof o.detail === 'string') return o.detail;
    if (typeof o.title === 'string') return o.title;
    if (typeof o.message === 'string') return o.message;
  }
  return null;
}

function mentionsMissingDisposalExecutionTable(text: string | null | undefined): boolean {
  if (!text) return false;
  const t = text.toLowerCase();
  return t.includes('disposalexecution') || t.includes("invalid object name 'disposal");
}

export interface LiquidationExecutionModalProps {
  open: boolean;
  assetRequestId: number | null;
  requestCode?: string;
  userId: number | undefined;
  onClose: () => void;
  onSuccess: () => void | Promise<void>;
}

export function LiquidationExecutionModal({
  open,
  assetRequestId,
  requestCode,
  userId,
  onClose,
  onSuccess,
}: LiquidationExecutionModalProps) {
  const [hydrating, setHydrating] = useState(false);
  const [loadError, setLoadError] = useState<string | null>(null);
  const [saving, setSaving] = useState(false);
  const [finalizing, setFinalizing] = useState(false);
  const [dto, setDto] = useState<DisposalExecutionDto | null>(null);

  const [plannedAt, setPlannedAt] = useState<Dayjs | null>(null);
  const [executedAt, setExecutedAt] = useState<Dayjs | null>(null);
  const [buyerName, setBuyerName] = useState('');
  const [buyerContact, setBuyerContact] = useState('');
  const [contractNo, setContractNo] = useState('');
  const [invoiceNo, setInvoiceNo] = useState('');
  const [minutesNo, setMinutesNo] = useState('');
  const [actualValueText, setActualValueText] = useState('');
  const [expenseText, setExpenseText] = useState('');
  const [executionNote, setExecutionNote] = useState('');

  useEffect(() => {
    if (!open || assetRequestId == null) {
      setDto(null);
      setLoadError(null);
      return;
    }
    let cancelled = false;

    setPlannedAt(null);
    setExecutedAt(null);
    setBuyerName('');
    setBuyerContact('');
    setContractNo('');
    setInvoiceNo('');
    setMinutesNo('');
    setActualValueText('');
    setExpenseText('');
    setExecutionNote('');
    setLoadError(null);

    // Form là nhập tay; GET chỉ để gộp bản nháp đã lưu (nếu có). Luôn cho phép nhập ngay.
    setDto({
      assetRequestId,
      disposalExecutionId: null,
      status: 0,
      canEdit: true,
      canFinalize: false,
      assetRequestStatus: 2,
      blockFinalizeReason: 'Ghi nhận biên bản thẩm định trước, sau đó mới ghi nhận biên bản thanh lý.',
    });

    setHydrating(true);
    disposalExecutionService
      .getByAssetRequest(assetRequestId)
      .then((d) => {
        if (cancelled) return;
        setDto(d);
        setLoadError(null);
        setPlannedAt(d.plannedExecutionDate ? dayjs(d.plannedExecutionDate) : null);
        setExecutedAt(d.executedDate ? dayjs(d.executedDate) : null);
        setBuyerName(d.buyerName ?? '');
        setBuyerContact(d.buyerContact ?? '');
        setContractNo(d.contractNo ?? '');
        setInvoiceNo(d.invoiceNo ?? '');
        setMinutesNo(d.minutesNo ?? '');
        setActualValueText(
          d.actualDisposalValue != null && !Number.isNaN(Number(d.actualDisposalValue))
            ? Math.floor(Number(d.actualDisposalValue)).toLocaleString('en-US')
            : '',
        );
        setExpenseText(
          d.expenseValue != null && !Number.isNaN(Number(d.expenseValue))
            ? Math.floor(Number(d.expenseValue)).toLocaleString('en-US')
            : '',
        );
        setExecutionNote(d.executionNote ?? '');
      })
      .catch((e) => {
        if (cancelled) return;
        const detail = axiosErrorDetail(e);
        setLoadError(detail ?? 'Không đọc được dữ liệu từ máy chủ.');
        if (mentionsMissingDisposalExecutionTable(detail)) {
          message.warning(
            'Database chưa có bảng DisposalExecution. Chạy script SQL: g19-sep490-ealds.Server/Scripts/20260401_AddDisposalExecutionTable.sql — sau đó Lưu nháp mới ghi được.',
            8,
          );
        } else {
          message.warning(
            'Không tải được bản nháp đã lưu; bạn vẫn nhập form và Lưu nháp bình thường nếu API cho phép.',
            5,
          );
        }
      })
      .finally(() => {
        if (!cancelled) setHydrating(false);
      });
    return () => {
      cancelled = true;
    };
  }, [open, assetRequestId]);

  if (!open || assetRequestId == null) return null;

  const completed = (dto?.status ?? 0) >= 2;
  const canEdit = !completed && (dto?.canEdit ?? true);
  const canFinalize = !completed && !!dto?.canFinalize;
  const isAppraisalStep = (dto?.assetRequestStatus ?? 2) === 2;

  const buildPayload = () => {
    const actual = parseIntegerMoneyInput(actualValueText);
    const expense = parseIntegerMoneyInput(expenseText);
    return {
      userId: userId!,
      plannedExecutionDate: plannedAt?.toISOString() ?? null,
      executedDate: executedAt?.toISOString() ?? null,
      buyerName: buyerName.trim() || null,
      buyerContact: buyerContact.trim() || null,
      contractNo: contractNo.trim() || null,
      invoiceNo: invoiceNo.trim() || null,
      minutesNo: minutesNo.trim() || null,
      actualDisposalValue: actual,
      expenseValue: expense,
      executionNote: executionNote.trim() || null,
    };
  };

  const handleSave = async () => {
    if (!userId) return;
    setSaving(true);
    try {
      const next = await disposalExecutionService.save(assetRequestId, buildPayload());
      setDto(next);
      message.success('Đã lưu thông tin thực hiện thanh lý.');
    } catch (e) {
      const detail = axiosErrorDetail(e);
      if (mentionsMissingDisposalExecutionTable(detail)) {
        message.error(
          'Chưa có bảng DisposalExecution trên SQL Server. Chạy script: Scripts/20260401_AddDisposalExecutionTable.sql rồi thử lại.',
          8,
        );
      } else {
        message.error(detail?.slice(0, 200) ?? 'Lưu thất bại.');
      }
    } finally {
      setSaving(false);
    }
  };

  const handleFinalize = () => {
    if (!userId) return;
    Modal.confirm({
      title: 'Hoàn tất thanh lý?',
      content:
        'Hệ thống sẽ dừng khấu hao (gỡ chính sách, khóa bút toán khấu hao hiện tại) và chuyển cá thể tài sản sang trạng thái đã thanh lý. Thao tác không thể hoàn tác qua màn hình này.',
      okText: 'Hoàn tất',
      cancelText: 'Hủy',
      okButtonProps: { danger: true, loading: finalizing },
      onOk: async () => {
        setFinalizing(true);
        try {
          await disposalExecutionService.finalize(assetRequestId, userId);
          message.success('Đã hoàn tất thanh lý tài sản.');
          await onSuccess();
          onClose();
        } catch (e: unknown) {
          const msg =
            (e as { response?: { data?: string | { message?: string } } })?.response?.data;
          const text =
            typeof msg === 'string'
              ? msg
              : typeof msg === 'object' && msg && 'message' in msg
                ? String((msg as { message?: string }).message)
                : 'Hoàn tất thất bại.';
          message.error(text);
        } finally {
          setFinalizing(false);
        }
      },
    });
  };

  const handleRecordAppraisal = async () => {
    if (!userId) return;
    if (!plannedAt) {
      message.error('Vui lòng nhập ngày thẩm định.');
      return;
    }
    if (!minutesNo.trim() && !executionNote.trim()) {
      message.error('Vui lòng nhập số biên bản hoặc kết luận thẩm định.');
      return;
    }
    setSaving(true);
    try {
      const next = await disposalExecutionService.recordAppraisal(assetRequestId, {
        userId,
        appraisalDate: plannedAt.toISOString(),
        appraisalMinutesNo: minutesNo.trim() || null,
        appraisalConclusion: executionNote.trim() || null,
      });
      setDto(next);
      message.success('Đã ghi nhận biên bản thẩm định.');
      await onSuccess();
    } catch (e) {
      message.error(axiosErrorDetail(e)?.slice(0, 200) ?? 'Ghi nhận thẩm định thất bại.');
    } finally {
      setSaving(false);
    }
  };

  return (
    <div className="mark-damaged-modal-overlay" role="dialog" aria-modal="true">
      <div className="mark-damaged-modal">
        <button type="button" className="mark-damaged-modal__close-btn" onClick={onClose} aria-label="Đóng">
          <span className="mark-damaged-modal__close">×</span>
        </button>

        <div className="mark-damaged-modal__header">
          <h2 className="mark-damaged-modal__title">
            {isAppraisalStep ? 'Ghi nhận biên bản thẩm định' : 'Ghi nhận biên bản thanh lý'} — {requestCode ?? `YC-${assetRequestId}`}
          </h2>
        </div>

        <div className="mark-damaged-modal__body">
          <div className="mark-damaged-modal__content">
            <>
              
              {hydrating && (
                <p style={{ marginBottom: 8, fontSize: 13 }}>Đang kiểm tra bản nháp đã lưu (nếu có)…</p>
              )}
              {loadError && (
                <p className="disposal-appraisal-member-hint" style={{ marginBottom: 12 }}>
                  Không đọc được bản nháp: {loadError.slice(0, 280)}
                  {loadError.length > 280 ? '…' : ''}
                </p>
              )}
              {dto?.blockFinalizeReason && !loadError && (
                <p className="disposal-appraisal-member-hint" style={{ marginBottom: 12 }}>
                  {dto.blockFinalizeReason}
                </p>
              )}
              {completed && (
                <p style={{ marginBottom: 12, color: '#16a34a' }}>Đã hoàn tất thực hiện thanh lý.</p>
              )}

              <div className="mark-damaged-form-section">
                <h3 className="mark-damaged-section-title">
                  {isAppraisalStep ? 'Thông tin biên bản thẩm định' : 'Thông tin biên bản thanh lý'}
                </h3>
                  <div className="mark-damaged-form__item">
                    <label htmlFor="lex-planned">{isAppraisalStep ? 'Ngày thẩm định' : 'Ngày dự kiến'}</label>
                    <input
                      id="lex-planned"
                      type="date"
                      className={canEdit ? 'mark-damaged-input' : 'mark-damaged-input--disabled'}
                      disabled={!canEdit}
                      value={plannedAt ? plannedAt.format('YYYY-MM-DD') : ''}
                      onChange={(e) =>
                        setPlannedAt(e.target.value ? dayjs(e.target.value) : null)
                      }
                    />
                  </div>
                  <div className="mark-damaged-form__item">
                    <label htmlFor="lex-executed">
                      Ngày thực hiện<span style={{ color: '#ef4444' }}>*</span>
                    </label>
                    <input
                      id="lex-executed"
                      type="date"
                      className={canEdit ? 'mark-damaged-input' : 'mark-damaged-input--disabled'}
                      disabled={!canEdit}
                      value={executedAt ? executedAt.format('YYYY-MM-DD') : ''}
                      onChange={(e) =>
                        setExecutedAt(e.target.value ? dayjs(e.target.value) : null)
                      }
                    />
                  </div>
                  <div className="mark-damaged-form__item">
                    <label htmlFor="lex-buyer">Bên nhận / đơn vị thu mua</label>
                    <input
                      id="lex-buyer"
                      type="text"
                      className={canEdit ? 'mark-damaged-input' : 'mark-damaged-input--disabled'}
                      disabled={!canEdit}
                      value={buyerName}
                      onChange={(e) => setBuyerName(e.target.value)}
                    />
                  </div>
                  <div className="mark-damaged-form__item">
                    <label htmlFor="lex-contact">Liên hệ</label>
                    <input
                      id="lex-contact"
                      type="text"
                      className={canEdit ? 'mark-damaged-input' : 'mark-damaged-input--disabled'}
                      disabled={!canEdit}
                      value={buyerContact}
                      onChange={(e) => setBuyerContact(e.target.value)}
                    />
                  </div>
                  <div className="mark-damaged-form__item">
                    <label htmlFor="lex-contract">Số hợp đồng</label>
                    <input
                      id="lex-contract"
                      type="text"
                      className={canEdit ? 'mark-damaged-input' : 'mark-damaged-input--disabled'}
                      disabled={!canEdit}
                      value={contractNo}
                      onChange={(e) => setContractNo(e.target.value)}
                    />
                  </div>
                  <div className="mark-damaged-form__item">
                    <label htmlFor="lex-invoice">Số hóa đơn</label>
                    <input
                      id="lex-invoice"
                      type="text"
                      className={canEdit ? 'mark-damaged-input' : 'mark-damaged-input--disabled'}
                      disabled={!canEdit}
                      value={invoiceNo}
                      onChange={(e) => setInvoiceNo(e.target.value)}
                    />
                  </div>
                  <div className="mark-damaged-form__item">
                    <label htmlFor="lex-minutes">
                      {isAppraisalStep ? 'Số biên bản thẩm định' : 'Số biên bản giao nhận'}
                    </label>
                    <input
                      id="lex-minutes"
                      type="text"
                      className={canEdit ? 'mark-damaged-input' : 'mark-damaged-input--disabled'}
                      disabled={!canEdit}
                      value={minutesNo}
                      onChange={(e) => setMinutesNo(e.target.value)}
                    />
                  </div>
                  <div className="mark-damaged-form__item">
                    <label htmlFor="lex-actual">
                      Số tiền thu được<span style={{ color: '#ef4444' }}>*</span>
                    </label>
                    <div className="disposal-appraisal-money-input">
                      <input
                        id="lex-actual"
                        type="text"
                        inputMode="numeric"
                        className={canEdit ? 'mark-damaged-input' : 'mark-damaged-input--disabled'}
                        disabled={!canEdit}
                        value={actualValueText}
                        onChange={(e) => {
                          const n = parseIntegerMoneyInput(e.target.value);
                          setActualValueText(
                            n == null ? '' : Math.floor(n).toLocaleString('en-US'),
                          );
                        }}
                      />
                      <span className="disposal-appraisal-money-suffix">đ</span>
                    </div>
                  </div>
                  <div className="mark-damaged-form__item">
                    <label htmlFor="lex-expense">Chi phí liên quan</label>
                    <div className="disposal-appraisal-money-input">
                      <input
                        id="lex-expense"
                        type="text"
                        inputMode="numeric"
                        className={canEdit ? 'mark-damaged-input' : 'mark-damaged-input--disabled'}
                        disabled={!canEdit}
                        value={expenseText}
                        onChange={(e) => {
                          const n = parseIntegerMoneyInput(e.target.value);
                          setExpenseText(n == null ? '' : Math.floor(n).toLocaleString('en-US'));
                        }}
                      />
                      <span className="disposal-appraisal-money-suffix">đ</span>
                    </div>
                  </div>
                  <div className="mark-damaged-form__item">
                    <label htmlFor="lex-note">{isAppraisalStep ? 'Kết luận thẩm định' : 'Ghi chú'}</label>
                    <textarea
                      id="lex-note"
                      className="mark-damaged-textarea"
                      rows={3}
                      readOnly={!canEdit}
                      disabled={!canEdit}
                      value={executionNote}
                      onChange={(e) => setExecutionNote(e.target.value)}
                    />
                  </div>
                </div>
            </>
          </div>
        </div>

        <div className="mark-damaged-modal__footer">
          <button type="button" className="mark-damaged-btn-draft" onClick={onClose} disabled={saving || finalizing}>
            Đóng
          </button>
          {canEdit && (
            <button
              type="button"
              className="mark-damaged-btn-submit"
              disabled={saving || finalizing || !userId}
              onClick={() => void handleSave()}
            >
              {saving ? 'Đang lưu...' : 'Lưu nháp'}
            </button>
          )}
          {isAppraisalStep && canEdit && (
            <button
              type="button"
              className="mark-damaged-btn-submit"
              disabled={saving || finalizing || !userId}
              onClick={() => void handleRecordAppraisal()}
            >
              Ghi nhận biên bản thẩm định
            </button>
          )}
          {canFinalize && !isAppraisalStep && (
            <button
              type="button"
              className="mark-damaged-btn-submit"
              disabled={saving || finalizing || !userId}
              onClick={() => handleFinalize()}
            >
              Ghi nhận biên bản thanh lý
            </button>
          )}
        </div>
      </div>
    </div>
  );
}

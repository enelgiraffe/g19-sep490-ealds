import { useEffect, useMemo, useState } from 'react';
import { message } from 'antd';
import { LiquidationDisposalDetailModal } from '../../liquidation/components/LiquidationDisposalDetailModal';
import { disposalRequestService } from '../../assets/services/disposalRequestService';
import type { TransferRequestListItem } from '../../assets/services/transferRequestService';
import '../../assets/components/MarkDamagedAssetModal.css';
import './DisposalAppraisalDetailModal.css';
import {
  disposalAppraisalService,
  type DisposalAppraisalDetail,
  type DisposalAppraisalReport,
} from '../services/disposalAppraisalService';
import { parseIntegerMoneyInput } from '../../../shared/utils/moneyInput';
import { vndIntegerToVietnameseWords } from '../../../shared/utils/vietnameseMoneyWords';

function formatDate(iso: string): string {
  try {
    return new Date(iso).toLocaleDateString('vi-VN');
  } catch {
    return iso;
  }
}

function isReportComplete(report: DisposalAppraisalReport | null | undefined): boolean {
  if (!report) return false;
  const v = report.appraisedValue;
  if (v == null || Number.isNaN(Number(v)) || Math.floor(Number(v)) <= 0) return false;
  const fields = [
    report.minutesNo,
    report.appraisalMethod,
    report.appraisedValueInWords,
    report.appraisalOutcome,
    report.summary,
    report.recommendation,
  ];
  return fields.every((s) => (s ?? '').trim().length > 0);
}

/** Khi tải từ API: giữ chữ đã lưu; chỉ tự sinh nếu đang trống. */
function mergeAppraisedWordsFromApi(d: DisposalAppraisalDetail): DisposalAppraisalDetail {
  const v = d.report?.appraisedValue;
  if (v == null || Number.isNaN(Number(v))) return d;
  const n = Math.floor(Number(v));
  const existing = (d.report?.appraisedValueInWords ?? '').trim();
  const words = existing.length > 0 ? d.report!.appraisedValueInWords! : vndIntegerToVietnameseWords(n);
  return {
    ...d,
    report: {
      ...(d.report ?? {}),
      appraisedValue: n,
      appraisedValueInWords: words,
    },
  };
}

export interface DisposalAppraisalDetailModalProps {
  open: boolean;
  appraisalId: number | null;
  userId: number | undefined;
  onClose: () => void;
  onRefreshList: () => void | Promise<void>;
}

export function DisposalAppraisalDetailModal({
  open,
  appraisalId,
  userId,
  onClose,
  onRefreshList,
}: DisposalAppraisalDetailModalProps) {
  const [detail, setDetail] = useState<DisposalAppraisalDetail | null>(null);
  const [loading, setLoading] = useState(false);
  const [appraisalSaving, setAppraisalSaving] = useState(false);
  const [decisionModalOpen, setDecisionModalOpen] = useState(false);
  const [decisionChoice, setDecisionChoice] = useState<'confirm' | 'reject'>('confirm');
  const [decisionNote, setDecisionNote] = useState('');
  const [decisionSubmitting, setDecisionSubmitting] = useState(false);
  const [liquidationDetailOpen, setLiquidationDetailOpen] = useState(false);
  const [liquidationDetailRow, setLiquidationDetailRow] = useState<TransferRequestListItem | null>(null);
  const [liquidationDetailLoading, setLiquidationDetailLoading] = useState(false);

  useEffect(() => {
    if (!open || appraisalId == null || !userId) {
      setDetail(null);
      setDecisionModalOpen(false);
      setDecisionNote('');
      setLiquidationDetailOpen(false);
      setLiquidationDetailRow(null);
      return;
    }
    let cancelled = false;
    setLoading(true);
    disposalAppraisalService
      .getDetail(appraisalId, userId)
      .then((d) => {
        if (!cancelled) {
          setDetail(mergeAppraisedWordsFromApi(d));
          setDecisionModalOpen(false);
          setDecisionNote('');
        }
      })
      .catch(() => {
        if (!cancelled) {
          message.error('Không tải được chi tiết thẩm định.');
          onClose();
        }
      })
      .finally(() => {
        if (!cancelled) setLoading(false);
      });
    return () => {
      cancelled = true;
    };
  }, [open, appraisalId, userId]);

  const myMember = useMemo(() => {
    if (!detail || !userId) return undefined;
    return detail.members.find((m) => m.userId === userId);
  }, [detail, userId]);

  const reportFilled = detail ? isReportComplete(detail.report) : false;
  const reportLocked = (detail?.status ?? 0) >= 4;
  const canEditReport = !!detail?.isReporter && !reportLocked;

  const showMemberConfirmAction =
    !!detail &&
    detail.status >= 2 &&
    detail.status < 4 &&
    detail.isRelatedMember &&
    !!myMember &&
    myMember.decision === 0 &&
    reportFilled;

  const showAwaitingReportHint =
    !!detail &&
    detail.status < 4 &&
    detail.isRelatedMember &&
    !!myMember &&
    myMember.decision === 0 &&
    !reportFilled;

  if (!open) return null;

  const handleClose = () => {
    onClose();
    setDetail(null);
    setDecisionModalOpen(false);
    setDecisionNote('');
    setLiquidationDetailOpen(false);
    setLiquidationDetailRow(null);
  };

  const instanceReturnPath =
    detail != null
      ? `/requests?tab=liquidation&liquidationPill=appraisals&openAppraisal=${detail.appraisalId}`
      : '/requests?tab=liquidation&liquidationPill=appraisals';

  const submitDecisionFromModal = async () => {
    if (!userId || !detail) return;
    if (decisionChoice === 'reject' && !decisionNote.trim()) {
      message.warning('Vui lòng nhập ghi chú / lý do khi không xác nhận.');
      return;
    }
    setDecisionSubmitting(true);
    try {
      if (decisionChoice === 'confirm') {
        await disposalAppraisalService.saveDecision(detail.appraisalId, {
          userId,
          decision: 1,
        });
        message.success('Đã xác nhận biên bản.');
      } else {
        await disposalAppraisalService.saveDecision(detail.appraisalId, {
          userId,
          decision: 2,
          rejectReason: decisionNote.trim(),
        });
        message.success('Đã ghi nhận không xác nhận.');
      }
      const next = await disposalAppraisalService.getDetail(detail.appraisalId, userId);
      setDetail(mergeAppraisedWordsFromApi(next));
      setDecisionModalOpen(false);
      setDecisionNote('');
      await onRefreshList();
    } catch {
      message.error('Cập nhật quyết định thất bại.');
    } finally {
      setDecisionSubmitting(false);
    }
  };

  return (
    <>
      <div className="mark-damaged-modal-overlay" role="dialog" aria-modal="true">
        <div className="mark-damaged-modal disposal-appraisal-detail-modal">
          <button
            type="button"
            className="mark-damaged-modal__close-btn"
            onClick={handleClose}
            aria-label="Đóng"
          >
            <span className="mark-damaged-modal__close">×</span>
          </button>

          <div className="mark-damaged-modal__header">
            <h2 className="mark-damaged-modal__title">
              Thẩm định YC-{detail?.assetRequestId ?? appraisalId ?? '...'}
            </h2>
          </div>

          <div className="mark-damaged-modal__body">
            {loading || !detail ? (
              <div className="mark-damaged-modal__content">
                <div className="mark-damaged-form__item" style={{ marginBottom: 0 }}>
                  Đang tải chi tiết thẩm định...
                </div>
              </div>
            ) : (
              <div className="mark-damaged-modal__content">
                <div className="mark-damaged-info-section">
                  <h3 className="mark-damaged-section-title">Thông tin yêu cầu</h3>
                  <div className="mark-damaged-info-grid">
                    <div className="mark-damaged-info-row">
                      <div className="mark-damaged-info-item">
                        <label>Nội dung</label>
                        <div className="mark-damaged-info-value">{detail.requestTitle || '—'}</div>
                      </div>
                      <div className="mark-damaged-info-item">
                        <label>Ngày hẹn thẩm định</label>
                        <div className="mark-damaged-info-value">
                          {detail.scheduledAt ? formatDate(detail.scheduledAt) : '—'}
                        </div>
                      </div>
                    </div>
                    <div className="mark-damaged-info-row">
                      <div className="mark-damaged-info-item">
                        <label>Địa điểm (phòng ban)</label>
                        <div className="mark-damaged-info-value">
                          {detail.meetingDepartmentName || detail.meetingLocation || '—'}
                        </div>
                      </div>
                    </div>
                    <div className="mark-damaged-info-row">
                      <div className="mark-damaged-info-item" style={{ gridColumn: '1 / -1' }}>
                        <button
                          type="button"
                          className="mark-damaged-btn-submit"
                          style={{ marginTop: 4, width: 'auto', alignSelf: 'flex-start' }}
                          disabled={liquidationDetailLoading}
                          onClick={() => {
                            if (!detail) return;
                            setLiquidationDetailLoading(true);
                            disposalRequestService
                              .getList()
                              .then((list) => {
                                const row = list.find((r) => r.assetRequestId === detail.assetRequestId);
                                if (!row) {
                                  message.warning('Không tìm thấy yêu cầu thanh lý tương ứng.');
                                  return;
                                }
                                setLiquidationDetailRow(row);
                                setLiquidationDetailOpen(true);
                              })
                              .catch(() => {
                                message.error('Không tải được chi tiết yêu cầu thanh lý.');
                              })
                              .finally(() => setLiquidationDetailLoading(false));
                          }}
                        >
                          {liquidationDetailLoading ? 'Đang tải...' : 'Chi tiết yêu cầu thanh lý'}
                        </button>
                      </div>
                    </div>
                  </div>
                </div>

                <div className="mark-damaged-info-section">
                  <h3 className="mark-damaged-section-title">Hội đồng thẩm định</h3>
                  <div className="mark-damaged-info-grid">
                    {detail.members.map((m) => (
                      <div key={m.appraisalMemberId} className="mark-damaged-info-row">
                        <div className="mark-damaged-info-item" style={{ gridColumn: '1 / -1' }}>
                          <label>
                            {m.memberName}
                            {m.memberRole ? ` (${m.memberRole})` : ''}
                            {m.isReporter ? ' — Người nhập biên bản' : ''}
                          </label>
                          <div className="mark-damaged-info-value">
                            {m.decision === 1
                              ? 'Đã xác nhận'
                              : m.decision === 2
                                ? `Không xác nhận${m.rejectReason ? `: ${m.rejectReason}` : ''}`
                                : 'Chờ xác nhận'}
                          </div>
                        </div>
                      </div>
                    ))}
                  </div>
                </div>

                {showAwaitingReportHint && (
                  <p className="disposal-appraisal-member-hint">
                    Người nhập biên bản chưa hoàn tất đầy đủ các trường trong biên bản thẩm định. Khi biên bản đã
                    được lưu đủ thông tin, bạn mới có thể thực hiện xác nhận.
                  </p>
                )}

                <div className="mark-damaged-form-section">
                  <h3 className="mark-damaged-section-title">Biên bản thẩm định</h3>

                  {reportLocked && (
                    <p className="disposal-appraisal-member-hint" style={{ marginBottom: 12 }}>
                      Hội đồng đã xác nhận biên bản — không thể chỉnh sửa hoặc lưu lại.
                    </p>
                  )}

                  <div className="mark-damaged-form__item">
                    <label htmlFor="appraisal-minutes-no">Số biên bản</label>
                    <input
                      id="appraisal-minutes-no"
                      type="text"
                      className={canEditReport ? 'mark-damaged-input' : 'mark-damaged-input--disabled'}
                      placeholder="Số biên bản"
                      readOnly={!canEditReport}
                      disabled={!canEditReport || appraisalSaving}
                      value={detail.report?.minutesNo ?? ''}
                      onChange={(e) =>
                        setDetail((prev) =>
                          prev
                            ? {
                                ...prev,
                                report: { ...(prev.report ?? {}), minutesNo: e.target.value },
                              }
                            : prev,
                        )
                      }
                    />
                  </div>

                  <div className="mark-damaged-form__item">
                    <label htmlFor="appraisal-method">Phương pháp thẩm định</label>
                    <input
                      id="appraisal-method"
                      type="text"
                      className={canEditReport ? 'mark-damaged-input' : 'mark-damaged-input--disabled'}
                      placeholder="Phương pháp thẩm định"
                      readOnly={!canEditReport}
                      disabled={!canEditReport || appraisalSaving}
                      value={detail.report?.appraisalMethod ?? ''}
                      onChange={(e) =>
                        setDetail((prev) =>
                          prev
                            ? {
                                ...prev,
                                report: { ...(prev.report ?? {}), appraisalMethod: e.target.value },
                              }
                            : prev,
                        )
                      }
                    />
                  </div>

                  <div className="mark-damaged-form__item">
                    <label htmlFor="appraisal-value-num">Giá trị đề xuất thẩm định</label>
                    <div className="disposal-appraisal-money-input">
                      <input
                        id="appraisal-value-num"
                        type="text"
                        className={canEditReport ? 'mark-damaged-input' : 'mark-damaged-input--disabled'}
                        inputMode="numeric"
                        placeholder="Nhập số tiền"
                        readOnly={!canEditReport}
                        disabled={!canEditReport || appraisalSaving}
                        value={
                          detail.report?.appraisedValue != null &&
                          !Number.isNaN(Number(detail.report.appraisedValue))
                            ? Math.floor(Number(detail.report.appraisedValue)).toLocaleString('en-US')
                            : ''
                        }
                        onChange={(e) => {
                          const num = parseIntegerMoneyInput(e.target.value);
                          setDetail((prev) => {
                            if (!prev) return prev;
                            if (num == null) {
                              return {
                                ...prev,
                                report: {
                                  ...(prev.report ?? {}),
                                  appraisedValue: null,
                                  appraisedValueInWords: '',
                                },
                              };
                            }
                            return {
                              ...prev,
                              report: {
                                ...(prev.report ?? {}),
                                appraisedValue: num,
                                appraisedValueInWords: vndIntegerToVietnameseWords(num),
                              },
                            };
                          });
                        }}
                      />
                      <span className="disposal-appraisal-money-suffix">đ</span>
                    </div>
                  </div>

                  <div className="mark-damaged-form__item">
                    <label htmlFor="appraisal-value-words">
                      Giá trị bằng chữ<span style={{ color: '#ef4444' }}>*</span>
                      <span style={{ fontWeight: 400, fontSize: 12, color: '#64748b' }}>
                        {' '}
                        (tự sinh khi đổi số tiền, có thể sửa tay)
                      </span>
                    </label>
                    <textarea
                      id="appraisal-value-words"
                      className={
                        canEditReport ? 'mark-damaged-textarea' : 'mark-damaged-textarea mark-damaged-input--disabled'
                      }
                      rows={3}
                      readOnly={!canEditReport}
                      disabled={!canEditReport || appraisalSaving}
                      value={detail.report?.appraisedValueInWords ?? ''}
                      placeholder="Nhập số tiền để tự sinh, hoặc gõ trực tiếp"
                      onChange={(e) =>
                        setDetail((prev) =>
                          prev
                            ? {
                                ...prev,
                                report: { ...(prev.report ?? {}), appraisedValueInWords: e.target.value },
                              }
                            : prev,
                        )
                      }
                    />
                  </div>

                  <div className="mark-damaged-form__item">
                    <label htmlFor="appraisal-outcome">
                      Kết quả thẩm định<span style={{ color: '#ef4444' }}>*</span>
                    </label>
                    <textarea
                      id="appraisal-outcome"
                      className="mark-damaged-textarea"
                      rows={4}
                      placeholder="Mô tả kết quả thẩm định tài sản"
                      readOnly={!canEditReport}
                      disabled={!canEditReport || appraisalSaving}
                      value={detail.report?.appraisalOutcome ?? ''}
                      onChange={(e) =>
                        setDetail((prev) =>
                          prev
                            ? {
                                ...prev,
                                report: { ...(prev.report ?? {}), appraisalOutcome: e.target.value },
                              }
                            : prev,
                        )
                      }
                    />
                  </div>

                  <div className="mark-damaged-form__item">
                    <label htmlFor="appraisal-summary">Tóm tắt biên bản</label>
                    <textarea
                      id="appraisal-summary"
                      className="mark-damaged-textarea"
                      rows={4}
                      placeholder="Tóm tắt biên bản"
                      readOnly={!canEditReport}
                      disabled={!canEditReport || appraisalSaving}
                      value={detail.report?.summary ?? ''}
                      onChange={(e) =>
                        setDetail((prev) =>
                          prev
                            ? {
                                ...prev,
                                report: { ...(prev.report ?? {}), summary: e.target.value },
                              }
                            : prev,
                        )
                      }
                    />
                  </div>

                  <div className="mark-damaged-form__item">
                    <label htmlFor="appraisal-recommendation">Kiến nghị</label>
                    <textarea
                      id="appraisal-recommendation"
                      className="mark-damaged-textarea"
                      rows={4}
                      placeholder="Kiến nghị"
                      readOnly={!canEditReport}
                      disabled={!canEditReport || appraisalSaving}
                      value={detail.report?.recommendation ?? ''}
                      onChange={(e) =>
                        setDetail((prev) =>
                          prev
                            ? {
                                ...prev,
                                report: { ...(prev.report ?? {}), recommendation: e.target.value },
                              }
                            : prev,
                        )
                      }
                    />
                  </div>
                </div>
              </div>
            )}
          </div>

          <div className="mark-damaged-modal__footer">
            <button type="button" className="mark-damaged-btn-draft" onClick={handleClose}>
              Đóng
            </button>

            {detail?.isReporter && !reportLocked && (
              <button
                type="button"
                className="mark-damaged-btn-submit"
                disabled={appraisalSaving || !userId || !detail}
                onClick={async () => {
                  if (!userId || !detail) return;
                  setAppraisalSaving(true);
                  try {
                    await disposalAppraisalService.saveReport(detail.appraisalId, {
                      userId,
                      minutesNo: detail.report?.minutesNo ?? null,
                      appraisalMethod: detail.report?.appraisalMethod ?? null,
                      appraisedValue: detail.report?.appraisedValue ?? null,
                      marketReferenceValue: null,
                      appraisedValueInWords: detail.report?.appraisedValueInWords ?? null,
                      appraisalOutcome: detail.report?.appraisalOutcome ?? null,
                      summary: detail.report?.summary ?? null,
                      recommendation: detail.report?.recommendation ?? null,
                    });
                    message.success('Đã lưu biên bản thẩm định.');
                    const next = await disposalAppraisalService.getDetail(detail.appraisalId, userId);
                    setDetail(mergeAppraisedWordsFromApi(next));
                    await onRefreshList();
                  } catch {
                    message.error('Lưu biên bản thất bại.');
                  } finally {
                    setAppraisalSaving(false);
                  }
                }}
              >
                {appraisalSaving ? 'Đang lưu...' : 'Lưu biên bản'}
              </button>
            )}

            {showMemberConfirmAction && (
              <button
                type="button"
                className="mark-damaged-btn-submit"
                onClick={() => {
                  setDecisionChoice('confirm');
                  setDecisionNote('');
                  setDecisionModalOpen(true);
                }}
              >
                Xác nhận biên bản
              </button>
            )}
          </div>
        </div>
      </div>

      <LiquidationDisposalDetailModal
        open={liquidationDetailOpen}
        row={liquidationDetailRow}
        onClose={() => {
          setLiquidationDetailOpen(false);
          setLiquidationDetailRow(null);
        }}
        showAccountantExtras
        overlayClassName="disposal-appraisal-liquidation-overlay"
        returnPathAfterInstance={instanceReturnPath}
        returnLabelAfterInstance="← Quay lại biên bản thẩm định"
      />

      {decisionModalOpen && detail && (
        <div
          className="disposal-appraisal-decision-overlay"
          role="dialog"
          aria-modal="true"
          aria-labelledby="appraisal-decision-title"
        >
          <div className="disposal-appraisal-decision-modal">
            <div className="mark-damaged-modal__header">
              <h2 id="appraisal-decision-title" className="mark-damaged-modal__title">
                Xác nhận biên bản thẩm định
              </h2>
            </div>
            <div className="mark-damaged-modal__body">
              <div className="mark-damaged-modal__content">
                <div className="mark-damaged-form-section" style={{ marginBottom: 0 }}>
                  <div className="mark-damaged-form__item">
                    <label htmlFor="appraisal-decision-select">Quyết định</label>
                    <select
                      id="appraisal-decision-select"
                      className="mark-damaged-input"
                      value={decisionChoice}
                      onChange={(e) =>
                        setDecisionChoice(e.target.value === 'reject' ? 'reject' : 'confirm')
                      }
                      disabled={decisionSubmitting}
                    >
                      <option value="confirm">Xác nhận</option>
                      <option value="reject">Không xác nhận</option>
                    </select>
                  </div>
                  <div className="mark-damaged-form__item">
                    <label htmlFor="appraisal-decision-note">
                      Ghi chú{decisionChoice === 'reject' ? <span style={{ color: '#ef4444' }}> *</span> : null}
                    </label>
                    <textarea
                      id="appraisal-decision-note"
                      className="mark-damaged-textarea"
                      rows={4}
                      placeholder={
                        decisionChoice === 'reject'
                          ? 'Bắt buộc khi không xác nhận'
                          : 'Không bắt buộc'
                      }
                      value={decisionNote}
                      onChange={(e) => setDecisionNote(e.target.value)}
                      disabled={decisionSubmitting}
                    />
                  </div>
                </div>
              </div>
            </div>
            <div className="mark-damaged-modal__footer">
              <button
                type="button"
                className="mark-damaged-btn-draft"
                onClick={() => {
                  setDecisionModalOpen(false);
                  setDecisionNote('');
                }}
                disabled={decisionSubmitting}
              >
                ← Quay lại
              </button>
              <button
                type="button"
                className="mark-damaged-btn-submit"
                disabled={decisionSubmitting}
                onClick={() => void submitDecisionFromModal()}
              >
                {decisionSubmitting ? 'Đang gửi...' : 'Xác nhận'}
              </button>
            </div>
          </div>
        </div>
      )}
    </>
  );
}

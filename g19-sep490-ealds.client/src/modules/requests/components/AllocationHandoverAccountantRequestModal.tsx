import { useCallback, useEffect, useMemo, useState } from 'react';
import { Link } from 'react-router-dom';
import { Spin, message } from 'antd';
import type { AccountantRequestListItem } from '../services/accountantRequestService';
import { accountantRequestService } from '../services/accountantRequestService';
import {
  ALLOCATION_REQUEST_TYPE_ID,
  allocationRequestService,
  type AllocationOrderDetail,
} from '../../allocations/services/allocationRequestService';
import {
  HANDOVER_REQUEST_TYPE_ID,
  handoverRequestService,
} from '../../allocations/services/handoverRequestService';
import { assetService } from '../../assets/services/assetService';
import './AccountantTransferRequestDetailModal.css';
import './AllocationHandoverAccountantRequestModal.css';

type ProposedLine = {
  assetTypeId?: number;
  assetId?: number;
  quantity?: number;
  reason?: string | null;
};

function parseProposedLines(proposedData?: string | null): ProposedLine[] {
  if (!proposedData?.trim()) return [];
  try {
    const root = JSON.parse(proposedData) as { lines?: ProposedLine[] };
    return Array.isArray(root.lines) ? root.lines : [];
  } catch {
    return [];
  }
}

const ALLOC_STATUS: Record<number, { label: string; color: string }> = {
  0: { label: 'Chờ duyệt', color: 'warning' },
  2: { label: 'Đã duyệt', color: 'processing' },
  3: { label: 'Từ chối', color: 'error' },
  4: { label: 'Hoàn tất', color: 'success' },
  5: { label: 'Chờ nhận hàng (PR)', color: 'default' },
};

function statusTagClassName(color: string): string {
  const base = 'acct-transfer-status-tag';
  if (color === 'success') return `${base} acct-transfer-status-tag--success`;
  if (color === 'error') return `${base} acct-transfer-status-tag--error`;
  if (color === 'warning') return `${base} acct-transfer-status-tag--warning`;
  if (color === 'processing') return `${base} acct-transfer-status-tag--processing`;
  return base;
}

function formatDateVi(iso?: string | null): string {
  if (!iso) return '—';
  try {
    return new Date(iso).toLocaleDateString('vi-VN');
  } catch {
    return iso.slice(0, 10);
  }
}

type Props = {
  open: boolean;
  onClose: () => void;
  item: AccountantRequestListItem | null;
  variant: 'allocation' | 'handover';
  userId: number | null;
  onAfterAction: () => Promise<void>;
};

export function AllocationHandoverAccountantRequestModal({
  open,
  onClose,
  item,
  variant,
  userId,
  onAfterAction,
}: Props) {
  const [orderDetail, setOrderDetail] = useState<AllocationOrderDetail | null>(null);
  const [orderLoading, setOrderLoading] = useState(false);
  const [actionOpen, setActionOpen] = useState(false);
  const [actionMode, setActionMode] = useState<'approve' | 'reject'>('approve');
  const [comment, setComment] = useState('');
  const [submitting, setSubmitting] = useState(false);
  const [proposedAssetLabels, setProposedAssetLabels] = useState<
    Record<number, { name: string; code: string; assetTypeName: string }>
  >({});
  const [proposedLabelsLoading, setProposedLabelsLoading] = useState(false);

  const isAlloc = variant === 'allocation';
  const typeId = isAlloc ? ALLOCATION_REQUEST_TYPE_ID : HANDOVER_REQUEST_TYPE_ID;
  const orderPath = isAlloc ? 'order' : 'handover-order';

  const loadOrder = useCallback(async () => {
    if (!item?.assetAllocationOrderId) {
      setOrderDetail(null);
      setOrderLoading(false);
      return;
    }
    setOrderLoading(true);
    try {
      const d = isAlloc
        ? await allocationRequestService.getOrder(item.assetAllocationOrderId)
        : await handoverRequestService.getOrder(item.assetAllocationOrderId);
      setOrderDetail(d);
    } catch {
      setOrderDetail(null);
    } finally {
      setOrderLoading(false);
    }
  }, [isAlloc, item?.assetAllocationOrderId]);

  useEffect(() => {
    if (!open || !item) {
      setOrderDetail(null);
      setOrderLoading(false);
      return;
    }
    void loadOrder();
  }, [open, item, loadOrder]);

  useEffect(() => {
    if (!open || !item || orderLoading || orderDetail != null) {
      setProposedAssetLabels({});
      setProposedLabelsLoading(false);
      return;
    }
    const lines = parseProposedLines(item.proposedData);
    const assetIds = [
      ...new Set(lines.map((l) => l.assetId).filter((id): id is number => typeof id === 'number' && id > 0)),
    ];
    if (assetIds.length === 0) {
      setProposedAssetLabels({});
      setProposedLabelsLoading(false);
      return;
    }

    let cancelled = false;
    setProposedLabelsLoading(true);
    void (async () => {
      const entries = await Promise.all(
        assetIds.map(async (id) => {
          try {
            const a = await assetService.getById(id);
            return [
              id,
              {
                name: a.name,
                code: a.code,
                assetTypeName: (a.assetTypeName ?? '').trim() || '—',
              },
            ] as const;
          } catch {
            return [id, null] as const;
          }
        }),
      );
      if (cancelled) return;
      const next: Record<number, { name: string; code: string; assetTypeName: string }> = {};
      for (const [id, row] of entries) {
        if (row) next[id] = row;
      }
      setProposedAssetLabels(next);
      setProposedLabelsLoading(false);
    })();

    return () => {
      cancelled = true;
    };
  }, [open, item, item?.proposedData, item?.assetRequestId, orderLoading, orderDetail]);

  const openAction = (mode: 'approve' | 'reject') => {
    setActionMode(mode);
    setComment('');
    setActionOpen(true);
  };

  const submitAction = async () => {
    if (!item || userId == null) return;
    setSubmitting(true);
    try {
      if (actionMode === 'approve') {
        await accountantRequestService.approve(item.assetRequestId, {
          approvedBy: userId,
          comment: comment.trim() || null,
        });
        message.success(isAlloc ? 'Đã duyệt và tạo đơn cấp phát.' : 'Đã duyệt và tạo đơn hoàn trả.');
      } else {
        await accountantRequestService.reject(item.assetRequestId, {
          approvedBy: userId,
          comment: comment.trim() || null,
        });
        message.success('Đã từ chối yêu cầu.');
      }
      setActionOpen(false);
      onClose();
      await onAfterAction();
    } catch (e: unknown) {
      const err = e as { response?: { data?: { message?: string } | string } };
      const msg =
        typeof err?.response?.data === 'object' && err.response.data?.message
          ? err.response.data.message
          : typeof err?.response?.data === 'string'
            ? err.response.data
            : 'Thao tác thất bại.';
      message.error(msg);
    } finally {
      setSubmitting(false);
    }
  };

  const st = useMemo(
    () => (item ? ALLOC_STATUS[item.status] ?? { label: `Trạng thái ${item.status}`, color: 'default' } : null),
    [item],
  );

  const proposedLines = useMemo(() => (item ? parseProposedLines(item.proposedData) : []), [item]);

  const deptLabel = item?.targetDepartmentName?.trim() || '—';

  if (!open) return null;

  if (!item || !st) return null;

  const canAct = item.status === 0 && userId != null && item.requestTypeId === typeId;
  const modalTitle = isAlloc ? 'Chi tiết yêu cầu cấp phát' : 'Chi tiết yêu cầu hoàn trả';

  return (
    <>
      <div className="acct-transfer-modal-overlay" role="dialog" aria-modal="true">
        <div className="acct-transfer-modal">
          <button
            type="button"
            className="alloc-handover-modal__close-btn"
            onClick={onClose}
            aria-label="Đóng"
          >
            ×
          </button>

          <div className="acct-transfer-modal__header">
            <div className="acct-transfer-modal__header-left">
              <h2 className="acct-transfer-modal__title">{modalTitle}</h2>
              <span className={statusTagClassName(st.color)}>{st.label}</span>
            </div>
          </div>

          <div className="acct-transfer-modal__body">
            <div className="acct-transfer-modal__content">
              <div className="acct-transfer-form__row">
                <div className="acct-transfer-form__field">
                  <label>Mã yêu cầu</label>
                  <div className="acct-transfer-form__value">YC-{item.assetRequestId}</div>
                </div>
                <div className="acct-transfer-form__field">
                  <label>Ngày gửi</label>
                  <div className="acct-transfer-form__value">{formatDateVi(item.createDate)}</div>
                </div>
              </div>

              <div className="acct-transfer-form__row">
                <div className="acct-transfer-form__field">
                  <label>Phòng ban</label>
                  <div className="acct-transfer-form__value">{deptLabel}</div>
                </div>
                <div className="acct-transfer-form__field">
                  <label>Mã người gửi</label>
                  <div className="acct-transfer-form__value">User #{item.userId}</div>
                </div>
              </div>

              <div className="acct-transfer-form__section">
                <h3 className="acct-transfer-form__section-title">Tiêu đề yêu cầu</h3>
                <div className="acct-transfer-form__value">{item.title || '—'}</div>
              </div>

              {item.assetAllocationOrderId != null && (
                <div className="acct-transfer-form__section">
                  <h3 className="acct-transfer-form__section-title">Đơn liên quan</h3>
                  <Link
                    className="alloc-handover-inline-link"
                    to={`/allocations/${orderPath}/${item.assetAllocationOrderId}`}
                  >
                    Mở đơn {isAlloc ? 'cấp phát' : 'hoàn trả'} #{item.assetAllocationOrderId}
                  </Link>
                </div>
              )}

              {orderLoading ? (
                <div className="alloc-handover-spin-wrap">
                  <Spin />
                </div>
              ) : orderDetail ? (
                <div className="acct-transfer-form__section">
                  <h3 className="acct-transfer-form__section-title">Chi tiết đơn</h3>
                  <table className="alloc-handover-lines-table">
                    <thead>
                      <tr>
                        <th>Tài sản</th>
                        <th>Loại</th>
                        <th style={{ width: 88 }}>SL</th>
                        <th>Ghi chú</th>
                      </tr>
                    </thead>
                    <tbody>
                      {orderDetail.lines.map((l, idx) => (
                        <tr key={`${l.assetId}-${idx}`}>
                          <td>
                            <strong>{l.assetName}</strong>
                            <div className="alloc-handover-lines-table__muted">{l.assetCode}</div>
                          </td>
                          <td>{l.assetTypeName}</td>
                          <td>{l.quantity}</td>
                          <td className="alloc-handover-lines-table__muted">{l.reason?.trim() || '—'}</td>
                        </tr>
                      ))}
                    </tbody>
                  </table>
                </div>
              ) : proposedLines.length > 0 ? (
                <div className="acct-transfer-form__section">
                  <h3 className="acct-transfer-form__section-title">Dự kiến (theo yêu cầu)</h3>
                  {proposedLabelsLoading ? (
                    <div className="alloc-handover-spin-wrap">
                      <Spin />
                    </div>
                  ) : (
                    <table className="alloc-handover-lines-table">
                      <thead>
                        <tr>
                          <th>Tài sản</th>
                          <th>Loại</th>
                          <th style={{ width: 88 }}>SL</th>
                          <th>Ghi chú</th>
                        </tr>
                      </thead>
                      <tbody>
                        {proposedLines.map((l, idx) => {
                          const id = l.assetId;
                          const resolved = id != null && id > 0 ? proposedAssetLabels[id] : undefined;
                          const typeLabel =
                            resolved?.assetTypeName ?? (l.assetTypeId != null ? `Loại #${l.assetTypeId}` : '—');
                          const assetMain = resolved
                            ? resolved.name
                            : id != null && id > 0
                              ? `Tài sản #${id}`
                              : '—';
                          const assetCode = resolved?.code ?? null;
                          return (
                            <tr key={idx}>
                              <td>
                                <strong>{assetMain}</strong>
                                {assetCode ? (
                                  <div className="alloc-handover-lines-table__muted">{assetCode}</div>
                                ) : null}
                              </td>
                              <td>{typeLabel}</td>
                              <td>{l.quantity ?? '—'}</td>
                              <td className="alloc-handover-lines-table__muted">
                                {l.reason?.trim() || '—'}
                              </td>
                            </tr>
                          );
                        })}
                      </tbody>
                    </table>
                  )}
                </div>
              ) : (
                <div className="acct-transfer-form__section">
                  <div className="acct-transfer-form__value" style={{ color: '#6b7280' }}>
                    Không có dòng chi tiết trong yêu cầu.
                  </div>
                </div>
              )}
            </div>
          </div>

          <div className="acct-transfer-modal__footer">
            <button type="button" className="acct-transfer-btn-close" onClick={onClose}>
              Đóng
            </button>
            {canAct ? (
              <div className="acct-transfer-footer-actions">
                <button type="button" className="acct-transfer-btn-reject" onClick={() => openAction('reject')}>
                  Từ chối
                </button>
                <button type="button" className="acct-transfer-btn-approve" onClick={() => openAction('approve')}>
                  Duyệt
                </button>
              </div>
            ) : null}
          </div>
        </div>
      </div>

      {actionOpen ? (
        <div className="acct-transfer-approve-overlay" role="dialog" aria-modal="true">
          <div className="acct-transfer-approve-modal">
            <div className="acct-transfer-approve-modal__header">
              <h3 className="acct-transfer-approve-modal__title">
                {actionMode === 'approve' ? 'Duyệt yêu cầu' : 'Từ chối yêu cầu'}
              </h3>
            </div>

            <div className="acct-transfer-approve-modal__body">
              <div className="acct-transfer-approve-form">
                <div className="acct-transfer-approve-form__row">
                  <div className="acct-transfer-approve-form__field" style={{ gridColumn: '1 / -1' }}>
                    <label>Ghi chú (tùy chọn)</label>
                    <textarea
                      className="acct-transfer-approve-textarea"
                      rows={3}
                      value={comment}
                      onChange={(e) => setComment(e.target.value)}
                      placeholder="Nhập ghi chú nếu cần"
                    />
                  </div>
                </div>
              </div>
            </div>

            <div className="acct-transfer-approve-modal__footer">
              <button
                type="button"
                className="acct-transfer-approve-btn-back"
                disabled={submitting}
                onClick={() => setActionOpen(false)}
              >
                ← Quay lại
              </button>
              <button
                type="button"
                className={
                  actionMode === 'reject' ? 'acct-transfer-btn-reject' : 'acct-transfer-approve-btn-submit'
                }
                style={actionMode === 'reject' ? { height: 40 } : undefined}
                disabled={submitting}
                onClick={() => void submitAction()}
              >
                {submitting ? 'Đang xử lý…' : actionMode === 'approve' ? 'Duyệt' : 'Từ chối'}
              </button>
            </div>
          </div>
        </div>
      ) : null}
    </>
  );
}

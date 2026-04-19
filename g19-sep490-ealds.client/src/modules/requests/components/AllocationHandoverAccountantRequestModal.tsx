import { useCallback, useEffect, useState } from 'react';
import { Link } from 'react-router-dom';
import { Input, Modal, Spin, Typography, message } from 'antd';
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
import '../../purchase-orders/components/CreatePurchaseOrderModal.css';

const { Text, Title } = Typography;
const { TextArea } = Input;

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

function statusPillClass(color: string): string {
  if (color === 'success') return 'asset-status-pill asset-status-pill--active';
  if (color === 'error') return 'asset-status-pill asset-status-pill--danger';
  if (color === 'warning') return 'asset-status-pill asset-status-pill--warning';
  if (color === 'processing') return 'asset-status-pill asset-status-pill--processing';
  return 'asset-status-pill';
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
  /** When đơn chưa có, map assetId → labels for proposed lines. */
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

  if (!item) return null;

  const st = ALLOC_STATUS[item.status] ?? { label: `Trạng thái ${item.status}`, color: 'default' };
  const proposedLines = parseProposedLines(item.proposedData);
  const deptLabel = item.targetDepartmentName?.trim() || '—';

  return (
    <>
      {open ? (
        <div className="create-purchase-modal-overlay" role="dialog" aria-modal="true">
          <div className="create-purchase-modal">
            <button
              type="button"
              className="create-purchase-modal__close-btn"
              onClick={onClose}
              aria-label="Đóng"
            >
              <span className="create-purchase-modal__close">×</span>
            </button>

            <div className="create-purchase-modal__header">
              <h2 className="create-purchase-modal__title">Yêu cầu YC-{item.assetRequestId}</h2>
            </div>

            <div className="create-purchase-modal__body">
              <div className="create-purchase-form">
                <div className="create-purchase-form__section" style={{ marginBottom: 16 }}>
                  <Text type="secondary">Gửi {item.createDate?.slice(0, 10)}</Text>
                  <Title level={5} style={{ marginTop: 8, marginBottom: 0 }}>
                    {item.title}
                  </Title>
                </div>

                <div className="create-purchase-form__row create-purchase-form__row--single">
                  <div className="create-purchase-form__item" style={{ marginBottom: 12 }}>
                    <Text strong>Phòng ban: </Text>
                    {deptLabel}
                  </div>
                </div>

                <div style={{ marginBottom: 16 }}>
                  <span className={statusPillClass(st.color)}>{st.label}</span>
                </div>

                {item.assetAllocationOrderId != null && (
                  <div style={{ marginBottom: 16 }}>
                    <Link to={`/allocations/${orderPath}/${item.assetAllocationOrderId}`}>
                      Mở đơn {isAlloc ? 'cấp phát' : 'hoàn trả'} #{item.assetAllocationOrderId}
                    </Link>
                  </div>
                )}

                {orderLoading ? (
                  <Spin />
                ) : orderDetail ? (
                  <div className="create-purchase-form__section">
                    <h3 className="create-purchase-form__section-title">Chi tiết đơn</h3>
                    <ul style={{ paddingLeft: 18, margin: '8px 0 0' }}>
                      {orderDetail.lines.map((l, idx) => (
                        <li key={`${l.assetId}-${idx}`} style={{ marginBottom: 6 }}>
                          <strong>{l.assetName}</strong> ({l.assetCode}) — {l.assetTypeName}
                          <br />
                          <Text type="secondary">
                            SL: {l.quantity}
                            {l.reason ? ` · ${l.reason}` : ''}
                          </Text>
                        </li>
                      ))}
                    </ul>
                  </div>
                ) : proposedLines.length > 0 ? (
                  <div className="create-purchase-form__section">
                    <h3 className="create-purchase-form__section-title">Dự kiến (theo yêu cầu)</h3>
                    {proposedLabelsLoading ? (
                      <Spin style={{ marginTop: 8 }} />
                    ) : (
                      <ul style={{ paddingLeft: 18, margin: '8px 0 0' }}>
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
                            <li key={idx} style={{ marginBottom: 6 }}>
                              <strong>{assetMain}</strong>
                              {assetCode ? ` (${assetCode})` : ''} — {typeLabel}
                              <br />
                              <Text type="secondary">
                                SL: {l.quantity ?? '—'}
                                {l.reason ? ` · ${l.reason}` : ''}
                              </Text>
                            </li>
                          );
                        })}
                      </ul>
                    )}
                  </div>
                ) : (
                  <Text type="secondary">Không có dòng chi tiết trong yêu cầu.</Text>
                )}
              </div>
            </div>

            <div className="create-purchase-modal__footer">
              {item.status === 0 && userId != null && item.requestTypeId === typeId && (
                <>
                  <button
                    type="button"
                    className="create-purchase-btn-submit"
                    onClick={() => openAction('approve')}
                  >
                    Duyệt
                  </button>
                  <button
                    type="button"
                    className="create-purchase-btn-cancel"
                    style={{
                      borderColor: '#ff4d4f',
                      color: '#dc2626',
                      background: '#fff',
                    }}
                    onClick={() => openAction('reject')}
                  >
                    Từ chối
                  </button>
                </>
              )}
              <button type="button" className="create-purchase-btn-cancel" onClick={onClose}>
                Đóng
              </button>
            </div>
          </div>
        </div>
      ) : null}

      <Modal
        title={actionMode === 'approve' ? 'Duyệt yêu cầu' : 'Từ chối yêu cầu'}
        open={actionOpen}
        onCancel={() => setActionOpen(false)}
        onOk={() => void submitAction()}
        okText={actionMode === 'approve' ? 'Duyệt' : 'Từ chối'}
        okButtonProps={{ danger: actionMode === 'reject', loading: submitting }}
        confirmLoading={submitting}
        destroyOnClose
      >
        <p style={{ marginBottom: 8 }}>Ghi chú (tùy chọn)</p>
        <TextArea rows={3} value={comment} onChange={(e) => setComment(e.target.value)} />
      </Modal>
    </>
  );
}

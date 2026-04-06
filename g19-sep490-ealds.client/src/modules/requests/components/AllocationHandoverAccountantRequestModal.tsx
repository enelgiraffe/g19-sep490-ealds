import { useCallback, useEffect, useState } from 'react';
import { Link } from 'react-router-dom';
import { Button, Input, Modal, Space, Spin, Typography, message } from 'antd';
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

  const isAlloc = variant === 'allocation';
  const typeId = isAlloc ? ALLOCATION_REQUEST_TYPE_ID : HANDOVER_REQUEST_TYPE_ID;
  const orderPath = isAlloc ? 'order' : 'handover-order';

  const loadOrder = useCallback(async () => {
    if (!item?.assetAllocationOrderId) {
      setOrderDetail(null);
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
      return;
    }
    void loadOrder();
  }, [open, item, loadOrder]);

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
      <Modal
        title={`Yêu cầu YC-${item.assetRequestId}`}
        open={open}
        onCancel={onClose}
        width={720}
        footer={
          <Space>
            {item.status === 0 && userId != null && item.requestTypeId === typeId && (
              <>
                <Button type="primary" onClick={() => openAction('approve')}>
                  Duyệt
                </Button>
                <Button danger onClick={() => openAction('reject')}>
                  Từ chối
                </Button>
              </>
            )}
            <Button onClick={onClose}>Đóng</Button>
          </Space>
        }
        destroyOnClose
      >
        <div style={{ marginBottom: 12 }}>
          <Text type="secondary">
            Gửi {item.createDate?.slice(0, 10)} · Loại #{item.requestTypeId}
          </Text>
        </div>
        <Title level={5} style={{ marginTop: 0 }}>
          {item.title}
        </Title>
        <div style={{ marginBottom: 12 }}>
          <Text strong>Phòng ban: </Text>
          {deptLabel}
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
          <>
            <Title level={5}>Chi tiết đơn</Title>
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
          </>
        ) : proposedLines.length > 0 ? (
          <>
            <Title level={5}>Dự kiến (theo yêu cầu)</Title>
            <ul style={{ paddingLeft: 18, margin: '8px 0 0' }}>
              {proposedLines.map((l, idx) => (
                <li key={idx} style={{ marginBottom: 4 }}>
                  <Text type="secondary">
                    Loại #{l.assetTypeId ?? '—'} · Tài sản #{l.assetId ?? '—'} · SL: {l.quantity ?? '—'}
                    {l.reason ? ` · ${l.reason}` : ''}
                  </Text>
                </li>
              ))}
            </ul>
          </>
        ) : (
          <Text type="secondary">Không có dòng chi tiết trong yêu cầu.</Text>
        )}
      </Modal>

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

import { useEffect, useState, useCallback, useMemo, type Key } from 'react';
import { useParams, useNavigate } from 'react-router-dom';
import { Button, Spin, Table, Tag, message, Modal, Input } from 'antd';
import type { ColumnsType } from 'antd/es/table';
import type { TableRowSelection } from 'antd/es/table/interface';
import { ArrowLeftOutlined, SyncOutlined, CheckCircleOutlined } from '@ant-design/icons';
import {
  inventoryService,
  getCurrentUserId,
  SESSION_STATUS,
  type InventoryDiscrepancyDetail,
  type InventoryReviewSummary,
} from '../services/inventoryService';
import { getStatusLabel } from '../../assets/services/assetService';
import { useAppStore } from '../../../stores/appStore';
import './InventoryReviewPage.css';

function formatReviewBookUseLine(bookCondition: string | undefined): string {
  const s = bookCondition?.trim() ?? '';
  return s ? getStatusLabel(s) : '—';
}

function formatReviewActualUseLine(
  actualQuantity: number | null | undefined,
  actualCondition: string | undefined,
): string {
  const cond = actualCondition?.trim();
  if (cond) return getStatusLabel(cond);
  if (actualQuantity === null || actualQuantity === undefined) return '—';
  if (actualQuantity === 1) return 'Đang sử dụng';
  return '—';
}

function formatDate(dateStr: string | null | undefined): string {
  if (!dateStr) return '-';
  const d = new Date(dateStr);
  if (Number.isNaN(d.getTime())) return '-';
  return `${String(d.getDate()).padStart(2, '0')}/${String(d.getMonth() + 1).padStart(2, '0')}/${d.getFullYear()}`;
}

const STATUS_COLOR: Record<number, string> = {
  0: 'blue',
  1: 'processing',
  2: 'warning',
  3: 'error',
  4: 'success',
  5: 'orange',
  6: 'purple',
};

export function InventoryReviewPage() {
  const { sessionId } = useParams<{ sessionId: string }>();
  const navigate = useNavigate();
  const id = Number(sessionId);
  const currentRole = useAppStore((s) => s.currentRole);

  const [loading, setLoading] = useState(true);
  const [summary, setSummary] = useState<InventoryReviewSummary | null>(null);
  const [selectedRowKeys, setSelectedRowKeys] = useState<Key[]>([]);
  const [batchResolving, setBatchResolving] = useState(false);
  const [finishModalOpen, setFinishModalOpen] = useState(false);
  const [finishNotes, setFinishNotes] = useState('');
  const [finishSubmitting, setFinishSubmitting] = useState(false);

  const load = useCallback(async () => {
    if (!id) return;
    setLoading(true);
    try {
      const data = await inventoryService.getReviewSummary(id);
      setSummary(data);
      setSelectedRowKeys([]);
    } catch {
      message.error('Không thể tải báo cáo kiểm kê.');
      setSummary(null);
    } finally {
      setLoading(false);
    }
  }, [id]);

  useEffect(() => {
    load();
  }, [load]);

  const canResolveOnBook =
    currentRole === 'department_head' || currentRole === 'admin';

  const showResolveUi =
    summary?.status === SESSION_STATUS.PendingAccountant && canResolveOnBook;

  const columns: ColumnsType<InventoryDiscrepancyDetail> = useMemo(
    () => [
      { title: 'Mã tài sản', dataIndex: 'assetCode', key: 'assetCode', width: 100 },
      { title: 'Mã cá thể', dataIndex: 'instanceCode', key: 'instanceCode', width: 110 },
      { title: 'Tên tài sản', dataIndex: 'assetName', key: 'assetName', ellipsis: true },
      {
        title: 'Sổ sách',
        key: 'book',
        render: (_, r) => (
          <div className="inv-review__cell-stack">
            <span>{formatReviewBookUseLine(r.bookCondition)}</span>
            <span>{r.bookDepartmentName ?? '—'}</span>
            <span className="inv-review__muted">{r.bookUserName ?? '—'}</span>
          </div>
        ),
      },
      {
        title: 'Thực tế',
        key: 'actual',
        render: (_, r) => (
          <div className="inv-review__cell-stack">
            <span>{formatReviewActualUseLine(r.actualQuantity, r.actualCondition)}</span>
            <span>{r.actualDepartmentName ?? '—'}</span>
            <span className="inv-review__muted">{r.actualUserName ?? '—'}</span>
          </div>
        ),
      },
      ...(showResolveUi
        ? [
            {
              title: 'Trạng thái',
              key: 'resolveStatus',
              width: 110,
              render: (_: unknown, r: InventoryDiscrepancyDetail) =>
                r.resolvedAt ? (
                  <Tag color="success" className="inv-review__resolved-tag">
                    Đã xử lý
                  </Tag>
                ) : (
                  <span className="inv-review__muted">Chưa cập nhật sổ</span>
                ),
            } as ColumnsType<InventoryDiscrepancyDetail>[number],
          ]
        : []),
    ],
    [showResolveUi],
  );

  const rowSelection: TableRowSelection<InventoryDiscrepancyDetail> | undefined = useMemo(
    () =>
      showResolveUi
        ? {
            selectedRowKeys,
            preserveSelectedRowKeys: true,
            onChange: (keys) => setSelectedRowKeys(keys),
            getCheckboxProps: (record) => ({
              disabled: !!record.resolvedAt,
            }),
          }
        : undefined,
    [showResolveUi, selectedRowKeys],
  );

  const handleBatchApplyActual = useCallback(async () => {
    if (!id || selectedRowKeys.length === 0) return;
    setBatchResolving(true);
    let ok = 0;
    let fail = 0;
    try {
      for (const key of selectedRowKeys) {
        const discrepancyId = Number(key);
        try {
          await inventoryService.applyDiscrepancyActual(id, discrepancyId);
          ok += 1;
        } catch {
          fail += 1;
        }
      }
      if (fail === 0) {
        message.success(ok === 1 ? 'Đã cập nhật sổ theo thực tế.' : `Đã cập nhật sổ cho ${ok} dòng.`);
      } else if (ok > 0) {
        message.warning(`Thành công ${ok} dòng, thất bại ${fail} dòng.`);
      } else {
        message.error('Không thể cập nhật sổ. Vui lòng thử lại.');
      }
      setSelectedRowKeys([]);
      await load();
    } finally {
      setBatchResolving(false);
    }
  }, [id, selectedRowKeys, load]);

  if (loading) {
    return (
      <div className="inv-review inv-review--center">
        <Spin size="large" />
      </div>
    );
  }

  if (!summary) {
    return (
      <div className="inv-review inv-review--center">
        <Button type="primary" icon={<ArrowLeftOutlined />} onClick={() => navigate(-1)}>
          Quay lại
        </Button>
      </div>
    );
  }

  const awaitingDirector = summary.status === SESSION_STATUS.Completed;
  const awaitingHeadResolution = summary.status === SESSION_STATUS.PendingAccountant;
  const unresolvedDiscrepancyCount = summary.discrepancies.filter((d) => !d.resolvedAt).length;
  const canFinishSession =
    awaitingHeadResolution &&
    canResolveOnBook &&
    unresolvedDiscrepancyCount === 0;

  const handleFinishResolution = async () => {
    if (!id) return;
    setFinishSubmitting(true);
    try {
      const res = await inventoryService.confirmSession(id, {
        reviewedBy: getCurrentUserId(),
        reviewerRoleId: currentRole === 'admin' ? 1 : 4,
        reviewNotes: finishNotes.trim() || undefined,
        applyCorrections: false,
      });
      message.success(
        (res as { message?: string })?.message ?? 'Phiên kiểm kê đã được đánh dấu Đã xử lý.',
      );
      setFinishModalOpen(false);
      setFinishNotes('');
      await load();
    } catch (err: unknown) {
      const axiosErr = err as { response?: { data?: { message?: string } } };
      message.error(axiosErr?.response?.data?.message ?? 'Thao tác thất bại. Vui lòng thử lại.');
    } finally {
      setFinishSubmitting(false);
    }
  };

  return (
    <div className="inv-review">
      <div className="inv-review__header">
        <div className="inv-review__header-main">
          <Button type="text" icon={<ArrowLeftOutlined />} onClick={() => navigate(-1)} className="inv-review__back">
            Quay lại
          </Button>
          <h1 className="inv-review__title">Báo cáo kiểm kê</h1>
        </div>
        {canFinishSession && (
          <div className="inv-review__header-actions">
            <Button
              type="primary"
              size="large"
              icon={<CheckCircleOutlined />}
              onClick={() => setFinishModalOpen(true)}
            >
              Hoàn tất
            </Button>
          </div>
        )}
      </div>

      <div className="inv-review__meta">
        <div className="inv-review__meta-row">
          <span className="inv-review__code">{summary.code}</span>
          <Tag color={STATUS_COLOR[summary.status] ?? 'default'}>{summary.statusName}</Tag>
        </div>
        <p className="inv-review__purpose">{summary.purpose}</p>
        <div className="inv-review__grid">
          <div><span className="inv-review__label">Phòng ban</span>{summary.departmentName}</div>
          <div><span className="inv-review__label">Thời gian</span>{formatDate(summary.startDate)} — {formatDate(summary.endDate)}</div>
        </div>
      </div>

      <div className="inv-review__table-wrap">
        <h2 className="inv-review__section-title">Chi tiết chênh lệch</h2>
        <Table<InventoryDiscrepancyDetail>
          rowKey="discrepancyId"
          columns={columns}
          dataSource={summary.discrepancies}
          rowSelection={rowSelection}
          pagination={{ pageSize: 10, showSizeChanger: true }}
          size="small"
          locale={{ emptyText: 'Không có chênh lệch ghi nhận.' }}
        />
        {showResolveUi && (
          <div className="inv-review__table-footer">
            <Button
              type="primary"
              size="large"
              icon={<SyncOutlined />}
              loading={batchResolving}
              disabled={selectedRowKeys.length === 0}
              onClick={handleBatchApplyActual}
            >
              Cập nhật sổ đã chọn
            </Button>
          </div>
        )}
      </div>

      <Modal
        title="Hoàn tất xử lý chênh lệch"
        open={finishModalOpen}
        okText="Xác nhận"
        cancelText="Hủy"
        confirmLoading={finishSubmitting}
        onOk={handleFinishResolution}
        onCancel={() => {
          setFinishModalOpen(false);
          setFinishNotes('');
        }}
        centered
        width={440}
      >
        <p style={{ marginBottom: 12 }}>
          Phiên sẽ được đánh dấu <strong>Đã xử lý</strong> và kết thúc quy trình kiểm kê. Giám đốc không cần xác nhận;
          có thể xem kết quả trong mục báo cáo nếu cần.
        </p>
        <label htmlFor="inv-finish-notes" style={{ display: 'block', marginBottom: 6, fontWeight: 500 }}>
          Ghi chú (tuỳ chọn)
        </label>
        <Input.TextArea
          id="inv-finish-notes"
          rows={3}
          value={finishNotes}
          onChange={(e) => setFinishNotes(e.target.value)}
          placeholder="Ghi chú gửi kèm (nếu có)…"
        />
      </Modal>
    </div>
  );
}

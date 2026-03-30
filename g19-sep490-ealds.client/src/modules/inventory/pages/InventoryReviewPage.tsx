import { useEffect, useState, useCallback } from 'react';
import { useParams, useNavigate } from 'react-router-dom';
import { Button, Spin, Table, Tag, message } from 'antd';
import type { ColumnsType } from 'antd/es/table';
import { ArrowLeftOutlined } from '@ant-design/icons';
import {
  inventoryService,
  SESSION_STATUS,
  type InventoryDiscrepancyDetail,
  type InventoryReviewSummary,
} from '../services/inventoryService';
import { getStatusLabel } from '../../assets/services/assetService';
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

  const [loading, setLoading] = useState(true);
  const [summary, setSummary] = useState<InventoryReviewSummary | null>(null);

  const load = useCallback(async () => {
    if (!id) return;
    setLoading(true);
    try {
      const data = await inventoryService.getReviewSummary(id);
      setSummary(data);
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

  const columns: ColumnsType<InventoryDiscrepancyDetail> = [
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
  ];

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
  const awaitingAccountant = summary.status === SESSION_STATUS.PendingAccountant;

  return (
    <div className="inv-review">
      <div className="inv-review__header">
        <Button type="text" icon={<ArrowLeftOutlined />} onClick={() => navigate(-1)} className="inv-review__back">
          Quay lại
        </Button>
        <h1 className="inv-review__title">Báo cáo kiểm kê</h1>
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

      {awaitingDirector && (
        <p className="inv-review__hint">
          Phiên đang chờ giám đốc xác nhận. Sau khi xác nhận, nếu có chênh lệch số lượng hoặc người phụ trách
          so với sổ, phiên chuyển sang Chờ xử lý (kế toán); nếu không, phiên được đánh dấu Đã xử lý.
        </p>
      )}
      {awaitingAccountant && (
        <p className="inv-review__hint">
          Phiên đang chờ kế toán xử lý chênh lệch trên sổ sách. Sau khi xử lý xong, kế toán có thể đánh dấu trạng thái Đã xử lý.
        </p>
      )}

      <div className="inv-review__table-wrap">
        <h2 className="inv-review__section-title">Chi tiết chênh lệch</h2>
        <Table<InventoryDiscrepancyDetail>
          rowKey="discrepancyId"
          columns={columns}
          dataSource={summary.discrepancies}
          pagination={{ pageSize: 10, showSizeChanger: true }}
          size="small"
          locale={{ emptyText: 'Không có chênh lệch ghi nhận.' }}
        />
      </div>
    </div>
  );
}

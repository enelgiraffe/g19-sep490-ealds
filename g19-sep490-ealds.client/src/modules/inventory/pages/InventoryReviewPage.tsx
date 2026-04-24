import { useEffect, useState, useCallback, useMemo, type Key } from 'react';
import { useParams, useNavigate } from 'react-router-dom';
import { Button, Spin, Table, Tag, message } from 'antd';
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
import '../../maintenance/pages/MaintenancePage.css';
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
  const [finishSubmitting, setFinishSubmitting] = useState(false);
  const [pageSize, setPageSize] = useState(25);
  const [currentPage, setCurrentPage] = useState(1);

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

  useEffect(() => {
    setCurrentPage(1);
  }, [sessionId]);

  const canResolveOnBook =
    currentRole === 'department_head' ||
    currentRole === 'accountant' ||
    currentRole === 'admin';

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

  const discrepancyList = summary?.discrepancies;
  const totalFiltered = discrepancyList?.length ?? 0;
  const totalPages = Math.max(1, Math.ceil(totalFiltered / pageSize));

  useEffect(() => {
    setCurrentPage((p) => Math.min(p, totalPages));
  }, [totalPages]);

  const safePage = Math.min(currentPage, totalPages);
  const rangeStart = totalFiltered === 0 ? 0 : (safePage - 1) * pageSize + 1;
  const rangeEnd = Math.min(safePage * pageSize, totalFiltered);
  const paginatedDiscrepancies = useMemo(() => {
    const list = discrepancyList ?? [];
    return list.slice((safePage - 1) * pageSize, safePage * pageSize);
  }, [discrepancyList, safePage, pageSize]);

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
      <div className="maintenance-page inv-review">
        <div className="maintenance-card inv-review__loading-card">
          <div className="inv-review__loading-inner">
            <Spin size="large" />
          </div>
        </div>
      </div>
    );
  }

  if (!summary) {
    return (
      <div className="maintenance-page inv-review">
        <div className="maintenance-card inv-review__loading-card">
          <div className="inv-review__loading-inner">
            <Button
              type="primary"
              className="maintenance-btn-add"
              icon={<ArrowLeftOutlined />}
              onClick={() => navigate(-1)}
            >
              Quay lại
            </Button>
          </div>
        </div>
      </div>
    );
  }

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
        reviewerRoleId:
          currentRole === 'admin' ? 1 : currentRole === 'accountant' ? 3 : 4,
        applyCorrections: false,
      });
      message.success(
        (res as { message?: string })?.message ?? 'Phiên kiểm kê đã được đánh dấu Đã xử lý.',
      );
      await load();
    } catch (err: unknown) {
      const axiosErr = err as { response?: { data?: { message?: string } } };
      message.error(axiosErr?.response?.data?.message ?? 'Thao tác thất bại. Vui lòng thử lại.');
    } finally {
      setFinishSubmitting(false);
    }
  };

  return (
    <div className="maintenance-page inv-review">
      <div className="maintenance-header inv-review__page-header">
        <div className="inv-review__header-main">
          <Button type="text" icon={<ArrowLeftOutlined />} onClick={() => navigate(-1)} className="inv-review__back">
            Quay lại
          </Button>
          <h1 className="maintenance-page__title inv-review__title">Báo cáo kiểm kê</h1>
        </div>
        {canFinishSession && (
          <div className="inv-review__header-actions">
            <Button
              type="primary"
              className="maintenance-btn-add"
              icon={<CheckCircleOutlined />}
              loading={finishSubmitting}
              onClick={() => void handleFinishResolution()}
            >
              Hoàn tất
            </Button>
          </div>
        )}
      </div>

      <div className="maintenance-card inv-review__summary-card">
        <div className="inv-review__meta-row">
          <span className="inv-review__code">{summary.code}</span>
          <Tag color={STATUS_COLOR[summary.status] ?? 'default'} className="inv-review__status-tag">
            {summary.statusName}
          </Tag>
        </div>
        <p className="inv-review__purpose">{summary.purpose}</p>
        <div className="inv-review__grid">
          <div>
            <span className="inv-review__label">Phòng ban</span>
            {summary.departmentName}
          </div>
          <div>
            <span className="inv-review__label">Thời gian</span>
            {formatDate(summary.startDate)} — {formatDate(summary.endDate)}
          </div>
        </div>
      </div>

      <div className="maintenance-card inv-review__table-card">
        <h2 className="inv-review__section-title">Chi tiết chênh lệch</h2>
        <div className="inv-review-table-container">
          <Table<InventoryDiscrepancyDetail>
            rowKey="discrepancyId"
            columns={columns}
            dataSource={paginatedDiscrepancies}
            rowSelection={rowSelection}
            pagination={false}
            size="small"
            scroll={{ x: 'max-content' }}
            locale={{ emptyText: 'Không có chênh lệch ghi nhận.' }}
          />
        </div>
        <div className="maintenance-card__footer inv-review__pagination-footer">
          <div className="maintenance-footer__left">
            Số lượng trên trang:
            <select
              className="maintenance-footer__select"
              value={pageSize}
              onChange={(e) => {
                setPageSize(Number(e.target.value));
                setCurrentPage(1);
              }}
            >
              <option value={10}>10</option>
              <option value={25}>25</option>
              <option value={50}>50</option>
              <option value={100}>100</option>
            </select>
          </div>
          <div className="maintenance-footer__center">
            {totalFiltered === 0 ? '0 trên 0' : `${rangeStart}-${rangeEnd} trên ${totalFiltered}`}
          </div>
          <div className="maintenance-footer__right">
            <button
              type="button"
              className="maintenance-footer__pager"
              disabled={safePage <= 1}
              onClick={() => setCurrentPage((p) => Math.max(1, p - 1))}
              aria-label="Trang trước"
            >
              ⟨
            </button>
            <button
              type="button"
              className="maintenance-footer__pager maintenance-footer__pager--active"
              tabIndex={-1}
              aria-current="page"
            >
              {safePage}
            </button>
            <button
              type="button"
              className="maintenance-footer__pager"
              disabled={safePage >= totalPages}
              onClick={() => setCurrentPage((p) => Math.min(totalPages, p + 1))}
              aria-label="Trang sau"
            >
              ⟩
            </button>
          </div>
        </div>
        {showResolveUi && (
          <div className="inv-review__table-footer">
            <Button
              type="primary"
              className="maintenance-btn-add"
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
    </div>
  );
}

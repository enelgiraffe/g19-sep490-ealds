import { useCallback, useEffect, useMemo, useState } from 'react';
import { Link } from 'react-router-dom';
import { Button, message } from 'antd';
import { ReloadOutlined } from '@ant-design/icons';
import {
  allocationRequestService,
  type AllocationOrderSummaryRow,
} from '../services/allocationRequestService';
import { handoverRequestService } from '../services/handoverRequestService';
import '../../requests/pages/RequestsPage.css';
import '../../maintenance/pages/MaintenancePage.css';

function formatDate(iso: string): string {
  try {
    return new Date(iso).toLocaleDateString('vi-VN');
  } catch {
    return iso;
  }
}

const ORDER_STATUS_LABEL: Record<string, { label: string; color: string }> = {
  confirmed: { label: 'Đã xác nhận', color: 'success' },
  awaiting_confirm: { label: 'Chờ phòng ban', color: 'processing' },
};

function pillClass(color: string): string {
  if (color === 'success') return 'asset-status-pill asset-status-pill--active';
  if (color === 'processing') return 'asset-status-pill asset-status-pill--processing';
  return 'asset-status-pill';
}

export function AllocationOrdersPanel({ kind }: { kind: 'allocation' | 'handover' }) {
  const [rows, setRows] = useState<AllocationOrderSummaryRow[]>([]);
  const [loading, setLoading] = useState(false);
  const [page, setPage] = useState(1);
  const [pageSize, setPageSize] = useState(25);

  const load = useCallback(async () => {
    setLoading(true);
    try {
      const list =
        kind === 'handover'
          ? await handoverRequestService.listOrdersSummary()
          : await allocationRequestService.listOrdersSummary();
      setRows(list);
    } catch {
      message.error('Không tải được danh sách đơn.');
      setRows([]);
    } finally {
      setLoading(false);
    }
  }, [kind]);

  useEffect(() => {
    void load();
  }, [load]);

  const total = rows.length;
  const totalPages = Math.max(1, Math.ceil(total / pageSize));
  const safePage = Math.min(page, totalPages);
  const startIndex = total === 0 ? 0 : (safePage - 1) * pageSize + 1;
  const endIndex = Math.min(safePage * pageSize, total);
  const pagedRows = useMemo(
    () => rows.slice((safePage - 1) * pageSize, safePage * pageSize),
    [rows, safePage, pageSize],
  );

  const orderPath = kind === 'handover' ? 'handover-order' : 'order';
  const deptHeader = kind === 'handover' ? 'PHÒNG BAN TRẢ' : 'PHÒNG BAN NHẬN';

  return (
    <>
      <div className="requests-filters" style={{ marginBottom: 0 }}>
        <Button icon={<ReloadOutlined />} onClick={() => void load()}>
          Làm mới
        </Button>
      </div>

      <div
        className="asset-table-wrapper maintenance-table-wrapper requests-table-wrapper"
        style={{ marginTop: 12 }}
      >
        {loading ? (
          <div className="requests-table-loading">Đang tải danh sách đơn...</div>
        ) : (
          <table className="asset-table requests-table">
            <thead>
              <tr>
                <th>MÃ ĐƠN</th>
                <th>MÃ YÊU CẦU</th>
                <th>TIÊU ĐỀ</th>
                <th>{deptHeader}</th>
                <th>TRẠNG THÁI ĐƠN</th>
                <th>NGÀY TẠO</th>
                <th>XÁC NHẬN</th>
                <th className="asset-table__cell asset-table__cell--actions" />
              </tr>
            </thead>
            <tbody>
              {pagedRows.length === 0 ? (
                <tr>
                  <td colSpan={8} className="requests-table-empty">
                    Không có dữ liệu.
                  </td>
                </tr>
              ) : (
                pagedRows.map((row) => {
                  const st =
                    ORDER_STATUS_LABEL[row.orderStatus] ?? ORDER_STATUS_LABEL.awaiting_confirm;
                  return (
                    <tr key={row.assetAllocationOrderId} className="asset-row">
                      <td>ĐH-{row.assetAllocationOrderId}</td>
                      <td>YC-{row.assetRequestId}</td>
                      <td>{row.title}</td>
                      <td>{row.departmentName}</td>
                      <td>
                        <span className={pillClass(st.color)}>{st.label}</span>
                      </td>
                      <td>{formatDate(row.createdAt)}</td>
                      <td>{row.confirmedAt ? formatDate(row.confirmedAt) : '—'}</td>
                      <td className="asset-table__cell asset-table__cell--actions">
                        <Link
                          className="asset-code asset-code--link"
                          to={`/allocations/${orderPath}/${row.assetAllocationOrderId}`}
                        >
                          Chi tiết
                        </Link>
                      </td>
                    </tr>
                  );
                })
              )}
            </tbody>
          </table>
        )}
      </div>

      <div className="maintenance-card__footer">
        <div className="maintenance-footer__left">
          Số lượng trên trang:
          <select
            className="maintenance-footer__select"
            value={pageSize}
            onChange={(e) => {
              setPageSize(Number(e.target.value));
              setPage(1);
            }}
          >
            <option value={10}>10</option>
            <option value={25}>25</option>
            <option value={50}>50</option>
            <option value={100}>100</option>
          </select>
        </div>
        <div className="maintenance-footer__center">
          {total === 0 ? '0-0 trên 0' : `${startIndex}-${endIndex} trên ${total}`}
        </div>
        <div className="maintenance-footer__right">
          <button
            className="maintenance-footer__pager"
            disabled={safePage <= 1}
            onClick={() => setPage((p) => Math.max(1, p - 1))}
            type="button"
          >
            ⟨
          </button>
          <button
            className="maintenance-footer__pager maintenance-footer__pager--active"
            type="button"
          >
            {safePage}
          </button>
          <button
            className="maintenance-footer__pager"
            disabled={safePage >= totalPages}
            onClick={() => setPage((p) => Math.min(totalPages, p + 1))}
            type="button"
          >
            ⟩
          </button>
        </div>
      </div>
    </>
  );
}

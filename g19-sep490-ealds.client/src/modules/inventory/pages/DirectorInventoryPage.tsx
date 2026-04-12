import { useState, useEffect, useCallback, useMemo } from 'react';
import { useNavigate } from 'react-router-dom';
import { Input, Select, Tag, Spin, message } from 'antd';
import { SearchOutlined } from '@ant-design/icons';
import {
  inventoryService,
  SESSION_STATUS,
  SESSION_STATUS_LABEL,
  type DropdownItem,
  type InventorySessionListItem,
} from '../services/inventoryService';
import '../../maintenance/pages/MaintenancePage.css';
import './InventoryPage.css';

const { Option } = Select;

/** Rows giám đốc mở được báo cáo chi tiết (theo trạng thái hiển thị API). */
const DIRECTOR_REVIEWABLE_STATUSES = new Set<number>([
  SESSION_STATUS.Completed,
  SESSION_STATUS.PendingAccountant,
  SESSION_STATUS.Confirmed,
]);

const DIRECTOR_STATUS_FILTER: { value: number; label: string }[] = [
  { value: SESSION_STATUS.PendingAccountant, label: SESSION_STATUS_LABEL[SESSION_STATUS.PendingAccountant] },
  { value: SESSION_STATUS.Confirmed, label: SESSION_STATUS_LABEL[SESSION_STATUS.Confirmed] },
];

const STATUS_COLOR: Record<number, string> = {
  0: 'blue',
  1: 'processing',
  2: 'warning',
  3: 'error',
  4: 'success',
  5: 'orange',
  6: 'purple',
};

function inventoryProgressFillColor(percent: number | null | undefined): string {
  const p = Math.max(0, Math.min(100, Number(percent) || 0));
  if (p === 0) return '#bfbfbf';
  const hue = (p / 100) * 120;
  return `hsl(${hue}, 72%, 44%)`;
}

function formatDate(dateStr: string | null | undefined): string {
  if (!dateStr) return '-';
  const d = new Date(dateStr);
  if (Number.isNaN(d.getTime())) return '-';
  return `${String(d.getDate()).padStart(2, '0')}/${String(d.getMonth() + 1).padStart(2, '0')}/${d.getFullYear()}`;
}

function execDurationLabel(startStr: string | null | undefined, endStr: string | null | undefined): string {
  if (!startStr || !endStr) return '-';
  const start = new Date(startStr);
  const end = new Date(endStr);
  if (Number.isNaN(start.getTime()) || Number.isNaN(end.getTime())) return '-';
  const su = Date.UTC(start.getUTCFullYear(), start.getUTCMonth(), start.getUTCDate());
  const eu = Date.UTC(end.getUTCFullYear(), end.getUTCMonth(), end.getUTCDate());
  const days = Math.max(1, Math.round((eu - su) / 86_400_000));
  return `${days} ngày`;
}

export function DirectorInventoryPage() {
  const navigate = useNavigate();
  const [sessions, setSessions] = useState<InventorySessionListItem[]>([]);
  const [loading, setLoading] = useState(false);

  const [searchText, setSearchText] = useState('');
  const [departmentFilter, setDepartmentFilter] = useState<number | undefined>(undefined);
  const [departmentOptions, setDepartmentOptions] = useState<DropdownItem[]>([]);
  const [statusFilter, setStatusFilter] = useState<number | undefined>(undefined);

  const [pageSize, setPageSize] = useState(25);
  const [currentPage, setCurrentPage] = useState(1);

  const fetchSessions = useCallback(async () => {
    setLoading(true);
    try {
      const data = await inventoryService.getSessions({
        keyword: searchText || undefined,
        status: statusFilter,
        departmentId: departmentFilter,
        directorInventoryReport: true,
      });
      setSessions(data);
    } catch {
      message.error('Không thể tải danh sách kiểm kê.');
    } finally {
      setLoading(false);
    }
  }, [searchText, statusFilter, departmentFilter]);

  useEffect(() => {
    void inventoryService
      .getDepartments()
      .then(setDepartmentOptions)
      .catch(() => setDepartmentOptions([]));
  }, []);

  useEffect(() => {
    fetchSessions();
  }, [fetchSessions]);

  useEffect(() => {
    setCurrentPage(1);
  }, [searchText, statusFilter, departmentFilter]);

  const totalFiltered = sessions.length;
  const totalPages = Math.max(1, Math.ceil(totalFiltered / pageSize));

  useEffect(() => {
    setCurrentPage((p) => Math.min(p, totalPages));
  }, [totalPages]);

  const safePage = Math.min(currentPage, totalPages);
  const rangeStart = totalFiltered === 0 ? 0 : (safePage - 1) * pageSize + 1;
  const rangeEnd = Math.min(safePage * pageSize, totalFiltered);
  const paginatedSessions = useMemo(
    () => sessions.slice((safePage - 1) * pageSize, safePage * pageSize),
    [sessions, safePage, pageSize],
  );

  return (
    <div className="maintenance-page">
      <div className="maintenance-header">
        <h1 className="maintenance-page__title">Báo cáo kiểm kê</h1>
      </div>

      <div className="maintenance-card">
        <div className="maintenance-filters">
          <Input
            placeholder="Tìm kiếm nội dung kiểm kê"
            prefix={<SearchOutlined />}
            className="maintenance-search"
            value={searchText}
            onChange={(e) => setSearchText(e.target.value)}
            allowClear
          />
          <Select
            placeholder="Phòng ban"
            className="maintenance-select"
            value={departmentFilter}
            onChange={(v) => setDepartmentFilter(v)}
            allowClear
            showSearch
            optionFilterProp="children"
          >
            {departmentOptions.map((d) => (
              <Option key={d.id} value={d.id}>{d.name}</Option>
            ))}
          </Select>
          <Select
            placeholder="Trạng thái"
            className="maintenance-select"
            value={statusFilter}
            onChange={(v) => setStatusFilter(v)}
            allowClear
          >
            {DIRECTOR_STATUS_FILTER.map(({ value, label }) => (
              <Option key={value} value={value}>{label}</Option>
            ))}
          </Select>
        </div>

        <div className="asset-table-wrapper maintenance-table-wrapper">
          <table className="asset-table maintenance-table">
            <thead>
              <tr>
                <th>NGÀY BẮT ĐẦU</th>
                <th>MỤC ĐÍCH</th>
                <th>THỜI GIAN THỰC HIỆN</th>
                <th>PHÒNG BAN</th>
                <th>TRẠNG THÁI</th>
                <th>TIẾN ĐỘ</th>
              </tr>
            </thead>
            <tbody>
              {loading ? (
                <tr>
                  <td colSpan={6} className="maintenance-table-empty">
                    <Spin size="large" />
                  </td>
                </tr>
              ) : paginatedSessions.length === 0 ? (
                <tr>
                  <td colSpan={6} className="maintenance-table-empty">
                    Không có dữ liệu.
                  </td>
                </tr>
              ) : (
                paginatedSessions.map((row) => {
                  const canOpenReview = DIRECTOR_REVIEWABLE_STATUSES.has(row.status);
                  return (
                    <tr
                      key={row.sessionId}
                      className={`asset-row${canOpenReview ? ' inventory-table__row--clickable' : ''}`}
                      onClick={
                        canOpenReview
                          ? () => navigate(`/inventory-review/${row.sessionId}`)
                          : undefined
                      }
                    >
                      <td>{formatDate(row.startDate)}</td>
                      <td>{row.purpose}</td>
                      <td>{execDurationLabel(row.startDate, row.endDate)}</td>
                      <td>{row.departmentName}</td>
                      <td>
                        <Tag
                          color={STATUS_COLOR[row.status] ?? 'default'}
                          className="inventory-status-tag"
                        >
                          {row.statusName}
                        </Tag>
                      </td>
                      <td className="inventory-table__progress-cell">
                        <div className="inventory-progress">
                          <div className="inventory-progress__bar">
                            <div
                              className="inventory-progress__fill"
                              style={{
                                width: `${row.progressPercent ?? 0}%`,
                                backgroundColor: inventoryProgressFillColor(row.progressPercent),
                              }}
                            />
                          </div>
                          <span className="inventory-progress__label">
                            {row.progressPercent ?? 0}%
                          </span>
                        </div>
                      </td>
                    </tr>
                  );
                })
              )}
            </tbody>
          </table>
        </div>

        <div className="maintenance-card__footer">
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
            <button type="button" className="maintenance-footer__pager maintenance-footer__pager--active" tabIndex={-1} aria-current="page">
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
      </div>
    </div>
  );
}

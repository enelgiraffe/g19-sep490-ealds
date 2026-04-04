import { useState, useEffect, useCallback } from 'react';
import { useNavigate } from 'react-router-dom';
import { Input, Select, Tag, Spin, message } from 'antd';
import { SearchOutlined } from '@ant-design/icons';
import {
  inventoryService,
  SESSION_STATUS,
  SESSION_STATUS_LABEL,
  type InventorySessionListItem,
} from '../services/inventoryService';
import './DirectorInventoryPage.css';

const { Option } = Select;

/** Giám đốc chỉ xem: Chờ xác nhận (2), Đã xử lý (4) — không hiển thị Chờ xử lý (6). */
const DIRECTOR_STATUS_FILTER: { value: number; label: string }[] = [
  { value: SESSION_STATUS.Completed, label: SESSION_STATUS_LABEL[SESSION_STATUS.Completed] },
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

function formatDate(dateStr: string | null | undefined): string {
  if (!dateStr) return '-';
  const d = new Date(dateStr);
  if (Number.isNaN(d.getTime())) return '-';
  return `${String(d.getDate()).padStart(2, '0')}/${String(d.getMonth() + 1).padStart(2, '0')}/${d.getFullYear()}`;
}

export function DirectorInventoryPage() {
  const navigate = useNavigate();
  const [sessions, setSessions] = useState<InventorySessionListItem[]>([]);
  const [loading, setLoading] = useState(false);

  const [searchText, setSearchText] = useState('');
  const [departmentFilter, setDepartmentFilter] = useState<string | undefined>(undefined);
  const [statusFilter, setStatusFilter] = useState<number | undefined>(undefined);

  const fetchSessions = useCallback(async () => {
    setLoading(true);
    try {
      const data = await inventoryService.getSessions({
        keyword: searchText || undefined,
        status: statusFilter,
      });
      setSessions(data);
    } catch {
      message.error('Không thể tải danh sách kiểm kê.');
    } finally {
      setLoading(false);
    }
  }, [searchText, statusFilter]);

  useEffect(() => {
    fetchSessions();
  }, [fetchSessions]);

  const uniqueDepartments = Array.from(new Set(sessions.map((s) => s.departmentName)));

  const filteredSessions = sessions
    .filter((s) => !departmentFilter || s.departmentName === departmentFilter)
    .filter(
      (s) =>
        s.status === SESSION_STATUS.Completed /* Chờ xác nhận */
        || s.status === SESSION_STATUS.Confirmed /* Đã xử lý */,
    );

  return (
    <div className="dir-inv-page">
      <div className="dir-inv-page__header">
        <h1 className="dir-inv-page__title">Kiểm kê tài sản</h1>
      </div>

      <div className="dir-inv-card">
        <div className="dir-inv-card__filters">
          <Input
            placeholder="Tìm kiếm mã / mục đích kiểm kê"
            prefix={<SearchOutlined className="dir-inv-search-icon" />}
            className="dir-inv-search"
            value={searchText}
            onChange={(e) => setSearchText(e.target.value)}
            allowClear
          />
          <Select
            placeholder="Phòng ban"
            className="dir-inv-filter-select"
            value={departmentFilter}
            onChange={(v) => setDepartmentFilter(v)}
            allowClear
          >
            {uniqueDepartments.map((d) => (
              <Option key={d} value={d}>
                {d}
              </Option>
            ))}
          </Select>
          <Select
            placeholder="Trạng thái"
            className="dir-inv-filter-select"
            value={statusFilter}
            onChange={(v) => setStatusFilter(v)}
            allowClear
          >
            {DIRECTOR_STATUS_FILTER.map(({ value, label }) => (
              <Option key={value} value={value}>
                {label}
              </Option>
            ))}
          </Select>
        </div>

        <div className="dir-inv-table-wrapper">
          {loading ? (
            <div className="dir-inv-table__loading">
              <Spin size="large" />
            </div>
          ) : (
            <table className="dir-inv-table">
              <thead>
                <tr>
                  <th>NGÀY TẠO</th>
                  <th>MÃ PHIÊN</th>
                  <th>MỤC ĐÍCH</th>
                  <th>ĐẾN NGÀY</th>
                  <th>PHÒNG BAN</th>
                  <th>TRẠNG THÁI</th>
                </tr>
              </thead>
              <tbody>
                {filteredSessions.length === 0 ? (
                  <tr>
                    <td colSpan={6} className="dir-inv-table__empty">
                      Không có dữ liệu
                    </td>
                  </tr>
                ) : (
                  filteredSessions.map((row) => {
                    const isViewable =
                      row.status === SESSION_STATUS.Completed
                      || row.status === SESSION_STATUS.Confirmed;
                    return (
                      <tr
                        key={row.sessionId}
                        className={`dir-inv-table__row${isViewable ? ' dir-inv-table__row--clickable' : ''}`}
                        onClick={
                          isViewable
                            ? () => navigate(`/inventory-review/${row.sessionId}`)
                            : undefined
                        }
                      >
                        <td>{formatDate(row.createDate)}</td>
                        <td className="dir-inv-code">{row.code}</td>
                        <td>{row.purpose}</td>
                        <td>{formatDate(row.endDate)}</td>
                        <td>{row.departmentName}</td>
                        <td>
                          <Tag
                            color={STATUS_COLOR[row.status] ?? 'default'}
                            className="dir-inv-status-tag"
                          >
                            {row.statusName}
                          </Tag>
                        </td>
                      </tr>
                    );
                  })
                )}
              </tbody>
            </table>
          )}
        </div>
      </div>
    </div>
  );
}

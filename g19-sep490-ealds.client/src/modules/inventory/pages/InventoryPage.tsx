import { useMemo, useState } from 'react';
import { Button, Input, Select } from 'antd';
import {
  SearchOutlined,
  FilterOutlined,
  SettingOutlined,
  PlayCircleOutlined,
  EditOutlined,
  DeleteOutlined,
  HistoryOutlined,
} from '@ant-design/icons';
import './InventoryPage.css';

type InventoryStatus = 'not-started' | 'in-progress' | 'completed';

interface InventoryRow {
  id: string;
  createdDate: string;
  purpose: string;
  endDate: string;
  location: string;
  assetGroup: string;
  status: InventoryStatus;
  progress: number;
}

const STATUS_LABEL: Record<InventoryStatus, string> = {
  'not-started': 'Chưa kiểm kê',
  'in-progress': 'Đang kiểm kê',
  completed: 'Kiểm kê xong',
};

function getStatusClass(status: InventoryStatus): string {
  if (status === 'completed') {
    return 'inventory-status-pill inventory-status-pill--completed';
  }
  if (status === 'in-progress') {
    return 'inventory-status-pill inventory-status-pill--in-progress';
  }
  return 'inventory-status-pill inventory-status-pill--not-started';
}

const MOCK_ROWS: InventoryRow[] = [
  {
    id: '1',
    createdDate: '10/01/2026',
    purpose: 'Kiểm kê định kỳ quý I',
    endDate: '31/01/2026',
    location: 'Kho trung tâm',
    assetGroup: 'Thiết bị CNTT',
    status: 'not-started',
    progress: 0,
  },
  {
    id: '2',
    createdDate: '15/01/2026',
    purpose: 'Kiểm kê định kỳ tháng 1',
    endDate: '31/01/2026',
    location: 'Tầng 3 - Phòng họp',
    assetGroup: 'Nội thất văn phòng',
    status: 'completed',
    progress: 100,
  },
  {
    id: '3',
    createdDate: '20/01/2026',
    purpose: 'Kiểm kê định kỳ năm 2026',
    endDate: '31/01/2026',
    location: 'Toàn bộ tài sản',
    assetGroup: 'Tất cả nhóm',
    status: 'not-started',
    progress: 10,
  },
];

export function InventoryPage() {
  const [search, setSearch] = useState('');
  const [locationFilter, setLocationFilter] = useState<string | 'all'>('all');
  const [groupFilter, setGroupFilter] = useState<string | 'all'>('all');
  const [statusFilter, setStatusFilter] = useState<'all' | InventoryStatus>('all');

  const filteredRows: InventoryRow[] = useMemo(() => {
    const keyword = search.trim().toLowerCase();
    return MOCK_ROWS.filter((row) => {
      const matchKeyword =
        !keyword ||
        row.purpose.toLowerCase().includes(keyword) ||
        row.location.toLowerCase().includes(keyword);
      const matchLocation =
        locationFilter === 'all' || row.location.toLowerCase().includes(locationFilter.toLowerCase());
      const matchGroup =
        groupFilter === 'all' ||
        row.assetGroup.toLowerCase().includes(groupFilter.toLowerCase());
      const matchStatus = statusFilter === 'all' || row.status === statusFilter;
      return matchKeyword && matchLocation && matchGroup && matchStatus;
    });
  }, [search, locationFilter, groupFilter, statusFilter]);

  return (
    <div className="inventory-page">
      <div className="inventory-header">
        <h1 className="inventory-title">Kiểm kê</h1>
        <div className="inventory-header-actions">
          <Button className="inventory-btn-secondary">Hẹn lịch kiểm kê</Button>
          <Button type="primary" className="inventory-btn-primary">
            Lập lịch kiểm kê
          </Button>
        </div>
      </div>

      <div className="inventory-card">
        <div className="inventory-filters">
          <Input
            placeholder="Tìm kiếm nội dung kiểm kê"
            prefix={<SearchOutlined />}
            className="inventory-search"
            value={search}
            onChange={(e) => setSearch(e.target.value)}
          />
          <Select
            placeholder="Vị trí tài sản"
            className="inventory-select"
            suffixIcon={<FilterOutlined />}
            value={locationFilter}
            onChange={(v) => setLocationFilter(v)}
            options={[
              { value: 'all', label: 'Tất cả vị trí' },
              { value: 'Kho trung tâm', label: 'Kho trung tâm' },
              { value: 'Tầng 3', label: 'Tầng 3' },
              { value: 'Toàn bộ tài sản', label: 'Toàn bộ tài sản' },
            ]}
          />
          <Select
            placeholder="Nhóm tài sản"
            className="inventory-select"
            suffixIcon={<FilterOutlined />}
            value={groupFilter}
            onChange={(v) => setGroupFilter(v)}
            options={[
              { value: 'all', label: 'Tất cả nhóm' },
              { value: 'Thiết bị CNTT', label: 'Thiết bị CNTT' },
              { value: 'Nội thất văn phòng', label: 'Nội thất văn phòng' },
            ]}
          />
          <Select
            placeholder="Trạng thái"
            className="inventory-select"
            suffixIcon={<FilterOutlined />}
            value={statusFilter}
            onChange={(v) => setStatusFilter(v)}
            options={[
              { value: 'all', label: 'Tất cả trạng thái' },
              { value: 'not-started', label: STATUS_LABEL['not-started'] },
              { value: 'in-progress', label: STATUS_LABEL['in-progress'] },
              { value: 'completed', label: STATUS_LABEL.completed },
            ]}
          />
          <Button
            icon={<FilterOutlined />}
            className="inventory-filter-reset"
            onClick={() => {
              setSearch('');
              setLocationFilter('all');
              setGroupFilter('all');
              setStatusFilter('all');
            }}
          >
            Gỡ bộ lọc
          </Button>
          <Button icon={<SettingOutlined />} className="inventory-settings" />
        </div>

        <div className="asset-table-wrapper">
          <table className="asset-table inventory-table">
            <thead>
              <tr>
                <th>Ngày tạo lịch</th>
                <th>Mục đích (định kỳ)</th>
                <th>Đến ngày</th>
                <th>Vị trí tài sản</th>
                <th>Nhóm tài sản</th>
                <th>Trạng thái</th>
                <th>Tiến độ</th>
                <th className="asset-table__cell asset-table__cell--actions" />
              </tr>
            </thead>
            <tbody>
              {filteredRows.length === 0 ? (
                <tr>
                  <td colSpan={8} className="inventory-table-empty">
                    Không có dữ liệu.
                  </td>
                </tr>
              ) : (
                filteredRows.map((row) => (
                  <tr key={row.id} className="asset-row">
                    <td>{row.createdDate}</td>
                    <td>{row.purpose}</td>
                    <td>{row.endDate}</td>
                    <td>{row.location}</td>
                    <td>{row.assetGroup}</td>
                    <td>
                      <span className={getStatusClass(row.status)}>
                        {STATUS_LABEL[row.status]}
                      </span>
                    </td>
                    <td className="asset-align-right">{row.progress}%</td>
                    <td className="asset-table__cell asset-table__cell--actions">
                      <div className="inventory-actions">
                        <Button
                          type="text"
                          icon={<PlayCircleOutlined />}
                          className="inventory-action-btn"
                        />
                        <Button
                          type="text"
                          icon={<EditOutlined />}
                          className="inventory-action-btn"
                        />
                        <Button
                          type="text"
                          icon={<DeleteOutlined />}
                          danger
                          className="inventory-action-btn"
                        />
                        <Button
                          type="text"
                          icon={<HistoryOutlined />}
                          className="inventory-action-btn"
                        />
                      </div>
                    </td>
                  </tr>
                ))
              )}
            </tbody>
          </table>
        </div>

        <div className="inventory-card__footer">
          <div className="inventory-footer__left">
            Số lượng trên trang:
            <select className="inventory-footer__select" defaultValue={25}>
              <option value={10}>10</option>
              <option value={25}>25</option>
              <option value={50}>50</option>
              <option value={100}>100</option>
            </select>
          </div>
          <div className="inventory-footer__center">1-25 trên 289</div>
          <div className="inventory-footer__right">
            <button className="inventory-footer__pager" disabled type="button">
              ⟨
            </button>
            <button
              className="inventory-footer__pager inventory-footer__pager--active"
              type="button"
            >
              1
            </button>
            <button className="inventory-footer__pager" type="button">
              ⟩
            </button>
          </div>
        </div>
      </div>
    </div>
  );
}


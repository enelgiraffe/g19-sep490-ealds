import { useMemo, useState } from 'react';
import { Button, Input, Select, Tabs } from 'antd';
import {
  EyeOutlined,
  FilterOutlined,
  SearchOutlined,
  SettingOutlined,
} from '@ant-design/icons';
import './MaintenancePage.css';

type MaintenanceStatus = 'draft' | 'pending' | 'approved' | 'rejected';

interface MaintenanceRow {
  id: string;
  assetCode: string;
  assetName: string;
  assetType: string;
  purpose: string;
  setupDate: string;
  expectedDate: string;
  assetState: string;
  status: MaintenanceStatus;
}

function getStatusLabel(status: MaintenanceStatus): string {
  if (status === 'draft') return 'Chưa gửi';
  if (status === 'pending') return 'Chờ phê duyệt';
  if (status === 'approved') return 'Phê duyệt';
  return 'Từ chối';
}

function getStatusClass(status: MaintenanceStatus): string {
  if (status === 'draft') return 'maintenance-status-pill maintenance-status-pill--draft';
  if (status === 'pending') {
    return 'maintenance-status-pill maintenance-status-pill--pending';
  }
  if (status === 'approved') {
    return 'maintenance-status-pill maintenance-status-pill--approved';
  }
  return 'maintenance-status-pill maintenance-status-pill--rejected';
}

const MOCK_ROWS: MaintenanceRow[] = [
  {
    id: '1',
    assetCode: 'MCS',
    assetName: 'Máy cắt sắt',
    assetType: 'Thiết bị kỹ thuật',
    purpose: 'Duy trì hiệu suất',
    setupDate: '28/01/2026',
    expectedDate: '28/01/2026',
    assetState: 'Đang sử dụng',
    status: 'draft',
  },
  {
    id: '2',
    assetCode: 'MUV',
    assetName: 'Máy uốn vòm',
    assetType: 'Thiết bị kỹ thuật',
    purpose: 'Đảm bảo an toàn',
    setupDate: '28/01/2026',
    expectedDate: '28/01/2026',
    assetState: 'Đang sử dụng',
    status: 'pending',
  },
  {
    id: '3',
    assetCode: 'MDK',
    assetName: 'Màn điều khiển',
    assetType: 'Thiết bị kỹ thuật',
    purpose: 'Thay film',
    setupDate: '28/01/2026',
    expectedDate: '28/01/2026',
    assetState: 'Đang sử dụng',
    status: 'approved',
  },
  {
    id: '4',
    assetCode: 'MDK-2',
    assetName: 'Màn điều khiển',
    assetType: 'Thiết bị kỹ thuật',
    purpose: 'Thay film',
    setupDate: '28/01/2026',
    expectedDate: '28/01/2026',
    assetState: 'Đang sử dụng',
    status: 'rejected',
  },
];

export function MaintenancePage() {
  const [activeTab, setActiveTab] = useState<'need-maintenance' | 'in-maintenance'>(
    'need-maintenance'
  );
  const [search, setSearch] = useState('');
  const [statusFilter, setStatusFilter] = useState<'all' | MaintenanceStatus>('all');

  const filteredRows: MaintenanceRow[] = useMemo(() => {
    const keyword = search.trim().toLowerCase();
    return MOCK_ROWS.filter((row) => {
      const matchStatus = statusFilter === 'all' || row.status === statusFilter;
      const matchKeyword =
        !keyword ||
        row.assetCode.toLowerCase().includes(keyword) ||
        row.assetName.toLowerCase().includes(keyword) ||
        row.purpose.toLowerCase().includes(keyword);
      return matchStatus && matchKeyword;
    });
  }, [search, statusFilter]);

  return (
    <div className="maintenance-page">
      <div className="maintenance-header">
        <h1 className="maintenance-page__title">Bảo dưỡng</h1>
        <Button type="primary" className="maintenance-btn-add">
          + Thiết lập quy định bảo dưỡng
        </Button>
      </div>

      <div className="maintenance-card">
        <Tabs
          activeKey={activeTab}
          onChange={(key) => setActiveTab(key as 'need-maintenance' | 'in-maintenance')}
          className="maintenance-tabs"
          items={[
            { key: 'need-maintenance', label: 'Tài sản cần bảo dưỡng' },
            { key: 'in-maintenance', label: 'Đang bảo dưỡng' },
          ]}
        />

        <div className="maintenance-filters">
          <Input
            placeholder="Tìm kiếm"
            prefix={<SearchOutlined />}
            className="maintenance-search"
            value={search}
            onChange={(e) => setSearch(e.target.value)}
          />
          <Select
            placeholder="Trạng thái"
            className="maintenance-select"
            suffixIcon={<FilterOutlined />}
            value={statusFilter}
            onChange={(v) => setStatusFilter(v)}
            options={[
              { value: 'all', label: 'Tất cả' },
              { value: 'draft', label: 'Chưa gửi' },
              { value: 'pending', label: 'Chờ phê duyệt' },
              { value: 'approved', label: 'Phê duyệt' },
              { value: 'rejected', label: 'Từ chối' },
            ]}
          />
          <Button
            icon={<FilterOutlined />}
            className="maintenance-filter-advanced"
            onClick={() => {
              setSearch('');
              setStatusFilter('all');
            }}
          >
            Gỡ bộ lọc
          </Button>
          <Button icon={<SettingOutlined />} className="maintenance-settings" />
        </div>

        <div className="asset-table-wrapper maintenance-table-wrapper">
          <table className="asset-table maintenance-table">
            <thead>
              <tr>
                <th className="asset-table__cell asset-table__cell--checkbox">
                  <input type="checkbox" />
                </th>
                <th>MÃ TÀI SẢN</th>
                <th>TÊN TÀI SẢN</th>
                <th>LOẠI TÀI SẢN</th>
                <th>MỤC ĐÍCH</th>
                <th>NGÀY THIẾT LẬP BD</th>
                <th>NGÀY BD DỰ KIẾN</th>
                <th>TÌNH TRẠNG TS</th>
                <th>TRẠNG THÁI</th>
                <th className="asset-table__cell asset-table__cell--actions" />
              </tr>
            </thead>
            <tbody>
              {filteredRows.length === 0 ? (
                <tr>
                  <td colSpan={10} className="maintenance-table-empty">
                    Không có dữ liệu.
                  </td>
                </tr>
              ) : (
                filteredRows.map((row) => (
                  <tr key={row.id} className="asset-row">
                    <td className="asset-table__cell asset-table__cell--checkbox">
                      <input type="checkbox" />
                    </td>
                    <td>
                      <button type="button" className="asset-code asset-code--link">
                        {row.assetCode}
                      </button>
                    </td>
                    <td>{row.assetName}</td>
                    <td>{row.assetType}</td>
                    <td>{row.purpose}</td>
                    <td>{row.setupDate}</td>
                    <td>{row.expectedDate}</td>
                    <td>{row.assetState}</td>
                    <td>
                      <span className={getStatusClass(row.status)}>
                        {getStatusLabel(row.status)}
                      </span>
                    </td>
                    <td className="asset-table__cell asset-table__cell--actions">
                      <Button type="text" icon={<EyeOutlined />} />
                    </td>
                  </tr>
                ))
              )}
            </tbody>
          </table>
        </div>

        <div className="maintenance-card__footer">
          <div className="maintenance-footer__left">
            Số lượng trên trang:
            <select className="maintenance-footer__select" defaultValue={25}>
              <option value={10}>10</option>
              <option value={25}>25</option>
              <option value={50}>50</option>
              <option value={100}>100</option>
            </select>
          </div>
          <div className="maintenance-footer__center">1-25 trên 289</div>
          <div className="maintenance-footer__right">
            <button className="maintenance-footer__pager" disabled type="button">
              ⟨
            </button>
            <button
              className="maintenance-footer__pager maintenance-footer__pager--active"
              type="button"
            >
              1
            </button>
            <button className="maintenance-footer__pager" type="button">
              ⟩
            </button>
          </div>
        </div>
      </div>
    </div>
  );
}


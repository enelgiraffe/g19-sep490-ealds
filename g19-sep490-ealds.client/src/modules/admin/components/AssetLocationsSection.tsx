import { Input, Select } from 'antd';
import { FilterOutlined, SearchOutlined } from '@ant-design/icons';
import type { CategoryStatus } from './AssetTypesSection';

const { Option } = Select;

export interface AssetLocationRow {
  key: number;
  index: number;
  name: string;
  parentName: string | null;
  note: string | null;
  status: CategoryStatus;
}

interface AssetLocationsSectionProps {
  searchText: string;
  onSearchTextChange: (value: string) => void;
  statusFilter: 'all' | CategoryStatus;
  onStatusFilterChange: (value: 'all' | CategoryStatus) => void;
  rows: AssetLocationRow[];
  statusLabels: Record<CategoryStatus, { label: string; className: string }>;
  onOpenEditLocation: (row: AssetLocationRow) => void;
}

export function AssetLocationsSection({
  searchText,
  onSearchTextChange,
  statusFilter,
  onStatusFilterChange,
  rows,
  statusLabels,
  onOpenEditLocation,
}: AssetLocationsSectionProps) {
  return (
    <>
      <div className="categories-filters">
        <Input
          placeholder="Tìm kiếm"
          prefix={<SearchOutlined />}
          className="categories-search"
          value={searchText}
          onChange={(e) => onSearchTextChange(e.target.value)}
        />
        <Select
          placeholder="Trạng thái"
          className="categories-select"
          suffixIcon={<FilterOutlined />}
          value={statusFilter}
          onChange={(v) => onStatusFilterChange(v as 'all' | CategoryStatus)}
        >
          <Option value="all">Tất cả</Option>
          <Option value="tracking">Đang theo dõi</Option>
          <Option value="stopped">Không theo dõi</Option>
        </Select>
      </div>

      <div className="asset-table-wrapper categories-table-wrapper">
        <table className="asset-table categories-table categories-table--locations">
          <thead>
            <tr>
              <th>STT</th>
              <th>TÊN VỊ TRÍ</th>
              <th>THUỘC</th>
              <th>GHI CHÚ</th>
              <th>TRẠNG THÁI</th>
              <th className="asset-table__cell asset-table__cell--actions" />
            </tr>
          </thead>
          <tbody>
            {rows.length === 0 ? (
              <tr>
                <td colSpan={6} className="categories-table-empty">
                  Không có dữ liệu.
                </td>
              </tr>
            ) : (
              rows.map((row) => (
                <tr key={row.key} className="asset-row">
                  <td className="asset-align-right">{row.index}</td>
                  <td>{row.name}</td>
                  <td>{row.parentName ?? '—'}</td>
                  <td>{row.note ?? '—'}</td>
                  <td>
                    <span className={statusLabels[row.status].className}>
                      {statusLabels[row.status].label}
                    </span>
                  </td>
                  <td className="asset-table__cell asset-table__cell--actions">
                    <button
                      type="button"
                      className="categories-action-btn"
                      onClick={() => onOpenEditLocation(row)}
                    >
                      ✎
                    </button>
                    <button type="button" className="categories-action-btn categories-action-btn--danger">🗑</button>
                  </td>
                </tr>
              ))
            )}
          </tbody>
        </table>
      </div>
    </>
  );
}


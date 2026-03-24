import { Button, Input, Select } from 'antd';
import { DownloadOutlined, FilterOutlined, SearchOutlined } from '@ant-design/icons';

const { Option } = Select;

export type CategoryStatus = 'tracking' | 'stopped';

export interface CategoryRow {
  key: number;
  code: string;
  name: string;
  group: string;
  managementMethod: string;
  quantityTracking: number;
  displayStatus: CategoryStatus;
}

interface AssetTypesSectionProps {
  searchText: string;
  onSearchTextChange: (value: string) => void;
  statusFilter: 'all' | CategoryStatus;
  onStatusFilterChange: (value: 'all' | CategoryStatus) => void;
  onResetFilters: () => void;
  isLoadingAssetTypes: boolean;
  rows: CategoryRow[];
  statusLabels: Record<CategoryStatus, { label: string; className: string }>;
}

export function AssetTypesSection({
  searchText,
  onSearchTextChange,
  statusFilter,
  onStatusFilterChange,
  onResetFilters,
  isLoadingAssetTypes,
  rows,
  statusLabels,
}: AssetTypesSectionProps) {
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
          <Option value="stopped">Ngừng theo dõi</Option>
        </Select>
        <Button className="categories-filter-reset" icon={<FilterOutlined />} onClick={onResetFilters}>
          Gỡ bộ lọc
        </Button>
        <Button icon={<DownloadOutlined />} className="categories-export-btn">
          Export
        </Button>
      </div>

      <div className="asset-table-wrapper categories-table-wrapper">
        <table className="asset-table categories-table">
          <thead>
            <tr>
              <th className="asset-table__cell asset-table__cell--checkbox">
                <input type="checkbox" />
              </th>
              <th>MÃ LOẠI TÀI SẢN</th>
              <th>TÊN LOẠI TÀI SẢN</th>
              <th>NHÓM TÀI SẢN</th>
              <th>CÁCH QUẢN LÝ</th>
              <th>SỐ LƯỢNG</th>
              <th>SỐ LƯỢNG</th>
              <th className="asset-table__cell asset-table__cell--actions" />
            </tr>
          </thead>
          <tbody>
            {isLoadingAssetTypes ? (
              <tr>
                <td colSpan={8} className="categories-table-empty">
                  Đang tải dữ liệu...
                </td>
              </tr>
            ) : rows.length === 0 ? (
              <tr>
                <td colSpan={8} className="categories-table-empty">
                  Không có dữ liệu.
                </td>
              </tr>
            ) : (
              rows.map((row) => (
                <tr key={row.key} className="asset-row">
                  <td className="asset-table__cell asset-table__cell--checkbox">
                    <input type="checkbox" />
                  </td>
                  <td>{row.code}</td>
                  <td>{row.name}</td>
                  <td>{row.group}</td>
                  <td>{row.managementMethod}</td>
                  <td className="asset-align-right">{row.quantityTracking}</td>
                  <td>
                    <span className={statusLabels[row.displayStatus].className}>
                      {statusLabels[row.displayStatus].label}
                    </span>
                  </td>
                  <td className="asset-table__cell asset-table__cell--actions">
                    <button type="button" className="categories-action-btn">✎</button>
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


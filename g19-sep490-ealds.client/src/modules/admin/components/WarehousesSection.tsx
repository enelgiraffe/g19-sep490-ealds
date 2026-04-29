import { Input } from 'antd';
import { SearchOutlined } from '@ant-design/icons';
import type { WarehouseItem } from '../../assets/services/assetService';

interface WarehousesSectionProps {
  searchText: string;
  onSearchTextChange: (value: string) => void;
  isLoadingWarehouses: boolean;
  rows: WarehouseItem[];
  onEditWarehouse: (row: WarehouseItem) => void;
  onRequestDeleteWarehouse: (row: WarehouseItem) => void;
}

export function WarehousesSection({
  searchText,
  onSearchTextChange,
  isLoadingWarehouses,
  rows,
  onEditWarehouse,
  onRequestDeleteWarehouse,
}: WarehousesSectionProps) {
  return (
    <>
      <div className="categories-filters">
        <Input
          placeholder="Tìm kiếm theo tên kho hoặc địa điểm"
          prefix={<SearchOutlined />}
          className="categories-search"
          value={searchText}
          onChange={(e) => onSearchTextChange(e.target.value)}
        />
      </div>

      <div className="asset-table-wrapper categories-table-wrapper">
        <table className="asset-table categories-table">
          <thead>
            <tr>
              <th>MÃ KHO</th>
              <th>TÊN KHO</th>
              <th>ĐỊA ĐIỂM</th>
              <th className="asset-table__cell asset-table__cell--actions" />
            </tr>
          </thead>
          <tbody>
            {isLoadingWarehouses ? (
              <tr>
                <td colSpan={4} className="categories-table-empty">
                  Đang tải dữ liệu...
                </td>
              </tr>
            ) : rows.length === 0 ? (
              <tr>
                <td colSpan={4} className="categories-table-empty">
                  Không có dữ liệu.
                </td>
              </tr>
            ) : (
              rows.map((row) => {
                const canDelete = row.canDelete !== false;
                return (
                  <tr key={row.warehouseId} className="asset-row">
                    <td className="asset-align-right">{row.warehouseId}</td>
                    <td>{row.name}</td>
                    <td>{row.description?.trim() ? row.description : '—'}</td>
                    <td className="asset-table__cell asset-table__cell--actions">
                      <button
                        type="button"
                        className="categories-action-btn"
                        onClick={() => onEditWarehouse(row)}
                        aria-label="Chỉnh sửa"
                      >
                        ✎
                      </button>
                      <button
                        type="button"
                        className="categories-action-btn categories-action-btn--danger"
                        disabled={!canDelete}
                        title={
                          canDelete
                            ? 'Xóa kho'
                            : 'Không thể xóa: còn cá thể tài sản gắn với kho này.'
                        }
                        onClick={() => {
                          if (canDelete) onRequestDeleteWarehouse(row);
                        }}
                        aria-label="Xóa"
                      >
                        🗑
                      </button>
                    </td>
                  </tr>
                );
              })
            )}
          </tbody>
        </table>
      </div>
    </>
  );
}

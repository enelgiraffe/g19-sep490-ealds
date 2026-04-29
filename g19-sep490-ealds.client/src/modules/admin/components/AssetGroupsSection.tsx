import { Button, Input } from 'antd';
import { SearchOutlined, SettingOutlined } from '@ant-design/icons';

export interface AssetGroupRow {
  key: number;
  code: number;
  name: string;
  assetTypeCount: number;
}

interface AssetGroupsSectionProps {
  searchText: string;
  onSearchTextChange: (value: string) => void;
  isLoadingCategories: boolean;
  rows: AssetGroupRow[];
  onEditAssetCategory: (row: AssetGroupRow) => void;
  onDeleteAssetCategory: (row: AssetGroupRow) => void;
}

export function AssetGroupsSection({
  searchText,
  onSearchTextChange,
  isLoadingCategories,
  rows,
  onEditAssetCategory,
  onDeleteAssetCategory,
}: AssetGroupsSectionProps) {
  return (
    <>
      <div className="categories-filters categories-filters--group">
        <div className="categories-filters__left">
          <Input
            placeholder="Tìm kiếm"
            prefix={<SearchOutlined />}
            className="categories-search"
            value={searchText}
            onChange={(e) => onSearchTextChange(e.target.value)}
          />
          <Button icon={<SettingOutlined />} className="categories-settings-btn" />
        </div>
      </div>

      <div className="asset-table-wrapper categories-table-wrapper">
        <table className="asset-table categories-table categories-table--groups">
          <thead>
            <tr>
              <th>MÃ NHÓM</th>
              <th>TÊN NHÓM TÀI SẢN</th>
              <th>SỐ LOẠI TÀI SẢN</th>
              <th className="asset-table__cell asset-table__cell--actions" />
            </tr>
          </thead>
          <tbody>
            {isLoadingCategories ? (
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
              rows.map((row) => (
                <tr key={row.key} className="categories-group-row categories-group-row--parent">
                  <td className="categories-group-cell">{row.code}</td>
                  <td className="categories-group-cell categories-group-cell--name">{row.name}</td>
                  <td className="categories-group-cell asset-align-right">{row.assetTypeCount}</td>
                  <td className="asset-table__cell asset-table__cell--actions">
                    <button
                      type="button"
                      className="categories-action-btn"
                      onClick={() => onEditAssetCategory(row)}
                      aria-label="Chỉnh sửa"
                    >
                      ✎
                    </button>
                    <button
                      type="button"
                      className="categories-action-btn categories-action-btn--danger"
                      onClick={() => onDeleteAssetCategory(row)}
                      aria-label="Xóa"
                    >
                      🗑
                    </button>
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

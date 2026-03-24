import { Button, Input } from 'antd';
import { DownloadOutlined, SearchOutlined, SettingOutlined } from '@ant-design/icons';

export interface AssetGroupRow {
  key: number;
  code: number;
  name: string;
  parentCode: string | null;
}

interface AssetGroupsSectionProps {
  searchText: string;
  onSearchTextChange: (value: string) => void;
  isLoadingCategories: boolean;
  rows: AssetGroupRow[];
  expandedGroupCodes: string[];
  onToggleGroup: (code: string) => void;
}

export function AssetGroupsSection({
  searchText,
  onSearchTextChange,
  isLoadingCategories,
  rows,
  expandedGroupCodes,
  onToggleGroup,
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
        <Button icon={<DownloadOutlined />} className="categories-import-btn">
          Nhập excel
        </Button>
      </div>

      <div className="asset-table-wrapper categories-table-wrapper">
        <table className="asset-table categories-table categories-table--groups">
          <thead>
            <tr>
              <th className="categories-groups-toggle-col" />
              <th>MÃ NHÓM TÀI SẢN</th>
              <th>TÊN NHÓM TÀI SẢN</th>
              <th>THUỘC NHÓM</th>
              <th className="asset-table__cell asset-table__cell--actions" />
            </tr>
          </thead>
          <tbody>
            {isLoadingCategories ? (
              <tr>
                <td colSpan={5} className="categories-table-empty">
                  Đang tải dữ liệu...
                </td>
              </tr>
            ) : rows.length === 0 ? (
              <tr>
                <td colSpan={5} className="categories-table-empty">
                  Không có dữ liệu.
                </td>
              </tr>
            ) : (
              rows.map((row) => {
                const isParent = row.parentCode === null;
                const codeStr = String(row.code);
                const hasChildren = false;
                const isExpanded = isParent && expandedGroupCodes.includes(codeStr);

                return (
                  <tr
                    key={row.key}
                    className={
                      isParent
                        ? 'categories-group-row categories-group-row--parent'
                        : 'categories-group-row categories-group-row--child'
                    }
                  >
                    <td className="categories-group-cell categories-group-cell--toggle">
                      {isParent && hasChildren ? (
                        <button
                          type="button"
                          className="categories-group-toggle-btn"
                          onClick={() => onToggleGroup(codeStr)}
                        >
                          {isExpanded ? '▾' : '▸'}
                        </button>
                      ) : (
                        <span className="categories-group-toggle-placeholder" />
                      )}
                    </td>
                    <td className="categories-group-cell">{row.code}</td>
                    <td className="categories-group-cell categories-group-cell--name">{row.name}</td>
                    <td className="categories-group-cell">{row.parentCode ?? '—'}</td>
                    <td className="asset-table__cell asset-table__cell--actions">
                      <button type="button" className="categories-action-btn">✎</button>
                      <button type="button" className="categories-action-btn categories-action-btn--danger">🗑</button>
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


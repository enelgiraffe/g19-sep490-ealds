import { useMemo, useState } from 'react';
import { Button, Input, Select } from 'antd';
import {
  DownloadOutlined,
  FilterOutlined,
  SearchOutlined,
  SettingOutlined,
  EyeOutlined,
} from '@ant-design/icons';
import './LiquidationPage.css';

const { Option } = Select;

type LiquidationStatus = 'stopped' | 'liquidated';

interface LiquidationRow {
  id: string;
  assetCode: string;
  assetName: string;
  assetType: string;
  quantity: number;
  originalPrice: string;
  depreciationValue: string;
  status: LiquidationStatus;
}

function getStatusLabel(status: LiquidationStatus): string {
  if (status === 'stopped') return 'Dừng sử dụng';
  return 'Đã thanh lý';
}

export function LiquidationPage() {
  const [search, setSearch] = useState('');
  const [assetTypeFilter, setAssetTypeFilter] = useState<'all' | string>('all');
  const [statusFilter, setStatusFilter] = useState<'all' | LiquidationStatus>('all');

  const allData: LiquidationRow[] = useMemo(
    () => [
      {
        id: '1',
        assetCode: 'MCS',
        assetName: 'Máy cắt sắt',
        assetType: 'Cơ khí',
        quantity: 1,
        originalPrice: '910,000,000 đ',
        depreciationValue: '910,000,000 đ',
        status: 'stopped',
      },
      {
        id: '2',
        assetCode: 'MUV',
        assetName: 'Máy uốn vòm',
        assetType: 'Cơ khí',
        quantity: 1,
        originalPrice: '500,000,000 đ',
        depreciationValue: '500,000,000 đ',
        status: 'liquidated',
      },
      {
        id: '3',
        assetCode: 'FSF90',
        assetName: 'Ôtô Ferrari SF90',
        assetType: 'Máy móc',
        quantity: 1,
        originalPrice: '34,000,500,000,000 đ',
        depreciationValue: '34,000,500,000,000 đ',
        status: 'liquidated',
      },
    ],
    []
  );

  const filteredData = useMemo(() => {
    return allData.filter((row) => {
      const matchSearch =
        !search ||
        row.assetCode.toLowerCase().includes(search.toLowerCase()) ||
        row.assetName.toLowerCase().includes(search.toLowerCase());
      const matchType =
        assetTypeFilter === 'all' || row.assetType.toLowerCase() === assetTypeFilter.toLowerCase();
      const matchStatus = statusFilter === 'all' || row.status === statusFilter;
      return matchSearch && matchType && matchStatus;
    });
  }, [allData, search, assetTypeFilter, statusFilter]);

  const total = filteredData.length;

  return (
    <div className="liquidation-page">
      <h1 className="liquidation-page__title">Thanh lý</h1>

      <div className="liquidation-card">
        <div className="liquidation-card__header">
          <div className="liquidation-filters">
            <Input
              placeholder="Tìm kiếm theo tên, mã tài sản"
              prefix={<SearchOutlined />}
              className="liquidation-search"
              value={search}
              onChange={(e) => setSearch(e.target.value)}
            />
            <Select
              placeholder="Loại tài sản"
              className="liquidation-select"
              suffixIcon={<FilterOutlined />}
              value={assetTypeFilter}
              onChange={(v) => setAssetTypeFilter(v)}
            >
              <Option value="all">Tất cả loại tài sản</Option>
              <Option value="Cơ khí">Cơ khí</Option>
              <Option value="Máy móc">Máy móc</Option>
            </Select>
            <Select
              placeholder="Tình trạng"
              className="liquidation-select"
              suffixIcon={<FilterOutlined />}
              value={statusFilter}
              onChange={(v) => setStatusFilter(v)}
            >
              <Option value="all">Tất cả tình trạng</Option>
              <Option value="stopped">Dừng sử dụng</Option>
              <Option value="liquidated">Đã thanh lý</Option>
            </Select>
            <Button
              icon={<FilterOutlined />}
              className="liquidation-filter-reset"
              onClick={() => {
                setSearch('');
                setAssetTypeFilter('all');
                setStatusFilter('all');
              }}
            >
              Gỡ bộ lọc
            </Button>
            <Button icon={<SettingOutlined />} className="liquidation-settings" />
          </div>
          <div className="liquidation-header-actions">
            <Button icon={<DownloadOutlined />} className="liquidation-download">
              Tải xuống
            </Button>
          </div>
        </div>

        <div className="asset-table-wrapper liquidation-table-wrapper">
          <table className="asset-table liquidation-table">
            <thead>
              <tr>
                <th className="asset-table__cell asset-table__cell--checkbox">
                  <input type="checkbox" />
                </th>
                <th>MÃ TÀI SẢN</th>
                <th>TÊN TÀI SẢN</th>
                <th>LOẠI TÀI SẢN</th>
                <th>SỐ LƯỢNG</th>
                <th>NGUYÊN GIÁ</th>
                <th>GIÁ TRỊ KHẤU HAO</th>
                <th>TÌNH TRẠNG</th>
                <th className="asset-table__cell asset-table__cell--actions" />
              </tr>
            </thead>
            <tbody>
              {filteredData.length === 0 ? (
                <tr>
                  <td colSpan={9} className="liquidation-table-empty">
                    Không có dữ liệu.
                  </td>
                </tr>
              ) : (
                filteredData.map((row) => (
                  <tr key={row.id} className="asset-row">
                    <td className="asset-table__cell asset-table__cell--checkbox">
                      <input type="checkbox" />
                    </td>
                    <td className="asset-code asset-code--link">{row.assetCode}</td>
                    <td>{row.assetName}</td>
                    <td>{row.assetType}</td>
                    <td className="asset-align-right">{row.quantity}</td>
                    <td className="asset-align-right">{row.originalPrice}</td>
                    <td className="asset-align-right">{row.depreciationValue}</td>
                    <td>
                      <span
                        className={
                          row.status === 'liquidated'
                            ? 'asset-status-pill asset-status-pill--active'
                            : 'asset-status-pill asset-status-pill--inactive'
                        }
                      >
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

        <div className="liquidation-card__footer">
          <div className="liquidation-footer__left">
            Số lượng trên trang:
            <select className="liquidation-footer__select" defaultValue={25}>
              <option value={10}>10</option>
              <option value={25}>25</option>
              <option value={50}>50</option>
              <option value={100}>100</option>
            </select>
          </div>
          <div className="liquidation-footer__center">
            {total === 0 ? '0-0 trên 0' : `1-${total} trên ${total}`}
          </div>
          <div className="liquidation-footer__right">
            <button className="liquidation-footer__pager" disabled type="button">
              ⟨
            </button>
            <button
              className="liquidation-footer__pager liquidation-footer__pager--active"
              type="button"
            >
              1
            </button>
            <button className="liquidation-footer__pager" type="button">
              ⟩
            </button>
          </div>
        </div>
      </div>
    </div>
  );
}


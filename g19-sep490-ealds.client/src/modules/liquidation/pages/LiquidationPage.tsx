import { useEffect, useMemo, useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { Button, Input, Select } from 'antd';
import {
  DownloadOutlined,
  FilterOutlined,
  SearchOutlined,
  SettingOutlined,
  EyeOutlined,
} from '@ant-design/icons';
import {
  assetInstanceService,
  assetService,
  formatVnd,
  getStatusLabel,
  type AssetInstanceResponse,
  type AssetTypeItem,
} from '../../assets/services/assetService';
import './LiquidationPage.css';

const { Option } = Select;

type LiquidationStatusFilter = 'all' | 'disposed' | 'liquidated';

interface LiquidationRow {
  id: number;
  catalogAssetId: number;
  assetCode: string;
  assetName: string;
  assetType: string;
  assetTypeId?: number | null;
  quantity: number;
  originalPrice: string;
  depreciationValue: string;
  statusName: string;
}

function toRow(a: AssetInstanceResponse): LiquidationRow {
  return {
    id: a.assetInstanceId,
    catalogAssetId: a.assetId,
    assetCode: a.assetCode ?? a.instanceCode,
    assetName: a.assetName ?? a.instanceCode,
    assetType: '—',
    assetTypeId: null,
    quantity: 1,
    originalPrice: formatVnd(a.originalPrice ?? 0),
    depreciationValue: formatVnd(a.currentValue ?? 0),
    statusName: a.statusName ?? '',
  };
}

export function LiquidationPage() {
  const [search, setSearch] = useState('');
  const [assetTypeFilter, setAssetTypeFilter] = useState<'all' | number>('all');
  const [statusFilter, setStatusFilter] = useState<LiquidationStatusFilter>('all');
  const [assets, setAssets] = useState<AssetInstanceResponse[] | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [assetTypes, setAssetTypes] = useState<AssetTypeItem[]>([]);
  const navigate = useNavigate();

  useEffect(() => {
    async function loadAssetTypes() {
      try {
        const items = await assetService.getAssetTypes();
        setAssetTypes(items);
      } catch {
        setAssetTypes([]);
      }
    }
    loadAssetTypes();
  }, []);

  useEffect(() => {
    async function loadDisposedAssets() {
      setLoading(true);
      setError(null);
      try {
        // Backend enum: Disposed=4, Liquidated=6 (cả hai đều là "Đã thanh lý" ở UI)
        const [disposed, liquidated] = await Promise.all([
          assetInstanceService.getAll({ status: 4 }),
          assetInstanceService.getAll({ status: 6 }),
        ]);

        const merged = [...disposed, ...liquidated];
        const map = new Map<number, AssetInstanceResponse>();
        for (const a of merged) map.set(a.assetInstanceId, a);
        setAssets(Array.from(map.values()));
      } catch {
        setError('Không tải được danh sách tài sản đã thanh lý. Kiểm tra kết nối backend.');
        setAssets([]);
      } finally {
        setLoading(false);
      }
    }
    loadDisposedAssets();
  }, []);

  const allRows: LiquidationRow[] = useMemo(() => {
    return (assets ?? []).map(toRow);
  }, [assets]);

  const filteredData = useMemo(() => {
    return allRows.filter((row) => {
      const matchSearch =
        !search ||
        row.assetCode.toLowerCase().includes(search.toLowerCase()) ||
        row.assetName.toLowerCase().includes(search.toLowerCase());
      const matchType =
        assetTypeFilter === 'all' || row.assetTypeId === assetTypeFilter;
      const isDisposed = row.statusName === 'Disposed';
      const isLiquidated = row.statusName === 'Liquidated';
      const matchStatus =
        statusFilter === 'all' ||
        (statusFilter === 'disposed' && isDisposed) ||
        (statusFilter === 'liquidated' && isLiquidated);
      return matchSearch && matchType && matchStatus;
    });
  }, [allRows, search, assetTypeFilter, statusFilter]);

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
              {assetTypes.map((t) => (
                <Option key={t.assetTypeId} value={t.assetTypeId}>
                  {t.name}
                </Option>
              ))}
            </Select>
            <Select
              placeholder="Tình trạng"
              className="liquidation-select"
              suffixIcon={<FilterOutlined />}
              value={statusFilter}
              onChange={(v) => setStatusFilter(v)}
            >
              <Option value="all">Tất cả tình trạng</Option>
              <Option value="disposed">Đã thanh lý (Disposed)</Option>
              <Option value="liquidated">Đã thanh lý (Liquidated)</Option>
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
              {loading ? (
                <tr>
                  <td colSpan={9} className="liquidation-table-empty">
                    Đang tải dữ liệu...
                  </td>
                </tr>
              ) : error ? (
                <tr>
                  <td colSpan={9} className="liquidation-table-empty">
                    {error}
                  </td>
                </tr>
              ) : filteredData.length === 0 ? (
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
                          row.statusName === 'Disposed' || row.statusName === 'Liquidated'
                            ? 'asset-status-pill asset-status-pill--inactive'
                            : 'asset-status-pill asset-status-pill--inactive'
                        }
                      >
                        {getStatusLabel(row.statusName)}
                      </span>
                    </td>
                    <td className="asset-table__cell asset-table__cell--actions">
                      <Button
                        type="text"
                        icon={<EyeOutlined />}
                        onClick={() => navigate(`/assets/${row.catalogAssetId}`)}
                      />
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


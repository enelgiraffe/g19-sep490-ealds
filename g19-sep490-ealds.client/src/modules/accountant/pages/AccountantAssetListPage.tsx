import { useEffect, useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { Button, Modal, message } from 'antd';
import { DownloadOutlined, PlusOutlined } from '@ant-design/icons';
import {
  assetInstanceService,
  formatVnd,
  getStatusLabel,
  type AssetInstanceResponse,
  type GetAssetInstancesParams,
} from '../../assets/services/assetService';
import '../../assets/pages/AssetListPage.css';
import './AccountantAssetListPage.css';

interface AccountantAssetRow {
  key: string;
  id: number;
  /** Catalog Asset id for /assets/:id */
  catalogAssetId: number;
  code: string;
  name: string;
  type: string;
  location: string;
  quantity: number;
  price: string;
  status: 'in-use' | 'pending-use';
  statusLabel: string;
  depreciation: string;
}

function mapAssetToRow(a: AssetInstanceResponse): AccountantAssetRow {
  const isInUse = a.status === 1; // InUse
  const depValue = Math.max(0, a.originalPrice - a.currentValue);
  return {
    key: String(a.assetInstanceId),
    id: a.assetInstanceId,
    catalogAssetId: a.assetId,
    code: a.instanceCode,
    name: a.assetName ?? a.assetCode ?? a.instanceCode,
    type: '—',
    location: a.warehouseName ?? '—',
    quantity: 1,
    price: formatVnd(a.currentValue),
    status: isInUse ? 'in-use' : 'pending-use',
    statusLabel: getStatusLabel(a.statusName),
    depreciation: formatVnd(depValue),
  };
}

export function AccountantAssetListPage() {
  const [data, setData] = useState<AccountantAssetRow[]>([]);
  const [loading, setLoading] = useState(true);
  const [searchInput, setSearchInput] = useState('');
  const [keyword, setKeyword] = useState('');
  const [statusFilter, setStatusFilter] = useState<number | undefined>(undefined);
  const [assetTypeFilter, setAssetTypeFilter] = useState<number | undefined>(undefined);
  const [refreshKey, setRefreshKey] = useState(0);
  const navigate = useNavigate();

  useEffect(() => {
    const t = setTimeout(() => setKeyword(searchInput.trim()), 400);
    return () => clearTimeout(t);
  }, [searchInput]);

  useEffect(() => {
    let cancelled = false;
    setLoading(true);
    const params: GetAssetInstancesParams = {
      keyword: keyword || undefined,
      status: statusFilter,
      assetTypeId: assetTypeFilter,
    };
    assetInstanceService
      .getAll(params)
      .then((list) => {
        if (!cancelled) setData(list.map(mapAssetToRow));
      })
      .catch(() => {
        if (!cancelled) {
          message.error('Không tải được danh sách tài sản.');
          setData([]);
        }
      })
      .finally(() => {
        if (!cancelled) setLoading(false);
      });
    return () => {
      cancelled = true;
    };
  }, [keyword, statusFilter, assetTypeFilter, refreshKey]);

  const handleDeleteAsset = (asset: AccountantAssetRow) => {
    Modal.confirm({
      title: 'Xóa tài sản',
      content: `Bạn có chắc muốn xóa tài sản "${asset.name}" (${asset.code})?`,
      okText: 'Xóa',
      okType: 'danger',
      cancelText: 'Hủy',
      async onOk() {
        try {
          await assetInstanceService.softDelete(asset.id, { status: 4, reason: null });
          message.success('Đã gửi yêu cầu xóa (Disposed) lên hệ thống.');
          setRefreshKey((k) => k + 1);
        } catch {
          message.error('Xóa tài sản thất bại. Vui lòng thử lại.');
        }
      },
    });
  };

  const statusPillClass = (row: AccountantAssetRow) =>
    row.status === 'in-use'
      ? 'asset-status-pill asset-status-pill--active'
      : 'asset-status-pill asset-status-pill--inactive';

  return (
    <div className="asset-page">
      <div className="accountant-asset-title-row">
        <h1 className="asset-page__title">Tài sản</h1>
        <div className="accountant-asset-header-right">
          <Button
            type="primary"
            icon={<PlusOutlined />}
            className="accountant-asset-btn-add"
            onClick={() => navigate('/assets/new')}
          >
            Thêm tài sản
          </Button>
          <Button
            icon={<DownloadOutlined />}
            className="accountant-asset-btn-template"
          >
            Template ghi tăng
          </Button>
        </div>
      </div>
      <div className="asset-card">
        <div className="asset-card__header">
          <div className="accountant-asset-header">
            <div className="accountant-asset-header-left">
              <input
                type="text"
                className="asset-search-input"
                placeholder="Tìm kiếm"
                value={searchInput}
                onChange={(e) => setSearchInput(e.target.value)}
              />
              <select
                className="asset-filter-select"
                value={assetTypeFilter ?? ''}
                onChange={(e) => {
                  const v = e.target.value;
                  setAssetTypeFilter(v === '' ? undefined : Number(v));
                }}
              >
                <option value="">Tất cả loại tài sản</option>
                <option value={1}>Máy móc</option>
                <option value={2}>Thiết bị</option>
              </select>
              <select
                className="asset-filter-select"
                value={statusFilter ?? ''}
                onChange={(e) => {
                  const v = e.target.value;
                  setStatusFilter(v === '' ? undefined : Number(v));
                }}
              >
                <option value="">Tất cả trạng thái</option>
                <option value={0}>Sẵn có</option>
                <option value={1}>Đang sử dụng</option>
                <option value={2}>Đang bảo trì</option>
              </select>
              <button
                className="asset-filter-reset"
                type="button"
                onClick={() => {
                  setSearchInput('');
                  setKeyword('');
                  setStatusFilter(undefined);
                  setAssetTypeFilter(undefined);
                }}
              >
                Gỡ bộ lọc
              </button>
              <button className="asset-filter-settings" aria-label="Cài đặt hiển thị">
                ⚙
              </button>
            </div>
          </div>
        </div>

        <div className="asset-table-wrapper">
          <table className="asset-table">
            <thead>
              <tr>
                <th className="asset-table__cell asset-table__cell--checkbox">
                  <input type="checkbox" />
                </th>
                <th>MÃ TÀI SẢN</th>
                <th>TÊN TÀI SẢN</th>
                <th>LOẠI TÀI SẢN</th>
                <th>VỊ TRÍ TÀI SẢN</th>
                <th>SỐ LƯỢNG</th>
                <th>GIÁ</th>
                <th>TRẠNG THÁI</th>
                <th>GIÁ TRỊ KHẤU HAO</th>
                <th className="asset-table__cell asset-table__cell--actions" />
              </tr>
            </thead>
            <tbody>
              {data.length === 0 ? (
                <tr>
                  <td colSpan={10} style={{ textAlign: 'center', padding: '16px' }}>
                    Không có dữ liệu.
                  </td>
                </tr>
              ) : (
                data.map((row) => (
                  <tr key={row.key} className="asset-row">
                    <td className="asset-table__cell asset-table__cell--checkbox">
                      <input type="checkbox" />
                    </td>
                    <td>
                      <button
                        type="button"
                        className="asset-code asset-code--link"
                        onClick={() => navigate(`/assets/${row.catalogAssetId}`)}
                      >
                        {row.code}
                      </button>
                    </td>
                    <td>{row.name}</td>
                    <td>{row.type}</td>
                    <td>{row.location}</td>
                    <td className="asset-align-right">{row.quantity}</td>
                    <td className="asset-align-right">{row.price}</td>
                    <td>
                      <span className={statusPillClass(row)}>{row.statusLabel}</span>
                    </td>
                    <td className="asset-align-right">{row.depreciation}</td>
                    <td className="asset-table__cell asset-table__cell--actions">
                      <button
                        type="button"
                        className="asset-row__more-btn"
                        onClick={(e) => {
                          e.stopPropagation();
                          handleDeleteAsset(row);
                        }}
                        title="Xóa"
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

        <div className="asset-card__footer">
          <div className="asset-footer__left">
            Số lượng trên trang:
            <select className="asset-footer__select" defaultValue={25}>
              <option value={10}>10</option>
              <option value={25}>25</option>
              <option value={50}>50</option>
              <option value={100}>100</option>
            </select>
          </div>
          <div className="asset-footer__center">1-25 trên {data.length}</div>
          <div className="asset-footer__right">
            <button className="asset-footer__pager" disabled type="button">
              ⟨
            </button>
            <button
              className="asset-footer__pager asset-footer__pager--active"
              type="button"
            >
              1
            </button>
            <button className="asset-footer__pager" type="button">
              ⟩
            </button>
          </div>
        </div>
      </div>
    </div>
  );
}

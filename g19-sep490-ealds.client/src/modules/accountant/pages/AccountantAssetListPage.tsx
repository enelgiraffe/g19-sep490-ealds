import { useEffect, useMemo, useState } from 'react';
import { Link, useNavigate } from 'react-router-dom';
import { Button, message } from 'antd';
import { DownloadOutlined, EditOutlined, EyeOutlined, PlusOutlined } from '@ant-design/icons';
import {
  assetInstanceService,
  getStatusLabel,
  type AssetInstanceResponse,
  type GetAssetInstancesParams,
} from '../../assets/services/assetService';
import '../../assets/pages/AssetListPage.css';
import './AccountantAssetListPage.css';

interface AccountantAssetItem {
  id: number;
  code: string;
  name: string;
  type: string;
  quantity: number;
}

interface AccountantInstanceItem {
  assetInstanceId: number;
  instanceCode: string;
  serialNumber: string;
  status: string;
  originalPrice: string;
  currentValue: string;
  statusColor: 'green' | 'gray';
}

function formatVnd(value: number): string {
  return (
    new Intl.NumberFormat('vi-VN', {
      style: 'decimal',
      minimumFractionDigits: 0,
    }).format(value) + ' đ'
  );
}

function mapInstanceToItem(a: AssetInstanceResponse): AccountantInstanceItem {
  const statusName = a.statusName ?? 'Available';
  const activeStatuses = ['Available', 'InUse', 'InMaintenance', 'Reserved'];
  const statusColor: 'green' | 'gray' =
    activeStatuses.includes(statusName) ? 'green' : 'gray';
  return {
    assetInstanceId: a.assetInstanceId,
    instanceCode: a.instanceCode,
    serialNumber: a.serialNumber ?? '—',
    status: getStatusLabel(statusName),
    originalPrice: formatVnd(a.originalPrice),
    currentValue: formatVnd(a.currentValue),
    statusColor,
  };
}

export function AccountantAssetListPage() {
  const [allInstances, setAllInstances] = useState<AssetInstanceResponse[]>([]);
  const [loading, setLoading] = useState(true);
  const [searchInput, setSearchInput] = useState('');
  const [keyword, setKeyword] = useState('');
  const [statusFilter, setStatusFilter] = useState<number | undefined>(undefined);
  const [assetTypeFilter, setAssetTypeFilter] = useState<number | undefined>(undefined);
  const [expandedAssetId, setExpandedAssetId] = useState<number | null>(null);
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
        if (!cancelled) setAllInstances(list);
      })
      .catch(() => {
        if (!cancelled) {
          message.error('Không tải được danh sách tài sản.');
          setAllInstances([]);
        }
      })
      .finally(() => {
        if (!cancelled) setLoading(false);
      });
    return () => {
      cancelled = true;
    };
  }, [keyword, statusFilter, assetTypeFilter]);

  const assets: AccountantAssetItem[] = useMemo(() => {
    const grouped = new Map<number, AccountantAssetItem>();
    for (const item of allInstances) {
      const current = grouped.get(item.assetId);
      const typeLabel = item.assetTypeName?.trim() || '—';
      if (current) {
        current.quantity += 1;
        if (current.type === '—' && typeLabel !== '—') {
          current.type = typeLabel;
        }
      } else {
        grouped.set(item.assetId, {
          id: item.assetId,
          code: item.assetCode ?? item.instanceCode,
          name: item.assetName ?? item.assetCode ?? item.instanceCode,
          type: typeLabel,
          quantity: 1,
        });
      }
    }
    return Array.from(grouped.values());
  }, [allInstances]);

  const instancesMap = useMemo(() => {
    const grouped: Record<number, AccountantInstanceItem[]> = {};
    for (const item of allInstances) {
      const key = item.assetId;
      if (!grouped[key]) grouped[key] = [];
      grouped[key].push(mapInstanceToItem(item));
    }
    return grouped;
  }, [allInstances]);

  const handleAssetCodeClick = (assetId: number) => {
    setExpandedAssetId((prev) => (prev === assetId ? null : assetId));
  };
  const selectedListAsset =
    expandedAssetId != null ? assets.find((a) => a.id === expandedAssetId) : undefined;

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

        <div
          className={
            expandedAssetId != null
              ? 'asset-list-split asset-list-split--with-panel'
              : 'asset-list-split'
          }
        >
          <div className="asset-list-split__top">
            <div className="asset-table-wrapper">
              <table className="asset-table asset-table--compact">
                <thead>
                  <tr>
                    <th className="asset-table__cell asset-table__cell--stt">STT</th>
                    <th>MÃ TÀI SẢN</th>
                    <th>TÊN TÀI SẢN</th>
                    <th>LOẠI TÀI SẢN</th>
                    <th>SỐ LƯỢNG</th>
                    <th className="asset-table__cell asset-table__cell--actions" />
                  </tr>
                </thead>
                <tbody>
                  {assets.length === 0 ? (
                    <tr>
                      <td colSpan={6} style={{ textAlign: 'center', padding: '16px' }}>
                        Không có dữ liệu.
                      </td>
                    </tr>
                  ) : (
                    assets.map((asset, index) => (
                      <tr
                        key={asset.id}
                        className={
                          expandedAssetId === asset.id
                            ? 'asset-row asset-row--selected'
                            : 'asset-row'
                        }
                      >
                        <td className="asset-table__cell asset-table__cell--stt">{index + 1}</td>
                        <td>
                          <button
                            type="button"
                            className="asset-code asset-code--link"
                            onClick={() => handleAssetCodeClick(asset.id)}
                          >
                            {expandedAssetId === asset.id ? '▼ ' : '▶ '}
                            {asset.code}
                          </button>
                        </td>
                        <td>{asset.name}</td>
                        <td>{asset.type}</td>
                        <td className="asset-align-right">{asset.quantity}</td>
                        <td className="asset-table__cell asset-table__cell--actions">
                          <div className="asset-row__more">
                            <button
                              type="button"
                              className="asset-row__more-btn asset-row__more-btn--icon"
                              aria-label="Xem chi tiết"
                              onClick={() => navigate(`/assets/${asset.id}`)}
                            >
                              <EyeOutlined />
                            </button>
                          </div>
                        </td>
                      </tr>
                    ))
                  )}
                </tbody>
              </table>
            </div>
          </div>
          {expandedAssetId != null && (
            <div className="asset-list-split__bottom">
              <div className="asset-instances-panel">
                <div className="asset-instances-panel__header">
                  Cá thể tài sản: <strong>{selectedListAsset?.code ?? expandedAssetId}</strong>
                  {selectedListAsset?.name ? ` — ${selectedListAsset.name}` : null}
                </div>
                <div className="asset-instances-panel__body">
                  {instancesMap[expandedAssetId]?.length ? (
                    <div className="asset-table-wrapper asset-table-wrapper--panel">
                      <table className="asset-table asset-table--panel">
                        <thead>
                          <tr>
                            <th className="asset-table__cell asset-table__cell--stt">STT</th>
                            <th>MÃ CÁ THỂ</th>
                            <th>SERIAL NUMBER</th>
                            <th>TRẠNG THÁI</th>
                            <th>GIÁ GỐC</th>
                            <th>GIÁ TRỊ HIỆN TẠI</th>
                            <th className="asset-table__cell asset-table__cell--actions" />
                          </tr>
                        </thead>
                        <tbody>
                          {instancesMap[expandedAssetId].map((instance, index) => (
                            <tr key={instance.assetInstanceId} className="asset-instance-row">
                              <td className="asset-table__cell asset-table__cell--stt">{index + 1}</td>
                              <td>
                                <Link
                                  className="asset-code asset-code--link"
                                  to={`/asset-instances/${instance.assetInstanceId}`}
                                  state={{
                                    backToPath: '/accountant-assets',
                                    backLabel: '← Quay lại danh sách tài sản',
                                  }}
                                >
                                  {instance.instanceCode}
                                </Link>
                              </td>
                              <td>{instance.serialNumber}</td>
                              <td>
                                <span
                                  className={
                                    instance.statusColor === 'green'
                                      ? 'asset-status-pill asset-status-pill--active'
                                      : 'asset-status-pill asset-status-pill--inactive'
                                  }
                                >
                                  {instance.status}
                                </span>
                              </td>
                              <td className="asset-align-right">{instance.originalPrice}</td>
                              <td className="asset-align-right">{instance.currentValue}</td>
                              <td className="asset-table__cell asset-table__cell--actions">
                                <button
                                  type="button"
                                  className="asset-row__more-btn asset-row__more-btn--icon"
                                  aria-label="Sửa thông tin cá thể"
                                  title="Sửa thông tin cá thể"
                                  onClick={() =>
                                    navigate(`/asset-instances/${instance.assetInstanceId}/edit`)
                                  }
                                >
                                  <EditOutlined />
                                </button>
                              </td>
                            </tr>
                          ))}
                        </tbody>
                      </table>
                    </div>
                  ) : (
                    <p className="asset-instances-panel__empty">Không có cá thể nào.</p>
                  )}
                </div>
              </div>
            </div>
          )}
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
          <div className="asset-footer__center">1-25 trên {assets.length}</div>
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

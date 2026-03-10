import { useCallback, useEffect, useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { message } from 'antd';
import {
  assetService,
  formatVnd,
  getStatusLabel,
  type AssetResponse,
} from '../services/assetService';
import { transferRequestService } from '../services/transferRequestService';
import { maintenanceRequestService } from '../services/maintenanceRequestService';
import { MarkDamagedAssetModal } from '../components/MarkDamagedAssetModal';
import { LiquidationRequestModal } from '../components/LiquidationRequestModal';
import { TransferAssetModal } from '../components/TransferAssetModal';
import { MaintenanceProposalModal } from '../components/MaintenanceProposalModal';
import { profileService, type UserProfile } from '../../profile/services/profileService';
import './AssetListPage.css';

interface AssetItem {
  id: number;
  code: string;
  name: string;
  type: string;
  quantity: number;
  price: string;
  status: string;
  statusColor: 'green' | 'gray';
  depreciation: string;
}

interface AssetInfo {
  code: string;
  name: string;
  type: string;
  specification: string;
  purchaseDate: string;
  warrantyExpiry: string;
  currentValue: string;
  remainingValue: string;
  location: string;
  status: string;
  admissionDate: string;
  department: string;
  currentDepartmentId?: number | null;
}

function getStoredUserId(): number | null {
  const raw = localStorage.getItem('user');
  if (!raw) return null;
  try {
    const parsed = JSON.parse(raw) as { id?: string | number | null };
    const idNum = typeof parsed.id === 'number' ? parsed.id : Number(parsed.id);
    return Number.isFinite(idNum) && idNum > 0 ? idNum : null;
  } catch {
    return null;
  }
}

function formatDate(iso?: string | null): string {
  if (!iso) return '—';
  try {
    const d = new Date(iso);
    return d.toLocaleDateString('vi-VN');
  } catch {
    return iso;
  }
}

function mapResponseToItem(a: AssetResponse): AssetItem {
  const statusName = a.statusName ?? 'Available';
  const activeStatuses = ['Available', 'InUse', 'InMaintenance', 'Reserved'];
  const statusColor: 'green' | 'gray' =
    activeStatuses.includes(statusName) ? 'green' : 'gray';
  return {
    id: a.assetId,
    code: a.code,
    name: a.name,
    type: a.assetTypeName ?? '—',
    quantity: a.quantity,
    price: formatVnd(a.currentValue),
    status: getStatusLabel(statusName),
    statusColor,
    depreciation: formatVnd(a.currentValue),
  };
}

function assetResponseToAssetInfo(a: AssetResponse): AssetInfo {
  return {
    code: a.code,
    name: a.name,
    type: a.assetTypeName ?? '—',
    specification: '—',
    purchaseDate: formatDate(a.purchaseDate),
    warrantyExpiry: formatDate(a.warrantyEndDate),
    currentValue: formatVnd(a.currentValue),
    remainingValue: formatVnd(a.currentValue),
    location: a.warehouseName ?? '—',
    status: getStatusLabel(a.statusName),
    admissionDate: formatDate(a.inUseDate),
    department: a.currentDepartmentName ?? '—',
    currentDepartmentId: a.currentDepartmentId ?? null,
  };
}

export function AssetListPage() {
  const [apiAssets, setApiAssets] = useState<AssetResponse[] | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [openMenuId, setOpenMenuId] = useState<number | null>(null);
  const [profile, setProfile] = useState<UserProfile | null>(null);
  const [isMarkDamagedModalOpen, setIsMarkDamagedModalOpen] = useState(false);
  const [isLiquidationModalOpen, setIsLiquidationModalOpen] = useState(false);
  const [isTransferModalOpen, setIsTransferModalOpen] = useState(false);
  const [isMaintenanceModalOpen, setIsMaintenanceModalOpen] = useState(false);
  const [selectedAssetInfo, setSelectedAssetInfo] = useState<AssetInfo | null>(null);
  const [transferAssetId, setTransferAssetId] = useState<number | null>(null);
  const [maintenanceAssetId, setMaintenanceAssetId] = useState<number | null>(null);
  const navigate = useNavigate();

  const assets: AssetItem[] = apiAssets?.map(mapResponseToItem) ?? [];

  const fetchAssets = useCallback(async () => {
    setLoading(true);
    setError(null);
    try {
      const data = await assetService.getAll();
      setApiAssets(data);
    } catch {
      setError('Không tải được danh sách tài sản. Kiểm tra kết nối backend.');
    } finally {
      setLoading(false);
    }
  }, []);

  useEffect(() => {
    fetchAssets();
  }, [fetchAssets]);

  useEffect(() => {
    async function loadProfile() {
      try {
        const p = await profileService.getProfile();
        setProfile(p);
      } catch {
        // Không chặn trang tài sản nếu không lấy được profile
      }
    }
    loadProfile();
  }, []);

  useEffect(() => {
    function handleClickOutside(e: MouseEvent) {
      const target = e.target as HTMLElement;
      if (
        !target.closest('.asset-row-menu') &&
        !target.closest('.asset-row__more-btn')
      ) {
        setOpenMenuId(null);
      }
    }

    document.addEventListener('click', handleClickOutside);
    return () => {
      document.removeEventListener('click', handleClickOutside);
    };
  }, []);

  const handleToggleMenu = (id: number) => {
    setOpenMenuId((current) => (current === id ? null : id));
  };

  const handleMenuAction = (actionKey: string, asset: AssetItem) => {
    setOpenMenuId(null);
    const raw = apiAssets?.find((a) => a.assetId === asset.id);
    if (actionKey === 'move' && raw) {
      setSelectedAssetInfo(assetResponseToAssetInfo(raw));
      setTransferAssetId(asset.id);
      setIsTransferModalOpen(true);
    } else if (actionKey === 'mark-broken' && raw) {
      setSelectedAssetInfo(assetResponseToAssetInfo(raw));
      setIsMarkDamagedModalOpen(true);
    } else if (actionKey === 'liquidate' && raw) {
      setSelectedAssetInfo(assetResponseToAssetInfo(raw));
      setIsLiquidationModalOpen(true);
    } else if (actionKey === 'maintenance' && raw) {
      setSelectedAssetInfo(assetResponseToAssetInfo(raw));
      setMaintenanceAssetId(asset.id);
      setIsMaintenanceModalOpen(true);
    } else {
      console.log('Action', actionKey, 'for asset', asset);
    }
  };

  const handleCloseMarkDamagedModal = () => {
    setIsMarkDamagedModalOpen(false);
    setSelectedAssetInfo(null);
  };

  const handleCloseLiquidationModal = () => {
    setIsLiquidationModalOpen(false);
    setSelectedAssetInfo(null);
  };

  const handleCloseTransferModal = () => {
    setIsTransferModalOpen(false);
    setSelectedAssetInfo(null);
    setTransferAssetId(null);
  };

  const handleCloseMaintenanceModal = () => {
    setIsMaintenanceModalOpen(false);
    setSelectedAssetInfo(null);
    setMaintenanceAssetId(null);
  };

  const handleSubmitMarkDamaged = (values: unknown) => {
    console.log('Mark damaged:', values);
    // TODO: Call API to mark asset as damaged
  };

  const handleSubmitLiquidation = (values: unknown) => {
    console.log('Liquidation request:', values);
    // TODO: Call API to submit liquidation request
  };

  const handleSubmitMaintenance = async (values: {
    assetId: number;
    recordNumber?: string;
    maintenanceContent: string;
  }) => {
    const createdBy = profile?.id ?? getStoredUserId();
    if (!createdBy || values.assetId == null) {
      message.error('Không xác định được người dùng hoặc tài sản để gửi đề xuất bảo dưỡng.');
      return;
    }
    try {
      await maintenanceRequestService.create({
        assetId: values.assetId,
        requestTypeId: 2,
        createdBy,
        title: values.recordNumber
          ? `Bảo dưỡng - Số biên bản ${values.recordNumber}`
          : 'Đề xuất bảo dưỡng máy móc',
        description: values.maintenanceContent || undefined,
      });
      message.success('Gửi đề xuất bảo dưỡng thành công.');
      handleCloseMaintenanceModal();
    } catch (e: any) {
      const data = e?.response?.data;
      const errors = data?.errors as Record<string, string[] | string> | undefined;
      if (errors && typeof errors === 'object') {
        const flat = Object.entries(errors)
          .flatMap(([k, v]) => {
            if (Array.isArray(v)) return v.map((m) => `${k}: ${m}`);
            if (typeof v === 'string') return [`${k}: ${v}`];
            return [];
          })
          .filter(Boolean);
        message.error(flat.join(' | ') || data?.title || 'Gửi đề xuất bảo dưỡng thất bại.');
      } else {
        const msg =
          data?.title ??
          data ??
          'Gửi đề xuất bảo dưỡng thất bại.';
        message.error(typeof msg === 'string' ? msg : 'Gửi đề xuất bảo dưỡng thất bại.');
      }
    }
  };

  const handleSubmitTransfer = async (values: any) => {
    if (!profile || transferAssetId == null) {
      message.error('Không xác định được người dùng hoặc tài sản để điều chuyển.');
      return;
    }
    try {
      const fromLocationId = Number(values.fromLocationId);
      const toLocationId = Number(values.toLocationId);
      if (!fromLocationId || !toLocationId) {
        message.error('Vui lòng nhập ID vị trí hợp lệ.');
        return;
      }

      const transferDateValue = values.transferDate;
      const transferDate =
        transferDateValue && typeof transferDateValue.toISOString === 'function'
          ? transferDateValue.toISOString()
          : undefined;

      await transferRequestService.create({
        assetId: transferAssetId,
        requestTypeId: 1, // Tạm dùng cùng RequestTypeId với đơn mua
        fromLocationId,
        toLocationId,
        fromUserId: profile.id,
        toUserId: null,
        transferDate,
        executeBy: profile.id,
        createdBy: profile.id,
        title: values.reason
          ? `Điều chuyển: ${values.reason}`
          : `Yêu cầu điều chuyển tài sản ${transferAssetId}`,
        description: values.reason ?? undefined,
      });

      message.success('Gửi yêu cầu điều chuyển thành công.');
      handleCloseTransferModal();
    } catch (e: any) {
      const msg = e?.response?.data ?? 'Gửi yêu cầu điều chuyển thất bại.';
      message.error(msg);
    }
  };

  if (loading) {
    return (
      <div className="asset-page">
        <h1 className="asset-page__title">Tài sản</h1>
        <div className="asset-card">
          <p>Đang tải danh sách tài sản...</p>
        </div>
      </div>
    );
  }

  if (error) {
    return (
      <div className="asset-page">
        <h1 className="asset-page__title">Tài sản</h1>
        <div className="asset-card">
          <p>{error}</p>
          <button type="button" onClick={() => fetchAssets()}>
            Thử lại
          </button>
        </div>
      </div>
    );
  }

  return (
    <div className="asset-page">
      <h1 className="asset-page__title">Tài sản</h1>
      <div className="asset-card">
        <div className="asset-card__header">
          <div className="asset-card__search-group">
            <input
              type="text"
              className="asset-search-input"
              placeholder="Tìm kiếm"
            />
          </div>
          <div className="asset-card__filters">
            <select className="asset-filter-select">
              <option>Loại tài sản</option>
            </select>
            <select className="asset-filter-select">
              <option>Trạng thái</option>
            </select>
            <select className="asset-filter-select">
              <option>Giá</option>
            </select>
            <button className="asset-filter-reset">Gỡ bộ lọc</button>
            <button
              className="asset-filter-settings"
              aria-label="Cài đặt hiển thị"
            >
              ⚙
            </button>
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
                <th>SỐ LƯỢNG</th>
                <th>GIÁ</th>
                <th>TRẠNG THÁI</th>
                <th>GIÁ TRỊ KHẤU HAO</th>
                <th className="asset-table__cell asset-table__cell--actions" />
              </tr>
            </thead>
            <tbody>
              {assets.map((asset) => (
                <tr key={asset.id} className="asset-row">
                  <td className="asset-table__cell asset-table__cell--checkbox">
                    <input type="checkbox" />
                  </td>
                  <td>
                    <button
                      type="button"
                      className="asset-code asset-code--link"
                      onClick={() => navigate(`/assets/${asset.id}`)}
                    >
                      {asset.code}
                    </button>
                  </td>
                  <td>{asset.name}</td>
                  <td>{asset.type}</td>
                  <td className="asset-align-right">{asset.quantity}</td>
                  <td className="asset-align-right">{asset.price}</td>
                  <td>
                    <span
                      className={
                        asset.statusColor === 'green'
                          ? 'asset-status-pill asset-status-pill--active'
                          : 'asset-status-pill asset-status-pill--inactive'
                      }
                    >
                      {asset.status}
                    </span>
                  </td>
                  <td className="asset-align-right">{asset.depreciation}</td>
                  <td className="asset-table__cell asset-table__cell--actions">
                    <div className="asset-row__more">
                      <button
                        type="button"
                        className="asset-row__more-btn"
                        onClick={(e) => {
                          e.stopPropagation();
                          handleToggleMenu(asset.id);
                        }}
                      >
                        ⋯
                      </button>
                      {openMenuId === asset.id && (
                        <div className="asset-row-menu">
                          <button
                            className="asset-row-menu__item"
                            onClick={() => handleMenuAction('move', asset)}
                          >
                            <span className="asset-row-menu__icon">↔</span>
                            <span>Di chuyển</span>
                          </button>
                          <button
                            className="asset-row-menu__item"
                            onClick={() =>
                              handleMenuAction('maintenance', asset)
                            }
                          >
                            <span className="asset-row-menu__icon">🛠</span>
                            <span>Bảo dưỡng</span>
                          </button>
                          <button
                            className="asset-row-menu__item"
                            onClick={() => handleMenuAction('mark-lost', asset)}
                          >
                            <span className="asset-row-menu__icon">−</span>
                            <span>Đánh dấu mất</span>
                          </button>
                          <button
                            className="asset-row-menu__item"
                            onClick={() => handleMenuAction('liquidate', asset)}
                          >
                            <span className="asset-row-menu__icon">$</span>
                            <span>Đề nghị thanh lý</span>
                          </button>
                          <button
                            className="asset-row-menu__item"
                            onClick={() =>
                              handleMenuAction('mark-broken', asset)
                            }
                          >
                            <span className="asset-row-menu__icon">!</span>
                            <span>Đánh dấu hỏng</span>
                          </button>
                          <button
                            className="asset-row-menu__item"
                            onClick={() => handleMenuAction('print-qr', asset)}
                          >
                            <span className="asset-row-menu__icon">▤</span>
                            <span>In mã QR</span>
                          </button>
                          <button
                            className="asset-row-menu__item asset-row-menu__item--danger"
                            onClick={() => handleMenuAction('delete', asset)}
                          >
                            <span className="asset-row-menu__icon">🗑</span>
                            <span>Xóa</span>
                          </button>
                        </div>
                      )}
                    </div>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>

        <div className="asset-card__footer">
          <div className="asset-footer__left">
            Items per page:
            <select className="asset-footer__select">
              <option>25</option>
              <option>50</option>
              <option>100</option>
            </select>
          </div>
          <div className="asset-footer__center">1-25 of 289</div>
          <div className="asset-footer__right">
            <button className="asset-footer__pager" disabled>
              ⟨
            </button>
            <button className="asset-footer__pager asset-footer__pager--active">
              1
            </button>
            <button className="asset-footer__pager">2</button>
            <button className="asset-footer__pager">⟩</button>
          </div>
        </div>
      </div>

      <MarkDamagedAssetModal
        open={isMarkDamagedModalOpen}
        onClose={handleCloseMarkDamagedModal}
        onSubmit={handleSubmitMarkDamaged}
        assetInfo={selectedAssetInfo}
      />

      <LiquidationRequestModal
        open={isLiquidationModalOpen}
        onClose={handleCloseLiquidationModal}
        onSubmit={handleSubmitLiquidation}
        assetInfo={selectedAssetInfo}
      />

      <TransferAssetModal
        open={isTransferModalOpen}
        onClose={handleCloseTransferModal}
        onSubmit={handleSubmitTransfer}
        assetInfo={selectedAssetInfo}
        fromDepartmentId={selectedAssetInfo?.currentDepartmentId ?? null}
      />

      <MaintenanceProposalModal
        open={isMaintenanceModalOpen}
        onClose={handleCloseMaintenanceModal}
        onSubmit={handleSubmitMaintenance}
        assetInfo={selectedAssetInfo}
        assetId={maintenanceAssetId}
      />
    </div>
  );
}


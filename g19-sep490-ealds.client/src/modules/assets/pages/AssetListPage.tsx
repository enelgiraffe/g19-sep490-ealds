import { useCallback, useEffect, useState, type KeyboardEvent } from 'react';
import { useNavigate } from 'react-router-dom';
import { message, Popover, Slider, Button } from 'antd';
import {
  assetInstanceService,
  assetService,
  formatVnd,
  getStatusLabel,
  type AssetInstanceResponse,
  type GetAssetInstancesParams,
  type AssetTypeItem,
} from '../services/assetService';
import { transferRequestService } from '../services/transferRequestService';
import { maintenanceRequestService } from '../services/maintenanceRequestService';
import { damageReportService } from '../services/damageReportService';
import { disposalRequestService } from '../services/disposalRequestService';
import { MarkDamagedAssetModal } from '../components/MarkDamagedAssetModal';
import { LiquidationRequestModal } from '../components/LiquidationRequestModal';
import { TransferAssetModal } from '../components/TransferAssetModal';
import { MaintenanceProposalModal } from '../components/MaintenanceProposalModal';
import { profileService, type UserProfile } from '../../profile/services/profileService';
import './AssetListPage.css';

interface AssetItem {
  /** Physical row id (AssetInstance) */
  id: number;
  /** Catalog Asset id for navigation and requests that still use AssetId */
  catalogAssetId: number;
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
  assetId: number;
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

function mapInstanceToItem(a: AssetInstanceResponse): AssetItem {
  const statusName = a.statusName ?? 'Available';
  const activeStatuses = ['Available', 'InUse', 'InMaintenance', 'Reserved'];
  const statusColor: 'green' | 'gray' =
    activeStatuses.includes(statusName) ? 'green' : 'gray';
  return {
    id: a.assetInstanceId,
    catalogAssetId: a.assetId,
    code: a.instanceCode,
    name: a.assetName ?? a.assetCode ?? a.instanceCode,
    type: '—',
    quantity: 1,
    price: formatVnd(a.currentValue),
    status: getStatusLabel(statusName),
    statusColor,
    depreciation: formatVnd(a.remainingValue ?? a.currentValue),
  };
}

function instanceToAssetInfo(a: AssetInstanceResponse): AssetInfo {
  return {
    assetId: a.assetId,
    code: a.assetCode ?? a.instanceCode,
    name: a.assetName ?? a.instanceCode,
    type: '—',
    specification: a.condition ?? '—',
    purchaseDate: formatDate(a.purchaseDate),
    warrantyExpiry: '—',
    currentValue: formatVnd(a.currentValue),
    remainingValue: formatVnd(a.remainingValue ?? a.currentValue),
    location: a.warehouseName ?? a.currentDepartmentName ?? '—',
    status: getStatusLabel(a.statusName),
    admissionDate: formatDate(a.inUseDate),
    department: a.currentDepartmentName ?? '—',
    currentDepartmentId: a.currentDepartmentId ?? null,
  };
}

function buildPriceFilterLabel(
  minPrice: string,
  maxPrice: string
): string {
  if (!minPrice && !maxPrice) return 'Giá';
  const hasMin = !!minPrice;
  const hasMax = !!maxPrice;
  const minLabel = hasMin ? formatVnd(Number(minPrice)) : 'Tất cả';
  const maxLabel = hasMax ? formatVnd(Number(maxPrice)) : 'Tất cả';
  return `${minLabel} - ${maxLabel}`;
}

export function AssetListPage() {
  const [apiAssets, setApiAssets] = useState<AssetInstanceResponse[] | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [openMenuId, setOpenMenuId] = useState<number | null>(null);
  const [searchInput, setSearchInput] = useState('');
  const [keyword, setKeyword] = useState('');
  const [statusFilter, setStatusFilter] = useState<number | undefined>(undefined);
  const [assetTypeFilter, setAssetTypeFilter] = useState<number | undefined>(undefined);
  const [minPrice, setMinPrice] = useState<string>('');
  const [maxPrice, setMaxPrice] = useState<string>('');
  const [isPriceFilterOpen, setIsPriceFilterOpen] = useState(false);
  const [markDamagedAssetId, setMarkDamagedAssetId] = useState<number | null>(null);
  const [draftPriceRange, setDraftPriceRange] = useState<[number, number] | null>(null);
  const [profile, setProfile] = useState<UserProfile | null>(null);
  const [isMarkDamagedModalOpen, setIsMarkDamagedModalOpen] = useState(false);
  const [isLiquidationModalOpen, setIsLiquidationModalOpen] = useState(false);
  const [isTransferModalOpen, setIsTransferModalOpen] = useState(false);
  const [isMaintenanceModalOpen, setIsMaintenanceModalOpen] = useState(false);
  const [selectedAssetInfo, setSelectedAssetInfo] = useState<AssetInfo | null>(null);
  const [transferAssetId, setTransferAssetId] = useState<number | null>(null);
  const [maintenanceAssetId, setMaintenanceAssetId] = useState<number | null>(null);
  const [assetTypes, setAssetTypes] = useState<AssetTypeItem[]>([]);
  const navigate = useNavigate();

  const assets: AssetItem[] = apiAssets?.map(mapInstanceToItem) ?? [];

  const PRICE_SLIDER_MIN = 0;
  const PRICE_SLIDER_MAX = 1_000_000_000;

  const currentMinNumber = minPrice ? Number(minPrice) : PRICE_SLIDER_MIN;
  const currentMaxNumber = maxPrice ? Number(maxPrice) : PRICE_SLIDER_MAX;

  const effectiveDraftRange: [number, number] =
    draftPriceRange ?? [currentMinNumber, currentMaxNumber];

  const priceButtonLabel = buildPriceFilterLabel(minPrice, maxPrice);

  const handleOpenPriceFilter = (open: boolean) => {
    if (open) {
      setDraftPriceRange([currentMinNumber, currentMaxNumber]);
    }
    setIsPriceFilterOpen(open);
  };

  const handleApplyPriceFilter = () => {
    if (!draftPriceRange) {
      setMinPrice('');
      setMaxPrice('');
    } else {
      const [min, max] = draftPriceRange;
      setMinPrice(min > PRICE_SLIDER_MIN ? String(min) : '');
      setMaxPrice(max < PRICE_SLIDER_MAX ? String(max) : '');
    }
    setIsPriceFilterOpen(false);
  };

  const handleClearPriceFilter = () => {
    setDraftPriceRange([PRICE_SLIDER_MIN, PRICE_SLIDER_MAX]);
    setMinPrice('');
    setMaxPrice('');
    setIsPriceFilterOpen(false);
  };

  const fetchAssets = useCallback(async () => {
    setLoading(true);
    setError(null);
    try {
      const params: GetAssetInstancesParams = {
        keyword: keyword || undefined,
        status: statusFilter,
        assetTypeId: assetTypeFilter,
        minPrice: minPrice ? Number(minPrice) : undefined,
        maxPrice: maxPrice ? Number(maxPrice) : undefined,
      };
      const data = await assetInstanceService.getAll(params);
      setApiAssets(data);
    } catch {
      setError('Không tải được danh sách tài sản. Kiểm tra kết nối backend.');
    } finally {
      setLoading(false);
    }
  }, [keyword, statusFilter, assetTypeFilter, minPrice, maxPrice]);

  useEffect(() => {
    fetchAssets();
  }, [fetchAssets]);

  const handleSearchKeyDown = (e: KeyboardEvent<HTMLInputElement>) => {
    if (e.key === 'Enter') {
      setKeyword(searchInput.trim());
    }
  };

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
    async function loadAssetTypes() {
      try {
        const items = await assetService.getAssetTypes();
        setAssetTypes(items);
      } catch {
        // Nếu lỗi thì vẫn để dropdown hiển thị "Tất cả loại tài sản"
      }
    }
    loadAssetTypes();
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
    const raw = apiAssets?.find((a) => a.assetInstanceId === asset.id);
    if (actionKey === 'move' && raw) {
      setSelectedAssetInfo(instanceToAssetInfo(raw));
      setTransferAssetId(raw.assetId);
      setIsTransferModalOpen(true);
    } else if (actionKey === 'mark-broken' && raw) {
      setSelectedAssetInfo(instanceToAssetInfo(raw));
      setMarkDamagedAssetId(raw.assetId);
      setIsMarkDamagedModalOpen(true);
    } else if (actionKey === 'liquidate' && raw) {
      setSelectedAssetInfo(instanceToAssetInfo(raw));
      setIsLiquidationModalOpen(true);
    } else if (actionKey === 'maintenance' && raw) {
      setSelectedAssetInfo(instanceToAssetInfo(raw));
      setMaintenanceAssetId(raw.assetId);
      setIsMaintenanceModalOpen(true);
    } else {
      console.log('Action', actionKey, 'for asset', asset);
    }
  };

  const handleCloseMarkDamagedModal = () => {
    setIsMarkDamagedModalOpen(false);
    setSelectedAssetInfo(null);
    setMarkDamagedAssetId(null);
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

  const handleSubmitMarkDamaged = async (values: { damageDate: string; condition: string }) => {
    const reportedBy = profile?.id ?? getStoredUserId();
    if (!reportedBy || markDamagedAssetId == null) {
      message.error('Không xác định được người dùng hoặc tài sản để báo hỏng.');
      return;
    }

    const damageDate = values.damageDate?.trim();
    const condition = values.condition?.trim();
    const descriptionParts = [
      damageDate ? `Ngày hỏng: ${damageDate}` : null,
      condition ? `Tình trạng: ${condition}` : null,
    ].filter(Boolean);

    try {
      await damageReportService.report({
        assetId: markDamagedAssetId,
        reportedBy,
        requestTypeId: undefined,
        reportDate: damageDate
          ? new Date(damageDate).toISOString()
          : new Date().toISOString(),
        description: descriptionParts.join('\n') || null,
        severity: null,
        documentId: null,
      });
      message.success('Gửi báo hỏng thành công.');
      await fetchAssets();
      handleCloseMarkDamagedModal();
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
        message.error(flat.join(' | ') || data?.title || 'Gửi báo hỏng thất bại.');
      } else {
        const msg = data?.title ?? data ?? 'Gửi báo hỏng thất bại.';
        message.error(typeof msg === 'string' ? msg : 'Gửi báo hỏng thất bại.');
      }
    }
  };

  const handleSubmitLiquidation = async (values: {
    liquidationDate: Date | null;
    reason?: string;
    disposalMethod?: string;
    notes?: string;
  }) => {
    const createdBy = profile?.id ?? getStoredUserId();
    const assetId = selectedAssetInfo?.assetId;
    if (!createdBy || !assetId) {
      message.error('Không xác định được người dùng hoặc tài sản để gửi đề nghị thanh lý.');
      return;
    }

    const disposalMethod = Number(values.disposalMethod);
    const methodValue = Number.isFinite(disposalMethod) ? disposalMethod : 0;
    const submittedAt = values.liquidationDate ?? new Date();
    const noteText = values.notes?.trim();
    const reasonText = values.reason?.trim();

    try {
      await disposalRequestService.create({
        userId: createdBy,
        assetId,
        createdBy,
        title: `Đề nghị thanh lý tài sản ${selectedAssetInfo?.code ?? assetId}`,
        description: noteText || null,
        diposalMethod: methodValue,
        diposalValue: 0,
        diposalDate: submittedAt.toISOString(),
        reason: reasonText || null,
      });
      message.success('Gửi yêu cầu thanh lý thành công.');
      await fetchAssets();
      handleCloseLiquidationModal();
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
        message.error(flat.join(' | ') || data?.title || 'Gửi yêu cầu thanh lý thất bại.');
      } else if (e?.response?.status === 403) {
        const actorUserId = data?.actorUserId;
        const roleNames = Array.isArray(data?.currentUserRoleNames)
          ? data.currentUserRoleNames.filter(Boolean).join(', ')
          : '';
        const detail = [
          data?.message || 'Bạn không có quyền gửi yêu cầu thanh lý (chỉ trưởng phòng ban).',
          actorUserId ? `UserId token: ${actorUserId}` : null,
          roleNames ? `Role hiện tại: ${roleNames}` : null,
        ]
          .filter(Boolean)
          .join(' | ');
        message.error(detail);
      } else {
        const msg = data?.title ?? data ?? 'Gửi yêu cầu thanh lý thất bại.';
        message.error(typeof msg === 'string' ? msg : 'Gửi yêu cầu thanh lý thất bại.');
      }
    }
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
    if (!profile) {
      message.error('Không xác định được người dùng hoặc tài sản để điều chuyển.');
      return;
    }
    try {
      const assetIds: number[] =
        Array.isArray(values.assetIds) && values.assetIds.length > 0
          ? values.assetIds.map((x: any) => Number(x)).filter((n: number) => Number.isFinite(n) && n > 0)
          : transferAssetId != null
            ? [transferAssetId]
            : [];
      if (assetIds.length === 0) {
        message.error('Vui lòng chọn ít nhất 1 tài sản.');
        return;
      }

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

      for (const assetId of assetIds) {
        await transferRequestService.create({
          assetId,
          requestTypeId: 1, // TODO: Align with backend request type config if needed
          fromLocationId,
          toLocationId,
          fromUserId: profile.id,
          toUserId: null,
          transferDate,
          executeBy: profile.id,
          createdBy: profile.id,
          title: values.reason
            ? `Điều chuyển: ${values.reason}`
            : `Yêu cầu điều chuyển tài sản ${assetId}`,
          description: values.reason ?? undefined,
        });
      }

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
              value={searchInput}
              onChange={(e) => setSearchInput(e.target.value)}
              onKeyDown={handleSearchKeyDown}
            />
          </div>
          <div className="asset-card__filters">
            <select
              className="asset-filter-select"
              value={assetTypeFilter ?? ''}
              onChange={(e) => {
                const v = e.target.value;
                setAssetTypeFilter(v === '' ? undefined : Number(v));
              }}
            >
              <option value="">Tất cả loại tài sản</option>
              {assetTypes.map((t) => (
                <option key={t.assetTypeId} value={t.assetTypeId}>
                  {t.name}
                </option>
              ))}
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
              <option value={3}>Đã đặt trước</option>
              <option value={4}>Đã thanh lý</option>
              <option value={5}>Mất</option>
              <option value={6}>Đã thanh lý</option>
              <option value={7}>Đã ghi tăng</option>
            </select>
            <Popover
              open={isPriceFilterOpen}
              onOpenChange={handleOpenPriceFilter}
              trigger="click"
              placement="bottomRight"
              overlayClassName="asset-price-popover"
              content={
                <div className="asset-price-popover__content">
                  <Slider
                    range
                    min={PRICE_SLIDER_MIN}
                    max={PRICE_SLIDER_MAX}
                    step={1_000_000}
                    value={effectiveDraftRange}
                    onChange={(value) => {
                      const [min, max] = value as [number, number];
                      setDraftPriceRange([min, max]);
                    }}
                  />
                  <div className="asset-price-popover__labels">
                    <div className="asset-price-label">
                      {formatVnd(effectiveDraftRange[0])}
                    </div>
                    <span className="asset-price-label-separator">-</span>
                    <div className="asset-price-label">
                      {formatVnd(effectiveDraftRange[1])}
                    </div>
                  </div>
                  <Button
                    type="primary"
                    block
                    className="asset-price-popover__apply"
                    onClick={handleApplyPriceFilter}
                  >
                    Áp dụng
                  </Button>
                  <button
                    type="button"
                    className="asset-price-popover__clear"
                    onClick={handleClearPriceFilter}
                  >
                    Xóa bộ lọc
                  </button>
                </div>
              }
            >
              <button type="button" className="asset-filter-select">
                {priceButtonLabel}
              </button>
            </Popover>
            <button
              className="asset-filter-reset"
              type="button"
              onClick={() => {
                setSearchInput('');
                setKeyword('');
                setStatusFilter(undefined);
                setAssetTypeFilter(undefined);
                setMinPrice('');
                setMaxPrice('');
                setDraftPriceRange(null);
              }}
            >
              Gỡ bộ lọc
            </button>
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
                      onClick={() => navigate(`/assets/${asset.catalogAssetId}`)}
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
            Số lượng trên trang:
            <select className="asset-footer__select" defaultValue={25}>
              <option value={10}>10</option>
              <option value={25}>25</option>
              <option value={50}>50</option>
              <option value={100}>100</option>
            </select>
          </div>
          <div className="asset-footer__center">1-25 trên 289</div>
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


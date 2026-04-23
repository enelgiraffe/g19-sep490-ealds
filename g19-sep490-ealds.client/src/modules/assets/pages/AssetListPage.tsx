import { useCallback, useEffect, useMemo, useState, type KeyboardEvent } from 'react';
import { Link, useNavigate } from 'react-router-dom';
import { message } from 'antd';
import { EyeOutlined } from '@ant-design/icons';
import {
  assetInstanceService,
  assetService,
  formatVnd,
  getAssetInstanceStatusFilterOptions,
  getStatusLabel,
  type AssetCatalogResponse,
  type AssetInstanceResponse,
  type GetAssetInstancesParams,
  type AssetTypeItem,
} from '../services/assetService';
import { transferRequestService, TRANSFER_REQUEST_TYPE_ID } from '../services/transferRequestService';
import { maintenanceRequestService } from '../services/maintenanceRequestService';
import { damageReportService } from '../services/damageReportService';
import { disposalRequestService } from '../services/disposalRequestService';
import { MarkDamagedAssetModal } from '../components/MarkDamagedAssetModal';
import { LiquidationRequestModal } from '../components/LiquidationRequestModal';
import { TransferAssetModal } from '../components/TransferAssetModal';
import { MaintenanceProposalModal } from '../components/MaintenanceProposalModal';
import { profileService, type UserProfile } from '../../profile/services/profileService';
import { allocationRequestApiErrorMessage } from '../../allocations/services/allocationRequestService';
import { isDepartmentHeadRoleCode } from '../../../shared/utils/departmentHeadRole';
import axios from 'axios';
import './AssetListPage.css';

interface AssetItem {
  id: number;
  code: string;
  name: string;
  type: string;
  quantity: number;
}

interface InstanceItem {
  assetInstanceId: number;
  instanceCode: string;
  serialNumber: string;
  status: string;
  originalPrice: string;
  currentValue: string;
  statusColor: 'green' | 'gray';
}

interface AssetInfo {
  assetInstanceId: number;
  assetId: number;
  assetCatalogCode: string;
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

function mapAssetToItem(a: AssetCatalogResponse, instanceCount: number): AssetItem {
  return {
    id: a.assetId,
    code: a.code,
    name: a.name,
    type: a.assetTypeName ?? '—',
    quantity: instanceCount,
  };
}

/** One row per catalog asset, built from instances so list filters match cá thể status (GET /api/assetinstances). */
function buildCatalogRowsFromInstances(instances: AssetInstanceResponse[]): AssetCatalogResponse[] {
  const byAsset = new Map<number, AssetInstanceResponse[]>();
  for (const i of instances) {
    const list = byAsset.get(i.assetId) ?? [];
    list.push(i);
    byAsset.set(i.assetId, list);
  }
  const rows: AssetCatalogResponse[] = [];
  for (const insts of byAsset.values()) {
    const first = insts[0];
    rows.push({
      assetId: first.assetId,
      code: (first.assetCode?.trim() || first.instanceCode) ?? '—',
      name: (first.assetName?.trim() || first.instanceCode) ?? '—',
      assetTypeId: first.assetTypeId,
      assetTypeName: first.assetTypeName,
      status: 0,
      statusName: 'Available',
      unit: '—',
      quantity: insts.length,
      createdBy: 0,
      inUseDate: null,
      specification: first.specification ?? null,
      note: null,
    });
  }
  rows.sort((a, b) => a.code.localeCompare(b.code, 'vi', { sensitivity: 'base' }));
  return rows;
}

function mapInstanceToInstanceItem(a: AssetInstanceResponse): InstanceItem {
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

function instanceToAssetInfo(
  a: AssetInstanceResponse,
  catalog?: AssetCatalogResponse | null
): AssetInfo {
  const latestWarrantyEndDate =
    a.warrantyEndDate ??
    (a.guarantees && a.guarantees.length > 0
      ? a.guarantees.reduce<string | null>((latest, current) => {
          if (!current.warrantyEndDate) return latest;
          if (!latest) return current.warrantyEndDate;
          return new Date(current.warrantyEndDate) > new Date(latest)
            ? current.warrantyEndDate
            : latest;
        }, null)
      : null);

  return {
    assetInstanceId: a.assetInstanceId,
    assetId: a.assetId,
    assetCatalogCode: (a.assetCode && a.assetCode.trim()) || catalog?.code?.trim() || '—',
    code: a.instanceCode,
    name: a.assetName ?? a.instanceCode,
    type: (a.assetTypeName && a.assetTypeName.trim()) || catalog?.assetTypeName?.trim() || '—',
    specification: catalog?.specification ?? '—',
    purchaseDate: formatDate(a.purchaseDate),
    warrantyExpiry: formatDate(latestWarrantyEndDate),
    currentValue: formatVnd(a.currentValue),
    remainingValue: formatVnd(a.remainingValue ?? a.currentValue),
    location: a.warehouseName ?? a.currentDepartmentName ?? '—',
    status: getStatusLabel(a.statusName),
    admissionDate: formatDate(a.inUseDate),
    department: a.currentDepartmentName ?? '—',
    currentDepartmentId: a.currentDepartmentId ?? null,
  };
}


export function AssetListPage() {
  const [apiAssets, setApiAssets] = useState<AssetCatalogResponse[] | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [openMenuId, setOpenMenuId] = useState<number | null>(null);
  const [searchInput, setSearchInput] = useState('');
  const [keyword, setKeyword] = useState('');
  const [statusFilter, setStatusFilter] = useState<number | undefined>(undefined);
  const [assetTypeFilter, setAssetTypeFilter] = useState<number | undefined>(undefined);
  const [expandedAssetId, setExpandedAssetId] = useState<number | null>(null);
  const [instancesMap, setInstancesMap] = useState<Record<number, InstanceItem[]>>({});
  const [loadingInstances, setLoadingInstances] = useState<Record<number, boolean>>({});
  const [instanceCountByAssetId, setInstanceCountByAssetId] = useState<Record<number, number>>({});
  const [markDamagedAssetId, setMarkDamagedAssetId] = useState<number | null>(null);
  const [profile, setProfile] = useState<UserProfile | null>(null);
  const [isMarkDamagedModalOpen, setIsMarkDamagedModalOpen] = useState(false);
  const [isLiquidationModalOpen, setIsLiquidationModalOpen] = useState(false);
  const [isTransferModalOpen, setIsTransferModalOpen] = useState(false);
  const [isMaintenanceModalOpen, setIsMaintenanceModalOpen] = useState(false);
  const [selectedAssetInfo, setSelectedAssetInfo] = useState<AssetInfo | null>(null);
  const [transferAssetInstanceId, setTransferAssetInstanceId] = useState<number | null>(null);
  const [maintenanceAssetInstanceId, setMaintenanceAssetInstanceId] = useState<number | null>(null);
  const [assetTypes, setAssetTypes] = useState<AssetTypeItem[]>([]);
  const [page, setPage] = useState(1);
  const [pageSize, setPageSize] = useState(25);
  const navigate = useNavigate();

  const instanceStatusFilterOptions = useMemo(() => getAssetInstanceStatusFilterOptions(), []);

  const isDeptHead = profile?.isDepartmentHead ?? isDepartmentHeadRoleCode(profile?.role);
  const deptHeadFromId =
    isDeptHead && profile?.departmentId != null && !Number.isNaN(Number(profile.departmentId))
      ? Number(profile.departmentId)
      : null;

  const assets: AssetItem[] = useMemo(() => {
    if (!apiAssets) return [];
    return apiAssets.map((a) =>
      mapAssetToItem(a, instanceCountByAssetId[a.assetId] ?? 0)
    );
  }, [apiAssets, instanceCountByAssetId]);

  const total = assets.length;
  const totalPages = useMemo(
    () => Math.max(1, Math.ceil(total / pageSize)),
    [total, pageSize],
  );

  useEffect(() => {
    setPage((p) => Math.min(p, totalPages));
  }, [totalPages]);

  const fetchAssets = useCallback(async () => {
    setLoading(true);
    setError(null);
    try {
      const params: GetAssetInstancesParams = {
        keyword: keyword || undefined,
        status: statusFilter,
        assetTypeId: assetTypeFilter,
      };
      const instances = await assetInstanceService.getAll(params);
      const countMap: Record<number, number> = {};
      for (const inst of instances) {
        countMap[inst.assetId] = (countMap[inst.assetId] ?? 0) + 1;
      }
      setInstanceCountByAssetId(countMap);
      setApiAssets(buildCatalogRowsFromInstances(instances));
    } catch (e: any) {
      const status = e?.response?.status;
      const data = e?.response?.data;
      
      if (!e?.response) {
        setError('Không tải được danh sách tài sản. Kiểm tra kết nối backend.');
      } else if (status === 401) {
        setError('Phiên đăng nhập đã hết hạn. Vui lòng đăng nhập lại.');
      } else if (status === 403) {
        setError('Bạn không có quyền xem danh sách tài sản.');
      } else if (status === 404) {
        setError('Không tìm thấy API endpoint. Vui lòng kiểm tra cấu hình backend.');
      } else if (status >= 500) {
        setError('Lỗi máy chủ. Vui lòng thử lại sau hoặc liên hệ quản trị viên.');
      } else {
        const msg = data?.title ?? data?.message ?? 'Không tải được danh sách tài sản.';
        setError(typeof msg === 'string' ? msg : 'Không tải được danh sách tài sản.');
      }
    } finally {
      setLoading(false);
    }
  }, [keyword, statusFilter, assetTypeFilter]);

  const fetchInstancesForAsset = async (assetId: number) => {
    setLoadingInstances((prev) => ({ ...prev, [assetId]: true }));
    try {
      const detail = await assetService.getById(assetId);
      const instances = detail.instances?.map(mapInstanceToInstanceItem) ?? [];
      setInstancesMap((prev) => ({ ...prev, [assetId]: instances }));
    } catch (e: any) {
      message.error('Không tải được danh sách cá thể tài sản.');
    } finally {
      setLoadingInstances((prev) => ({ ...prev, [assetId]: false }));
    }
  };

  useEffect(() => {
    fetchAssets();
  }, [fetchAssets]);

  useEffect(() => {
    setPage(1);
  }, [keyword, statusFilter, assetTypeFilter]);

  useEffect(() => {
    setExpandedAssetId(null);
  }, [keyword, statusFilter, assetTypeFilter, page, pageSize]);

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

  const handleAssetCodeClick = async (assetId: number) => {
    if (expandedAssetId === assetId) {
      setExpandedAssetId(null);
    } else {
      setExpandedAssetId(assetId);
      await fetchInstancesForAsset(assetId);
    }
  };

  const handleInstanceMenuAction = async (actionKey: string, instanceId: number) => {
    setOpenMenuId(null);
    try {
      const raw = await assetInstanceService.getById(instanceId);
      const catalog = await assetService.getById(raw.assetId);
      const assetInfo = instanceToAssetInfo(raw, catalog);
      
      if (actionKey === 'move') {
        setSelectedAssetInfo(assetInfo);
        setTransferAssetInstanceId(raw.assetInstanceId);
        setIsTransferModalOpen(true);
      } else if (actionKey === 'mark-broken') {
        setSelectedAssetInfo(assetInfo);
        setMarkDamagedAssetId(raw.assetInstanceId);
        setIsMarkDamagedModalOpen(true);
      } else if (actionKey === 'liquidate') {
        setSelectedAssetInfo(assetInfo);
        setIsLiquidationModalOpen(true);
      } else if (actionKey === 'maintenance') {
        setSelectedAssetInfo(assetInfo);
        setMaintenanceAssetInstanceId(raw.assetInstanceId);
        setIsMaintenanceModalOpen(true);
      } else {
        console.log('Action', actionKey, 'for instance', instanceId);
      }
    } catch (e: any) {
      message.error('Không tải được thông tin cá thể tài sản.');
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
    setTransferAssetInstanceId(null);
  };

  const handleCloseMaintenanceModal = () => {
    setIsMaintenanceModalOpen(false);
    setSelectedAssetInfo(null);
    setMaintenanceAssetInstanceId(null);
  };

  const handleSubmitMarkDamaged = async (values: { damageDate: string; condition: string }) => {
    const reportedBy = profile?.id ?? getStoredUserId();
    if (!reportedBy || markDamagedAssetId == null) {
      message.error('Không xác định được người dùng hoặc tài sản để báo hỏng.');
      return;
    }

    const damageDate = values.damageDate?.trim();
    const condition = values.condition?.trim();
    if (!condition || /^[-.\s]+$/.test(condition)) {
      message.warning('Vui lòng nhập tình trạng hỏng hợp lệ (không để "-", "...").');
      return;
    }
    const descriptionParts = [
      damageDate ? `Ngày hỏng: ${damageDate}` : null,
      condition ? `Tình trạng: ${condition}` : null,
    ].filter(Boolean);

    try {
      await damageReportService.report({
        assetInstanceId: markDamagedAssetId,
        reportedBy,
        reportDate: damageDate
          ? new Date(damageDate).toISOString()
          : new Date().toISOString(),
        description: descriptionParts.join('\n') || null,
        severity: null,
        documentId: null,
      });
      message.success('Đã đánh dấu hỏng. Vào Sửa chữa → Tài sản cần sửa chữa để lập đơn đề nghị sửa chữa.');
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
    notes?: string;
  }) => {
    const createdBy = profile?.id ?? getStoredUserId();
    const resolvedInstanceId = selectedAssetInfo?.assetInstanceId;
    if (!createdBy || !resolvedInstanceId) {
      message.error('Không xác định được người dùng hoặc thể hiện tài sản để gửi đề nghị thanh lý.');
      return;
    }

    const submittedAt = values.liquidationDate ?? new Date();
    const noteText = values.notes?.trim();
    const reasonText = values.reason?.trim();

    try {
      await disposalRequestService.create({
        userId: createdBy,
        assetInstanceId: resolvedInstanceId,
        createdBy,
        title: `Đề nghị thanh lý tài sản ${selectedAssetInfo?.code ?? resolvedInstanceId}`,
        description: noteText || null,
        diposalMethod: 0,
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
    assetInstanceId: number;
    recordNumber?: string;
    maintenanceContent: string;
  }) => {
    const createdBy = profile?.id ?? getStoredUserId();
    if (!createdBy || values.assetInstanceId == null) {
      message.error('Không xác định được người dùng hoặc tài sản để gửi đề xuất bảo dưỡng.');
      return;
    }
    try {
      await maintenanceRequestService.create({
        assetInstanceId: values.assetInstanceId,
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
    if (values.incompleteDraft === true) {
      if (typeof values.draftFormJson !== 'string') {
        message.error('Không lưu được bản nháp.');
        return;
      }
      try {
        await transferRequestService.create({
          assetInstanceId: 0,
          requestTypeId: TRANSFER_REQUEST_TYPE_ID,
          fromLocationId: 0,
          toLocationId: 0,
          executeBy: profile.id,
          createdBy: profile.id,
          incompleteDraft: true,
          saveAsDraft: true,
          draftFormJson: values.draftFormJson,
        });
        message.success('Đã lưu bản nháp (chưa hoàn tất thông tin).');
        handleCloseTransferModal();
      } catch (e: unknown) {
        const fromApi =
          axios.isAxiosError(e) && e.response?.data != null
            ? allocationRequestApiErrorMessage(e.response.data)
            : null;
        message.error(fromApi ?? 'Lưu bản nháp thất bại.');
      }
      return;
    }
    try {
      const assetIds: number[] =
        Array.isArray(values.assetIds) && values.assetIds.length > 0
          ? values.assetIds.map((x: any) => Number(x)).filter((n: number) => Number.isFinite(n) && n > 0)
          : transferAssetInstanceId != null
            ? [transferAssetInstanceId]
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

      const saveAsDraft = values.saveAsDraft === true;
      for (const assetInstanceId of assetIds) {
        await transferRequestService.create({
          assetInstanceId,
          requestTypeId: TRANSFER_REQUEST_TYPE_ID,
          fromLocationId,
          toLocationId,
          fromUserId: profile.id,
          toUserId: null,
          transferDate,
          executeBy: profile.id,
          createdBy: profile.id,
          title: values.reason
            ? `Điều chuyển: ${values.reason}`
            : `Yêu cầu điều chuyển tài sản ${assetInstanceId}`,
          description: values.reason ?? undefined,
          saveAsDraft,
          incompleteDraft: false,
        });
      }

      message.success(
        saveAsDraft ? 'Đã lưu bản nháp điều chuyển.' : 'Gửi yêu cầu điều chuyển thành công.',
      );
      handleCloseTransferModal();
    } catch (e: unknown) {
      const fromApi =
        axios.isAxiosError(e) && e.response?.data != null
          ? allocationRequestApiErrorMessage(e.response.data)
          : null;
      message.error(fromApi ?? 'Gửi yêu cầu điều chuyển thất bại.');
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

  const selectedListAsset =
    expandedAssetId != null ? assets.find((a) => a.id === expandedAssetId) : undefined;

  const safePage = Math.min(page, totalPages);
  const startIndex = total === 0 ? 0 : (safePage - 1) * pageSize + 1;
  const endIndex = Math.min(safePage * pageSize, total);
  const pagedAssets = assets.slice((safePage - 1) * pageSize, safePage * pageSize);

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
              {instanceStatusFilterOptions.map((o) => (
                <option key={o.value} value={o.value}>
                  {o.label}
                </option>
              ))}
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
            <button
              className="asset-filter-settings"
              aria-label="Cài đặt hiển thị"
            >
              ⚙
            </button>
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
                  {pagedAssets.map((asset, index) => (
                    <tr
                      key={asset.id}
                      className={
                        expandedAssetId === asset.id
                          ? 'asset-row asset-row--selected'
                          : 'asset-row'
                      }
                    >
                      <td className="asset-table__cell asset-table__cell--stt">
                        {(safePage - 1) * pageSize + index + 1}
                      </td>
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
                  ))}
                </tbody>
              </table>
            </div>
          </div>
          {expandedAssetId != null && (
            <div className="asset-list-split__bottom">
              <div className="asset-instances-panel">
                <div className="asset-instances-panel__header">
                  Cá thể tài sản:{' '}
                  <strong>{selectedListAsset?.code ?? expandedAssetId}</strong>
                  {selectedListAsset?.name ? ` — ${selectedListAsset.name}` : null}
                </div>
                <div className="asset-instances-panel__body">
                  {loadingInstances[expandedAssetId] ? (
                    <p className="asset-instances-panel__loading">Đang tải danh sách cá thể...</p>
                  ) : instancesMap[expandedAssetId]?.length ? (
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
                          {instancesMap[expandedAssetId]!.map((instance, index) => (
                              <tr key={instance.assetInstanceId} className="asset-instance-row">
                                <td className="asset-table__cell asset-table__cell--stt">{index + 1}</td>
                                <td>
                                  <Link
                                    className="asset-code asset-code--link"
                                    to={`/asset-instances/${instance.assetInstanceId}`}
                                    state={{
                                      backToPath: '/assets',
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
                                  <div className="asset-row__more">
                                    <button
                                      type="button"
                                      className="asset-row__more-btn"
                                      onClick={(e) => {
                                        e.stopPropagation();
                                        handleToggleMenu(instance.assetInstanceId);
                                      }}
                                    >
                                      ⋯
                                    </button>
                                    {openMenuId === instance.assetInstanceId && (
                                      <div className="asset-row-menu">
                                        <button
                                          className="asset-row-menu__item"
                                          onClick={() =>
                                            handleInstanceMenuAction('move', instance.assetInstanceId)
                                          }
                                        >
                                          <span className="asset-row-menu__icon">↔</span>
                                          <span>Di chuyển</span>
                                        </button>
                                        <button
                                          className="asset-row-menu__item"
                                          onClick={() =>
                                            handleInstanceMenuAction('maintenance', instance.assetInstanceId)
                                          }
                                        >
                                          <span className="asset-row-menu__icon">🛠</span>
                                          <span>Bảo dưỡng</span>
                                        </button>

                                        {instance.status !== 'Đã hỏng' && (
                                          <button
                                            className="asset-row-menu__item"
                                            onClick={() =>
                                              handleInstanceMenuAction('mark-broken', instance.assetInstanceId)
                                            }
                                          >
                                            <span className="asset-row-menu__icon">⚠</span>
                                            <span>Báo hỏng</span>
                                          </button>
                                        )}

                                        <button
                                          className="asset-row-menu__item"
                                          onClick={() =>
                                            handleInstanceMenuAction('liquidate', instance.assetInstanceId)
                                          }
                                        >
                                          <span className="asset-row-menu__icon">$</span>
                                          <span>Đề nghị thanh lý</span>
                                        </button>
                                        <button
                                          className="asset-row-menu__item"
                                          onClick={() =>
                                            handleInstanceMenuAction('print-qr', instance.assetInstanceId)
                                          }
                                        >
                                          <span className="asset-row-menu__icon">▤</span>
                                          <span>In mã QR</span>
                                        </button>
                                        <button
                                          className="asset-row-menu__item asset-row-menu__item--danger"
                                          onClick={() =>
                                            handleInstanceMenuAction('delete', instance.assetInstanceId)
                                          }
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
            <select
              className="asset-footer__select"
              value={pageSize}
              onChange={(e) => {
                const next = Number(e.target.value);
                setPageSize(next);
                setPage(1);
              }}
            >
              <option value={10}>10</option>
              <option value={25}>25</option>
              <option value={50}>50</option>
              <option value={100}>100</option>
            </select>
          </div>
          <div className="asset-footer__center">
            {total === 0 ? '0-0 trên 0' : `${startIndex}-${endIndex} trên ${total}`}
          </div>
          <div className="asset-footer__right">
            <button
              className="asset-footer__pager"
              disabled={safePage <= 1}
              onClick={() => setPage((p) => Math.max(1, p - 1))}
              type="button"
            >
              ⟨
            </button>
            <button
              className="asset-footer__pager asset-footer__pager--active"
              type="button"
            >
              {safePage}
            </button>
            <button
              className="asset-footer__pager"
              disabled={safePage >= totalPages}
              onClick={() => setPage((p) => Math.min(totalPages, p + 1))}
              type="button"
            >
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
        mode="department"
        currentUserDepartmentId={profile?.departmentId ?? null}
        fromDepartmentId={selectedAssetInfo?.currentDepartmentId ?? deptHeadFromId ?? null}
      />

      <MaintenanceProposalModal
        open={isMaintenanceModalOpen}
        onClose={handleCloseMaintenanceModal}
        onSubmit={handleSubmitMaintenance}
        assetInfo={selectedAssetInfo}
        assetInstanceId={maintenanceAssetInstanceId}
      />
    </div>
  );
}


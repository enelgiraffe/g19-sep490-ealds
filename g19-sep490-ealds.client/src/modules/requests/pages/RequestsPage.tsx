import { useEffect, useMemo, useRef, useState } from 'react';
import { useSearchParams } from 'react-router-dom';
import { Button, DatePicker, Select, Tabs, message, Input } from 'antd';
import { EyeOutlined, EditOutlined } from '@ant-design/icons';
import dayjs from 'dayjs';
import type { Dayjs } from 'dayjs';
import { CreatePurchaseOrderModal } from '../../purchase-orders/components/CreatePurchaseOrderModal';
import { ViewPurchaseOrderModal } from '../../purchase-orders/components/ViewPurchaseOrderModal';
import {
  purchaseOrderService,
  type PurchaseOrderDetail,
} from '../../purchase-orders/services/purchaseOrderService';
import { profileService, type UserProfile } from '../../profile/services/profileService';
import {
  transferRequestService,
  type TransferRequestListItem,
} from '../../assets/services/transferRequestService';
import { AccountantTransferRequestDetailModal } from '../components/AccountantTransferRequestDetailModal';
import {
  accountantRequestService,
  type AccountantRequestListItem,
} from '../services/accountantRequestService';
import {
  directorRequestService,
  REQUEST_TYPE_IDS,
  type DirectorRequestListItem,
} from '../services/directorRequestService';
import './RequestsPage.css';

const { Option } = Select;

type ActiveTabKey = 'purchase' | 'transfer' | 'liquidation';
type ActiveTabKeyAll = ActiveTabKey | 'maintenance' | 'repair';

const PURCHASE_STATUS_MAP: Record<number, { label: string; color: string }> = {
  [-1]: { label: 'Nháp', color: 'default' },
  0: { label: 'Đã gửi', color: 'processing' },
  1: { label: 'Chờ duyệt', color: 'warning' },
  2: { label: 'Duyệt', color: 'success' },
  3: { label: 'Từ chối', color: 'error' },
  4: { label: 'Chờ ngân sách', color: 'warning' },
  5: { label: 'Đã ghi tăng', color: 'success' },
};

const TRANSFER_STATUS_MAP: Record<number, { label: string; color: string }> = {
  1: { label: 'Đã gửi', color: 'processing' },
  2: { label: 'Chờ phê duyệt', color: 'warning' },
  3: { label: 'Từ chối', color: 'error' },
  4: { label: 'Phê duyệt', color: 'success' },
};

/**
 * Dùng chung cho tab Thanh lý (API director).
 * Backend đang dùng status:
 * -1: Nháp (nếu có)
 *  0: Đã gửi/đã nộp
 *  1: Chờ phê duyệt (director)
 *  2: Phê duyệt
 *  3: Từ chối
 */
const DIRECTOR_STATUS_MAP: Record<number, { label: string; color: string }> = {
  [-1]: { label: 'Nháp', color: 'default' },
  0: { label: 'Đã gửi', color: 'processing' },
  1: { label: 'Chờ phê duyệt', color: 'warning' },
  2: { label: 'Phê duyệt', color: 'success' },
  3: { label: 'Từ chối', color: 'error' },
};

/**
 * Status cho Bảo dưỡng / Sửa chữa: trưởng phòng ban gửi thẳng cho giám đốc (không qua kế toán).
 * Thực tế dữ liệu có thể còn lẫn các status cũ, nên map vẫn cover 0/4 để tránh hiển thị sai/trống.
 */
const MAINT_REPAIR_STATUS_MAP: Record<number, { label: string; color: string }> = {
  [-1]: { label: 'Nháp', color: 'default' },
  0: { label: 'Đã gửi', color: 'processing' },
  1: { label: 'Chờ phê duyệt', color: 'warning' },
  2: { label: 'Phê duyệt', color: 'success' },
  3: { label: 'Từ chối', color: 'error' },
  // Legacy/variant status value seen in some endpoints
  4: { label: 'Phê duyệt', color: 'success' },
};

interface PurchaseTableRow {
  assetRequestId: number;
  title: string;
  status: number;
  key: string;
  stt: number;
  code: string;
  requestDate: string;
  equipment: string;
  quantity: number;
  estimatedPrice: string;
}

interface TransferTableRow extends TransferRequestListItem {
  key: string;
  stt: number;
  transferDateText: string;
}

function formatDate(iso: string): string {
  try {
    const d = new Date(iso);
    return d.toLocaleDateString('vi-VN');
  } catch {
    return iso;
  }
}

function toPurchaseTableRow(item: AccountantRequestListItem, index: number): PurchaseTableRow {
  return {
    key: String(item.assetRequestId),
    stt: index + 1,
    assetRequestId: item.assetRequestId,
    title: item.title,
    status: item.status,
    code: `YC-${item.assetRequestId}`,
    requestDate: formatDate(item.createDate),
    equipment: item.title,
    quantity: 1,
    estimatedPrice: '—',
  };
}

function toTransferTableRow(item: TransferRequestListItem, index: number): TransferTableRow {
  return {
    ...item,
    key: String(item.recordId),
    stt: index + 1,
    transferDateText: formatDate(item.transferDate),
  };
}

const REQUESTS_TAB_KEYS: ActiveTabKeyAll[] = [
  'purchase',
  'transfer',
  'liquidation',
  'maintenance',
  'repair',
];

export function RequestsPage() {
  const [searchParams] = useSearchParams();
  const [activeTab, setActiveTab] = useState<ActiveTabKeyAll>('purchase');
  const didInitDirectorDefaultTabRef = useRef(false);

  // Deep link from notifications: /requests?tab=transfer
  useEffect(() => {
    const tab = searchParams.get('tab');
    if (tab && (REQUESTS_TAB_KEYS as string[]).includes(tab)) {
      setActiveTab(tab as ActiveTabKeyAll);
    }
  }, [searchParams]);

  const [purchaseRows, setPurchaseRows] = useState<PurchaseTableRow[]>([]);
  const [purchaseLoading, setPurchaseLoading] = useState(false);
  const [isCreatePurchaseOpen, setIsCreatePurchaseOpen] = useState(false);
  const [isViewPurchaseOpen, setIsViewPurchaseOpen] = useState(false);
  const [selectedPurchaseDetail, setSelectedPurchaseDetail] = useState<PurchaseOrderDetail | null>(
    null,
  );
  const [editingPurchaseDetail, setEditingPurchaseDetail] = useState<PurchaseOrderDetail | null>(
    null,
  );
  const [userProfile, setUserProfile] = useState<UserProfile | null>(null);

  const [transferRows, setTransferRows] = useState<TransferTableRow[]>([]);
  const [transferLoading, setTransferLoading] = useState(false);
  const [isTransferDetailOpen, setIsTransferDetailOpen] = useState(false);
  const [selectedTransfer, setSelectedTransfer] = useState<TransferTableRow | null>(null);
  const [liquidationRows, setLiquidationRows] = useState<AccountantRequestListItem[]>([]);
  const [liquidationLoading, setLiquidationLoading] = useState(false);
  const [selectedLiquidationItem, setSelectedLiquidationItem] = useState<AccountantRequestListItem | null>(
    null,
  );
  const [isLiquidationApproveOpen, setIsLiquidationApproveOpen] = useState(false);
  const [liquidationDecision, setLiquidationDecision] = useState<'approved' | 'rejected'>('approved');
  const [liquidationComment, setLiquidationComment] = useState('');
  const [liquidationSubmitting, setLiquidationSubmitting] = useState(false);

  const [directorRows, setDirectorRows] = useState<DirectorRequestListItem[]>([]);
  const [directorTotal, setDirectorTotal] = useState(0);
  const [directorLoading, setDirectorLoading] = useState(false);
  const [selectedDirectorItem, setSelectedDirectorItem] = useState<DirectorRequestListItem | null>(
    null,
  );
  const [isDirectorDetailOpen, setIsDirectorDetailOpen] = useState(false);
  const [isDirectorApproveOpen, setIsDirectorApproveOpen] = useState(false);
  const [directorDecision, setDirectorDecision] = useState<'approved' | 'rejected'>('approved');
  const [directorComment, setDirectorComment] = useState('');
  const [directorSubmitting, setDirectorSubmitting] = useState(false);

  const [statusFilter, setStatusFilter] = useState<number | 'all'>('all');
  const [departmentFilter, setDepartmentFilter] = useState<string | 'all'>('all');
  const [sentDateFilter, setSentDateFilter] = useState<string | null>(null);
  const [searchText, setSearchText] = useState('');
  const [page, setPage] = useState(1);
  const [pageSize, setPageSize] = useState(25);

  useEffect(() => {
    const loadPurchase = async () => {
      setPurchaseLoading(true);
      try {
        const list = await accountantRequestService.getPurchaseRequests();
        setPurchaseRows(list.map((item, i) => toPurchaseTableRow(item, i)));
      } catch {
        message.error('Không tải được danh sách đơn mua.');
        setPurchaseRows([]);
      } finally {
        setPurchaseLoading(false);
      }
    };

    const loadTransfers = async () => {
      setTransferLoading(true);
      try {
        const list = await transferRequestService.getList();
        setTransferRows(list.map((item, i) => toTransferTableRow(item, i)));
      } catch {
        message.error('Không tải được danh sách yêu cầu điều chuyển.');
        setTransferRows([]);
      } finally {
        setTransferLoading(false);
      }
    };

    const loadProfile = async () => {
      try {
        const profile = await profileService.getProfile();
        setUserProfile(profile);
      } catch {
        // ignore profile error here; will be handled on demand
      }
    };

    const loadLiquidations = async () => {
      setLiquidationLoading(true);
      try {
        const list = await accountantRequestService.getLiquidationRequests();
        setLiquidationRows(list);
      } catch {
        message.error('Không tải được danh sách yêu cầu thanh lý.');
        setLiquidationRows([]);
      } finally {
        setLiquidationLoading(false);
      }
    };

    loadPurchase();
    loadTransfers();
    loadLiquidations();
    loadProfile();
  }, []);

  // Director has all tabs; accountant / trưởng phòng only purchase, transfer, liquidation
  useEffect(() => {
    if (!userProfile?.role) return;
    const r = String(userProfile.role).toUpperCase();
    const limited: ActiveTabKeyAll[] = ['purchase', 'transfer', 'liquidation'];
    const allowed: ActiveTabKeyAll[] = r === 'DIRECTOR' ? REQUESTS_TAB_KEYS : limited;
    if (!allowed.includes(activeTab)) {
      setActiveTab('purchase');
    }
  }, [userProfile, activeTab]);

  // Director module: default tab purchase unless URL already specifies ?tab= (e.g. from notification link)
  useEffect(() => {
    if (didInitDirectorDefaultTabRef.current) return;
    if (!userProfile?.role) return;
    didInitDirectorDefaultTabRef.current = true;
    const tab = searchParams.get('tab');
    if (tab && (REQUESTS_TAB_KEYS as string[]).includes(tab)) return;
    if (String(userProfile.role).toUpperCase() === 'DIRECTOR') {
      setActiveTab('purchase');
    }
  }, [userProfile, searchParams]);

  const reloadPurchaseList = async () => {
    setPurchaseLoading(true);
    try {
      const list = await accountantRequestService.getPurchaseRequests();
      setPurchaseRows(list.map((item, i) => toPurchaseTableRow(item, i)));
    } catch {
      message.error('Không tải được danh sách đơn mua.');
      setPurchaseRows([]);
    } finally {
      setPurchaseLoading(false);
    }
  };

  const normalizedRole = String(userProfile?.role ?? '').toUpperCase();
  const isDirectorRole = normalizedRole === 'DIRECTOR';
  const isAccountantRole = normalizedRole === 'ACCOUNTANT';

  const shouldUseDirectorView = isDirectorRole &&
    (activeTab === 'purchase' ||
      activeTab === 'transfer' ||
      activeTab === 'maintenance' ||
      activeTab === 'repair' ||
      activeTab === 'liquidation');

  const directorRequestTypeId = shouldUseDirectorView ? REQUEST_TYPE_IDS[activeTab] : null;

  // Ensure director sees only requests already passed accountant step (per workflow)
  // - Purchase: accountant approves 0->1, director decides at status=1
  // - Transfer: accountant approves 1->2, director decides at status=2
  const enforcedDirectorStatus: number | undefined =
    isDirectorRole && activeTab === 'purchase'
      ? 1
      : isDirectorRole && activeTab === 'transfer'
        ? 2
        : isDirectorRole && activeTab === 'liquidation'
          ? 1
        : undefined;

  const canDirectorApprove =
    !!userProfile?.id &&
    !!selectedDirectorItem &&
    (selectedDirectorItem.status === 1 ||
      (selectedDirectorItem.requestTypeId === REQUEST_TYPE_IDS.transfer &&
        selectedDirectorItem.status === 2));

  const canAccountantLiquidationApprove =
    !!userProfile?.id && !!selectedLiquidationItem && selectedLiquidationItem.status === 0;

  const prevDirectorTypeIdRef = useRef<number | null>(null);
  const effectiveDirectorPage =
    prevDirectorTypeIdRef.current !== directorRequestTypeId ? 1 : page;

  useEffect(() => {
    if (!shouldUseDirectorView) return;
    if (directorRequestTypeId == null) return;
    if (prevDirectorTypeIdRef.current !== directorRequestTypeId) {
      setPage(1);
    }
    prevDirectorTypeIdRef.current = directorRequestTypeId;
    let cancelled = false;
    setDirectorLoading(true);
    directorRequestService
      .getView({
        requestTypeId: directorRequestTypeId,
        status:
          enforcedDirectorStatus ?? (statusFilter === 'all' ? undefined : statusFilter),
        page: effectiveDirectorPage,
        pageSize,
      })
      .then((res) => {
        if (!cancelled) {
          setDirectorRows(res.items as DirectorRequestListItem[]);
          setDirectorTotal(res.total);
        }
      })
      .catch(() => {
        if (!cancelled) {
          message.error('Không tải được danh sách yêu cầu.');
          setDirectorRows([]);
          setDirectorTotal(0);
        }
      })
      .finally(() => {
        if (!cancelled) setDirectorLoading(false);
      });
    return () => {
      cancelled = true;
    };
  }, [
    shouldUseDirectorView,
    directorRequestTypeId,
    enforcedDirectorStatus,
    statusFilter,
    effectiveDirectorPage,
    pageSize,
  ]);

  useEffect(() => {
    setPage(1);
  }, [activeTab, statusFilter, searchText, departmentFilter, sentDateFilter]);

  useEffect(() => {
    if (!shouldUseDirectorView) return;
    setPage(1);
    setSelectedDirectorItem(null);
    setIsDirectorDetailOpen(false);
  }, [activeTab, shouldUseDirectorView]);

  const filteredPurchaseRows = useMemo(() => {
    const keyword = searchText.trim().toLowerCase();
    return purchaseRows.filter((row) => {
      const matchStatus = statusFilter === 'all' || row.status === statusFilter;
      const matchKeyword =
        !keyword ||
        row.title.toLowerCase().includes(keyword) ||
        row.code.toLowerCase().includes(keyword);
      return matchStatus && matchKeyword;
    });
  }, [purchaseRows, searchText, statusFilter]);

  const filteredTransferRows = useMemo(() => {
    const keyword = searchText.trim().toLowerCase();
    return transferRows.filter((row) => {
      // Kế toán chỉ xem yêu cầu điều chuyển từ trạng thái "Đã gửi" trở đi (ẩn nháp của trưởng phòng ban).
      const matchMinStatus = row.status >= 1;
      const matchStatus = statusFilter === 'all' || row.status === statusFilter;
      const matchDepartment =
        departmentFilter === 'all' ||
        row.fromDepartment.toLowerCase() === String(departmentFilter).toLowerCase();
      let matchDate = true;
      if (sentDateFilter) {
        try {
          const rowDate = new Date(row.transferDate).toISOString().slice(0, 10);
          matchDate = rowDate === sentDateFilter;
        } catch {
          matchDate = true;
        }
      }
      const matchKeyword =
        !keyword ||
        row.code.toLowerCase().includes(keyword) ||
        row.assetCode.toLowerCase().includes(keyword) ||
        row.assetName.toLowerCase().includes(keyword);
      return matchMinStatus && matchStatus && matchDepartment && matchDate && matchKeyword;
    });
  }, [transferRows, searchText, statusFilter, departmentFilter, sentDateFilter]);

  const filteredLiquidationRows = useMemo(() => {
    const keyword = searchText.trim().toLowerCase();
    return liquidationRows.filter((row) => {
      const matchStatus = statusFilter === 'all' || row.status === statusFilter;
      let matchDate = true;
      if (sentDateFilter) {
        try {
          const rowDate = new Date(row.createDate).toISOString().slice(0, 10);
          matchDate = rowDate === sentDateFilter;
        } catch {
          matchDate = true;
        }
      }
      const matchKeyword =
        !keyword ||
        row.title.toLowerCase().includes(keyword) ||
        `yc-${row.assetRequestId}`.includes(keyword);
      return matchStatus && matchDate && matchKeyword;
    });
  }, [liquidationRows, searchText, statusFilter, sentDateFilter]);

  const departmentOptions = useMemo(
    () =>
      Array.from(new Set(transferRows.map((row) => row.fromDepartment)))
        .filter((name) => !!name)
        .sort(),
    [transferRows],
  );

  const isPurchaseTab = activeTab === 'purchase';
  const isTransferTab = activeTab === 'transfer';
  const isLiquidationTab = activeTab === 'liquidation';
  const hasDataTable = isPurchaseTab || isTransferTab || isLiquidationTab || shouldUseDirectorView;

  const currentRows = isPurchaseTab
    ? filteredPurchaseRows
    : isTransferTab
      ? filteredTransferRows
      : isLiquidationTab
        ? filteredLiquidationRows
      : [];
  const loading = isPurchaseTab
    ? purchaseLoading
    : isTransferTab
      ? transferLoading
      : isLiquidationTab && !shouldUseDirectorView
        ? liquidationLoading
      : shouldUseDirectorView
        ? directorLoading
        : false;

  const total = shouldUseDirectorView ? directorTotal : currentRows.length;
  const totalPages = Math.max(1, Math.ceil(total / pageSize));
  const safePage = Math.min(page, totalPages);
  const startIndex = total === 0 ? 0 : (safePage - 1) * pageSize + 1;
  const endIndex = Math.min(safePage * pageSize, total);
  const pagedRows = shouldUseDirectorView
    ? directorRows
    : currentRows.slice((safePage - 1) * pageSize, safePage * pageSize);

  const handleCloseCreatePurchase = () => {
    setIsCreatePurchaseOpen(false);
    setUserProfile(null);
    setEditingPurchaseDetail(null);
  };

  const handleSubmitPurchaseOrder = async (payload: {
    title: string;
    description?: string;
    proposedData?: string;
    status?: number;
  }) => {
    if (!userProfile) {
      message.error('Vui lòng đăng nhập lại.');
      return;
    }
    try {
      if (editingPurchaseDetail) {
        await purchaseOrderService.update(editingPurchaseDetail.assetRequestId, {
          userId: userProfile.id,
          title: payload.title,
          description: payload.description ?? null,
          proposedData: payload.proposedData ?? null,
          createdBy: userProfile.id,
          status: payload.status ?? -1,
        });
      } else {
        await purchaseOrderService.create({
          userId: userProfile.id,
          title: payload.title,
          description: payload.description ?? null,
          proposedData: payload.proposedData ?? null,
          createdBy: userProfile.id,
          status: payload.status ?? 0,
        });
      }
      if ((payload.status ?? 0) === -1) {
        message.success(editingPurchaseDetail ? 'Cập nhật nháp yêu cầu mua sắm thành công.' : 'Lưu nháp yêu cầu mua sắm thành công.');
      } else {
        message.success(editingPurchaseDetail ? 'Đã gửi yêu cầu mua sắm.' : 'Gửi yêu cầu mua sắm thành công.');
      }
      handleCloseCreatePurchase();
      const list = await accountantRequestService.getPurchaseRequests();
      setPurchaseRows(list.map((item, i) => toPurchaseTableRow(item, i)));
    } catch (e: unknown) {
      const err = e as { response?: { data?: string } };
      message.error(err?.response?.data ?? 'Tạo yêu cầu thất bại.');
    }
  };

  const parseToFormValues = (detail: PurchaseOrderDetail) => {
    const values: Record<string, unknown> = {
      title: detail.title ?? '',
      equipment: [{ name: '', quantity: 1, machineCode: '', unit: 'Cái', estimatedPrice: '' }],
    };
    try {
      const lines = (detail.description ?? '')
        .split('\n')
        .map((s) => s.trim())
        .filter(Boolean);
      for (const line of lines) {
        if (line.startsWith('Lý do:')) values.reason = line.replace('Lý do:', '').trim();
        if (line.startsWith('Thời gian cần:')) {
          const raw = line.replace('Thời gian cần:', '').trim();
          const parsed = dayjs(raw, 'DD/MM/YYYY', true);
          values.needDate = parsed.isValid() ? (parsed as Dayjs) : undefined;
        }
        if (line.startsWith('Nhà cung cấp đề xuất:')) values.supplier = line.replace('Nhà cung cấp đề xuất:', '').trim();
        if (line.startsWith('Loại tài sản:')) values.assetType = line.replace('Loại tài sản:', '').trim();
        if (line.startsWith('Mục đích:')) values.purpose = line.replace('Mục đích:', '').trim();
      }
    } catch {
      // ignore
    }
    try {
      if (detail.proposedData) {
        const parsed = JSON.parse(detail.proposedData) as {
          equipment?: {
            name?: string;
            quantity?: number;
            machineCode?: string;
            unit?: string;
            estimatedPrice?: string;
          }[];
        };
        if (Array.isArray(parsed.equipment) && parsed.equipment.length > 0) {
          values.equipment = parsed.equipment.map((e) => ({
            name: e.name ?? '',
            quantity: e.quantity ?? 1,
            machineCode: e.machineCode ?? '',
            unit: e.unit ?? 'Cái',
            estimatedPrice: e.estimatedPrice ?? '',
          }));
        }
      }
    } catch {
      // ignore
    }
    return values;
  };

  const handleEditDraftPurchase = async (row: PurchaseTableRow) => {
    try {
      const detail = await purchaseOrderService.getById(row.assetRequestId);
      if (detail.status !== -1) {
        message.warning('Chỉ được sửa khi yêu cầu đang ở trạng thái Nháp.');
        return;
      }
      if (!userProfile) {
        const profile = await profileService.getProfile();
        setUserProfile(profile);
      }
      setEditingPurchaseDetail(detail);
      setIsCreatePurchaseOpen(true);
    } catch {
      message.error('Không tải được dữ liệu nháp để sửa.');
    }
  };

  const handleViewPurchaseDetail = async (row: PurchaseTableRow) => {
    try {
      if (!userProfile) {
        const profile = await profileService.getProfile();
        setUserProfile(profile);
      }
      const detail = await purchaseOrderService.getById(row.assetRequestId);
      setSelectedPurchaseDetail(detail);
      setIsViewPurchaseOpen(true);
    } catch {
      message.error('Không tải được chi tiết đơn.');
    }
  };

  const renderStatusFilterOptions = () => {
    const map = isPurchaseTab
      ? PURCHASE_STATUS_MAP
      : isTransferTab
        ? TRANSFER_STATUS_MAP
        : DIRECTOR_STATUS_MAP;
    return Object.entries(map).map(([k, v]) => (
      <Option key={k} value={Number(k)}>
        {v.label}
      </Option>
    ));
  };

  const getDirectorStatusMapForType = (requestTypeId: number) => {
    if (requestTypeId === REQUEST_TYPE_IDS.transfer) return TRANSFER_STATUS_MAP;
    if (requestTypeId === REQUEST_TYPE_IDS.maintenance || requestTypeId === REQUEST_TYPE_IDS.repair) {
      return MAINT_REPAIR_STATUS_MAP;
    }
    return DIRECTOR_STATUS_MAP;
  };

  return (
    <div className="requests-page">
      <div className="requests-header">
        <h1 className="requests-title">Yêu cầu</h1>
      </div>

      <div className="requests-card">
        <Tabs
          activeKey={activeTab}
          onChange={(key) => setActiveTab(key as ActiveTabKeyAll)}
          className="requests-tabs"
          items={[
            ...(isAccountantRole
              ? ([
                  { key: 'purchase', label: 'Đơn mua' },
                  { key: 'transfer', label: 'Điều chuyển' },
                  { key: 'liquidation', label: 'Thanh lý' },
                ] as const)
              : isDirectorRole
                ? ([
                    { key: 'purchase', label: 'Đơn mua' },
                    { key: 'transfer', label: 'Điều chuyển' },
                    { key: 'maintenance', label: 'Bảo dưỡng' },
                    { key: 'repair', label: 'Sửa chữa' },
                    { key: 'liquidation', label: 'Thanh lý' },
                  ] as const)
                : ([
                    { key: 'purchase', label: 'Đơn mua' },
                    { key: 'transfer', label: 'Điều chuyển' },
                    { key: 'liquidation', label: 'Thanh lý' },
                  ] as const)),
          ]}
        />

        <div className="requests-filters">
          {(isPurchaseTab || isTransferTab || isLiquidationTab) && (
            <Input
              placeholder="Tìm kiếm"
              className="requests-search"
              value={searchText}
              onChange={(e) => setSearchText(e.target.value)}
            />
          )}
          {isTransferTab && (
            <>
              <Select
                placeholder="Phòng ban đề xuất"
                className="requests-select"
                value={departmentFilter}
                onChange={(v) => setDepartmentFilter((v ?? 'all') as string | 'all')}
              >
                <Option value="all">Tất cả phòng ban</Option>
                {departmentOptions.map((name) => (
                  <Option key={name} value={name}>
                    {name}
                  </Option>
                ))}
              </Select>
              <Select
                placeholder="Trạng thái"
                className="requests-select"
                value={statusFilter}
                onChange={(v) => setStatusFilter(v)}
              >
                <Option value="all">Tất cả</Option>
                {renderStatusFilterOptions()}
              </Select>
              <DatePicker
                placeholder="Ngày gửi"
                className="requests-date-picker"
                onChange={(_, dateString) => {
                  setSentDateFilter(dateString || null);
                }}
              />
            </>
          )}
          {isLiquidationTab && !shouldUseDirectorView && (
            <>
              <Select
                placeholder="Trạng thái"
                className="requests-select"
                value={statusFilter}
                onChange={(v) => setStatusFilter(v)}
              >
                <Option value="all">Tất cả</Option>
                {renderStatusFilterOptions()}
              </Select>
              <DatePicker
                placeholder="Ngày gửi"
                className="requests-date-picker"
                onChange={(_, dateString) => {
                  setSentDateFilter(dateString || null);
                }}
              />
            </>
          )}
          {shouldUseDirectorView &&
            (enforcedDirectorStatus != null ? (
              <div style={{ color: '#6b7280', fontSize: 13 }}>
                Chỉ hiển thị: <strong>Chờ phê duyệt</strong>
              </div>
            ) : (
              <Select
                placeholder="Trạng thái"
                className="requests-select"
                value={statusFilter}
                onChange={(v) => setStatusFilter(v)}
              >
                <Option value="all">Tất cả</Option>
                {Object.entries(
                  activeTab === 'maintenance' || activeTab === 'repair'
                    ? MAINT_REPAIR_STATUS_MAP
                    : DIRECTOR_STATUS_MAP,
                ).map(([k, v]) => (
                  <Option key={k} value={Number(k)}>
                    {v.label}
                  </Option>
                ))}
              </Select>
            ))}
        </div>

        <div className="asset-table-wrapper requests-table-wrapper">
          {!hasDataTable ? (
            <div className="requests-table-loading">
              Chức năng đang được phát triển cho tab này.
            </div>
          ) : loading ? (
            <div className="requests-table-loading">
              {isPurchaseTab
                ? 'Đang tải danh sách đơn mua...'
                : isTransferTab
                  ? 'Đang tải danh sách yêu cầu điều chuyển...'
                  : 'Đang tải danh sách yêu cầu...'}
            </div>
          ) : shouldUseDirectorView ? (
            <table className="asset-table requests-table">
              <thead>
                <tr>
                  <th>MÃ YÊU CẦU</th>
                  <th>PHÒNG BAN ĐỀ XUẤT</th>
                  <th>NGÀY GỬI</th>
                  <th>MÃ TÀI SẢN</th>
                  <th>TÊN TÀI SẢN</th>
                  <th>TRẠNG THÁI</th>
                  <th className="asset-table__cell asset-table__cell--actions" />
                </tr>
              </thead>
              <tbody>
                {pagedRows.length === 0 ? (
                  <tr>
                    <td colSpan={7} className="requests-table-empty">
                      Không có dữ liệu.
                    </td>
                  </tr>
                ) : (
                  (pagedRows as DirectorRequestListItem[]).map((row) => {
                    const map = getDirectorStatusMapForType(row.requestTypeId);
                    const fallback =
                      map === MAINT_REPAIR_STATUS_MAP ? MAINT_REPAIR_STATUS_MAP[0] : DIRECTOR_STATUS_MAP[0];
                    const config = map[row.status] ?? fallback;
                    return (
                      <tr key={row.assetRequestId} className="asset-row">
                        <td>
                          <button
                            type="button"
                            className="asset-code asset-code--link"
                            onClick={() => {
                              setSelectedDirectorItem(row);
                              setDirectorDecision('approved');
                              setDirectorComment('');
                              setIsDirectorDetailOpen(true);
                            }}
                          >
                            YC-{row.assetRequestId}
                          </button>
                        </td>
                        <td>{row.currentDepartmentName ?? row.creatorEmail ?? '—'}</td>
                        <td>{formatDate(row.createDate)}</td>
                        <td>{row.assetCode ?? '—'}</td>
                        <td>{row.assetName ?? row.title ?? '—'}</td>
                        <td>
                          <span
                            className={
                              config.color === 'success'
                                ? 'asset-status-pill asset-status-pill--active'
                                : config.color === 'default'
                                  ? 'asset-status-pill asset-status-pill--inactive'
                                : config.color === 'processing'
                                  ? 'asset-status-pill asset-status-pill--processing'
                                  : config.color === 'warning'
                                    ? 'asset-status-pill asset-status-pill--warning'
                                    : config.color === 'error'
                                      ? 'asset-status-pill asset-status-pill--danger'
                                      : 'asset-status-pill'
                            }
                          >
                            {config.label}
                          </span>
                        </td>
                        <td className="asset-table__cell asset-table__cell--actions">
                          <Button
                            type="text"
                            icon={<EyeOutlined />}
                            onClick={() => {
                              setSelectedDirectorItem(row);
                              setDirectorDecision('approved');
                              setDirectorComment('');
                              setIsDirectorDetailOpen(true);
                            }}
                          />
                        </td>
                      </tr>
                    );
                  })
                )}
              </tbody>
            </table>
          ) : isPurchaseTab ? (
            <table className="asset-table requests-table">
              <thead>
                <tr>
                  <th>MÃ YÊU CẦU</th>
                  <th>NGÀY ĐỀ XUẤT</th>
                  <th>MỤC ĐÍCH MUA</th>
                  <th>SỐ LƯỢNG</th>
                  <th>TỔNG GIÁ TRỊ DỰ KIẾN</th>
                  <th>TRẠNG THÁI</th>
                  <th className="asset-table__cell asset-table__cell--actions" />
                </tr>
              </thead>
              <tbody>
                {pagedRows.length === 0 ? (
                  <tr>
                    <td colSpan={7} className="requests-table-empty">
                      Không có dữ liệu.
                    </td>
                  </tr>
                ) : (
                  (pagedRows as PurchaseTableRow[]).map((row) => {
                    const config = PURCHASE_STATUS_MAP[row.status] ?? PURCHASE_STATUS_MAP[0];
                    return (
                      <tr key={row.key} className="asset-row">
                        <td>
                          <button
                            type="button"
                            className="asset-code asset-code--link"
                            onClick={() => handleViewPurchaseDetail(row)}
                          >
                            {row.code}
                          </button>
                        </td>
                        <td>{row.requestDate}</td>
                        <td>{row.equipment}</td>
                        <td className="asset-align-right">{row.quantity}</td>
                        <td className="asset-align-right">{row.estimatedPrice}</td>
                        <td>
                          <span
                            className={
                              config.color === 'success'
                                ? 'asset-status-pill asset-status-pill--active'
                                : config.color === 'default'
                                ? 'asset-status-pill asset-status-pill--inactive'
                                : config.color === 'processing'
                                  ? 'asset-status-pill asset-status-pill--processing'
                                  : config.color === 'warning'
                                    ? 'asset-status-pill asset-status-pill--warning'
                                    : config.color === 'error'
                                      ? 'asset-status-pill asset-status-pill--danger'
                                      : 'asset-status-pill'
                            }
                          >
                            {config.label}
                          </span>
                        </td>
                        <td className="asset-table__cell asset-table__cell--actions">
                          <div className="requests-actions">
                            <Button
                              type="text"
                              icon={<EyeOutlined />}
                              size="small"
                              onClick={() => handleViewPurchaseDetail(row)}
                            >
                              Xem
                            </Button>
                            {row.status === -1 && (
                              <Button
                                type="text"
                                icon={<EditOutlined />}
                                size="small"
                                onClick={() => handleEditDraftPurchase(row)}
                              >
                                Sửa
                              </Button>
                            )}
                          </div>
                        </td>
                      </tr>
                    );
                  })
                )}
              </tbody>
            </table>
          ) : isLiquidationTab ? (
            <table className="asset-table requests-table">
              <thead>
                <tr>
                  <th>MÃ YÊU CẦU</th>
                  <th>NGÀY GỬI</th>
                  <th>NỘI DUNG</th>
                  <th>TRẠNG THÁI</th>
                  <th className="asset-table__cell asset-table__cell--actions" />
                </tr>
              </thead>
              <tbody>
                {pagedRows.length === 0 ? (
                  <tr>
                    <td colSpan={5} className="requests-table-empty">
                      Không có dữ liệu.
                    </td>
                  </tr>
                ) : (
                  (pagedRows as AccountantRequestListItem[]).map((row) => {
                    const config = DIRECTOR_STATUS_MAP[row.status] ?? DIRECTOR_STATUS_MAP[0];
                    return (
                      <tr key={row.assetRequestId} className="asset-row">
                        <td>
                          <button
                            type="button"
                            className="asset-code asset-code--link"
                            onClick={() => {
                              setSelectedLiquidationItem(row);
                              setLiquidationDecision('approved');
                              setLiquidationComment('');
                              setIsLiquidationApproveOpen(true);
                            }}
                          >
                            YC-{row.assetRequestId}
                          </button>
                        </td>
                        <td>{formatDate(row.createDate)}</td>
                        <td>{row.title ?? '—'}</td>
                        <td>
                          <span
                            className={
                              config.color === 'success'
                                ? 'asset-status-pill asset-status-pill--active'
                                : config.color === 'default'
                                  ? 'asset-status-pill asset-status-pill--inactive'
                                : config.color === 'processing'
                                  ? 'asset-status-pill asset-status-pill--processing'
                                  : config.color === 'warning'
                                    ? 'asset-status-pill asset-status-pill--warning'
                                    : config.color === 'error'
                                      ? 'asset-status-pill asset-status-pill--danger'
                                      : 'asset-status-pill'
                            }
                          >
                            {config.label}
                          </span>
                        </td>
                        <td className="asset-table__cell asset-table__cell--actions">
                          <Button
                            type="text"
                            icon={<EyeOutlined />}
                            onClick={() => {
                              setSelectedLiquidationItem(row);
                              setLiquidationDecision('approved');
                              setLiquidationComment('');
                              setIsLiquidationApproveOpen(true);
                            }}
                          />
                        </td>
                      </tr>
                    );
                  })
                )}
              </tbody>
            </table>
          ) : (
            <table className="asset-table requests-table">
              <thead>
                <tr>
                  <th>MÃ YÊU CẦU</th>
                  <th>PHÒNG BAN ĐỀ XUẤT</th>
                  <th>NGÀY GỬI</th>
                  <th>ĐƠN VỊ CHUYỂN</th>
                  <th>ĐƠN VỊ NHẬN</th>
                  <th>TRẠNG THÁI</th>
                  <th className="asset-table__cell asset-table__cell--actions" />
                </tr>
              </thead>
              <tbody>
                {pagedRows.length === 0 ? (
                  <tr>
                    <td colSpan={7} className="requests-table-empty">
                      Không có dữ liệu.
                    </td>
                  </tr>
                ) : (
                  (pagedRows as TransferTableRow[]).map((row) => {
                    const config = TRANSFER_STATUS_MAP[row.status] ?? TRANSFER_STATUS_MAP[1];
                    return (
                      <tr key={row.key} className="asset-row">
                        <td>
                          <button
                            type="button"
                            className="asset-code asset-code--link"
                            onClick={() => {
                              setSelectedTransfer(row);
                              setIsTransferDetailOpen(true);
                            }}
                          >
                            {row.code}
                          </button>
                        </td>
                        <td>{row.fromDepartment}</td>
                        <td>{row.transferDateText}</td>
                        <td>{row.fromDepartment}</td>
                        <td>{row.toDepartment}</td>
                        <td>
                          <span
                            className={
                              config.color === 'success'
                                ? 'asset-status-pill asset-status-pill--active'
                                : config.color === 'default'
                                ? 'asset-status-pill asset-status-pill--inactive'
                                : config.color === 'processing'
                                  ? 'asset-status-pill asset-status-pill--processing'
                                  : config.color === 'warning'
                                    ? 'asset-status-pill asset-status-pill--warning'
                                    : config.color === 'error'
                                      ? 'asset-status-pill asset-status-pill--danger'
                                      : 'asset-status-pill'
                            }
                          >
                            {config.label}
                          </span>
                        </td>
                        <td className="asset-table__cell asset-table__cell--actions">
                          <Button
                            type="text"
                            icon={<EyeOutlined />}
                            onClick={() => {
                              setSelectedTransfer(row);
                              setIsTransferDetailOpen(true);
                            }}
                          />
                        </td>
                      </tr>
                    );
                  })
                )}
              </tbody>
            </table>
          )}
        </div>

        <div className="requests-card__footer">
          <div className="requests-footer__left">
            Số lượng trên trang:
            <select
              className="requests-footer__select"
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
          <div className="requests-footer__center">
            {total === 0 ? '0-0 trên 0' : `${startIndex}-${endIndex} trên ${total}`}
          </div>
          <div className="requests-footer__right">
            <button
              className="requests-footer__pager"
              disabled={safePage <= 1}
              onClick={() => setPage((p) => Math.max(1, p - 1))}
              type="button"
            >
              ⟨
            </button>
            <button
              className="requests-footer__pager requests-footer__pager--active"
              type="button"
            >
              {safePage}
            </button>
            <button
              className="requests-footer__pager"
              disabled={safePage >= totalPages}
              onClick={() => setPage((p) => Math.min(totalPages, p + 1))}
              type="button"
            >
              ⟩
            </button>
          </div>
        </div>
      </div>

      <CreatePurchaseOrderModal
        open={isCreatePurchaseOpen}
        onClose={handleCloseCreatePurchase}
        onSubmit={handleSubmitPurchaseOrder}
        creatorName={userProfile?.name ?? userProfile?.email ?? null}
        initialValues={editingPurchaseDetail ? parseToFormValues(editingPurchaseDetail) : undefined}
      />

      <ViewPurchaseOrderModal
        open={isViewPurchaseOpen}
        onClose={() => {
          setIsViewPurchaseOpen(false);
          setSelectedPurchaseDetail(null);
        }}
        data={selectedPurchaseDetail}
        currentUserId={userProfile?.id ?? null}
        currentUserRole={userProfile?.role ?? null}
        onActionCompleted={async (assetRequestId, nextStatus) => {
          // Optimistic update to reflect status immediately on UI.
          if (typeof nextStatus === 'number') {
            setPurchaseRows((prev) =>
              prev.map((row) =>
                row.assetRequestId === assetRequestId ? { ...row, status: nextStatus } : row,
              ),
            );
            setSelectedPurchaseDetail((prev) =>
              prev && prev.assetRequestId === assetRequestId ? { ...prev, status: nextStatus } : prev,
            );
          }
          // Refresh detail + list so status pill updates immediately after approve/reject.
          try {
            const detail = await purchaseOrderService.getById(assetRequestId);
            setSelectedPurchaseDetail(detail);
          } catch {
            // ignore detail reload; list reload will still reflect status
          }
          await reloadPurchaseList();
        }}
      />

      <AccountantTransferRequestDetailModal
        open={isTransferDetailOpen}
        onClose={() => {
          setIsTransferDetailOpen(false);
          setSelectedTransfer(null);
        }}
        data={selectedTransfer}
        currentUserId={userProfile?.id ?? null}
        onActionCompleted={async () => {
          // Refresh list so status + visibility update after approve/reject
          try {
            const list = await transferRequestService.getList();
            setTransferRows(list.map((item, i) => toTransferTableRow(item, i)));
          } catch {
            // ignore; list may be refreshed on next page visit
          }
        }}
      />

      {canAccountantLiquidationApprove && isLiquidationApproveOpen && (
        <div className="acct-transfer-approve-overlay" role="dialog" aria-modal="true">
          <div className="acct-transfer-approve-modal">
            <div className="acct-transfer-approve-modal__header">
              <h3 className="acct-transfer-approve-modal__title">Duyệt yêu cầu thanh lý</h3>
            </div>

            <div className="acct-transfer-approve-modal__body">
              <div className="acct-transfer-approve-form">
                <div className="acct-transfer-approve-form__row">
                  <div className="acct-transfer-approve-form__field">
                    <label>Phê duyệt</label>
                    <select
                      className="acct-transfer-approve-select"
                      value={liquidationDecision}
                      onChange={(e) =>
                        setLiquidationDecision(e.target.value === 'rejected' ? 'rejected' : 'approved')
                      }
                    >
                      <option value="approved">Phê duyệt</option>
                      <option value="rejected">Từ chối</option>
                    </select>
                  </div>
                  <div className="acct-transfer-approve-form__field">
                    <label>Ghi chú</label>
                    <textarea
                      className="acct-transfer-approve-textarea"
                      placeholder="Không cần thiết"
                      value={liquidationComment}
                      onChange={(e) => setLiquidationComment(e.target.value)}
                    />
                  </div>
                </div>
              </div>
            </div>

            <div className="acct-transfer-approve-modal__footer">
              <button
                type="button"
                className="acct-transfer-approve-btn-back"
                onClick={() => setIsLiquidationApproveOpen(false)}
                disabled={liquidationSubmitting}
              >
                ← Quay lại
              </button>
              <button
                type="button"
                className="acct-transfer-approve-btn-submit"
                disabled={liquidationSubmitting}
                onClick={async () => {
                  if (!selectedLiquidationItem || !userProfile?.id) return;
                  setLiquidationSubmitting(true);
                  try {
                    const payload = {
                      approvedBy: userProfile.id,
                      comment: liquidationComment.trim() || null,
                    };
                    if (liquidationDecision === 'approved') {
                      await accountantRequestService.approve(selectedLiquidationItem.assetRequestId, payload);
                      message.success('Đã phê duyệt yêu cầu thanh lý.');
                    } else {
                      await accountantRequestService.reject(selectedLiquidationItem.assetRequestId, payload);
                      message.success('Đã từ chối yêu cầu thanh lý.');
                    }

                    setIsLiquidationApproveOpen(false);
                    setSelectedLiquidationItem(null);

                    setLiquidationLoading(true);
                    const list = await accountantRequestService.getLiquidationRequests();
                    setLiquidationRows(list);
                  } catch (e: unknown) {
                    const err = e as { response?: { data?: string } };
                    message.error(err?.response?.data ?? 'Thao tác duyệt thanh lý thất bại.');
                  } finally {
                    setLiquidationSubmitting(false);
                    setLiquidationLoading(false);
                  }
                }}
              >
                <span className="acct-transfer-btn-approve-icon">📋</span>
                <span>Xác nhận</span>
              </button>
            </div>
          </div>
        </div>
      )}

      {isDirectorDetailOpen && selectedDirectorItem && (
        <div className="acct-transfer-modal-overlay" role="dialog" aria-modal="true">
          <div className="acct-transfer-modal">
            {(() => {
              const map = getDirectorStatusMapForType(selectedDirectorItem.requestTypeId);
              const fallback =
                map === MAINT_REPAIR_STATUS_MAP ? MAINT_REPAIR_STATUS_MAP[0] : DIRECTOR_STATUS_MAP[0];
              const statusConfig = map[selectedDirectorItem.status] ?? fallback;
              const statusClassName =
                statusConfig.color === 'success'
                  ? 'acct-transfer-status-tag acct-transfer-status-tag--success'
                  : statusConfig.color === 'warning'
                    ? 'acct-transfer-status-tag acct-transfer-status-tag--warning'
                    : statusConfig.color === 'processing'
                      ? 'acct-transfer-status-tag acct-transfer-status-tag--processing'
                      : statusConfig.color === 'error'
                        ? 'acct-transfer-status-tag acct-transfer-status-tag--error'
                        : 'acct-transfer-status-tag';

              return (
                <>
                  <div className="acct-transfer-modal__header">
                    <div className="acct-transfer-modal__header-left">
                      <h2 className="acct-transfer-modal__title">
                        Chi tiết yêu cầu YC-{selectedDirectorItem.assetRequestId}
                      </h2>
                      <span className={statusClassName}>{statusConfig.label}</span>
                    </div>
                  </div>

                  <div className="acct-transfer-modal__body">
                    <div className="acct-transfer-modal__content">
                      <div className="acct-transfer-form__row">
                        <div className="acct-transfer-form__field">
                          <label>Mã yêu cầu</label>
                          <div className="acct-transfer-form__value">
                            YC-{selectedDirectorItem.assetRequestId}
                          </div>
                        </div>
                        <div className="acct-transfer-form__field">
                          <label>Ngày gửi</label>
                          <div className="acct-transfer-form__value">
                            {formatDate(selectedDirectorItem.createDate)}
                          </div>
                        </div>
                      </div>

                      <div className="acct-transfer-form__row">
                        <div className="acct-transfer-form__field">
                          <label>Phòng ban đề xuất</label>
                          <div className="acct-transfer-form__value">
                            {selectedDirectorItem.currentDepartmentName ?? '—'}
                          </div>
                        </div>
                        <div className="acct-transfer-form__field">
                          <label>Người tạo</label>
                          <div className="acct-transfer-form__value">
                            {selectedDirectorItem.creatorEmail ?? '—'}
                          </div>
                        </div>
                      </div>

                      <div className="acct-transfer-form__section">
                        <h3 className="acct-transfer-form__section-title">Nội dung yêu cầu</h3>
                        <div className="acct-transfer-form__value">
                          {selectedDirectorItem.title ?? '—'}
                        </div>
                      </div>

                      {(selectedDirectorItem.assetCode || selectedDirectorItem.assetName) && (
                        <div className="acct-transfer-form__row">
                          <div className="acct-transfer-form__field">
                            <label>Mã tài sản</label>
                            <div className="acct-transfer-form__value">
                              {selectedDirectorItem.assetCode ?? '—'}
                            </div>
                          </div>
                          <div className="acct-transfer-form__field">
                            <label>Tên tài sản</label>
                            <div className="acct-transfer-form__value">
                              {selectedDirectorItem.assetName ?? '—'}
                            </div>
                          </div>
                        </div>
                      )}

                      {selectedDirectorItem.description && (
                        <div className="acct-transfer-form__section">
                          <h3 className="acct-transfer-form__section-title">Mô tả</h3>
                          <div className="acct-transfer-form__value">
                            {selectedDirectorItem.description}
                          </div>
                        </div>
                      )}

                      {selectedDirectorItem.proposedData && (
                        <div className="acct-transfer-form__section">
                          <h3 className="acct-transfer-form__section-title">Dữ liệu đề xuất</h3>
                          <pre
                            className="acct-transfer-form__value"
                            style={{ whiteSpace: 'pre-wrap', wordBreak: 'break-word' }}
                          >
                            {selectedDirectorItem.proposedData}
                          </pre>
                        </div>
                      )}
                    </div>
                  </div>

                  <div className="acct-transfer-modal__footer">
                    <button
                      type="button"
                      onClick={() => {
                        setIsDirectorDetailOpen(false);
                        setSelectedDirectorItem(null);
                      }}
                      className="acct-transfer-btn-close"
                    >
                      Quay lại
                    </button>

                    {canDirectorApprove && (
                      <button
                        type="button"
                        className="acct-transfer-btn-approve"
                        onClick={() => {
                          setDirectorDecision('approved');
                          setDirectorComment('');
                          setIsDirectorApproveOpen(true);
                        }}
                      >
                        <span className="acct-transfer-btn-approve-icon">📋</span>
                        <span>Phê duyệt</span>
                      </button>
                    )}
                  </div>
                </>
              );
            })()}
          </div>
        </div>
      )}

      {canDirectorApprove && isDirectorApproveOpen && (
        <div className="acct-transfer-approve-overlay" role="dialog" aria-modal="true">
          <div className="acct-transfer-approve-modal">
            <div className="acct-transfer-approve-modal__header">
              <h3 className="acct-transfer-approve-modal__title">Phê duyệt yêu cầu</h3>
            </div>

            <div className="acct-transfer-approve-modal__body">
              <div className="acct-transfer-approve-form">
                <div className="acct-transfer-approve-form__row">
                  <div className="acct-transfer-approve-form__field">
                    <label>Phê duyệt</label>
                    <select
                      className="acct-transfer-approve-select"
                      value={directorDecision}
                      onChange={(e) =>
                        setDirectorDecision(e.target.value === 'rejected' ? 'rejected' : 'approved')
                      }
                    >
                      <option value="approved">Phê duyệt</option>
                      <option value="rejected">Từ chối</option>
                    </select>
                  </div>
                  <div className="acct-transfer-approve-form__field">
                    <label>Ghi chú</label>
                    <textarea
                      className="acct-transfer-approve-textarea"
                      placeholder="Không cần thiết"
                      value={directorComment}
                      onChange={(e) => setDirectorComment(e.target.value)}
                    />
                  </div>
                </div>
              </div>
            </div>

            <div className="acct-transfer-approve-modal__footer">
              <button
                type="button"
                className="acct-transfer-approve-btn-back"
                onClick={() => setIsDirectorApproveOpen(false)}
                disabled={directorSubmitting}
              >
                ← Quay lại
              </button>
              <button
                type="button"
                className="acct-transfer-approve-btn-submit"
                disabled={directorSubmitting}
                onClick={async () => {
                  if (!selectedDirectorItem || !userProfile?.id) return;
                  setDirectorSubmitting(true);
                  try {
                    const payload = {
                      approvedBy: userProfile.id,
                      comment: directorComment.trim() || null,
                    };
                    if (directorDecision === 'approved') {
                      await directorRequestService.approve(selectedDirectorItem.assetRequestId, payload);
                      message.success('Đã phê duyệt yêu cầu.');
                    } else {
                      await directorRequestService.reject(selectedDirectorItem.assetRequestId, payload);
                      message.success('Đã từ chối yêu cầu.');
                    }

                    setIsDirectorApproveOpen(false);
                    setIsDirectorDetailOpen(false);
                    setSelectedDirectorItem(null);

                    setDirectorLoading(true);
                    const res = await directorRequestService.getView({
                      requestTypeId: selectedDirectorItem.requestTypeId,
                      status: enforcedDirectorStatus ?? (statusFilter === 'all' ? undefined : statusFilter),
                      page: 1,
                      pageSize,
                    });
                    setPage(1);
                    setDirectorRows(res.items as DirectorRequestListItem[]);
                    setDirectorTotal(res.total);
                  } catch (e: unknown) {
                    const err = e as { response?: { data?: string } };
                    message.error(err?.response?.data ?? 'Thao tác phê duyệt thất bại.');
                  } finally {
                    setDirectorSubmitting(false);
                    setDirectorLoading(false);
                  }
                }}
              >
                <span className="acct-transfer-btn-approve-icon">📋</span>
                <span>Phê duyệt</span>
              </button>
            </div>
          </div>
        </div>
      )}
    </div>
  );
}


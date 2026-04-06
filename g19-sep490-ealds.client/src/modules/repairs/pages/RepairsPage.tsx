import { useEffect, useMemo, useState } from 'react';
import {
  Button,
  Checkbox,
  Input,
  Modal,
  Select,
  Tabs,
  message,
} from 'antd';
import {
  EyeOutlined,
  FilterOutlined,
  SearchOutlined,
  SettingOutlined,
} from '@ant-design/icons';
import './RepairsPage.css';
import { assetRequestService } from '../../assets/services/assetRequestService';
import {
  assetInstanceService,
  assetService,
  type AssetDetailResponse,
} from '../../assets/services/assetService';
import {
  repairRequestService,
  type DamagedInstancePendingItem,
  type RepairRequestListItem,
  type RepairStartPayload,
  type RepairCompletePayload,
} from '../../assets/services/repairRequestService';
import { directorRequestService } from '../../requests/services/directorRequestService';
import { profileService, type UserProfile } from '../../profile/services/profileService';
import { RepairStartModal, type RepairStartFormValues } from '../components/RepairStartModal';
import { RepairCompleteModal, type RepairCompleteFormValues } from '../components/RepairCompleteModal';
import {
  RepairProposalModal,
  type RepairProposalFormValues,
} from '../components/RepairProposalModal';
import { isDepartmentHeadRoleCode } from '../../../shared/utils/departmentHeadRole';

type RepairStatus =
  | 'needsProposal'
  | 'draft'
  | 'submitted'
  | 'pending'
  | 'approved'
  | 'rejected'
  | 'inProgress'
  | 'completed';

export type RepairRowSource = 'damaged' | 'repair';

export interface RepairRow {
  rowSource: RepairRowSource;
  id: string;
  assetRequestId: number | null;
  taskId?: number;
  assetId: number;
  assetInstanceId: number | null;
  assetCode: string;
  assetName: string;
  condition: string;
  brokenDate: string;
  quantity: number;
  location: string;
  department: string;
  status: RepairStatus;
  rawStatus: number;
  repairKind?: string | null;
}

function formatDate(value?: string | null): string {
  if (!value) return '';
  const d = new Date(value);
  if (Number.isNaN(d.getTime())) return value;
  return d.toLocaleDateString('vi-VN');
}

function parseDamageDescription(description?: string | null): {
  damageDate?: string | null;
  condition: string;
} {
  if (!description) return { condition: '' };
  const lines = description
    .split('\n')
    .map((line) => line.trim())
    .filter(Boolean);
  const damageDateLine = lines.find((line) => /^Ngày hỏng:\s*/i.test(line));
  const conditionLine = lines.find((line) => /^Tình trạng:\s*/i.test(line));
  const fallbackCondition = lines.join(' ').trim();
  return {
    damageDate: damageDateLine?.replace(/^Ngày hỏng:\s*/i, '').trim() || null,
    condition: conditionLine?.replace(/^Tình trạng:\s*/i, '').trim() || fallbackCondition,
  };
}

function mapStatus(status: number): RepairStatus {
  // -2=Chưa lập đơn (chỉ cá thể hỏng), -1=Nháp, 0=Đã gửi, 1=Chờ phê duyệt, 2=Đã duyệt, 3=Từ chối, 4=Đang sửa chữa, 5=Hoàn thành
  if (status === -2) return 'needsProposal';
  if (status === -1) return 'draft';
  if (status === 0) return 'submitted';
  if (status === 1) return 'pending';
  if (status === 2) return 'approved';
  if (status === 3) return 'rejected';
  if (status === 4) return 'inProgress';
  if (status === 5) return 'completed';
  return 'rejected';
}

function getStatusLabel(status: RepairStatus): string {
  if (status === 'needsProposal') return 'Chưa lập đơn';
  if (status === 'draft') return 'Chưa gửi';
  if (status === 'submitted') return 'Đã gửi';
  if (status === 'pending') return 'Chờ phê duyệt';
  if (status === 'approved') return 'Phê duyệt';
  if (status === 'inProgress') return 'Đang sửa chữa';
  if (status === 'completed') return 'Đã hoàn thành';
  return 'Từ chối';
}

function getStatusClass(status: RepairStatus): string {
  // Reuse the same status pill styles used across Purchase/Transfer pages
  if (status === 'needsProposal') return 'asset-status-pill asset-status-pill--inactive';
  if (status === 'submitted') return 'asset-status-pill asset-status-pill--processing';
  if (status === 'pending') return 'asset-status-pill asset-status-pill--warning';
  if (status === 'approved') return 'asset-status-pill asset-status-pill--active';
  if (status === 'inProgress') return 'asset-status-pill asset-status-pill--processing';
  if (status === 'completed') return 'asset-status-pill asset-status-pill--active';
  if (status === 'draft') return 'asset-status-pill asset-status-pill--inactive';
  return 'asset-status-pill asset-status-pill--danger';
}

function transferItemToRepairRow(t: RepairRequestListItem): RepairRow {
  const code = t.instanceCode ?? t.assetCode ?? '';
  return {
    rowSource: 'repair',
    id: `repair-${t.assetRequestId}`,
    assetRequestId: t.assetRequestId,
    taskId: t.recordId,
    assetId: 0,
    assetInstanceId: t.assetInstanceId ?? null,
    assetCode: code || String(t.assetInstanceId ?? t.assetRequestId),
    assetName: t.assetName || '(Không có tên)',
    condition: (t.damageCondition ?? '').trim(),
    brokenDate: formatDate(t.transferDate),
    quantity: 1,
    location: t.fromDepartment ?? '',
    department: t.fromDepartment ?? '',
    status: mapStatus(t.status),
    rawStatus: t.status,
    repairKind: t.requestDescription ?? null,
  };
}

function damagedPendingToRepairRow(d: DamagedInstancePendingItem): RepairRow {
  const parsed = parseDamageDescription(d.damageNote);
  return {
    rowSource: 'damaged',
    id: `damaged-${d.assetInstanceId}`,
    assetRequestId: null,
    assetId: d.assetId,
    assetInstanceId: d.assetInstanceId,
    assetCode: d.instanceCode || d.assetCode || String(d.assetInstanceId),
    assetName: d.assetName || '(Không có tên)',
    condition: parsed.condition,
    brokenDate: formatDate(parsed.damageDate),
    quantity: 1,
    location: d.location ?? '',
    department: d.fromDepartment ?? '',
    status: mapStatus(-2),
    rawStatus: -2,
    repairKind: null,
  };
}

export function RepairsPage() {
  const [activeTab, setActiveTab] = useState<'need-repair' | 'in-repair'>('need-repair');
  const [search, setSearch] = useState('');
  const [statusFilter, setStatusFilter] = useState<'all' | RepairStatus>('all');
  const [repairList, setRepairList] = useState<RepairRequestListItem[]>([]);
  const [damagedPending, setDamagedPending] = useState<DamagedInstancePendingItem[]>([]);
  const [loading, setLoading] = useState(false);
  const [profile, setProfile] = useState<UserProfile | null>(null);
  const [selected, setSelected] = useState<RepairRow | null>(null);
  const [detailOpen, setDetailOpen] = useState(false);
  const [approveOpen, setApproveOpen] = useState(false);
  const [decision, setDecision] = useState<'approved' | 'rejected'>('approved');
  const [comment, setComment] = useState('');
  const [submitting, setSubmitting] = useState(false);

  const [repairStartOpen, setRepairStartOpen] = useState(false);
  const [repairStartRow, setRepairStartRow] = useState<RepairRow | null>(null);
  const [repairAsset, setRepairAsset] = useState<AssetDetailResponse | null>(null);
  const [repairStartLoading, setRepairStartLoading] = useState(false);
  const [repairStartSubmitting, setRepairStartSubmitting] = useState(false);

  const [repairCompleteOpen, setRepairCompleteOpen] = useState(false);
  const [repairCompleteRow, setRepairCompleteRow] = useState<RepairRow | null>(null);
  const [repairCompleteTaskId, setRepairCompleteTaskId] = useState<number | null>(null);
  const [repairCompleteAsset, setRepairCompleteAsset] = useState<AssetDetailResponse | null>(null);
  const [repairCompleteLoading, setRepairCompleteLoading] = useState(false);
  const [repairCompleteSubmitting, setRepairCompleteSubmitting] = useState(false);
  const [repairCompleteReportNumber, setRepairCompleteReportNumber] = useState('');

  const [proposalOpen, setProposalOpen] = useState(false);
  const [proposalRows, setProposalRows] = useState<RepairRow[]>([]);
  const [proposalSubmitting, setProposalSubmitting] = useState(false);
  const [selectedDamagedRowIds, setSelectedDamagedRowIds] = useState<Set<string>>(() => new Set());

  useEffect(() => {
    let cancelled = false;

    async function loadAll() {
      setLoading(true);
      try {
        const [repairs, damaged] = await Promise.all([
          repairRequestService.list(),
          repairRequestService.listDamagedPending(),
        ]);
        if (!cancelled) {
          setRepairList(repairs);
          setDamagedPending(damaged);
        }
      } catch (e: any) {
        const msg = e?.response?.data?.title ?? e?.response?.data ?? e?.message;
        message.error(typeof msg === 'string' ? msg : 'Không tải được danh sách sửa chữa.');
        if (!cancelled) {
          setRepairList([]);
          setDamagedPending([]);
        }
      } finally {
        if (!cancelled) setLoading(false);
      }
    }

    loadAll();

    return () => {
      cancelled = true;
    };
  }, []);

  const needRepairRows: RepairRow[] = useMemo(() => {
    const repairPart = repairList
      .filter((t) => t.status !== 4 && t.status !== 5)
      .map(transferItemToRepairRow);
    const busyIds = new Set(
      repairPart.map((r) => r.assetInstanceId).filter((x): x is number => x != null)
    );
    const damagedPart = damagedPending
      .filter((d) => !busyIds.has(d.assetInstanceId))
      .map(damagedPendingToRepairRow);
    return [...repairPart, ...damagedPart].sort((a, b) =>
      (b.brokenDate || '').localeCompare(a.brokenDate || '')
    );
  }, [repairList, damagedPending]);

  const inRepairRows: RepairRow[] = useMemo(
    () => repairList.filter((t) => t.status === 4).map(transferItemToRepairRow),
    [repairList]
  );

  const tabRows = activeTab === 'in-repair' ? inRepairRows : needRepairRows;

  useEffect(() => {
    (async () => {
      try {
        const p = await profileService.getProfile();
        setProfile(p);
      } catch {
        setProfile(null);
      }
    })();
  }, []);

  const isDirector = String(profile?.role ?? '').toUpperCase() === 'DIRECTOR';
  const isDeptHead = isDepartmentHeadRoleCode(profile?.role);
  const showDeptHeadProposalUi = isDeptHead && activeTab === 'need-repair';
  const canDirectorApprove =
    isDirector &&
    !!selected &&
    selected.rowSource === 'repair' &&
    selected.assetRequestId != null &&
    selected.rawStatus === 1;

  const reload = async () => {
    try {
      const [repairs, damaged] = await Promise.all([
        repairRequestService.list(),
        repairRequestService.listDamagedPending(),
      ]);
      setRepairList(repairs);
      setDamagedPending(damaged);
    } catch {
      // ignore
    }
  };

  const openRepairStart = async (row: RepairRow) => {
    if (row.assetRequestId == null) return;
    // Backend only allows start when request is approved at final workflow step.
    if (row.rawStatus !== 2) {
      message.warning('Chỉ có thể bắt đầu sửa chữa khi yêu cầu đã được duyệt ở bước cuối.');
      return;
    }
    setRepairStartRow(row);
    setRepairStartLoading(true);
    setRepairStartOpen(true);
    try {
      const det = await assetRequestService.getById(row.assetRequestId!);
      const aid = det.asset?.assetId ?? row.assetId;
      if (aid) {
        const asset = await assetService.getById(aid);
        if (row.assetInstanceId) {
          try {
            const instanceDetail = await assetInstanceService.getById(row.assetInstanceId);
            const existing = asset.instances ?? [];
            const mergedInstances = [
              instanceDetail,
              ...existing.filter((i) => i.assetInstanceId !== instanceDetail.assetInstanceId),
            ];
            setRepairAsset({
              ...asset,
              instances: mergedInstances,
            });
          } catch {
            setRepairAsset(asset);
          }
        } else {
          setRepairAsset(asset);
        }
      } else {
        setRepairAsset(null);
      }
    } catch {
      message.error('Không tải được thông tin tài sản.');
      setRepairStartOpen(false);
      setRepairStartRow(null);
      setRepairAsset(null);
    } finally {
      setRepairStartLoading(false);
    }
  };

  const submitRepairStart = async (values: RepairStartFormValues) => {
    if (!repairStartRow?.assetRequestId || !profile?.id) return;
    if (!values.damageDate) {
      message.warning('Vui lòng chọn ngày hỏng.');
      return;
    }
    if (!values.repairDate) {
      message.warning('Vui lòng chọn ngày sửa chữa.');
      return;
    }
    if (!values.repairProgressStatus.trim()) {
      message.warning('Vui lòng nhập tình trạng sửa chữa.');
      return;
    }
    setRepairStartSubmitting(true);
    try {
      const payload: RepairStartPayload = {
        startedBy: profile.id,
        reportNumber: values.reportNumber || null,
        damageDate: values.damageDate,
        damageCondition: values.damageCondition || null,
        repairDate: values.repairDate,
        expectedCompletionDate: values.expectedCompletionDate,
        expectedCompletionFrom: undefined,
        expectedCompletionTo: undefined,
        repairProgressStatus: values.repairProgressStatus.trim(),
        comment: null,
      };

      await repairRequestService.start(repairStartRow.assetRequestId!, payload);
      message.success('Đã bắt đầu sửa chữa.');
      setRepairStartOpen(false);
      setRepairStartRow(null);
      setRepairAsset(null);
      await reload();
    } catch (e: any) {
      const msg = e?.response?.data ?? 'Không thể bắt đầu sửa chữa.';
      message.error(typeof msg === 'string' ? msg : 'Không thể bắt đầu sửa chữa.');
    } finally {
      setRepairStartSubmitting(false);
    }
  };

  const openRepairComplete = async (row: RepairRow) => {
    if (row.assetRequestId == null) return;
    if (row.rawStatus !== 4) {
      message.warning('Chỉ có thể hoàn thành khi đơn đang trong trạng thái đang sửa chữa.');
      return;
    }
    setRepairCompleteRow(row);
    setRepairCompleteLoading(true);
    setRepairCompleteOpen(true);
    try {
      const det = await assetRequestService.getById(row.assetRequestId!);
      const task =
        det.repairTasks?.find((t) => t.status === 1) ?? det.repairTasks?.[0] ?? null;
      if (!task?.taskId) {
        message.error('Không tìm thấy công việc sửa chữa đang thực hiện.');
        setRepairCompleteOpen(false);
        setRepairCompleteRow(null);
        setRepairCompleteTaskId(null);
        return;
      }
      setRepairCompleteTaskId(task.taskId);

      const aid = det.asset?.assetId ?? row.assetId;
      if (aid) {
        const asset = await assetService.getById(aid);
        if (row.assetInstanceId) {
          try {
            const instanceDetail = await assetInstanceService.getById(row.assetInstanceId);
            const existing = asset.instances ?? [];
            const mergedInstances = [
              instanceDetail,
              ...existing.filter((i) => i.assetInstanceId !== instanceDetail.assetInstanceId),
            ];
            setRepairCompleteAsset({
              ...asset,
              instances: mergedInstances,
            });
          } catch {
            setRepairCompleteAsset(asset);
          }
        } else {
          setRepairCompleteAsset(asset);
        }
      } else {
        setRepairCompleteAsset(null);
      }

      let rep = '';
      if (det.proposedData) {
        try {
          const pd = JSON.parse(det.proposedData) as Record<string, unknown>;
          if (typeof pd.reportNumber === 'string') rep = pd.reportNumber;
        } catch {
          /* ignore */
        }
      }
      setRepairCompleteReportNumber(rep);
    } catch {
      message.error('Không tải được thông tin tài sản.');
      setRepairCompleteOpen(false);
      setRepairCompleteRow(null);
      setRepairCompleteTaskId(null);
      setRepairCompleteAsset(null);
    } finally {
      setRepairCompleteLoading(false);
    }
  };

  const submitRepairComplete = async (values: RepairCompleteFormValues) => {
    if (!repairCompleteRow || !profile?.id || repairCompleteTaskId == null) return;
    if (!values.completionDate) {
      message.warning('Vui lòng chọn ngày hoàn thành sửa chữa.');
      return;
    }
    if (!values.returnToUseDate) {
      message.warning('Vui lòng chọn ngày đưa vào sử dụng lại.');
      return;
    }
    if (values.actualCost == null || values.actualCost < 0) {
      message.warning('Vui lòng nhập chi phí thực tế.');
      return;
    }

    setRepairCompleteSubmitting(true);
    try {
      const payload: RepairCompletePayload = {
        completedBy: profile.id,
        reportNumber: values.reportNumber || null,
        completionDate: values.completionDate,
        returnToUseDate: values.returnToUseDate,
        actualCost: values.actualCost,
        result: values.result || undefined,
        detailedDescription: values.detail || null,
        attachmentUrls: null,
      };

      await repairRequestService.complete(repairCompleteTaskId, payload);
      message.success('Đã hoàn thành sửa chữa.');
      setRepairCompleteOpen(false);
      setRepairCompleteRow(null);
      setRepairCompleteTaskId(null);
      setRepairCompleteAsset(null);
      await reload();
    } catch (e: any) {
      const msg = e?.response?.data ?? 'Không thể hoàn thành sửa chữa.';
      message.error(typeof msg === 'string' ? msg : 'Không thể hoàn thành sửa chữa.');
    } finally {
      setRepairCompleteSubmitting(false);
    }
  };

  const submitDirectorDecision = async () => {
    if (!selected?.assetRequestId || !profile?.id) return;
    setSubmitting(true);
    try {
      const payload = { approvedBy: profile.id, comment: comment.trim() || null };
      if (decision === 'approved') {
        await directorRequestService.approve(selected.assetRequestId!, payload);
        message.success('Đã phê duyệt yêu cầu sửa chữa.');
      } else {
        await directorRequestService.reject(selected.assetRequestId!, payload);
        message.success('Đã từ chối yêu cầu sửa chữa.');
      }
      setApproveOpen(false);
      setDetailOpen(false);
      setSelected(null);
      await reload();
    } catch (e: any) {
      const msg = e?.response?.data ?? 'Thao tác phê duyệt thất bại.';
      message.error(typeof msg === 'string' ? msg : 'Thao tác phê duyệt thất bại.');
    } finally {
      setSubmitting(false);
    }
  };

  const submitRepairProposal = async (values: RepairProposalFormValues) => {
    const rows = proposalRows.filter((r) => r.assetInstanceId != null);
    if (!rows.length || !profile?.id) return;
    setProposalSubmitting(true);
    try {
      const damageCondition = values.damageCondition.trim();
      const repairKind = values.repairKind.trim();
      for (const row of rows) {
        await repairRequestService.create({
          assetInstanceId: row.assetInstanceId!,
          createdBy: profile.id,
          damageCondition,
          repairKind,
        });
      }
      message.success(
        rows.length > 1
          ? `Đã gửi ${rows.length} đơn sửa chữa, chờ giám đốc phê duyệt.`
          : 'Đã gửi đơn sửa chữa, chờ giám đốc phê duyệt.',
      );
      setProposalOpen(false);
      setProposalRows([]);
      setSelectedDamagedRowIds(new Set());
      await reload();
    } catch (e: any) {
      const msg = e?.response?.data ?? 'Gửi đơn sửa chữa thất bại.';
      message.error(typeof msg === 'string' ? msg : 'Gửi đơn sửa chữa thất bại.');
    } finally {
      setProposalSubmitting(false);
    }
  };

  const filteredData: RepairRow[] = useMemo(() => {
    const keyword = search.trim().toLowerCase();
    return tabRows.filter((row) => {
      const matchStatus = statusFilter === 'all' || row.status === statusFilter;
      const matchKeyword =
        !keyword ||
        row.assetCode.toLowerCase().includes(keyword) ||
        row.assetName.toLowerCase().includes(keyword) ||
        row.condition.toLowerCase().includes(keyword);
      return matchStatus && matchKeyword;
    });
  }, [tabRows, search, statusFilter]);

  const damagedRowsInView = useMemo(
    () => filteredData.filter((r) => r.rowSource === 'damaged'),
    [filteredData],
  );

  useEffect(() => {
    if (!showDeptHeadProposalUi) {
      setSelectedDamagedRowIds(new Set());
      return;
    }
    const allowed = new Set(damagedRowsInView.map((r) => r.id));
    setSelectedDamagedRowIds((prev) => {
      const next = new Set<string>();
      for (const id of prev) {
        if (allowed.has(id)) next.add(id);
      }
      return next.size === prev.size && [...prev].every((id) => allowed.has(id)) ? prev : next;
    });
  }, [showDeptHeadProposalUi, damagedRowsInView]);

  const tableColCount = showDeptHeadProposalUi ? 10 : 9;
  const allDamagedInViewSelected =
    damagedRowsInView.length > 0 &&
    damagedRowsInView.every((r) => selectedDamagedRowIds.has(r.id));
  const someDamagedInViewSelected = damagedRowsInView.some((r) =>
    selectedDamagedRowIds.has(r.id),
  );

  return (
    <div className="repairs-page">
      <h1 className="repairs-page__title">Sửa chữa</h1>

      <div className="repairs-card">
        <div className="repairs-card__tabs-row">
          <Tabs
            activeKey={activeTab}
            onChange={(key) => setActiveTab(key as 'need-repair' | 'in-repair')}
            className="repairs-tabs"
            items={[
              { key: 'need-repair', label: 'Tài sản cần sửa chữa' },
              { key: 'in-repair', label: 'Đang sửa chữa' },
            ]}
          />
          {showDeptHeadProposalUi ? (
            <Button
              type="primary"
              className="repairs-card__proposal-action"
              disabled={selectedDamagedRowIds.size === 0}
              onClick={() => {
                const picked = filteredData.filter(
                  (r) => r.rowSource === 'damaged' && selectedDamagedRowIds.has(r.id),
                );
                if (!picked.length) return;
                setProposalRows(picked);
                setProposalOpen(true);
              }}
            >
              Tạo đơn sửa chữa
            </Button>
          ) : null}
        </div>

        <div className="repairs-filters">
          <Input
            placeholder="Tìm kiếm"
            prefix={<SearchOutlined />}
            className="repairs-search"
            value={search}
            onChange={(e) => setSearch(e.target.value)}
          />
          <Select
            placeholder="Trạng thái"
            className="repairs-select"
            suffixIcon={<FilterOutlined />}
            value={statusFilter}
            onChange={(v) => setStatusFilter(v)}
            options={[
              { value: 'all', label: 'Tất cả' },
              { value: 'needsProposal', label: 'Chưa lập đơn' },
              { value: 'draft', label: 'Chưa gửi' },
              { value: 'submitted', label: 'Đã gửi' },
              { value: 'pending', label: 'Chờ phê duyệt' },
              { value: 'approved', label: 'Phê duyệt' },
              { value: 'inProgress', label: 'Đang sửa chữa' },
              { value: 'completed', label: 'Đã hoàn thành' },
              { value: 'rejected', label: 'Từ chối' },
            ]}
          />
          <Button
            icon={<FilterOutlined />}
            className="repairs-filter-advanced"
            onClick={() => {
              setSearch('');
              setStatusFilter('all');
            }}
          >
            Gỡ bộ lọc
          </Button>
          <Button icon={<SettingOutlined />} className="repairs-settings" />
        </div>

        <div className="asset-table-wrapper repairs-table-wrapper">
          <table className="asset-table repairs-table">
            <thead>
              <tr>
                {showDeptHeadProposalUi ? (
                  <th className="repairs-table__th-select">
                    <Checkbox
                      aria-label="Chọn tất cả tài sản chưa lập đơn"
                      indeterminate={someDamagedInViewSelected && !allDamagedInViewSelected}
                      checked={allDamagedInViewSelected}
                      disabled={damagedRowsInView.length === 0}
                      onChange={(e) => {
                        if (e.target.checked) {
                          setSelectedDamagedRowIds(new Set(damagedRowsInView.map((r) => r.id)));
                        } else {
                          setSelectedDamagedRowIds(new Set());
                        }
                      }}
                    />
                  </th>
                ) : null}
                <th>MÃ CÁ THỂ</th>
                <th>TÊN TÀI SẢN</th>
                <th>TÌNH TRẠNG</th>
                <th>NGÀY HỎNG</th>
                <th>SỐ LƯỢNG</th>
                <th>VỊ TRÍ TÀI SẢN</th>
                <th>PHÒNG BAN QUẢN LÝ</th>
                <th>TRẠNG THÁI</th>
                <th className="asset-table__cell asset-table__cell--actions" />
              </tr>
            </thead>
            <tbody>
              {loading ? (
                <tr>
                  <td colSpan={tableColCount} className="repairs-table-empty">
                    Đang tải dữ liệu...
                  </td>
                </tr>
              ) : filteredData.length === 0 ? (
                <tr>
                  <td colSpan={tableColCount} className="repairs-table-empty">
                    Không có dữ liệu.
                  </td>
                </tr>
              ) : (
                filteredData.map((row) => (
                  <tr key={row.id} className="asset-row">
                    {showDeptHeadProposalUi ? (
                      <td className="repairs-table__td-select">
                        {row.rowSource === 'damaged' ? (
                          <Checkbox
                            aria-label={`Chọn ${row.assetCode}`}
                            checked={selectedDamagedRowIds.has(row.id)}
                            onChange={(e) => {
                              setSelectedDamagedRowIds((prev) => {
                                const next = new Set(prev);
                                if (e.target.checked) next.add(row.id);
                                else next.delete(row.id);
                                return next;
                              });
                            }}
                          />
                        ) : null}
                      </td>
                    ) : null}
                    <td>
                      <button type="button" className="asset-code asset-code--link">
                        {row.assetCode}
                      </button>
                    </td>
                    <td>{row.assetName}</td>
                    <td>{row.condition}</td>
                    <td>{row.brokenDate}</td>
                    <td className="asset-align-right">{row.quantity}</td>
                    <td>{row.location}</td>
                    <td>{row.department}</td>
                    <td>
                      <span className={getStatusClass(row.status)}>{getStatusLabel(row.status)}</span>
                    </td>
                    <td className="asset-table__cell asset-table__cell--actions">
                      <Button
                        type="text"
                        icon={<EyeOutlined />}
                        onClick={() => {
                          setSelected(row);
                          setDetailOpen(true);
                        }}
                      />
                      {activeTab === 'need-repair' &&
                      row.rowSource === 'damaged' &&
                      !isDeptHead ? (
                        <Button
                          type="link"
                          size="small"
                          onClick={() => {
                            setProposalRows([row]);
                            setProposalOpen(true);
                          }}
                        >
                          Tạo đơn sửa chữa
                        </Button>
                      ) : null}
                      {activeTab === 'need-repair' &&
                      row.rowSource === 'repair' &&
                      row.status === 'approved' ? (
                        <Button type="link" size="small" onClick={() => openRepairStart(row)}>
                          Bắt đầu SC
                        </Button>
                      ) : null}
                      {activeTab === 'in-repair' && row.status === 'inProgress' ? (
                        <Button type="link" size="small" onClick={() => openRepairComplete(row)}>
                          Hoàn thành SC
                        </Button>
                      ) : null}
                    </td>
                  </tr>
                ))
              )}
            </tbody>
          </table>
        </div>

        <div className="repairs-card__footer">
          <div className="repairs-footer__left">
            Số lượng trên trang:
            <select className="repairs-footer__select" defaultValue={25}>
              <option value={10}>10</option>
              <option value={25}>25</option>
              <option value={50}>50</option>
              <option value={100}>100</option>
            </select>
          </div>
          <div className="repairs-footer__center">1-25 trên 289</div>
          <div className="repairs-footer__right">
            <button className="repairs-footer__pager" disabled type="button">
              ⟨
            </button>
            <button
              className="repairs-footer__pager repairs-footer__pager--active"
              type="button"
            >
              1
            </button>
            <button className="repairs-footer__pager" type="button">
              ⟩
            </button>
          </div>
        </div>
      </div>

      {detailOpen ? (
        <div className="repair-detail-modal-overlay" role="dialog" aria-modal="true">
          <div className="repair-detail-modal">
            <button
              type="button"
              className="repair-detail-modal__close-btn"
              onClick={() => {
                setDetailOpen(false);
                setSelected(null);
              }}
              aria-label="Đóng"
            >
              <span className="repair-detail-modal__close">×</span>
            </button>

            <div className="repair-detail-modal__header">
              <div className="repair-detail-modal__header-row">
                <h2 className="repair-detail-modal__title">
                  {selected
                    ? selected.rowSource === 'repair' && selected.assetRequestId != null
                      ? `Chi tiết yêu cầu SC - YC-${selected.assetRequestId}`
                      : 'Tài sản chờ lập đơn sửa chữa'
                    : 'Chi tiết yêu cầu sửa chữa'}
                </h2>
                {selected ? (
                  <span className={getStatusClass(selected.status)}>
                    {getStatusLabel(selected.status)}
                  </span>
                ) : null}
              </div>
            </div>

            <div className="repair-detail-modal__body">
              {!selected ? (
                <div className="repair-detail-modal__empty">Không có dữ liệu.</div>
              ) : (
                <div className="repair-detail-modal__content">
                  <div className="repair-detail-info-section">
                    <h3 className="repair-detail-section-title">
                      {selected.rowSource === 'repair'
                        ? 'Thông tin yêu cầu sửa chữa'
                        : 'Thông tin tài sản hỏng'}
                    </h3>
                    <div className="repair-detail-info-grid">
                      <div className="repair-detail-info-row">
                        <div className="repair-detail-info-item">
                          <label>Mã cá thể</label>
                          <div className="repair-detail-info-value">{selected.assetCode || '—'}</div>
                        </div>
                        <div className="repair-detail-info-item">
                          <label>Tên tài sản</label>
                          <div className="repair-detail-info-value">{selected.assetName || '—'}</div>
                        </div>
                      </div>
                      <div className="repair-detail-info-row">
                        <div className="repair-detail-info-item">
                          <label>
                            {selected.rowSource === 'repair'
                              ? 'Tình trạng hỏng hóc'
                              : 'Ghi nhận khi đánh dấu hỏng'}
                          </label>
                          <div className="repair-detail-info-value">{selected.condition || '—'}</div>
                        </div>
                        <div className="repair-detail-info-item">
                          <label>Ngày hỏng</label>
                          <div className="repair-detail-info-value">{selected.brokenDate || '—'}</div>
                        </div>
                      </div>
                      {selected.rowSource === 'repair' && selected.repairKind ? (
                        <div className="repair-detail-info-row">
                          <div className="repair-detail-info-item repair-detail-info-item--full">
                            <label>Phương án sửa chữa đề xuất</label>
                            <div className="repair-detail-info-value">{selected.repairKind}</div>
                          </div>
                        </div>
                      ) : null}
                      <div className="repair-detail-info-row">
                        <div className="repair-detail-info-item">
                          <label>Vị trí tài sản</label>
                          <div className="repair-detail-info-value">{selected.location || '—'}</div>
                        </div>
                        <div className="repair-detail-info-item">
                          <label>Phòng ban quản lý</label>
                          <div className="repair-detail-info-value">{selected.department || '—'}</div>
                        </div>
                      </div>
                      <div className="repair-detail-info-row">
                        <div className="repair-detail-info-item repair-detail-info-item--full">
                          <label>Số lượng</label>
                          <div className="repair-detail-info-value">{selected.quantity}</div>
                        </div>
                      </div>
                    </div>
                  </div>
                </div>
              )}
            </div>

            <div className="repair-detail-modal__footer">
              {canDirectorApprove ? (
                <button
                  type="button"
                  className="repair-detail-btn-approve"
                  onClick={() => {
                    setDecision('approved');
                    setComment('');
                    setApproveOpen(true);
                  }}
                >
                  Phê duyệt
                </button>
              ) : null}
              <button
                type="button"
                className="repair-detail-btn-close"
                onClick={() => {
                  setDetailOpen(false);
                  setSelected(null);
                }}
              >
                Đóng
              </button>
            </div>
          </div>
        </div>
      ) : null}

      <RepairStartModal
        open={repairStartOpen}
        loading={repairStartLoading}
        submitting={repairStartSubmitting}
        row={repairStartRow}
        asset={repairAsset}
        onClose={() => {
          setRepairStartOpen(false);
          setRepairStartRow(null);
          setRepairAsset(null);
        }}
        onSubmit={submitRepairStart}
      />

      <RepairCompleteModal
        open={repairCompleteOpen}
        loading={repairCompleteLoading}
        submitting={repairCompleteSubmitting}
        row={repairCompleteRow}
        asset={repairCompleteAsset}
        defaultReportNumber={repairCompleteReportNumber}
        onClose={() => {
          setRepairCompleteOpen(false);
          setRepairCompleteRow(null);
          setRepairCompleteTaskId(null);
          setRepairCompleteAsset(null);
        }}
        onSubmit={submitRepairComplete}
      />

      <RepairProposalModal
        open={proposalOpen}
        loading={proposalSubmitting}
        items={proposalRows.map((r) => ({
          assetCode: r.assetCode,
          assetName: r.assetName,
        }))}
        onClose={() => {
          setProposalOpen(false);
          setProposalRows([]);
        }}
        onSubmit={submitRepairProposal}
      />

      <Modal
        open={approveOpen}
        title="Phê duyệt yêu cầu sửa chữa"
        onCancel={() => setApproveOpen(false)}
        okText="Xác nhận"
        confirmLoading={submitting}
        onOk={submitDirectorDecision}
      >
        <div style={{ display: 'grid', gap: 12 }}>
          <div>
            <label>Quyết định</label>
            <select
              style={{ width: '100%', padding: 8, marginTop: 6 }}
              value={decision}
              onChange={(e) => setDecision(e.target.value === 'rejected' ? 'rejected' : 'approved')}
            >
              <option value="approved">Phê duyệt</option>
              <option value="rejected">Từ chối</option>
            </select>
          </div>
          <div>
            <label>Ghi chú</label>
            <textarea
              style={{ width: '100%', padding: 8, marginTop: 6, minHeight: 90 }}
              placeholder="Không cần thiết"
              value={comment}
              onChange={(e) => setComment(e.target.value)}
            />
          </div>
        </div>
      </Modal>
    </div>
  );
}


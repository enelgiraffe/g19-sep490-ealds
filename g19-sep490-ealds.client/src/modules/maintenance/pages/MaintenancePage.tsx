import { useEffect, useMemo, useState } from 'react';
import {
  Button,
  Input,
  Modal,
  Popconfirm,
  Select,
  Tabs,
  message,
} from 'antd';
import dayjs from 'dayjs';
import {
  DeleteOutlined,
  EyeOutlined,
  FilterOutlined,
  EditOutlined,
  SearchOutlined,
  SettingOutlined,
} from '@ant-design/icons';
import './MaintenancePage.css';
import {
  maintenanceRequestService,
  type MaintenanceCompletePayload,
  type MaintenanceRequestListItemDTO,
  type MaintenanceStartPayload,
} from '../../assets/services/maintenanceRequestService';
import { assetRequestService } from '../../assets/services/assetRequestService';
import {
  assetInstanceService,
  assetService,
  formatVnd,
  getStatusLabel as getAssetStatusLabel,
  type AssetInstanceResponse,
} from '../../assets/services/assetService';
import { directorRequestService } from '../../requests/services/directorRequestService';
import { profileService, type UserProfile } from '../../profile/services/profileService';
import { maintenanceTemplateService, type MaintenanceTemplateItem } from '../services/maintenanceTemplateService';

type MaintenanceStatus =
  | 'draft'
  | 'submitted'
  | 'pending'
  | 'approved'
  | 'rejected'
  | 'inProgress'
  | 'completed';

interface MaintenanceRow {
  id: string; // taskId (recordId from backend list)
  assetRequestId: number;
  assetInstanceId: number | null;
  assetCode: string;
  assetName: string;
  assetType: string;
  purpose: string;
  setupDate: string;
  expectedDate: string;
  assetState: string;
  status: MaintenanceStatus;
  rawStatus: number;
}

interface MaintenanceStartMeta {
  maintenanceProvider: string | null;
  location: string | null;
  locationType: string | null;
}

function getApiErrorMessage(error: unknown, fallback: string): string {
  const axiosErr = error as { response?: { data?: unknown } };
  const data = axiosErr?.response?.data;
  if (typeof data === 'string' && data.trim()) return data;
  if (data && typeof data === 'object' && 'message' in (data as Record<string, unknown>)) {
    const msg = (data as Record<string, unknown>).message;
    if (typeof msg === 'string' && msg.trim()) return msg;
  }
  return fallback;
}

function getStatusLabel(status: MaintenanceStatus): string {
  if (status === 'draft') return 'Chưa gửi';
  if (status === 'submitted') return 'Đã gửi';
  if (status === 'pending') return 'Chờ phê duyệt';
  if (status === 'approved') return 'Phê duyệt';
  if (status === 'inProgress') return 'Đang bảo dưỡng';
  if (status === 'completed') return 'Đã bảo dưỡng';
  return 'Từ chối';
}

function getStatusClass(status: MaintenanceStatus): string {
  if (status === 'draft') return 'maintenance-status-pill maintenance-status-pill--draft';
  if (status === 'submitted') return 'maintenance-status-pill maintenance-status-pill--pending';
  if (status === 'pending') {
    return 'maintenance-status-pill maintenance-status-pill--pending';
  }
  if (status === 'approved') {
    return 'maintenance-status-pill maintenance-status-pill--approved';
  }
  if (status === 'inProgress') {
    return 'maintenance-status-pill maintenance-status-pill--approved';
  }
  if (status === 'completed') {
    return 'maintenance-status-pill maintenance-status-pill--completed';
  }
  return 'maintenance-status-pill maintenance-status-pill--rejected';
}

function formatDate(value?: string | null): string {
  if (!value) return '';
  const d = new Date(value);
  if (Number.isNaN(d.getTime())) return value;
  return d.toLocaleDateString('vi-VN');
}

function toIsoDate(value: string): string {
  return new Date(`${value}T00:00:00`).toISOString();
}

function toTodayInputDate(): string {
  return dayjs().format('YYYY-MM-DD');
}

function generateMaintenanceReportNumber(): string {
  const datePart = dayjs().format('YYYYMMDD');
  const randomPart = Math.floor(Math.random() * 900 + 100);
  return `BB-BD-${datePart}-${randomPart}`;
}

function getWarrantyEndDateDisplay(instance?: AssetInstanceResponse | null): string {
  if (!instance) return '—';
  if (instance.warrantyEndDate) return formatDate(instance.warrantyEndDate) || '—';
  const guaranteeEnd = instance.guarantees?.[0]?.warrantyEndDate;
  if (guaranteeEnd) return formatDate(guaranteeEnd) || '—';
  return '—';
}

function parseMoneyInput(value: string): number | null {
  const digits = value.replace(/[^\d]/g, '');
  if (!digits) return null;
  return Number(digits);
}

function mapStatus(status: number): MaintenanceStatus {
  // -1=Nháp, 0=Đã gửi, 1=Chờ phê duyệt, 2=Đã duyệt (chưa bắt đầu), 3=Từ chối, 4=Đang thực hiện (đã start), 5=Đã bảo dưỡng
  if (status === -1) return 'draft';
  if (status === 0) return 'submitted';
  if (status === 1) return 'pending';
  if (status === 2) return 'approved';
  if (status === 3) return 'rejected';
  if (status === 4) return 'inProgress';
  if (status === 5) return 'completed';
  return 'submitted';
}

function mapListToRows(list: MaintenanceRequestListItemDTO[]): MaintenanceRow[] {
  return list.map((it) => ({
    id: String(it.recordId),
    assetRequestId: it.assetRequestId,
    assetInstanceId: it.assetInstanceId ?? null,
    assetCode: it.instanceCode ?? it.assetCode,
    assetName: it.assetName,
    assetType: it.assetTypeName ?? '',
    purpose: it.reason ?? '',
    setupDate: formatDate(it.transferDate),
    expectedDate: formatDate(it.transferDate),
    assetState: it.fromDepartment || it.toDepartment || '',
    status: mapStatus(it.status),
    rawStatus: it.status,
  }));
}

type MaintenanceTemplateForm = {
  assetTypeId: number | null;
  name: string;
  content: string;
  frequencyType: 1 | 2;
  repeatIntervalValue: number;
  repeatIntervalUnit: 1 | 2 | 3 | 4;
  /** yyyy-MM-dd khi tần suất = một lần */
  oneTimeScheduledDate: string;
  isActive: boolean;
};

function parseEnumNumber(value: number | string | null | undefined): number {
  if (typeof value === 'number') return value;
  if (!value) return 0;
  const parsed = Number(value);
  if (Number.isFinite(parsed)) return parsed;
  const normalized = String(value).toLowerCase();
  if (normalized === 'onetime') return 1;
  if (normalized === 'periodic') return 2;
  if (normalized === 'day') return 1;
  if (normalized === 'week') return 2;
  if (normalized === 'month') return 3;
  if (normalized === 'year') return 4;
  return 0;
}

function getFrequencyLabel(value: number | string): string {
  return parseEnumNumber(value) === 1 ? 'Một lần' : 'Định kỳ';
}

function getRepeatUnitLabel(value: number | string): string {
  const parsed = parseEnumNumber(value);
  if (parsed === 1) return 'Ngày';
  if (parsed === 2) return 'Tuần';
  if (parsed === 3) return 'Tháng';
  if (parsed === 4) return 'Năm';
  return '—';
}

export function MaintenancePage() {
  const [activeTab, setActiveTab] = useState<'need-maintenance' | 'in-maintenance'>(
    'need-maintenance'
  );
  const [search, setSearch] = useState('');
  const [statusFilter, setStatusFilter] = useState<'all' | MaintenanceStatus>('all');
  const [rows, setRows] = useState<MaintenanceRow[]>([]);
  const [loading, setLoading] = useState(false);
  const [profile, setProfile] = useState<UserProfile | null>(null);
  const [selected, setSelected] = useState<MaintenanceRow | null>(null);
  const [detailOpen, setDetailOpen] = useState(false);
  const [detailMeta, setDetailMeta] = useState<MaintenanceStartMeta | null>(null);
  const [approveOpen, setApproveOpen] = useState(false);
  const [decision, setDecision] = useState<'approved' | 'rejected'>('approved');
  const [comment, setComment] = useState('');
  const [submitting, setSubmitting] = useState(false);

  /** Modal bắt đầu bảo dưỡng (sau phê duyệt) */
  const [startOpen, setStartOpen] = useState(false);
  const [startRow, setStartRow] = useState<MaintenanceRow | null>(null);
  const [startAsset, setStartAsset] = useState<AssetInstanceResponse | null>(null);
  const [startLoading, setStartLoading] = useState(false);
  const [startSubmitting, setStartSubmitting] = useState(false);
  const [reportNumber, setReportNumber] = useState('');
  const [maintenanceDate, setMaintenanceDate] = useState('');
  const [expectedCompletionDate, setExpectedCompletionDate] = useState('');
  const [detailedDescription, setDetailedDescription] = useState('');
  const [maintenanceProvider, setMaintenanceProvider] = useState('');
  const [locationType, setLocationType] = useState<'at-unit' | 'provider'>('at-unit');

  /** Modal hoàn thành bảo dưỡng (task đang thực hiện) */
  const [completeOpen, setCompleteOpen] = useState(false);
  const [completeRow, setCompleteRow] = useState<MaintenanceRow | null>(null);
  const [completeAsset, setCompleteAsset] = useState<AssetInstanceResponse | null>(null);
  const [completeLoading, setCompleteLoading] = useState(false);
  const [completeSubmitting, setCompleteSubmitting] = useState(false);
  const [completeReportNumber, setCompleteReportNumber] = useState('');
  const [completeMaintenanceDateLabel, setCompleteMaintenanceDateLabel] = useState('');
  const [completeMaintenanceStartDateRaw, setCompleteMaintenanceStartDateRaw] = useState('');
  const [completeCompletionDate, setCompleteCompletionDate] = useState('');
  const [completeReturnDate, setCompleteReturnDate] = useState('');
  const [completeActualCost, setCompleteActualCost] = useState<number | null>(null);
  const [completeMaintenanceContent, setCompleteMaintenanceContent] = useState('');
  const [completeDetailedDescription, setCompleteDetailedDescription] = useState('');
  const [assetTypes, setAssetTypes] = useState<Array<{ assetTypeId: number; name: string }>>([]);

  /** Modal thiết lập quy định bảo dưỡng */
  const [setupTemplateOpen, setSetupTemplateOpen] = useState(false);
  const [templatesLoading, setTemplatesLoading] = useState(false);
  const [templates, setTemplates] = useState<MaintenanceTemplateItem[]>([]);
  const [templateSearch, setTemplateSearch] = useState('');
  const [templateFormOpen, setTemplateFormOpen] = useState(false);
  const [templateFormSubmitting, setTemplateFormSubmitting] = useState(false);
  const [editingTemplateId, setEditingTemplateId] = useState<number | null>(null);
  const [deleteTemplateId, setDeleteTemplateId] = useState<number | null>(null);
  const [templateForm, setTemplateForm] = useState<MaintenanceTemplateForm>({
    assetTypeId: null,
    name: '',
    content: '',
    frequencyType: 2,
    repeatIntervalValue: 1,
    repeatIntervalUnit: 3,
    oneTimeScheduledDate: toTodayInputDate(),
    isActive: true,
  });

  useEffect(() => {
    let cancelled = false;

    async function loadMaintenanceList() {
      setLoading(true);
      try {
        const list = await maintenanceRequestService.list();
        const mapped = mapListToRows(list);
        if (!cancelled) setRows(mapped);
      } catch {
        message.error('Không tải được danh sách bảo dưỡng. Vui lòng thử lại.');
        if (!cancelled) setRows([]);
      } finally {
        if (!cancelled) setLoading(false);
      }
    }

    if (activeTab === 'need-maintenance' || activeTab === 'in-maintenance') {
      loadMaintenanceList();
    }

    return () => {
      cancelled = true;
    };
  }, [activeTab]);

  useEffect(() => {
    (async () => {
      try {
        const p = await profileService.getProfile();
        setProfile(p);
      } catch (e: unknown) {
        setProfile(null);
        const status = (e as { response?: { status?: number } })?.response?.status;
        if (status === 401) {
          message.error('Phiên đăng nhập đã hết hạn. Vui lòng đăng nhập lại.');
        } else {
          message.error('Không tải được thông tin người dùng. Vui lòng thử lại.');
        }
      }
    })();
  }, []);

  const isDirector = String(profile?.role ?? '').toUpperCase() === 'DIRECTOR';
  const canDirectorApprove = isDirector && !!selected && selected.rawStatus === 1;

  const loadTemplates = async () => {
    setTemplatesLoading(true);
    try {
      const data = await maintenanceTemplateService.getAll();
      setTemplates(Array.isArray(data) ? data : []);
    } catch {
      message.error('Không tải được danh sách quy định bảo dưỡng.');
      setTemplates([]);
    } finally {
      setTemplatesLoading(false);
    }
  };

  const reload = async () => {
    try {
      const list = await maintenanceRequestService.list();
      setRows(mapListToRows(list));
    } catch {
      // ignore
    }
  };

  const loadDetailMeta = async (assetRequestId: number) => {
    if (!assetRequestId || assetRequestId <= 0) {
      setDetailMeta(null);
      return;
    }
    try {
      const detail = await assetRequestService.getById(assetRequestId);
      if (!detail?.proposedData) {
        setDetailMeta(null);
        return;
      }
      const parsed = JSON.parse(detail.proposedData) as Record<string, unknown>;
      setDetailMeta({
        maintenanceProvider:
          typeof parsed.maintenanceProvider === 'string' ? parsed.maintenanceProvider : null,
        location: typeof parsed.location === 'string' ? parsed.location : null,
        locationType: typeof parsed.locationType === 'string' ? parsed.locationType : null,
      });
    } catch {
      setDetailMeta(null);
    }
  };

  const openTemplateSetupModal = async () => {
    setSetupTemplateOpen(true);
    try {
      if (assetTypes.length === 0) {
        const types = await assetService.getAssetTypes();
        setAssetTypes(types ?? []);
      }
    } catch {
      setAssetTypes([]);
    }
    await loadTemplates();
  };

  const resetTemplateForm = () => {
    setEditingTemplateId(null);
    setTemplateForm({
      assetTypeId: null,
      name: '',
      content: '',
      frequencyType: 2,
      repeatIntervalValue: 1,
      repeatIntervalUnit: 3,
      oneTimeScheduledDate: toTodayInputDate(),
      isActive: true,
    });
  };

  const openCreateTemplateForm = () => {
    resetTemplateForm();
    setSetupTemplateOpen(false);
    setTemplateFormOpen(true);
  };

  const openEditTemplateForm = async (templateId: number) => {
    setSetupTemplateOpen(false);
    setTemplateFormOpen(true);
    setTemplateFormSubmitting(true);
    try {
      const detail = await maintenanceTemplateService.getById(templateId);
      setEditingTemplateId(templateId);
      setTemplateForm({
        assetTypeId: detail.assetTypeId,
        name: detail.name ?? '',
        content: detail.content ?? '',
        frequencyType: parseEnumNumber(detail.frequencyType) === 1 ? 1 : 2,
        repeatIntervalValue: Math.max(1, Number(detail.repeatIntervalValue || 1)),
        repeatIntervalUnit: ([1, 2, 3, 4].includes(parseEnumNumber(detail.repeatIntervalUnit))
          ? parseEnumNumber(detail.repeatIntervalUnit)
          : 3) as 1 | 2 | 3 | 4,
        oneTimeScheduledDate: detail.oneTimeScheduledDate
          ? dayjs(detail.oneTimeScheduledDate).format('YYYY-MM-DD')
          : toTodayInputDate(),
        isActive: Boolean(detail.isActive),
      });
    } catch {
      message.error('Không tải được chi tiết quy định bảo dưỡng.');
      setTemplateFormOpen(false);
    } finally {
      setTemplateFormSubmitting(false);
    }
  };

  const submitTemplateForm = async () => {
    if (!templateForm.assetTypeId) {
      message.warning('Vui lòng chọn loại tài sản.');
      return;
    }
    if (!templateForm.name.trim()) {
      message.warning('Vui lòng nhập tên quy định.');
      return;
    }
    if (!templateForm.content.trim()) {
      message.warning('Vui lòng nhập nội dung bảo dưỡng.');
      return;
    }

    const isOneTime = templateForm.frequencyType === 1;
    if (isOneTime && !templateForm.oneTimeScheduledDate?.trim()) {
      message.warning('Vui lòng chọn ngày bảo dưỡng.');
      return;
    }

    const payload = {
      assetTypeId: templateForm.assetTypeId,
      name: templateForm.name.trim(),
      content: templateForm.content.trim(),
      frequencyType: templateForm.frequencyType,
      // Backend yêu cầu OneTime phải gửi 0 cho 2 trường lặp lại.
      repeatIntervalValue: isOneTime ? 0 : Math.max(1, Number(templateForm.repeatIntervalValue || 1)),
      repeatIntervalUnit: isOneTime ? 0 : templateForm.repeatIntervalUnit,
      isActive: templateForm.isActive,
      oneTimeScheduledDate: isOneTime ? templateForm.oneTimeScheduledDate.trim() : null,
    } as const;

    setTemplateFormSubmitting(true);
    try {
      if (editingTemplateId) {
        await maintenanceTemplateService.update(editingTemplateId, payload);
        message.success('Cập nhật quy định bảo dưỡng thành công.');
      } else {
        await maintenanceTemplateService.create(payload);
        message.success('Thêm quy định bảo dưỡng thành công.');
      }
      setTemplateFormOpen(false);
      setSetupTemplateOpen(true);
      resetTemplateForm();
      await loadTemplates();
    } catch (error) {
      message.error(getApiErrorMessage(error, 'Lưu quy định bảo dưỡng thất bại.'));
    } finally {
      setTemplateFormSubmitting(false);
    }
  };

  const toggleTemplateStatus = async (templateId: number) => {
    try {
      await maintenanceTemplateService.changeStatus(templateId);
      message.success('Đã cập nhật trạng thái quy định.');
      await loadTemplates();
    } catch (error) {
      message.error(getApiErrorMessage(error, 'Không thể đổi trạng thái quy định.'));
    }
  };

  const deleteTemplatePermanent = async (templateId: number) => {
    try {
      await maintenanceTemplateService.deletePermanent(templateId);
      message.success('Đã xóa quy định bảo dưỡng.');
      await loadTemplates();
    } catch (error) {
      message.error(getApiErrorMessage(error, 'Không thể xóa quy định bảo dưỡng.'));
    }
  };

  const openStartMaintenance = async (row: MaintenanceRow) => {
    setStartRow(row);
    setStartLoading(true);
    setStartOpen(true);
    try {
      if (row.assetInstanceId && row.assetInstanceId > 0) {
        const instance = await assetInstanceService.getById(row.assetInstanceId);
        setStartAsset(instance);
      } else {
        setStartAsset(null);
      }
      setReportNumber(generateMaintenanceReportNumber());
      setMaintenanceDate(toTodayInputDate());
      setExpectedCompletionDate('');
      setDetailedDescription(row.purpose || '');
      setMaintenanceProvider('');
      setLocationType('at-unit');
    } catch {
      message.error('Không tải được thông tin tài sản.');
      setStartOpen(false);
      setStartRow(null);
      setStartAsset(null);
    } finally {
      setStartLoading(false);
    }
  };

  const submitStartMaintenance = async () => {
    if (!startRow) return;
    if (!profile?.id) {
      message.error('Không xác định được người thực hiện. Vui lòng đăng nhập lại.');
      return;
    }
    if (!maintenanceDate) {
      message.warning('Vui lòng chọn ngày bảo dưỡng.');
      return;
    }
    if (expectedCompletionDate && expectedCompletionDate < maintenanceDate) {
      message.warning('Ngày dự kiến hoàn thành phải từ ngày bảo dưỡng trở đi.');
      return;
    }
    setStartSubmitting(true);
    try {
      const loc = locationType === 'at-unit' ? 'Tại đơn vị' : 'Nhà cung cấp';

      const expectedCompletionDateIso = expectedCompletionDate
        ? toIsoDate(expectedCompletionDate)
        : undefined;

      const payload: MaintenanceStartPayload = {
        startedBy: profile.id,
        comment: null,
        reportNumber: reportNumber.trim() || null,
        maintenanceDate: toIsoDate(maintenanceDate),
        maintenanceProvider: maintenanceProvider.trim() || null,
        expectedCompletionDate: expectedCompletionDateIso,
        maintenanceContent: detailedDescription.trim() || null,
        detailedDescription: detailedDescription.trim() || null,
        locationType,
        location: loc || null,
      };

      if (startRow.assetRequestId > 0) {
        // Keep current flow unchanged for rows linked to AssetRequest.
        await maintenanceRequestService.start(startRow.assetRequestId, payload);
      } else {
        // Tasks auto-generated by jobs do not have AssetRequestId.
        const taskId = Number(startRow.id);
        if (!Number.isFinite(taskId) || taskId <= 0) {
          message.error('Không xác định được Task để bắt đầu bảo dưỡng.');
          return;
        }
        await maintenanceRequestService.startTask(taskId, payload);
      }
      message.success('Đã bắt đầu bảo dưỡng.');
      setStartOpen(false);
      setStartRow(null);
      setStartAsset(null);
      await reload();
    } catch (e: unknown) {
      message.error(getApiErrorMessage(e, 'Không thể bắt đầu bảo dưỡng.'));
    } finally {
      setStartSubmitting(false);
    }
  };

  const openCompleteMaintenance = async (row: MaintenanceRow) => {
    if (row.rawStatus !== 4) {
      message.warning('Chỉ có thể hoàn thành khi đơn đang trong trạng thái đang bảo dưỡng.');
      return;
    }
    setCompleteRow(row);
    setCompleteLoading(true);
    setCompleteOpen(true);
    try {
      let det: Awaited<ReturnType<typeof assetRequestService.getById>> | null = null;
      if (row.assetRequestId > 0) {
        det = await assetRequestService.getById(row.assetRequestId);
      }
      if (row.assetInstanceId && row.assetInstanceId > 0) {
        const instance = await assetInstanceService.getById(row.assetInstanceId);
        setCompleteAsset(instance);
      } else {
        setCompleteAsset(null);
      }

      let maintLabel = row.setupDate || row.expectedDate || '';
      let maintDateRaw = '';
      let rep = '';
      let contentFromStart = '';
      let detailFromStart = '';
      let prefillCompletionDate = toTodayInputDate();
      if (det?.proposedData) {
        try {
          const pd = JSON.parse(det.proposedData) as Record<string, unknown>;
          if (pd.maintenanceDate) {
            maintDateRaw = String(pd.maintenanceDate);
            maintLabel = formatDate(maintDateRaw);
          }
          if (typeof pd.reportNumber === 'string') rep = pd.reportNumber;
          if (typeof pd.maintenanceContent === 'string') contentFromStart = pd.maintenanceContent;
          if (typeof pd.detailedDescription === 'string') detailFromStart = pd.detailedDescription;
          if (pd.expectedCompletionDate) {
            const expDate = dayjs(String(pd.expectedCompletionDate));
            if (expDate.isValid()) prefillCompletionDate = expDate.format('YYYY-MM-DD');
          }
        } catch {
          /* ignore */
        }
      }
      setCompleteMaintenanceDateLabel(maintLabel);
      setCompleteMaintenanceStartDateRaw(maintDateRaw);
      setCompleteReportNumber(rep);
      setCompleteCompletionDate(prefillCompletionDate);
      setCompleteReturnDate(prefillCompletionDate);
      setCompleteActualCost(null);
      setCompleteMaintenanceContent(contentFromStart);
      setCompleteDetailedDescription(detailFromStart || row.purpose || '');
    } catch {
      message.error('Không tải được thông tin tài sản.');
      setCompleteOpen(false);
      setCompleteRow(null);
      setCompleteAsset(null);
    } finally {
      setCompleteLoading(false);
    }
  };

  const submitCompleteMaintenance = async () => {
    if (!completeRow) return;
    if (!profile?.id) {
      message.error('Không xác định được người thực hiện. Vui lòng đăng nhập lại.');
      return;
    }
    if (!completeCompletionDate) {
      message.warning('Vui lòng chọn ngày hoàn thành bảo dưỡng.');
      return;
    }
    if (completeMaintenanceStartDateRaw) {
      const startDate = dayjs(completeMaintenanceStartDateRaw).format('YYYY-MM-DD');
      if (completeCompletionDate < startDate) {
        message.warning('Ngày hoàn thành bảo dưỡng không được trước ngày bắt đầu bảo dưỡng.');
        return;
      }
    }
    if (!completeReturnDate) {
      message.warning('Vui lòng chọn ngày đưa vào sử dụng lại.');
      return;
    }
    if (completeReturnDate < completeCompletionDate) {
      message.warning('Ngày đưa vào sử dụng lại không được trước ngày hoàn thành bảo dưỡng.');
      return;
    }
    if (completeActualCost == null || completeActualCost < 0) {
      message.warning('Vui lòng nhập chi phí thực tế.');
      return;
    }

    const taskId = Number(completeRow.id);
    if (!Number.isFinite(taskId) || taskId <= 0) {
      message.error('Không xác định được mã công việc bảo dưỡng.');
      return;
    }

    setCompleteSubmitting(true);
    try {
      const payload: MaintenanceCompletePayload = {
        completedBy: profile.id,
        reportNumber: completeReportNumber.trim() || null,
        completionDate: toIsoDate(completeCompletionDate),
        returnToUseDate: toIsoDate(completeReturnDate),
        actualCost: completeActualCost,
        totalCost: completeActualCost,
        maintenanceContent: completeMaintenanceContent.trim() || null,
        detailedDescription: completeDetailedDescription.trim() || null,
        attachmentUrls: null,
      };

      await maintenanceRequestService.complete(taskId, payload);
      message.success('Đã hoàn thành bảo dưỡng.');
      setCompleteOpen(false);
      setCompleteRow(null);
      setCompleteAsset(null);
      await reload();
    } catch (e: unknown) {
      message.error(getApiErrorMessage(e, 'Không thể hoàn thành bảo dưỡng.'));
    } finally {
      setCompleteSubmitting(false);
    }
  };

  const submitDirectorDecision = async () => {
    if (!selected) return;
    if (!profile?.id) {
      message.error('Không xác định được người phê duyệt. Vui lòng đăng nhập lại.');
      return;
    }
    setSubmitting(true);
    try {
      const payload = { approvedBy: profile.id, comment: comment.trim() || null };
      if (decision === 'approved') {
        await directorRequestService.approve(selected.assetRequestId, payload);
        message.success('Đã phê duyệt yêu cầu bảo dưỡng.');
      } else {
        await directorRequestService.reject(selected.assetRequestId, payload);
        message.success('Đã từ chối yêu cầu bảo dưỡng.');
      }
      setApproveOpen(false);
      setDetailOpen(false);
      setSelected(null);
      await reload();
    } catch (e: unknown) {
      message.error(getApiErrorMessage(e, 'Thao tác phê duyệt thất bại.'));
    } finally {
      setSubmitting(false);
    }
  };

  const filteredRows: MaintenanceRow[] = useMemo(() => {
    const keyword = search.trim().toLowerCase();
    const byTab =
      activeTab === 'in-maintenance' ? rows.filter((r) => r.rawStatus === 4) : rows;
    return byTab.filter((row) => {
      const matchStatus = statusFilter === 'all' || row.status === statusFilter;
      const matchKeyword =
        !keyword ||
        row.assetCode.toLowerCase().includes(keyword) ||
        row.assetName.toLowerCase().includes(keyword) ||
        row.purpose.toLowerCase().includes(keyword);
      return matchStatus && matchKeyword;
    });
  }, [rows, search, statusFilter, activeTab]);

  const filteredTemplates = useMemo(() => {
    const keyword = templateSearch.trim().toLowerCase();
    if (!keyword) return templates;
    return templates.filter((t) => {
      const assetTypeName =
        assetTypes.find((a) => a.assetTypeId === t.assetTypeId)?.name?.toLowerCase() ?? '';
      return (
        String(t.name ?? '').toLowerCase().includes(keyword) ||
        String(t.content ?? '').toLowerCase().includes(keyword) ||
        assetTypeName.includes(keyword)
      );
    });
  }, [assetTypes, templateSearch, templates]);

  return (
    <div className="maintenance-page">
      <div className="maintenance-header">
        <h1 className="maintenance-page__title">Bảo dưỡng</h1>
        <Button type="primary" className="maintenance-btn-add" onClick={openTemplateSetupModal}>
          + Thiết lập quy định bảo dưỡng
        </Button>
      </div>

      <div className="maintenance-card">
        <Tabs
          activeKey={activeTab}
          onChange={(key) => setActiveTab(key as 'need-maintenance' | 'in-maintenance')}
          className="maintenance-tabs"
          items={[
            { key: 'need-maintenance', label: 'Tài sản cần bảo dưỡng' },
            { key: 'in-maintenance', label: 'Đang bảo dưỡng' },
          ]}
        />

        <div className="maintenance-filters">
          <Input
            placeholder="Tìm kiếm"
            prefix={<SearchOutlined />}
            className="maintenance-search"
            value={search}
            onChange={(e) => setSearch(e.target.value)}
          />
          <Select
            placeholder="Trạng thái"
            className="maintenance-select"
            suffixIcon={<FilterOutlined />}
            value={statusFilter}
            onChange={(v) => setStatusFilter(v)}
            options={[
              { value: 'all', label: 'Tất cả' },
              { value: 'draft', label: 'Chưa gửi' },
              { value: 'submitted', label: 'Đã nộp' },
              { value: 'pending', label: 'Chờ phê duyệt' },
              { value: 'approved', label: 'Phê duyệt' },
              { value: 'inProgress', label: 'Đang bảo dưỡng' },
              { value: 'completed', label: 'Đã bảo dưỡng' },
              { value: 'rejected', label: 'Từ chối' },
            ]}
          />
          <Button
            icon={<FilterOutlined />}
            className="maintenance-filter-advanced"
            onClick={() => {
              setSearch('');
              setStatusFilter('all');
            }}
          >
            Gỡ bộ lọc
          </Button>
          <Button icon={<SettingOutlined />} className="maintenance-settings" />
        </div>

        <div className="asset-table-wrapper maintenance-table-wrapper">
          <table className="asset-table maintenance-table">
            <thead>
              <tr>
                <th>MÃ CÁ THỂ</th>
                <th>TÊN TÀI SẢN</th>
                <th>LOẠI TÀI SẢN</th>
                <th>MỤC ĐÍCH</th>
                <th>NGÀY THIẾT LẬP BD</th>
                <th>NGÀY BD DỰ KIẾN</th>
                <th>VỊ TRÍ TS</th>
                <th>TRẠNG THÁI</th>
                <th className="asset-table__cell asset-table__cell--actions" />
              </tr>
            </thead>
            <tbody>
              {loading ? (
                <tr>
                  <td colSpan={9} className="maintenance-table-empty">
                    Đang tải dữ liệu...
                  </td>
                </tr>
              ) : filteredRows.length === 0 ? (
                <tr>
                  <td colSpan={9} className="maintenance-table-empty">
                    Không có dữ liệu.
                  </td>
                </tr>
              ) : (
                filteredRows.map((row) => (
                  <tr key={row.id} className="asset-row">
                    <td>
                      <button type="button" className="asset-code asset-code--link">
                        {row.assetCode}
                      </button>
                    </td>
                    <td>{row.assetName}</td>
                    <td>{row.assetType}</td>
                    <td>{row.purpose}</td>
                    <td>{row.setupDate}</td>
                    <td>{row.expectedDate}</td>
                    <td>{row.assetState}</td>
                    <td>
                      <span className={getStatusClass(row.status)}>
                        {getStatusLabel(row.status)}
                      </span>
                    </td>
                    <td className="asset-table__cell asset-table__cell--actions">
                      <Button
                        type="text"
                        icon={<EyeOutlined />}
                        onClick={() => {
                          setSelected(row);
                          void loadDetailMeta(row.assetRequestId);
                          setDetailOpen(true);
                        }}
                      />
                      {activeTab === 'need-maintenance' && row.status === 'approved' ? (
                        <Button type="link" size="small" onClick={() => openStartMaintenance(row)}>
                          Bắt đầu BD
                        </Button>
                      ) : null}
                      {activeTab === 'in-maintenance' && row.status === 'inProgress' ? (
                        <Button type="link" size="small" onClick={() => openCompleteMaintenance(row)}>
                          Hoàn thành BD
                        </Button>
                      ) : null}
                      {activeTab === 'need-maintenance' &&
                      (row.status === 'draft' || row.status === 'submitted') ? (
                        <Popconfirm
                          title="Xóa đề xuất bảo dưỡng?"
                          okText="Xóa"
                          cancelText="Hủy"
                          onConfirm={async () => {
                            try {
                              await maintenanceRequestService.remove(row.assetRequestId);
                              setRows((prev) => prev.filter((x) => x.assetRequestId !== row.assetRequestId));
                              message.success('Đã xóa đề xuất bảo dưỡng.');
                            } catch {
                              message.error('Không thể xóa đề xuất bảo dưỡng. Vui lòng thử lại.');
                            }
                          }}
                        >
                          <Button type="text" danger icon={<DeleteOutlined />} />
                        </Popconfirm>
                      ) : null}
                    </td>
                  </tr>
                ))
              )}
            </tbody>
          </table>
        </div>

        <div className="maintenance-card__footer">
          <div className="maintenance-footer__left">
            Số lượng trên trang:
            <select className="maintenance-footer__select" defaultValue={25}>
              <option value={10}>10</option>
              <option value={25}>25</option>
              <option value={50}>50</option>
              <option value={100}>100</option>
            </select>
          </div>
          <div className="maintenance-footer__center">1-25 trên 289</div>
          <div className="maintenance-footer__right">
            <button className="maintenance-footer__pager" disabled type="button">
              ⟨
            </button>
            <button
              className="maintenance-footer__pager maintenance-footer__pager--active"
              type="button"
            >
              1
            </button>
            <button className="maintenance-footer__pager" type="button">
              ⟩
            </button>
          </div>
        </div>
      </div>

      {setupTemplateOpen && !templateFormOpen ? (
        <div className="template-setup-modal-overlay" role="dialog" aria-modal="true">
          <div className="template-setup-modal">
            <button
              type="button"
              className="template-setup-modal__close-btn"
              onClick={() => setSetupTemplateOpen(false)}
              aria-label="Đóng"
            >
              <span className="template-setup-modal__close">×</span>
            </button>
            <div className="template-setup-modal__header">
              <h2 className="template-setup-modal__title">Thiết lập quy định bảo dưỡng</h2>
            </div>
            <div className="template-setup-modal__body">
              <div className="template-setup-modal__toolbar">
                <input
                  className="template-setup-modal__search"
                  placeholder="Tìm kiếm loại tài sản"
                  value={templateSearch}
                  onChange={(e) => setTemplateSearch(e.target.value)}
                />
                <button
                  type="button"
                  className="template-setup-modal__btn-primary"
                  onClick={openCreateTemplateForm}
                >
                  Thêm quy định bảo dưỡng
                </button>
              </div>
              <div className="template-setup-modal__table-scroll">
                <table className="asset-table maintenance-table template-setup-modal__table">
                  <thead>
                    <tr>
                      <th>Loại tài sản</th>
                      <th>Tên quy định</th>
                      <th>Xác định bảo dưỡng theo</th>
                      <th>Tần suất bảo dưỡng</th>
                      <th>Lặp lại theo</th>
                      <th>Trạng thái</th>
                      <th className="asset-table__cell asset-table__cell--actions" />
                    </tr>
                  </thead>
                  <tbody>
                    {templatesLoading ? (
                      <tr>
                        <td colSpan={7} className="maintenance-table-empty">
                          Đang tải dữ liệu...
                        </td>
                      </tr>
                    ) : filteredTemplates.length === 0 ? (
                      <tr>
                        <td colSpan={7} className="maintenance-table-empty">
                          Không có dữ liệu.
                        </td>
                      </tr>
                    ) : (
                      filteredTemplates.map((template) => (
                        <tr key={template.templateId} className="asset-row">
                          <td>{assetTypes.find((a) => a.assetTypeId === template.assetTypeId)?.name ?? '—'}</td>
                          <td>{template.name?.trim() || '—'}</td>
                          <td>Thời gian</td>
                          <td>{getFrequencyLabel(template.frequencyType)}</td>
                          <td>
                            {parseEnumNumber(template.frequencyType) === 1 &&
                            template.oneTimeScheduledDate
                              ? formatDate(template.oneTimeScheduledDate)
                              : Number(template.repeatIntervalValue || 0) > 0
                                ? `${template.repeatIntervalValue} ${getRepeatUnitLabel(template.repeatIntervalUnit)}`
                                : '—'}
                          </td>
                          <td>
                            <button
                              type="button"
                              className={`template-setup-modal__status-btn ${template.isActive ? 'template-setup-modal__status-btn--active' : ''}`}
                              onClick={() => toggleTemplateStatus(template.templateId)}
                            >
                              {template.isActive ? 'Đang áp dụng' : 'Ngưng áp dụng'}
                            </button>
                          </td>
                          <td className="asset-table__cell asset-table__cell--actions">
                            <button
                              type="button"
                              className="template-setup-modal__icon-btn"
                              onClick={() => openEditTemplateForm(template.templateId)}
                              title="Sửa"
                            >
                              <EditOutlined />
                            </button>
                            <button
                              type="button"
                              className="template-setup-modal__icon-btn template-setup-modal__icon-btn--danger"
                              onClick={() => setDeleteTemplateId(template.templateId)}
                              title="Xóa"
                            >
                              <DeleteOutlined />
                            </button>
                          </td>
                        </tr>
                      ))
                    )}
                  </tbody>
                </table>
              </div>
            </div>
            <div className="template-setup-modal__footer">
              <button
                type="button"
                className="template-setup-modal__btn-secondary"
                onClick={() => setSetupTemplateOpen(false)}
              >
                Đóng
              </button>
            </div>
          </div>
        </div>
      ) : null}

      {deleteTemplateId != null ? (
        <div className="template-delete-confirm-modal-overlay" role="dialog" aria-modal="true">
          <div className="template-delete-confirm-modal">
            <div className="template-delete-confirm-modal__header">
              <h3 className="template-delete-confirm-modal__title">Xác nhận xóa</h3>
            </div>
            <div className="template-delete-confirm-modal__body">
              Bạn có chắc chắn muốn xóa vĩnh viễn quy định bảo dưỡng này không?
            </div>
            <div className="template-delete-confirm-modal__footer">
              <button
                type="button"
                className="template-delete-confirm-modal__btn template-delete-confirm-modal__btn--secondary"
                onClick={() => setDeleteTemplateId(null)}
              >
                Hủy
              </button>
              <button
                type="button"
                className="template-delete-confirm-modal__btn template-delete-confirm-modal__btn--danger"
                onClick={async () => {
                  const id = deleteTemplateId;
                  setDeleteTemplateId(null);
                  if (id != null) {
                    await deleteTemplatePermanent(id);
                  }
                }}
              >
                Xóa
              </button>
            </div>
          </div>
        </div>
      ) : null}

      {templateFormOpen ? (
        <div className="template-form-modal-overlay" role="dialog" aria-modal="true">
          <div className="template-form-modal">
            <button
              type="button"
              className="template-form-modal__close-btn"
              onClick={() => {
                setTemplateFormOpen(false);
                setSetupTemplateOpen(true);
                resetTemplateForm();
              }}
              aria-label="Đóng"
            >
              <span className="template-form-modal__close">×</span>
            </button>
            <div className="template-form-modal__header">
              <h2 className="template-form-modal__title">
                {editingTemplateId ? 'Cập nhật quy định bảo dưỡng' : 'Thêm quy định bảo dưỡng'}
              </h2>
            </div>
            <div className="template-form-modal__body">
              <div className="template-form-modal__grid">
                <div className="template-form-modal__item">
                  <label htmlFor="template-asset-type">Loại tài sản</label>
                  <select
                    id="template-asset-type"
                    className="template-form-modal__input"
                    value={templateForm.assetTypeId ?? ''}
                    onChange={(e) =>
                      setTemplateForm((prev) => ({
                        ...prev,
                        assetTypeId: e.target.value ? Number(e.target.value) : null,
                      }))
                    }
                  >
                    <option value="">Chọn loại tài sản</option>
                    {assetTypes.map((t) => (
                      <option key={t.assetTypeId} value={t.assetTypeId}>
                        {t.name}
                      </option>
                    ))}
                  </select>
                </div>
                <div className="template-form-modal__item">
                  <label htmlFor="template-name">Tên quy định</label>
                  <input
                    id="template-name"
                    className="template-form-modal__input"
                    value={templateForm.name}
                    onChange={(e) => setTemplateForm((prev) => ({ ...prev, name: e.target.value }))}
                    placeholder="Nhập tên quy định"
                  />
                </div>
                <div className="template-form-modal__item template-form-modal__item--full">
                  <label htmlFor="template-content">Nội dung bảo dưỡng</label>
                  <textarea
                    id="template-content"
                    className="template-form-modal__textarea"
                    rows={4}
                    value={templateForm.content}
                    onChange={(e) => setTemplateForm((prev) => ({ ...prev, content: e.target.value }))}
                    placeholder="Nhập nội dung bảo dưỡng"
                  />
                </div>
                <div className="template-form-modal__item template-form-modal__item--full">
                  <label>Tần suất bảo dưỡng</label>
                  <div className="template-form-modal__radio-group">
                    <label>
                      <input
                        type="radio"
                        checked={templateForm.frequencyType === 1}
                        onChange={() =>
                          setTemplateForm((prev) => ({
                            ...prev,
                            frequencyType: 1,
                            oneTimeScheduledDate: prev.oneTimeScheduledDate?.trim()
                              ? prev.oneTimeScheduledDate
                              : toTodayInputDate(),
                          }))
                        }
                      />{' '}
                      Một lần
                    </label>
                    <label>
                      <input
                        type="radio"
                        checked={templateForm.frequencyType === 2}
                        onChange={() => setTemplateForm((prev) => ({ ...prev, frequencyType: 2 }))}
                      />{' '}
                      Định kỳ
                    </label>
                  </div>
                </div>
                {templateForm.frequencyType === 1 ? (
                  <div className="template-form-modal__item">
                    <label htmlFor="template-one-time-date">Ngày bảo dưỡng</label>
                    <input
                      id="template-one-time-date"
                      className="template-form-modal__input"
                      type="date"
                      value={templateForm.oneTimeScheduledDate}
                      onChange={(e) =>
                        setTemplateForm((prev) => ({
                          ...prev,
                          oneTimeScheduledDate: e.target.value,
                        }))
                      }
                    />
                  </div>
                ) : (
                  <>
                    <div className="template-form-modal__item">
                      <label htmlFor="template-repeat-value">Giá trị lặp lại</label>
                      <input
                        id="template-repeat-value"
                        className="template-form-modal__input"
                        type="number"
                        min={1}
                        value={templateForm.repeatIntervalValue}
                        onChange={(e) =>
                          setTemplateForm((prev) => ({
                            ...prev,
                            repeatIntervalValue: Math.max(1, Number(e.target.value || 1)),
                          }))
                        }
                      />
                    </div>
                    <div className="template-form-modal__item">
                      <label htmlFor="template-repeat-unit">Lặp lại theo</label>
                      <select
                        id="template-repeat-unit"
                        className="template-form-modal__input"
                        value={templateForm.repeatIntervalUnit}
                        onChange={(e) =>
                          setTemplateForm((prev) => ({
                            ...prev,
                            repeatIntervalUnit: Number(e.target.value) as 1 | 2 | 3 | 4,
                          }))
                        }
                      >
                        <option value={1}>Ngày</option>
                        <option value={2}>Tuần</option>
                        <option value={3}>Tháng</option>
                        <option value={4}>Năm</option>
                      </select>
                    </div>
                  </>
                )}
              </div>
            </div>
            <div className="template-form-modal__footer">
              <button
                type="button"
                className="template-form-modal__btn-secondary"
                disabled={templateFormSubmitting}
                onClick={() => {
                  setTemplateFormOpen(false);
                  setSetupTemplateOpen(true);
                  resetTemplateForm();
                }}
              >
                Hủy
              </button>
              <button
                type="button"
                className="template-form-modal__btn-primary"
                disabled={templateFormSubmitting}
                onClick={submitTemplateForm}
              >
                {templateFormSubmitting ? 'Đang lưu...' : 'Lưu'}
              </button>
            </div>
          </div>
        </div>
      ) : null}

      {detailOpen ? (
        <div className="maintenance-form-modal-overlay" role="dialog" aria-modal="true">
          <div className="maintenance-form-modal">
            <button
              type="button"
              className="maintenance-form-modal__close-btn"
              onClick={() => {
                setDetailOpen(false);
                setSelected(null);
                setDetailMeta(null);
              }}
              aria-label="Đóng"
            >
              <span className="maintenance-form-modal__close">×</span>
            </button>
            <div className="maintenance-form-modal__header">
              <h2 className="maintenance-form-modal__title">
                {selected
                  ? `Chi tiết yêu cầu bảo dưỡng - YC-${selected.assetRequestId}`
                  : 'Chi tiết yêu cầu bảo dưỡng'}
              </h2>
            </div>
            <div className="maintenance-form-modal__body">
              {!selected ? (
                <div className="maintenance-form-modal__loading">Không có dữ liệu.</div>
              ) : (
                <div className="maintenance-form-modal__content">
                  <div className="maintenance-form-modal__section">
                    <div className="maintenance-form-modal__section-title">Thông tin tài sản</div>
                    <div className="maintenance-form-modal__info-grid">
                      <div className="maintenance-form-modal__info-row">
                        <div className="maintenance-form-modal__info-item">
                          <label>Mã cá thể</label>
                          <div className="maintenance-form-modal__info-value">
                            {selected.assetCode || '—'}
                          </div>
                        </div>
                        <div className="maintenance-form-modal__info-item">
                          <label>Tên tài sản</label>
                          <div className="maintenance-form-modal__info-value">
                            {selected.assetName || '—'}
                          </div>
                        </div>
                      </div>
                      <div className="maintenance-form-modal__info-row">
                        <div className="maintenance-form-modal__info-item">
                          <label>Loại tài sản</label>
                          <div className="maintenance-form-modal__info-value">
                            {selected.assetType || '—'}
                          </div>
                        </div>
                        <div className="maintenance-form-modal__info-item">
                          <label>Phòng ban</label>
                          <div className="maintenance-form-modal__info-value">
                            {selected.assetState || '—'}
                          </div>
                        </div>
                      </div>
                    </div>
                  </div>

                  <div className="maintenance-form-modal__section">
                    <div className="maintenance-form-modal__section-title">Thông tin yêu cầu</div>
                    <div className="maintenance-form-modal__info-grid">
                      <div className="maintenance-form-modal__info-row">
                        <div className="maintenance-form-modal__info-item">
                          <label>Mục đích</label>
                          <div className="maintenance-form-modal__info-value">
                            {selected.purpose || '—'}
                          </div>
                        </div>
                        <div className="maintenance-form-modal__info-item">
                          <label>Ngày thiết lập</label>
                          <div className="maintenance-form-modal__info-value">
                            {selected.setupDate || '—'}
                          </div>
                        </div>
                      </div>
                      <div className="maintenance-form-modal__info-row">
                        <div className="maintenance-form-modal__info-item">
                          <label>Ngày dự kiến bảo dưỡng</label>
                          <div className="maintenance-form-modal__info-value">
                            {selected.expectedDate || '—'}
                          </div>
                        </div>
                        <div className="maintenance-form-modal__info-item">
                          <label>Trạng thái</label>
                          <div className="maintenance-form-modal__info-value">
                            {getStatusLabel(selected.status)}
                          </div>
                        </div>
                      </div>
                      <div className="maintenance-form-modal__info-row">
                        <div className="maintenance-form-modal__info-item">
                          <label>Đơn vị bảo dưỡng</label>
                          <div className="maintenance-form-modal__info-value">
                            {detailMeta?.maintenanceProvider?.trim() || '—'}
                          </div>
                        </div>
                        <div className="maintenance-form-modal__info-item">
                          <label>Nơi thực hiện</label>
                          <div className="maintenance-form-modal__info-value">
                            {detailMeta?.location?.trim() ||
                              (detailMeta?.locationType === 'provider'
                                ? 'Tại nhà cung cấp'
                                : detailMeta?.locationType === 'at-unit'
                                  ? 'Tại đơn vị'
                                  : '—')}
                          </div>
                        </div>
                      </div>
                    </div>
                  </div>
                </div>
              )}
            </div>
            <div className="maintenance-form-modal__footer">
              <div className="maintenance-form-modal__footer-actions">
                <button
                  type="button"
                  className="maintenance-form-modal__btn-cancel"
                  onClick={() => {
                    setDetailOpen(false);
                    setSelected(null);
                    setDetailMeta(null);
                  }}
                >
                  Đóng
                </button>
                {canDirectorApprove ? (
                  <button
                    type="button"
                    className="maintenance-form-modal__btn-confirm"
                    onClick={() => {
                      setDecision('approved');
                      setComment('');
                      setApproveOpen(true);
                    }}
                  >
                    Phê duyệt
                  </button>
                ) : null}
              </div>
            </div>
          </div>
        </div>
      ) : null}

      {startOpen ? (
        <div className="maintenance-form-modal-overlay" role="dialog" aria-modal="true">
          <div className="maintenance-form-modal">
            <button
              type="button"
              className="maintenance-form-modal__close-btn"
              onClick={() => {
                if (startSubmitting) return;
                setStartOpen(false);
                setStartRow(null);
                setStartAsset(null);
              }}
              aria-label="Đóng"
            >
              <span className="maintenance-form-modal__close">×</span>
            </button>
            <div className="maintenance-form-modal__header">
              <h2 className="maintenance-form-modal__title">Bảo dưỡng tài sản</h2>
            </div>
            <div className="maintenance-form-modal__body">
              {startLoading ? (
                <div className="maintenance-form-modal__loading">Đang tải...</div>
              ) : !startRow ? (
                <div className="maintenance-form-modal__loading">Không có dữ liệu.</div>
              ) : (
                <div className="maintenance-form-modal__content">
            <div className="maintenance-form-modal__form-item">
              <label className="maintenance-form-modal__label" htmlFor="maintenance-start-report-number">
                Số biên bản
              </label>
              <input
                id="maintenance-start-report-number"
                className="maintenance-form-modal__input maintenance-form-modal__input--narrow"
                value={reportNumber}
                readOnly
              />
            </div>

            <div className="maintenance-form-modal__section">
              <div className="maintenance-form-modal__section-title">Thông tin tài sản</div>
              <div className="maintenance-form-modal__info-grid">
                <div className="maintenance-form-modal__info-row">
                  <div className="maintenance-form-modal__info-item">
                    <label>Mã cá thể</label>
                    <div className="maintenance-form-modal__info-value">
                      {startAsset?.instanceCode ?? startRow.assetCode}
                    </div>
                  </div>
                  <div className="maintenance-form-modal__info-item">
                    <label>Giá trị tài sản</label>
                    <div className="maintenance-form-modal__info-value">
                      {startAsset?.originalPrice != null ? formatVnd(startAsset.originalPrice) : '—'}
                    </div>
                  </div>
                </div>
                <div className="maintenance-form-modal__info-row">
                  <div className="maintenance-form-modal__info-item">
                    <label>Tên tài sản</label>
                    <div className="maintenance-form-modal__info-value">
                      {startAsset?.assetName ?? startRow.assetName}
                    </div>
                  </div>
                  <div className="maintenance-form-modal__info-item">
                    <label>Giá trị còn lại</label>
                    <div className="maintenance-form-modal__info-value">
                      {startAsset?.remainingValue != null
                        ? formatVnd(startAsset.remainingValue)
                        : startAsset?.currentValue != null
                          ? formatVnd(startAsset.currentValue)
                          : '—'}
                    </div>
                  </div>
                </div>
                <div className="maintenance-form-modal__info-row">
                  <div className="maintenance-form-modal__info-item">
                    <label>Loại tài sản</label>
                    <div className="maintenance-form-modal__info-value">
                      {startRow.assetType || '—'}
                    </div>
                  </div>
                  <div className="maintenance-form-modal__info-item">
                    <label>Vị trí tài sản</label>
                    <div className="maintenance-form-modal__info-value">
                      {startAsset?.warehouseName ||
                        startAsset?.currentDepartmentName ||
                        startRow.assetState ||
                        '—'}
                    </div>
                  </div>
                </div>
                <div className="maintenance-form-modal__info-row">
                  <div className="maintenance-form-modal__info-item maintenance-form-modal__info-item--full">
                    <label>Quy cách tài sản</label>
                    <div className="maintenance-form-modal__info-value">
                      {startAsset?.specification || '—'}
                    </div>
                  </div>
                </div>
                <div className="maintenance-form-modal__info-row">
                  <div className="maintenance-form-modal__info-item">
                    <label>Ngày mua</label>
                    <div className="maintenance-form-modal__info-value">
                      {startAsset?.purchaseDate ? formatDate(startAsset.purchaseDate) : '—'}
                    </div>
                  </div>
                  <div className="maintenance-form-modal__info-item">
                    <label>Ngày đưa vào SD</label>
                    <div className="maintenance-form-modal__info-value">
                      {startAsset?.inUseDate ? formatDate(startAsset.inUseDate) : '—'}
                    </div>
                  </div>
                </div>
                <div className="maintenance-form-modal__info-row">
                  <div className="maintenance-form-modal__info-item">
                    <label>Hạn bảo hành</label>
                    <div className="maintenance-form-modal__info-value">
                      {getWarrantyEndDateDisplay(startAsset)}
                    </div>
                  </div>
                  <div className="maintenance-form-modal__info-item">
                    <label>Phòng ban SD</label>
                    <div className="maintenance-form-modal__info-value">
                      {startAsset?.currentDepartmentName ?? '—'}
                    </div>
                  </div>
                </div>
              </div>
            </div>

            <div className="maintenance-form-modal__section">
              <div className="maintenance-form-modal__section-title">Thông tin bảo dưỡng tài sản</div>
              <div className="maintenance-form-modal__form-grid">
                <div>
                  <div className="maintenance-form-modal__label maintenance-form-modal__label--req">
                    Ngày bảo dưỡng
                  </div>
                  <input
                    type="date"
                    className="maintenance-form-modal__input"
                    value={maintenanceDate}
                    onChange={(e) => setMaintenanceDate(e.target.value)}
                  />
                </div>
                <div>
                  <div className="maintenance-form-modal__label">Đơn vị bảo dưỡng</div>
                  <input
                    className="maintenance-form-modal__input"
                    value={maintenanceProvider}
                    onChange={(e) => setMaintenanceProvider(e.target.value)}
                    placeholder="Tên đơn vị (để trống nếu nội bộ thực hiện)"
                  />
                </div>
                <div>
                  <div className="maintenance-form-modal__label">Ngày dự kiến hoàn thành</div>
                  <input
                    type="date"
                    className="maintenance-form-modal__input"
                    value={expectedCompletionDate}
                    onChange={(e) => setExpectedCompletionDate(e.target.value)}
                  />
                </div>
                <div className="maintenance-form-modal__form-grid-full">
                  <div className="maintenance-form-modal__label">Nội dung bảo dưỡng</div>
                  <textarea
                    className="maintenance-form-modal__textarea"
                    value={detailedDescription}
                    onChange={(e) => setDetailedDescription(e.target.value)}
                  />
                </div>
                <div className="maintenance-form-modal__form-grid-full">
                  <div className="maintenance-form-modal__label">Nơi thực hiện</div>
                  <div className="maintenance-form-modal__radio-group">
                    <label>
                      <input
                        type="radio"
                        name="maintenance-start-location-type"
                        value="at-unit"
                        checked={locationType === 'at-unit'}
                        onChange={() => setLocationType('at-unit')}
                      />
                      Tại chỗ
                    </label>
                    <label>
                      <input
                        type="radio"
                        name="maintenance-start-location-type"
                        value="provider"
                        checked={locationType === 'provider'}
                        onChange={() => setLocationType('provider')}
                      />
                      Tại đơn vị bảo dưỡng
                    </label>
                  </div>
                  
                </div>
              </div>
            </div>
          </div>
              )}
            </div>
            <div className="maintenance-form-modal__footer">
              <div className="maintenance-form-modal__footer-actions">
                <button
                  type="button"
                  className="maintenance-form-modal__btn-cancel"
                  disabled={startSubmitting}
                  onClick={() => {
                    setStartOpen(false);
                    setStartRow(null);
                    setStartAsset(null);
                  }}
                >
                  Hủy
                </button>
                <button
                  type="button"
                  className="maintenance-form-modal__btn-confirm"
                  disabled={startSubmitting || startLoading || !profile?.id}
                  onClick={submitStartMaintenance}
                >
                  {startSubmitting ? 'Đang gửi...' : 'Xác nhận bảo dưỡng'}
                </button>
              </div>
            </div>
          </div>
        </div>
      ) : null}

      {completeOpen ? (
        <div className="maintenance-form-modal-overlay" role="dialog" aria-modal="true">
          <div className="maintenance-form-modal">
            <button
              type="button"
              className="maintenance-form-modal__close-btn"
              onClick={() => {
                if (completeSubmitting) return;
                setCompleteOpen(false);
                setCompleteRow(null);
                setCompleteAsset(null);
              }}
              aria-label="Đóng"
            >
              <span className="maintenance-form-modal__close">×</span>
            </button>
            <div className="maintenance-form-modal__header">
              <h2 className="maintenance-form-modal__title">Hoàn thành bảo dưỡng tài sản</h2>
            </div>
            <div className="maintenance-form-modal__body">
              {completeLoading ? (
                <div className="maintenance-form-modal__loading">Đang tải...</div>
              ) : !completeRow ? (
                <div className="maintenance-form-modal__loading">Không có dữ liệu.</div>
              ) : (
                <div className="maintenance-form-modal__content">
            <div className="maintenance-form-modal__form-item">
              <label className="maintenance-form-modal__label" htmlFor="maintenance-complete-report-number">
                Số biên bản
              </label>
              <input
                id="maintenance-complete-report-number"
                className="maintenance-form-modal__input maintenance-form-modal__input--narrow"
                value={completeReportNumber}
                readOnly
              />
            </div>

            <div className="maintenance-form-modal__section">
              <div className="maintenance-form-modal__section-title">Thông tin tài sản</div>
              <div className="maintenance-form-modal__info-grid">
                <div className="maintenance-form-modal__info-row">
                  <div className="maintenance-form-modal__info-item">
                    <label>Mã cá thể</label>
                    <div className="maintenance-form-modal__info-value">
                      {completeAsset?.instanceCode ?? completeRow.assetCode}
                    </div>
                  </div>
                  <div className="maintenance-form-modal__info-item">
                    <label>Giá trị tài sản</label>
                    <div className="maintenance-form-modal__info-value">
                      {completeAsset?.originalPrice != null ? formatVnd(completeAsset.originalPrice) : '—'}
                    </div>
                  </div>
                </div>
                <div className="maintenance-form-modal__info-row">
                  <div className="maintenance-form-modal__info-item">
                    <label>Tên tài sản</label>
                    <div className="maintenance-form-modal__info-value">
                      {completeAsset?.assetName ?? completeRow.assetName}
                    </div>
                  </div>
                  <div className="maintenance-form-modal__info-item">
                    <label>Giá trị còn lại</label>
                    <div className="maintenance-form-modal__info-value">
                      {completeAsset?.remainingValue != null
                        ? formatVnd(completeAsset.remainingValue)
                        : completeAsset?.currentValue != null
                          ? formatVnd(completeAsset.currentValue)
                          : '—'}
                    </div>
                  </div>
                </div>
                <div className="maintenance-form-modal__info-row">
                  <div className="maintenance-form-modal__info-item">
                    <label>Loại tài sản</label>
                    <div className="maintenance-form-modal__info-value">
                      {completeRow.assetType || '—'}
                    </div>
                  </div>
                  <div className="maintenance-form-modal__info-item">
                    <label>Vị trí tài sản</label>
                    <div className="maintenance-form-modal__info-value">
                      {completeAsset?.warehouseName ||
                        completeAsset?.currentDepartmentName ||
                        completeRow.assetState ||
                        '—'}
                    </div>
                  </div>
                </div>
                <div className="maintenance-form-modal__info-row">
                  <div className="maintenance-form-modal__info-item">
                    <label>Quy cách tài sản</label>
                    <div className="maintenance-form-modal__info-value">
                      {completeAsset?.specification || '—'}
                    </div>
                  </div>
                  <div className="maintenance-form-modal__info-item">
                    <label>Tình trạng</label>
                    <div className="maintenance-form-modal__info-value">
                      {completeAsset?.statusName ? getAssetStatusLabel(completeAsset.statusName) : '—'}
                    </div>
                  </div>
                </div>
                <div className="maintenance-form-modal__info-row">
                  <div className="maintenance-form-modal__info-item">
                    <label>Hạn bảo hành</label>
                    <div className="maintenance-form-modal__info-value">
                      {getWarrantyEndDateDisplay(completeAsset)}
                    </div>
                  </div>
                  <div className="maintenance-form-modal__info-item">
                    <label>Ngày bảo dưỡng</label>
                    <div className="maintenance-form-modal__info-value">
                      {completeMaintenanceDateLabel || '—'}
                    </div>
                  </div>
                </div>
                <div className="maintenance-form-modal__info-row">
                  <div className="maintenance-form-modal__info-item">
                    <label>Phòng ban SD</label>
                    <div className="maintenance-form-modal__info-value">
                      {completeAsset?.currentDepartmentName ?? '—'}
                    </div>
                  </div>
                </div>
              </div>
            </div>

            <div className="maintenance-form-modal__section">
              <div className="maintenance-form-modal__section-title">
                Thông tin hoàn thành bảo dưỡng
              </div>
              <div className="maintenance-form-modal__form-grid">
                <div>
                  <div className="maintenance-form-modal__label maintenance-form-modal__label--req">
                    Ngày hoàn thành bảo dưỡng
                  </div>
                  <input
                    type="date"
                    className="maintenance-form-modal__input"
                    value={completeCompletionDate}
                    onChange={(e) => setCompleteCompletionDate(e.target.value)}
                  />
                </div>
                <div>
                  <div className="maintenance-form-modal__label maintenance-form-modal__label--req">
                    Ngày đưa vào sử dụng lại
                  </div>
                  <input
                    type="date"
                    className="maintenance-form-modal__input"
                    value={completeReturnDate}
                    onChange={(e) => setCompleteReturnDate(e.target.value)}
                  />
                </div>
                <div>
                  <div className="maintenance-form-modal__label maintenance-form-modal__label--req">
                    Chi phí thực tế
                  </div>
                  <div className="maintenance-form-modal__money-input">
                    <input
                      className="maintenance-form-modal__input"
                      type="text"
                      inputMode="numeric"
                      placeholder="Nhập chi phí"
                      value={completeActualCost != null ? completeActualCost.toLocaleString('en-US') : ''}
                      onChange={(e) => setCompleteActualCost(parseMoneyInput(e.target.value))}
                    />
                    <span className="maintenance-form-modal__money-suffix">đ</span>
                  </div>
                </div>
                <div className="maintenance-form-modal__form-grid-full">
                  <div className="maintenance-form-modal__label">Nội dung bảo dưỡng</div>
                  <input
                    className="maintenance-form-modal__input"
                    value={completeMaintenanceContent}
                    onChange={(e) => setCompleteMaintenanceContent(e.target.value)}
                    placeholder="VD: Thay dầu"
                  />
                </div>
                <div className="maintenance-form-modal__form-grid-full">
                  <div className="maintenance-form-modal__label">Mô tả chi tiết</div>
                  <textarea
                    className="maintenance-form-modal__textarea"
                    value={completeDetailedDescription}
                    onChange={(e) => setCompleteDetailedDescription(e.target.value)}
                    placeholder="VD: Hỏng nhẹ"
                  />
                </div>
              </div>

            </div>
          </div>
              )}
            </div>
            <div className="maintenance-form-modal__footer">
              <div className="maintenance-form-modal__footer-actions">
                <button
                  type="button"
                  className="maintenance-form-modal__btn-cancel"
                  disabled={completeSubmitting}
                  onClick={() => {
                    setCompleteOpen(false);
                    setCompleteRow(null);
                    setCompleteAsset(null);
                  }}
                >
                  Hủy
                </button>
                <button
                  type="button"
                  className="maintenance-form-modal__btn-confirm"
                  disabled={completeSubmitting || completeLoading || !profile?.id}
                  onClick={submitCompleteMaintenance}
                >
                  {completeSubmitting ? 'Đang lưu...' : 'Lưu'}
                </button>
              </div>
            </div>
          </div>
        </div>
      ) : null}

      <Modal
        open={approveOpen}
        title="Phê duyệt yêu cầu bảo dưỡng"
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


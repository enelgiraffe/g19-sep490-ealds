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
import { assetService, formatVnd, type AssetResponse } from '../../assets/services/assetService';
import { directorRequestService } from '../../requests/services/directorRequestService';
import { profileService, type UserProfile } from '../../profile/services/profileService';

type MaintenanceStatus =
  | 'draft'
  | 'submitted'
  | 'pending'
  | 'approved'
  | 'rejected'
  | 'inProgress';

interface MaintenanceRow {
  id: string; // taskId (recordId from backend list)
  assetRequestId: number;
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

function getStatusLabel(status: MaintenanceStatus): string {
  if (status === 'draft') return 'Chưa gửi';
  if (status === 'submitted') return 'Đã gửi';
  if (status === 'pending') return 'Chờ phê duyệt';
  if (status === 'approved') return 'Phê duyệt';
  if (status === 'inProgress') return 'Đang bảo dưỡng';
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

function mapStatus(status: number): MaintenanceStatus {
  // -1=Nháp, 0=Đã gửi, 1=Chờ phê duyệt, 2=Đã duyệt (chưa bắt đầu), 3=Từ chối, 4=Đang thực hiện (đã start)
  if (status === -1) return 'draft';
  if (status === 0) return 'submitted';
  if (status === 1) return 'pending';
  if (status === 2) return 'approved';
  if (status === 3) return 'rejected';
  if (status === 4) return 'inProgress';
  return 'submitted';
}

function mapListToRows(list: MaintenanceRequestListItemDTO[]): MaintenanceRow[] {
  return list.map((it) => ({
    id: String(it.recordId),
    assetRequestId: it.assetRequestId,
    assetCode: it.assetCode,
    assetName: it.assetName,
    assetType: '',
    purpose: it.reason ?? '',
    setupDate: formatDate(it.transferDate),
    expectedDate: formatDate(it.transferDate),
    assetState: it.fromDepartment || it.toDepartment || '',
    status: mapStatus(it.status),
    rawStatus: it.status,
  }));
}

type CompleteAttachmentRow = { key: string; name: string };

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
  const [approveOpen, setApproveOpen] = useState(false);
  const [decision, setDecision] = useState<'approved' | 'rejected'>('approved');
  const [comment, setComment] = useState('');
  const [submitting, setSubmitting] = useState(false);

  /** Modal bắt đầu bảo dưỡng (sau phê duyệt) */
  const [startOpen, setStartOpen] = useState(false);
  const [startRow, setStartRow] = useState<MaintenanceRow | null>(null);
  const [startAsset, setStartAsset] = useState<AssetResponse | null>(null);
  const [startLoading, setStartLoading] = useState(false);
  const [startSubmitting, setStartSubmitting] = useState(false);
  const [reportNumber, setReportNumber] = useState('');
  const [maintenanceDate, setMaintenanceDate] = useState('');
  const [markCompleted, setMarkCompleted] = useState(false);
  const [expectedCompletionDate, setExpectedCompletionDate] = useState('');
  const [maintenanceContent, setMaintenanceContent] = useState('');
  const [detailedDescription, setDetailedDescription] = useState('');
  const [performerUserId, setPerformerUserId] = useState<number | null>(null);
  const [maintenanceProvider, setMaintenanceProvider] = useState('');
  const [estimatedCost, setEstimatedCost] = useState<number | null>(null);
  const [locationType, setLocationType] = useState<'at-unit' | 'provider'>('at-unit');
  const [locationText, setLocationText] = useState('');

  /** Modal hoàn thành bảo dưỡng (task đang thực hiện) */
  const [completeOpen, setCompleteOpen] = useState(false);
  const [completeRow, setCompleteRow] = useState<MaintenanceRow | null>(null);
  const [completeAsset, setCompleteAsset] = useState<AssetResponse | null>(null);
  const [completeLoading, setCompleteLoading] = useState(false);
  const [completeSubmitting, setCompleteSubmitting] = useState(false);
  const [completeReportNumber, setCompleteReportNumber] = useState('');
  const [completeMaintenanceDateLabel, setCompleteMaintenanceDateLabel] = useState('');
  const [completeCompletionDate, setCompleteCompletionDate] = useState('');
  const [completeReturnDate, setCompleteReturnDate] = useState('');
  const [completeActualCost, setCompleteActualCost] = useState<number | null>(null);
  const [completeMaintenanceContent, setCompleteMaintenanceContent] = useState('');
  const [completeDetailedDescription, setCompleteDetailedDescription] = useState('');
  const [completeAttachments, setCompleteAttachments] = useState<CompleteAttachmentRow[]>([]);
  const [editingAttachKey, setEditingAttachKey] = useState<string | null>(null);

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
      } catch {
        setProfile(null);
      }
    })();
  }, []);

  const isDirector = String(profile?.role ?? '').toUpperCase() === 'DIRECTOR';
  const canDirectorApprove = isDirector && !!selected && selected.rawStatus === 1;

  const reload = async () => {
    try {
      const list = await maintenanceRequestService.list();
      setRows(mapListToRows(list));
    } catch {
      // ignore
    }
  };

  const openStartMaintenance = async (row: MaintenanceRow) => {
    setStartRow(row);
    setStartLoading(true);
    setStartOpen(true);
    try {
      const det = await assetRequestService.getById(row.assetRequestId);
      const aid = det.asset?.assetId;
      if (aid) {
        const asset = await assetService.getById(aid);
        setStartAsset(asset);
        setLocationText(asset.currentDepartmentName ?? '');
      } else {
        setStartAsset(null);
        setLocationText(row.assetState || '');
      }
      setReportNumber('');
      setMaintenanceDate(toTodayInputDate());
      setMarkCompleted(false);
      setExpectedCompletionDate('');
      setMaintenanceContent('');
      setDetailedDescription(row.purpose || '');
      setPerformerUserId(null);
      setMaintenanceProvider('');
      setEstimatedCost(null);
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
    if (!startRow || !profile?.id) return;
    if (!maintenanceDate) {
      message.warning('Vui lòng chọn ngày bảo dưỡng.');
      return;
    }
    setStartSubmitting(true);
    try {
      const loc =
        locationType === 'at-unit'
          ? startAsset?.currentDepartmentName ?? locationText
          : locationText;

      const expectedCompletionDateIso = expectedCompletionDate
        ? toIsoDate(expectedCompletionDate)
        : undefined;

      const payload: MaintenanceStartPayload = {
        startedBy: profile.id,
        comment: markCompleted ? 'Đánh dấu đã hoàn thành (ghi nhận lúc bắt đầu)' : null,
        reportNumber: reportNumber.trim() || null,
        maintenanceDate: toIsoDate(maintenanceDate),
        performerUserId: performerUserId ?? undefined,
        maintenanceProvider: maintenanceProvider.trim() || null,
        estimatedCost: estimatedCost ?? undefined,
        expectedCompletionDate: expectedCompletionDateIso,
        maintenanceContent: maintenanceContent.trim() || null,
        detailedDescription: detailedDescription.trim() || null,
        locationType,
        location: loc || null,
      };

      await maintenanceRequestService.start(startRow.assetRequestId, payload);
      message.success('Đã bắt đầu bảo dưỡng.');
      setStartOpen(false);
      setStartRow(null);
      setStartAsset(null);
      await reload();
    } catch (e: any) {
      const msg = e?.response?.data ?? 'Không thể bắt đầu bảo dưỡng.';
      message.error(typeof msg === 'string' ? msg : 'Không thể bắt đầu bảo dưỡng.');
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
    setCompleteAttachments([]);
    setEditingAttachKey(null);
    try {
      const det = await assetRequestService.getById(row.assetRequestId);
      const aid = det.asset?.assetId;
      if (aid) {
        const asset = await assetService.getById(aid);
        setCompleteAsset(asset);
      } else {
        setCompleteAsset(null);
      }

      let maintLabel = row.setupDate || row.expectedDate || '';
      let rep = '';
      let contentFromStart = '';
      let detailFromStart = '';
      if (det.proposedData) {
        try {
          const pd = JSON.parse(det.proposedData) as Record<string, unknown>;
          if (pd.maintenanceDate) maintLabel = formatDate(String(pd.maintenanceDate));
          if (typeof pd.reportNumber === 'string') rep = pd.reportNumber;
          if (typeof pd.maintenanceContent === 'string') contentFromStart = pd.maintenanceContent;
          if (typeof pd.detailedDescription === 'string') detailFromStart = pd.detailedDescription;
        } catch {
          /* ignore */
        }
      }
      setCompleteMaintenanceDateLabel(maintLabel);
      setCompleteReportNumber(rep);
      setCompleteCompletionDate(toTodayInputDate());
      setCompleteReturnDate(toTodayInputDate());
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
    if (!completeRow || !profile?.id) return;
    if (!completeCompletionDate) {
      message.warning('Vui lòng chọn ngày hoàn thành bảo dưỡng.');
      return;
    }
    if (!completeReturnDate) {
      message.warning('Vui lòng chọn ngày đưa vào sử dụng lại.');
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
        attachmentUrls:
          completeAttachments.length > 0
            ? completeAttachments.map((a) => a.name.trim()).filter(Boolean)
            : null,
      };

      await maintenanceRequestService.complete(taskId, payload);
      message.success('Đã hoàn thành bảo dưỡng.');
      setCompleteOpen(false);
      setCompleteRow(null);
      setCompleteAsset(null);
      await reload();
    } catch (e: any) {
      const msg = e?.response?.data ?? 'Không thể hoàn thành bảo dưỡng.';
      message.error(typeof msg === 'string' ? msg : 'Không thể hoàn thành bảo dưỡng.');
    } finally {
      setCompleteSubmitting(false);
    }
  };

  const submitDirectorDecision = async () => {
    if (!selected || !profile?.id) return;
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
    } catch (e: any) {
      const msg = e?.response?.data ?? 'Thao tác phê duyệt thất bại.';
      message.error(typeof msg === 'string' ? msg : 'Thao tác phê duyệt thất bại.');
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

  return (
    <div className="maintenance-page">
      <div className="maintenance-header">
        <h1 className="maintenance-page__title">Bảo dưỡng</h1>
        <Button type="primary" className="maintenance-btn-add">
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
                <th className="asset-table__cell asset-table__cell--checkbox">
                  <input type="checkbox" />
                </th>
                <th>MÃ TÀI SẢN</th>
                <th>TÊN TÀI SẢN</th>
                <th>LOẠI TÀI SẢN</th>
                <th>MỤC ĐÍCH</th>
                <th>NGÀY THIẾT LẬP BD</th>
                <th>NGÀY BD DỰ KIẾN</th>
                <th>TÌNH TRẠNG TS</th>
                <th>TRẠNG THÁI</th>
                <th className="asset-table__cell asset-table__cell--actions" />
              </tr>
            </thead>
            <tbody>
              {loading ? (
                <tr>
                  <td colSpan={10} className="maintenance-table-empty">
                    Đang tải dữ liệu...
                  </td>
                </tr>
              ) : filteredRows.length === 0 ? (
                <tr>
                  <td colSpan={10} className="maintenance-table-empty">
                    Không có dữ liệu.
                  </td>
                </tr>
              ) : (
                filteredRows.map((row) => (
                  <tr key={row.id} className="asset-row">
                    <td className="asset-table__cell asset-table__cell--checkbox">
                      <input type="checkbox" />
                    </td>
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

      <Modal
        open={detailOpen}
        title={selected ? `Chi tiết yêu cầu BD - YC-${selected.assetRequestId}` : 'Chi tiết yêu cầu bảo dưỡng'}
        onCancel={() => {
          setDetailOpen(false);
          setSelected(null);
        }}
        footer={[
          <Button
            key="close"
            onClick={() => {
              setDetailOpen(false);
              setSelected(null);
            }}
          >
            Đóng
          </Button>,
          canDirectorApprove ? (
            <Button
              key="approve"
              type="primary"
              onClick={() => {
                setDecision('approved');
                setComment('');
                setApproveOpen(true);
              }}
            >
              Phê duyệt
            </Button>
          ) : null,
        ]}
      >
        {!selected ? (
          <div>Không có dữ liệu.</div>
        ) : (
          <div style={{ display: 'grid', gap: 8 }}>
            <div>
              <b>Tài sản:</b> {[selected.assetCode, selected.assetName].filter(Boolean).join(' - ') || '—'}
            </div>
            <div>
              <b>Mục đích:</b> {selected.purpose || '—'}
            </div>
            <div>
              <b>Ngày dự kiến:</b> {selected.expectedDate || '—'}
            </div>
            <div>
              <b>Phòng ban:</b> {selected.assetState || '—'}
            </div>
            <div>
              <b>Trạng thái:</b> {getStatusLabel(selected.status)}
            </div>
          </div>
        )}
      </Modal>

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
                placeholder="VD: BA001"
                value={reportNumber}
                onChange={(e) => setReportNumber(e.target.value)}
              />
            </div>

            <div className="maintenance-form-modal__section">
              <div className="maintenance-form-modal__section-title">Thông tin tài sản</div>
              <div className="maintenance-form-modal__info-grid">
                <div className="maintenance-form-modal__info-row">
                  <div className="maintenance-form-modal__info-item">
                    <label>Mã tài sản</label>
                    <div className="maintenance-form-modal__info-value">
                      {startAsset?.code ?? startRow.assetCode}
                    </div>
                  </div>
                  <div className="maintenance-form-modal__info-item">
                    <label>Giá trị tài sản</label>
                    <div className="maintenance-form-modal__info-value">
                      {startAsset ? formatVnd(startAsset.originalPrice) : '—'}
                    </div>
                  </div>
                </div>
                <div className="maintenance-form-modal__info-row">
                  <div className="maintenance-form-modal__info-item">
                    <label>Tên tài sản</label>
                    <div className="maintenance-form-modal__info-value">
                      {startAsset?.name ?? startRow.assetName}
                    </div>
                  </div>
                  <div className="maintenance-form-modal__info-item">
                    <label>Giá trị còn lại</label>
                    <div className="maintenance-form-modal__info-value">
                      {startAsset?.remainingValue != null
                        ? formatVnd(startAsset.remainingValue)
                        : startAsset
                          ? formatVnd(startAsset.currentValue)
                          : '—'}
                    </div>
                  </div>
                </div>
                <div className="maintenance-form-modal__info-row">
                  <div className="maintenance-form-modal__info-item">
                    <label>Loại tài sản</label>
                    <div className="maintenance-form-modal__info-value">
                      {startAsset?.assetTypeName ?? '—'}
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
                  <div className="maintenance-form-modal__info-item">
                    <label>Quy cách tài sản</label>
                    <div className="maintenance-form-modal__info-value">
                      {startAsset
                        ? [startAsset.unit ? `Đơn vị: ${startAsset.unit}` : null, `SL: ${startAsset.quantity}`]
                            .filter(Boolean)
                            .join(' · ')
                        : '—'}
                    </div>
                  </div>
                  <div className="maintenance-form-modal__info-item">
                    <label>Tình trạng</label>
                    <div className="maintenance-form-modal__info-value">{startAsset?.statusName ?? '—'}</div>
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
                      {startAsset?.warrantyEndDate ? formatDate(startAsset.warrantyEndDate) : '—'}
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
                  <label className="maintenance-form-modal__checkbox-label">
                    <input
                      type="checkbox"
                      checked={markCompleted}
                      onChange={(e) => setMarkCompleted(e.target.checked)}
                    />{' '}
                    Đã hoàn thành
                  </label>
                </div>
                <div>
                  <div className="maintenance-form-modal__label">Người thực hiện (mã user)</div>
                  <input
                    className="maintenance-form-modal__input"
                    type="number"
                    min={1}
                    placeholder="UserId"
                    value={performerUserId ?? ''}
                    onChange={(e) => {
                      const v = e.target.value;
                      setPerformerUserId(v ? Number(v) : null);
                    }}
                  />
                </div>
                <div>
                  <div className="maintenance-form-modal__label">Đơn vị bảo dưỡng</div>
                  <input
                    className="maintenance-form-modal__input"
                    value={maintenanceProvider}
                    onChange={(e) => setMaintenanceProvider(e.target.value)}
                    placeholder="Tên đơn vị"
                  />
                </div>
                <div>
                  <div className="maintenance-form-modal__label">Chi phí dự kiến</div>
                  <input
                    className="maintenance-form-modal__input"
                    type="number"
                    min={0}
                    value={estimatedCost ?? ''}
                    onChange={(e) => {
                      const v = e.target.value;
                      setEstimatedCost(v ? Number(v) : null);
                    }}
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
                  <input
                    className="maintenance-form-modal__input"
                    value={maintenanceContent}
                    onChange={(e) => setMaintenanceContent(e.target.value)}
                  />
                </div>
                <div className="maintenance-form-modal__form-grid-full">
                  <div className="maintenance-form-modal__label">Mô tả chi tiết</div>
                  <textarea
                    className="maintenance-form-modal__textarea"
                    value={detailedDescription}
                    onChange={(e) => setDetailedDescription(e.target.value)}
                  />
                </div>
                <div className="maintenance-form-modal__form-grid-full">
                  <div className="maintenance-form-modal__label">Địa điểm bảo dưỡng</div>
                  <div className="maintenance-form-modal__radio-group">
                    <label>
                      <input
                        type="radio"
                        name="maintenance-start-location-type"
                        value="at-unit"
                        checked={locationType === 'at-unit'}
                        onChange={() => setLocationType('at-unit')}
                      />
                      Tại đơn vị
                    </label>
                    <label>
                      <input
                        type="radio"
                        name="maintenance-start-location-type"
                        value="provider"
                        checked={locationType === 'provider'}
                        onChange={() => setLocationType('provider')}
                      />
                      Nhà cung cấp
                    </label>
                  </div>
                  <div className="maintenance-form-modal__location-field">
                    {locationType === 'at-unit' ? (
                      <span>
                        <b>Địa chỉ:</b>{' '}
                        {(startAsset?.currentDepartmentName ?? locationText) || '—'}
                      </span>
                    ) : (
                      <input
                        className="maintenance-form-modal__input"
                        placeholder="Địa chỉ nhà cung cấp / chi tiết"
                        value={locationText}
                        onChange={(e) => setLocationText(e.target.value)}
                      />
                    )}
                  </div>
                </div>
              </div>
              <div className="maintenance-form-modal__label maintenance-form-modal__label--top-gap">
                Tài liệu đính kèm
              </div>
              <div className="maintenance-form-modal__attachments">
                {['Biên bản kiểm tra', 'Hợp đồng / báo giá'].map((name, i) => (
                  <div key={name} className="maintenance-form-modal__attach-row">
                    <span>
                      #{i + 1} {name}
                    </span>
                    <button type="button" className="maintenance-form-modal__attach-btn" disabled>
                      Tải xuống
                    </button>
                  </div>
                ))}
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
                  disabled={startSubmitting || startLoading}
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
                placeholder="VD: BA001"
                value={completeReportNumber}
                onChange={(e) => setCompleteReportNumber(e.target.value)}
              />
            </div>

            <div className="maintenance-form-modal__section">
              <div className="maintenance-form-modal__section-title">Thông tin tài sản</div>
              <div className="maintenance-form-modal__info-grid">
                <div className="maintenance-form-modal__info-row">
                  <div className="maintenance-form-modal__info-item">
                    <label>Mã tài sản</label>
                    <div className="maintenance-form-modal__info-value">
                      {completeAsset?.code ?? completeRow.assetCode}
                    </div>
                  </div>
                  <div className="maintenance-form-modal__info-item">
                    <label>Giá trị tài sản</label>
                    <div className="maintenance-form-modal__info-value">
                      {completeAsset ? formatVnd(completeAsset.originalPrice) : '—'}
                    </div>
                  </div>
                </div>
                <div className="maintenance-form-modal__info-row">
                  <div className="maintenance-form-modal__info-item">
                    <label>Tên tài sản</label>
                    <div className="maintenance-form-modal__info-value">
                      {completeAsset?.name ?? completeRow.assetName}
                    </div>
                  </div>
                  <div className="maintenance-form-modal__info-item">
                    <label>Giá trị còn lại</label>
                    <div className="maintenance-form-modal__info-value">
                      {completeAsset?.remainingValue != null
                        ? formatVnd(completeAsset.remainingValue)
                        : completeAsset
                          ? formatVnd(completeAsset.currentValue)
                          : '—'}
                    </div>
                  </div>
                </div>
                <div className="maintenance-form-modal__info-row">
                  <div className="maintenance-form-modal__info-item">
                    <label>Loại tài sản</label>
                    <div className="maintenance-form-modal__info-value">
                      {completeAsset?.assetTypeName ?? '—'}
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
                      {completeAsset
                        ? [
                            completeAsset.unit ? `Đơn vị: ${completeAsset.unit}` : null,
                            `SL: ${completeAsset.quantity}`,
                          ]
                            .filter(Boolean)
                            .join(' · ')
                        : '—'}
                    </div>
                  </div>
                  <div className="maintenance-form-modal__info-item">
                    <label>Tình trạng</label>
                    <div className="maintenance-form-modal__info-value">
                      {completeAsset?.statusName ?? '—'}
                    </div>
                  </div>
                </div>
                <div className="maintenance-form-modal__info-row">
                  <div className="maintenance-form-modal__info-item">
                    <label>Hạn bảo hành</label>
                    <div className="maintenance-form-modal__info-value">
                      {completeAsset?.warrantyEndDate
                        ? formatDate(completeAsset.warrantyEndDate)
                        : '—'}
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
                  <input
                    className="maintenance-form-modal__input"
                    type="number"
                    min={0}
                    value={completeActualCost ?? ''}
                    onChange={(e) => {
                      const v = e.target.value;
                      setCompleteActualCost(v ? Number(v) : null);
                    }}
                  />
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

              <div className="maintenance-form-modal__label maintenance-form-modal__label--top-gap">
                Tài liệu đính kèm
              </div>
              <div className="maintenance-form-modal__attachments">
                {completeAttachments.map((att) => (
                  <div key={att.key} className="maintenance-form-modal__attach-row">
                    {editingAttachKey === att.key ? (
                      <input
                        className="maintenance-form-modal__input"
                        defaultValue={att.name}
                        onBlur={(e) => {
                          const v = e.target.value.trim() || att.name;
                          setCompleteAttachments((prev) =>
                            prev.map((x) => (x.key === att.key ? { ...x, name: v } : x))
                          );
                          setEditingAttachKey(null);
                        }}
                        onKeyDown={(e) => {
                          if (e.key === 'Enter') (e.target as HTMLInputElement).blur();
                        }}
                        autoFocus
                      />
                    ) : (
                      <span>{att.name}</span>
                    )}
                    <div className="maintenance-form-modal__attach-actions">
                      <button
                        type="button"
                        className="maintenance-form-modal__attach-btn"
                        onClick={() =>
                          setEditingAttachKey(editingAttachKey === att.key ? null : att.key)
                        }
                      >
                        Sửa
                      </button>
                      <button
                        type="button"
                        className="maintenance-form-modal__attach-btn maintenance-form-modal__attach-btn--danger"
                        onClick={() =>
                          setCompleteAttachments((prev) => prev.filter((x) => x.key !== att.key))
                        }
                      >
                        Xóa
                      </button>
                    </div>
                  </div>
                ))}
              </div>
              <button
                type="button"
                className="maintenance-form-modal__btn-upload"
                onClick={() =>
                  setCompleteAttachments((prev) => [
                    ...prev,
                    {
                      key: `att-${Date.now()}`,
                      name: `Tài liệu ${prev.length + 1}`,
                    },
                  ])
                }
              >
                Thêm file đính kèm
              </button>
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
                  disabled={completeSubmitting || completeLoading}
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


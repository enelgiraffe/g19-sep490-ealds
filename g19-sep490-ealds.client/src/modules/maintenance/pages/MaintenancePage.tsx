import { useEffect, useMemo, useState } from 'react';
import {
  Button,
  DatePicker,
  Input,
  InputNumber,
  Modal,
  Popconfirm,
  Radio,
  Row,
  Col,
  Select,
  Tabs,
  message,
} from 'antd';
import type { Dayjs } from 'dayjs';
import dayjs from 'dayjs';
import {
  CheckOutlined,
  DeleteOutlined,
  DownloadOutlined,
  EditOutlined,
  EyeOutlined,
  FilterOutlined,
  PlusOutlined,
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
  const [maintenanceDate, setMaintenanceDate] = useState<Dayjs | null>(null);
  const [markCompleted, setMarkCompleted] = useState(false);
  const [expectedRange, setExpectedRange] = useState<[Dayjs | null, Dayjs | null] | null>(null);
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
  const [completeCompletionDate, setCompleteCompletionDate] = useState<Dayjs | null>(null);
  const [completeReturnDate, setCompleteReturnDate] = useState<Dayjs | null>(null);
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
      setMaintenanceDate(dayjs());
      setMarkCompleted(false);
      setExpectedRange(null);
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

      let expectedCompletionDate: string | undefined;
      let expectedCompletionFrom: string | undefined;
      let expectedCompletionTo: string | undefined;
      const er = expectedRange;
      if (er?.[0] && er[1]) {
        if (er[0].isSame(er[1], 'day')) {
          expectedCompletionDate = er[0].toISOString();
        } else {
          expectedCompletionFrom = er[0].toISOString();
          expectedCompletionTo = er[1].toISOString();
        }
      }

      const payload: MaintenanceStartPayload = {
        startedBy: profile.id,
        comment: markCompleted ? 'Đánh dấu đã hoàn thành (ghi nhận lúc bắt đầu)' : null,
        reportNumber: reportNumber.trim() || null,
        maintenanceDate: maintenanceDate.toISOString(),
        performerUserId: performerUserId ?? undefined,
        maintenanceProvider: maintenanceProvider.trim() || null,
        estimatedCost: estimatedCost ?? undefined,
        expectedCompletionDate,
        expectedCompletionFrom,
        expectedCompletionTo,
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
      setCompleteCompletionDate(dayjs());
      setCompleteReturnDate(dayjs());
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
        completionDate: completeCompletionDate.toISOString(),
        returnToUseDate: completeReturnDate.toISOString(),
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

      <Modal
        className="maintenance-form-modal"
        open={startOpen}
        title="Bảo dưỡng tài sản"
        width={920}
        onCancel={() => {
          setStartOpen(false);
          setStartRow(null);
          setStartAsset(null);
        }}
        footer={
          <div className="maintenance-form-modal__footer-actions">
            <Button
              className="maintenance-form-modal__btn-cancel"
              onClick={() => {
                setStartOpen(false);
                setStartRow(null);
                setStartAsset(null);
              }}
            >
              Hủy
            </Button>
            <Button
              className="maintenance-form-modal__btn-confirm"
              type="primary"
              loading={startSubmitting}
              onClick={submitStartMaintenance}
            >
              Xác nhận bảo dưỡng
            </Button>
          </div>
        }
        destroyOnClose
      >
        {startLoading ? (
          <div>Đang tải...</div>
        ) : !startRow ? (
          <div>Không có dữ liệu.</div>
        ) : (
          <div>
            <div className="maintenance-form-modal__section">
              <div className="maintenance-form-modal__label">Số biên bản</div>
              <Input
                style={{ maxWidth: 280, marginTop: 6 }}
                placeholder="VD: BA001"
                value={reportNumber}
                onChange={(e) => setReportNumber(e.target.value)}
              />
            </div>

            <div className="maintenance-form-modal__section">
              <div className="maintenance-form-modal__section-title">Thông tin tài sản</div>
              <div className="maintenance-form-modal__readonly-grid">
                <div>
                  <b>Mã tài sản:</b>
                  {startAsset?.code ?? startRow.assetCode}
                </div>
                <div>
                  <b>Giá trị tài sản:</b>
                  {startAsset ? formatVnd(startAsset.originalPrice) : '—'}
                </div>
                <div>
                  <b>Tên tài sản:</b>
                  {startAsset?.name ?? startRow.assetName}
                </div>
                <div>
                  <b>Giá trị còn lại:</b>
                  {startAsset?.remainingValue != null
                    ? formatVnd(startAsset.remainingValue)
                    : startAsset
                      ? formatVnd(startAsset.currentValue)
                      : '—'}
                </div>
                <div>
                  <b>Loại tài sản:</b>
                  {startAsset?.assetTypeName ?? '—'}
                </div>
                <div>
                  <b>Vị trí tài sản:</b>
                  {startAsset?.warehouseName ||
                    startAsset?.currentDepartmentName ||
                    startRow.assetState ||
                    '—'}
                </div>
                <div>
                  <b>Quy cách tài sản:</b>
                  {startAsset
                    ? [startAsset.unit ? `Đơn vị: ${startAsset.unit}` : null, `SL: ${startAsset.quantity}`]
                        .filter(Boolean)
                        .join(' · ')
                    : '—'}
                </div>
                <div>
                  <b>Tình trạng:</b>
                  {startAsset?.statusName ?? '—'}
                </div>
                <div>
                  <b>Ngày mua:</b>{' '}
                  {startAsset?.purchaseDate ? formatDate(startAsset.purchaseDate) : '—'}
                </div>
                <div>
                  <b>Ngày đưa vào SD:</b>{' '}
                  {startAsset?.inUseDate ? formatDate(startAsset.inUseDate) : '—'}
                </div>
                <div>
                  <b>Hạn bảo hành:</b>{' '}
                  {startAsset?.warrantyEndDate ? formatDate(startAsset.warrantyEndDate) : '—'}
                </div>
                <div>
                  <b>Phòng ban SD:</b>
                  {startAsset?.currentDepartmentName ?? '—'}
                </div>
              </div>
            </div>

            <div className="maintenance-form-modal__section">
              <div className="maintenance-form-modal__section-title">Thông tin bảo dưỡng tài sản</div>
              <Row gutter={[16, 12]}>
                <Col xs={24} md={12}>
                  <div className="maintenance-form-modal__label maintenance-form-modal__label--req">
                    Ngày bảo dưỡng
                  </div>
                  <DatePicker
                    style={{ width: '100%', marginTop: 4 }}
                    format="DD/MM/YYYY"
                    value={maintenanceDate}
                    onChange={(v) => setMaintenanceDate(v)}
                  />
                  <label style={{ display: 'block', marginTop: 10, fontSize: 13 }}>
                    <input
                      type="checkbox"
                      checked={markCompleted}
                      onChange={(e) => setMarkCompleted(e.target.checked)}
                    />{' '}
                    Đã hoàn thành
                  </label>
                </Col>
                <Col xs={24} md={12}>
                  <div className="maintenance-form-modal__label">Người thực hiện (mã user)</div>
                  <InputNumber
                    style={{ width: '100%', marginTop: 4 }}
                    min={1}
                    placeholder="UserId"
                    value={performerUserId ?? undefined}
                    onChange={(v) => setPerformerUserId(typeof v === 'number' ? v : null)}
                  />
                  <div className="maintenance-form-modal__label" style={{ marginTop: 8 }}>
                    Đơn vị bảo dưỡng
                  </div>
                  <Input
                    style={{ marginTop: 4 }}
                    value={maintenanceProvider}
                    onChange={(e) => setMaintenanceProvider(e.target.value)}
                    placeholder="Tên đơn vị"
                  />
                </Col>
                <Col xs={24} md={12}>
                  <div className="maintenance-form-modal__label">Chi phí dự kiến</div>
                  <InputNumber
                    style={{ width: '100%', marginTop: 4 }}
                    min={0}
                    value={estimatedCost ?? undefined}
                    onChange={(v) => setEstimatedCost(typeof v === 'number' ? v : null)}
                    formatter={(v) =>
                      v != null && String(v) !== ''
                        ? `${String(v).replace(/\B(?=(\d{3})+(?!\d))/g, '.')}₫`
                        : ''
                    }
                    parser={(v) => {
                      const n = Number(String(v ?? '').replace(/\./g, '').replace('₫', '').trim());
                      return Number.isFinite(n) ? n : 0;
                    }}
                  />
                </Col>
                <Col xs={24} md={12}>
                  <div className="maintenance-form-modal__label">Ngày dự kiến hoàn thành (khoảng)</div>
                  <DatePicker.RangePicker
                    style={{ width: '100%', marginTop: 4 }}
                    format="DD/MM/YYYY"
                    value={
                      expectedRange?.[0] && expectedRange[1]
                        ? [expectedRange[0], expectedRange[1]]
                        : null
                    }
                    onChange={(vals) => {
                      if (vals?.[0] && vals[1]) setExpectedRange([vals[0], vals[1]]);
                      else setExpectedRange(null);
                    }}
                  />
                </Col>
                <Col span={24}>
                  <div className="maintenance-form-modal__label">Nội dung bảo dưỡng</div>
                  <Input
                    style={{ marginTop: 4 }}
                    value={maintenanceContent}
                    onChange={(e) => setMaintenanceContent(e.target.value)}
                  />
                </Col>
                <Col span={24}>
                  <div className="maintenance-form-modal__label">Mô tả chi tiết</div>
                  <Input.TextArea
                    rows={3}
                    style={{ marginTop: 4 }}
                    value={detailedDescription}
                    onChange={(e) => setDetailedDescription(e.target.value)}
                  />
                </Col>
                <Col span={24}>
                  <div className="maintenance-form-modal__label">Địa điểm bảo dưỡng</div>
                  <Radio.Group
                    style={{ marginTop: 4 }}
                    value={locationType}
                    onChange={(e) => setLocationType(e.target.value)}
                  >
                    <Radio value="at-unit">Tại đơn vị</Radio>
                    <Radio value="provider">Nhà cung cấp</Radio>
                  </Radio.Group>
                  <div style={{ marginTop: 8 }}>
                    {locationType === 'at-unit' ? (
                      <span>
                        <b>Địa chỉ:</b>{' '}
                        {(startAsset?.currentDepartmentName ?? locationText) || '—'}
                      </span>
                    ) : (
                      <Input
                        placeholder="Địa chỉ nhà cung cấp / chi tiết"
                        value={locationText}
                        onChange={(e) => setLocationText(e.target.value)}
                      />
                    )}
                  </div>
                </Col>
              </Row>
              <div className="maintenance-form-modal__label" style={{ marginTop: 14 }}>
                Tài liệu đính kèm
              </div>
              <div className="maintenance-form-modal__attachments">
                {['Biên bản kiểm tra', 'Hợp đồng / báo giá'].map((name, i) => (
                  <div key={name} className="maintenance-form-modal__attach-row">
                    <span>
                      #{i + 1} {name}
                    </span>
                    <Button type="text" icon={<DownloadOutlined />} disabled title="Tích hợp tải file sau" />
                  </div>
                ))}
              </div>
            </div>
          </div>
        )}
      </Modal>

      <Modal
        className="maintenance-form-modal"
        open={completeOpen}
        title="Hoàn thành bảo dưỡng tài sản"
        width={920}
        onCancel={() => {
          setCompleteOpen(false);
          setCompleteRow(null);
          setCompleteAsset(null);
        }}
        footer={
          <div className="maintenance-form-modal__footer-actions">
            <Button
              className="maintenance-form-modal__btn-cancel"
              onClick={() => {
                setCompleteOpen(false);
                setCompleteRow(null);
                setCompleteAsset(null);
              }}
            >
              Hủy
            </Button>
            <Button
              className="maintenance-form-modal__btn-confirm"
              type="primary"
              icon={<CheckOutlined />}
              loading={completeSubmitting}
              onClick={submitCompleteMaintenance}
            >
              Lưu
            </Button>
          </div>
        }
        destroyOnClose
      >
        {completeLoading ? (
          <div>Đang tải...</div>
        ) : !completeRow ? (
          <div>Không có dữ liệu.</div>
        ) : (
          <div>
            <div className="maintenance-form-modal__section">
              <div className="maintenance-form-modal__label">Số biên bản</div>
              <Input
                style={{ maxWidth: 280, marginTop: 6 }}
                placeholder="VD: BA001"
                value={completeReportNumber}
                onChange={(e) => setCompleteReportNumber(e.target.value)}
              />
            </div>

            <div className="maintenance-form-modal__section">
              <div className="maintenance-form-modal__section-title">Thông tin tài sản</div>
              <div className="maintenance-form-modal__readonly-grid">
                <div>
                  <b>Mã tài sản:</b>
                  {completeAsset?.code ?? completeRow.assetCode}
                </div>
                <div>
                  <b>Giá trị tài sản:</b>
                  {completeAsset ? formatVnd(completeAsset.originalPrice) : '—'}
                </div>
                <div>
                  <b>Tên tài sản:</b>
                  {completeAsset?.name ?? completeRow.assetName}
                </div>
                <div>
                  <b>Giá trị còn lại:</b>
                  {completeAsset?.remainingValue != null
                    ? formatVnd(completeAsset.remainingValue)
                    : completeAsset
                      ? formatVnd(completeAsset.currentValue)
                      : '—'}
                </div>
                <div>
                  <b>Loại tài sản:</b>
                  {completeAsset?.assetTypeName ?? '—'}
                </div>
                <div>
                  <b>Vị trí tài sản:</b>
                  {completeAsset?.warehouseName ||
                    completeAsset?.currentDepartmentName ||
                    completeRow.assetState ||
                    '—'}
                </div>
                <div>
                  <b>Quy cách tài sản:</b>
                  {completeAsset
                    ? [
                        completeAsset.unit ? `Đơn vị: ${completeAsset.unit}` : null,
                        `SL: ${completeAsset.quantity}`,
                      ]
                        .filter(Boolean)
                        .join(' · ')
                    : '—'}
                </div>
                <div>
                  <b>Tình trạng:</b>
                  {completeAsset?.statusName ?? '—'}
                </div>
                <div>
                  <b>Hạn bảo hành:</b>{' '}
                  {completeAsset?.warrantyEndDate
                    ? formatDate(completeAsset.warrantyEndDate)
                    : '—'}
                </div>
                <div>
                  <b>Ngày bảo dưỡng:</b> {completeMaintenanceDateLabel || '—'}
                </div>
                <div>
                  <b>Phòng ban SD:</b>
                  {completeAsset?.currentDepartmentName ?? '—'}
                </div>
              </div>
            </div>

            <div className="maintenance-form-modal__section">
              <div className="maintenance-form-modal__section-title">
                Thông tin hoàn thành bảo dưỡng
              </div>
              <Row gutter={[16, 12]}>
                <Col xs={24} md={12}>
                  <div className="maintenance-form-modal__label maintenance-form-modal__label--req">
                    Ngày hoàn thành bảo dưỡng
                  </div>
                  <DatePicker
                    style={{ width: '100%', marginTop: 4 }}
                    format="DD/MM/YYYY"
                    value={completeCompletionDate}
                    onChange={(v) => setCompleteCompletionDate(v)}
                  />
                </Col>
                <Col xs={24} md={12}>
                  <div className="maintenance-form-modal__label maintenance-form-modal__label--req">
                    Ngày đưa vào sử dụng lại
                  </div>
                  <DatePicker
                    style={{ width: '100%', marginTop: 4 }}
                    format="DD/MM/YYYY"
                    value={completeReturnDate}
                    onChange={(v) => setCompleteReturnDate(v)}
                  />
                </Col>
                <Col xs={24} md={12}>
                  <div className="maintenance-form-modal__label maintenance-form-modal__label--req">
                    Chi phí thực tế
                  </div>
                  <InputNumber
                    style={{ width: '100%', marginTop: 4 }}
                    min={0}
                    value={completeActualCost ?? undefined}
                    onChange={(v) => setCompleteActualCost(typeof v === 'number' ? v : null)}
                    formatter={(v) =>
                      v != null && String(v) !== ''
                        ? `${String(v).replace(/\B(?=(\d{3})+(?!\d))/g, '.')}₫`
                        : ''
                    }
                    parser={(v) => {
                      const n = Number(String(v ?? '').replace(/\./g, '').replace('₫', '').trim());
                      return Number.isFinite(n) ? n : 0;
                    }}
                  />
                </Col>
                <Col span={24}>
                  <div className="maintenance-form-modal__label">Nội dung bảo dưỡng</div>
                  <Input
                    style={{ marginTop: 4 }}
                    value={completeMaintenanceContent}
                    onChange={(e) => setCompleteMaintenanceContent(e.target.value)}
                    placeholder="VD: Thay dầu"
                  />
                </Col>
                <Col span={24}>
                  <div className="maintenance-form-modal__label">Mô tả chi tiết</div>
                  <Input.TextArea
                    rows={3}
                    style={{ marginTop: 4 }}
                    value={completeDetailedDescription}
                    onChange={(e) => setCompleteDetailedDescription(e.target.value)}
                    placeholder="VD: Hỏng nhẹ"
                  />
                </Col>
              </Row>

              <div className="maintenance-form-modal__label" style={{ marginTop: 14 }}>
                Tài liệu đính kèm
              </div>
              <div className="maintenance-form-modal__attachments">
                {completeAttachments.map((att) => (
                  <div key={att.key} className="maintenance-form-modal__attach-row">
                    {editingAttachKey === att.key ? (
                      <Input
                        size="small"
                        defaultValue={att.name}
                        onBlur={(e) => {
                          const v = e.target.value.trim() || att.name;
                          setCompleteAttachments((prev) =>
                            prev.map((x) => (x.key === att.key ? { ...x, name: v } : x))
                          );
                          setEditingAttachKey(null);
                        }}
                        onPressEnter={(e) => (e.target as HTMLInputElement).blur()}
                        autoFocus
                      />
                    ) : (
                      <span>{att.name}</span>
                    )}
                    <span>
                      <Button
                        type="text"
                        size="small"
                        icon={<EditOutlined />}
                        onClick={() =>
                          setEditingAttachKey(editingAttachKey === att.key ? null : att.key)
                        }
                      />
                      <Button
                        type="text"
                        size="small"
                        danger
                        icon={<DeleteOutlined />}
                        onClick={() =>
                          setCompleteAttachments((prev) => prev.filter((x) => x.key !== att.key))
                        }
                      />
                    </span>
                  </div>
                ))}
              </div>
              <Button
                type="primary"
                ghost
                icon={<PlusOutlined />}
                style={{ marginTop: 8 }}
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
              </Button>
            </div>
          </div>
        )}
      </Modal>

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


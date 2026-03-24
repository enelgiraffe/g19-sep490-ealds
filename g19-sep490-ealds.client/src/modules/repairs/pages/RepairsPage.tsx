import { useEffect, useMemo, useState } from 'react';
import {
  Button,
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
import { damageReportService } from '../../assets/services/damageReportService';
import { assetRequestService } from '../../assets/services/assetRequestService';
import { assetService, formatVnd, type AssetResponse } from '../../assets/services/assetService';
import {
  repairRequestService,
  type RepairStartPayload,
  type RepairCompletePayload,
} from '../../assets/services/repairRequestService';
import { directorRequestService } from '../../requests/services/directorRequestService';
import { profileService, type UserProfile } from '../../profile/services/profileService';
import { RepairStartModal, type RepairStartFormValues } from '../components/RepairStartModal';
import { RepairCompleteModal, type RepairCompleteFormValues } from '../components/RepairCompleteModal';

type RepairStatus =
  | 'draft'
  | 'submitted'
  | 'pending'
  | 'approved'
  | 'rejected'
  | 'inProgress'
  | 'completed';

export interface RepairRow {
  id: string;
  assetRequestId: number;
  assetId: number;
  assetCode: string;
  assetName: string;
  condition: string;
  brokenDate: string;
  quantity: number;
  location: string;
  department: string;
  status: RepairStatus;
  rawStatus: number;
}

function formatDate(value?: string | null): string {
  if (!value) return '';
  const d = new Date(value);
  if (Number.isNaN(d.getTime())) return value;
  return d.toLocaleDateString('vi-VN');
}

function mapStatus(status: number): RepairStatus {
  // -1=Nháp, 0=Đã gửi, 1=Chờ phê duyệt, 2=Đã duyệt, 3=Từ chối, 4=Đang sửa chữa, 5=Hoàn thành
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
  if (status === 'submitted') return 'asset-status-pill asset-status-pill--processing';
  if (status === 'pending') return 'asset-status-pill asset-status-pill--warning';
  if (status === 'approved') return 'asset-status-pill asset-status-pill--active';
  if (status === 'inProgress') return 'asset-status-pill asset-status-pill--processing';
  if (status === 'completed') return 'asset-status-pill asset-status-pill--active';
  if (status === 'draft') return 'asset-status-pill asset-status-pill--inactive';
  return 'asset-status-pill asset-status-pill--danger';
}

export function RepairsPage() {
  const [activeTab, setActiveTab] = useState<'need-repair' | 'in-repair'>('need-repair');
  const [search, setSearch] = useState('');
  const [statusFilter, setStatusFilter] = useState<'all' | RepairStatus>('all');
  const [rows, setRows] = useState<RepairRow[]>([]);
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
  const [repairAsset, setRepairAsset] = useState<AssetResponse | null>(null);
  const [repairStartLoading, setRepairStartLoading] = useState(false);
  const [repairStartSubmitting, setRepairStartSubmitting] = useState(false);

  const [repairCompleteOpen, setRepairCompleteOpen] = useState(false);
  const [repairCompleteRow, setRepairCompleteRow] = useState<RepairRow | null>(null);
  const [repairCompleteTaskId, setRepairCompleteTaskId] = useState<number | null>(null);
  const [repairCompleteAsset, setRepairCompleteAsset] = useState<AssetResponse | null>(null);
  const [repairCompleteLoading, setRepairCompleteLoading] = useState(false);
  const [repairCompleteSubmitting, setRepairCompleteSubmitting] = useState(false);
  const [repairCompleteReportNumber, setRepairCompleteReportNumber] = useState('');

  useEffect(() => {
    let cancelled = false;

    async function loadRepairList() {
      setLoading(true);
      try {
        const res = await damageReportService.list({
          requestTypeId: 4,
          page: 1,
          pageSize: 200,
        });

        const mapped: RepairRow[] = (res.items ?? []).map((it) => {
          const assetCode = it.assetCode ?? '';
          const assetName = it.assetName ?? '';

          return {
            id: String(it.id),
            assetRequestId: it.id,
            assetId: it.assetId,
            assetCode: assetCode || String(it.assetId ?? ''),
            assetName:
              assetName ||
              (it.title
                ? it.title.replace(/^Báo hỏng tài sản\s*/i, '').trim() || it.title
                : '(Không có tên)'),
            condition: it.description ?? '',
            brokenDate: formatDate(it.createDate),
            quantity: it.assetQuantity && it.assetQuantity > 0 ? it.assetQuantity : 1,
            location: it.currentDepartmentName ?? '',
            department: it.currentDepartmentName ?? '',
            status: mapStatus(it.status),
            rawStatus: it.status,
          };
        });

        if (!cancelled) setRows(mapped);
      } catch (e: any) {
        const msg = e?.response?.data?.title ?? e?.response?.data ?? e?.message;
        message.error(typeof msg === 'string' ? msg : 'Không tải được danh sách sửa chữa.');
        if (!cancelled) setRows([]);
      } finally {
        if (!cancelled) setLoading(false);
      }
    }

    if (activeTab === 'need-repair' || activeTab === 'in-repair') {
      loadRepairList();
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
      const res = await damageReportService.list({ requestTypeId: 4, page: 1, pageSize: 200 });
      const mapped: RepairRow[] = (res.items ?? []).map((it) => ({
        id: String(it.id),
        assetRequestId: it.id,
        assetId: it.assetId,
        assetCode: (it.assetCode ?? '') || String(it.assetId ?? ''),
        assetName:
          (it.assetName ?? '') ||
          (it.title
            ? it.title.replace(/^Báo hỏng tài sản\s*/i, '').trim() || it.title
            : '(Không có tên)'),
        condition: it.description ?? '',
        brokenDate: formatDate(it.createDate),
        quantity: it.assetQuantity && it.assetQuantity > 0 ? it.assetQuantity : 1,
        location: it.currentDepartmentName ?? '',
        department: it.currentDepartmentName ?? '',
        status: mapStatus(it.status),
        rawStatus: it.status,
      }));
      setRows(mapped);
    } catch {
      // ignore
    }
  };

  const openRepairStart = async (row: RepairRow) => {
    setRepairStartRow(row);
    setRepairStartLoading(true);
    setRepairStartOpen(true);
    try {
      const det = await assetRequestService.getById(row.assetRequestId);
      const aid = det.asset?.assetId ?? row.assetId;
      if (aid) {
        const asset = await assetService.getById(aid);
        setRepairAsset(asset);
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
    if (!repairStartRow || !profile?.id) return;
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
        estimatedCost: values.estimatedCost ?? undefined,
        repairProgressStatus: values.repairProgressStatus.trim(),
        comment: null,
      };

      await repairRequestService.start(repairStartRow.assetRequestId, payload);
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
    if (row.rawStatus !== 4) {
      message.warning('Chỉ có thể hoàn thành khi đơn đang trong trạng thái đang sửa chữa.');
      return;
    }
    setRepairCompleteRow(row);
    setRepairCompleteLoading(true);
    setRepairCompleteOpen(true);
    try {
      const det = await assetRequestService.getById(row.assetRequestId);
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
        setRepairCompleteAsset(asset);
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
        attachmentUrls: values.attachmentUrls.length > 0 ? values.attachmentUrls : null,
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
    if (!selected || !profile?.id) return;
    setSubmitting(true);
    try {
      const payload = { approvedBy: profile.id, comment: comment.trim() || null };
      if (decision === 'approved') {
        await directorRequestService.approve(selected.assetRequestId, payload);
        message.success('Đã phê duyệt yêu cầu sửa chữa.');
      } else {
        await directorRequestService.reject(selected.assetRequestId, payload);
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

  const handleDeleteDamageReport = (row: RepairRow) => {
    const idNum = Number(row.id);
    if (!Number.isFinite(idNum) || idNum <= 0) {
      message.error('Không xác định được mã đơn báo hỏng.');
      return;
    }

    Modal.confirm({
      title: 'Xóa đơn báo hỏng?',
      content: `Bạn có chắc muốn xóa đơn báo hỏng của tài sản ${row.assetCode || ''}?`,
      okText: 'Xóa',
      okButtonProps: { danger: true },
      cancelText: 'Hủy',
      onOk: async () => {
        try {
          await damageReportService.delete(idNum);
          message.success('Đã xóa đơn báo hỏng.');
          setRows((prev) => prev.filter((x) => x.id !== row.id));
        } catch (e: any) {
          const data = e?.response?.data;
          const msg = data?.title ?? data ?? e?.message ?? 'Xóa đơn báo hỏng thất bại.';
          message.error(typeof msg === 'string' ? msg : 'Xóa đơn báo hỏng thất bại.');
        }
      },
    });
  };

  const filteredData: RepairRow[] = useMemo(() => {
    const keyword = search.trim().toLowerCase();
    const byTab = activeTab === 'in-repair' ? rows.filter((r) => r.rawStatus === 4) : rows;
    return byTab.filter((row) => {
      const matchStatus = statusFilter === 'all' || row.status === statusFilter;
      const matchKeyword =
        !keyword ||
        row.assetCode.toLowerCase().includes(keyword) ||
        row.assetName.toLowerCase().includes(keyword) ||
        row.condition.toLowerCase().includes(keyword);
      return matchStatus && matchKeyword;
    });
  }, [rows, search, statusFilter, activeTab]);

  return (
    <div className="repairs-page">
      <h1 className="repairs-page__title">Sửa chữa</h1>

      <div className="repairs-card">
        <Tabs
          activeKey={activeTab}
          onChange={(key) => setActiveTab(key as 'need-repair' | 'in-repair')}
          className="repairs-tabs"
          items={[
            { key: 'need-repair', label: 'Tài sản cần sửa chữa' },
            { key: 'in-repair', label: 'Đang sửa chữa' },
          ]}
        />

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
                <th className="asset-table__cell asset-table__cell--checkbox">
                  <input type="checkbox" />
                </th>
                <th>MÃ TÀI SẢN</th>
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
                  <td colSpan={10} className="repairs-table-empty">
                    Đang tải dữ liệu...
                  </td>
                </tr>
              ) : filteredData.length === 0 ? (
                <tr>
                  <td colSpan={10} className="repairs-table-empty">
                    Không có dữ liệu.
                  </td>
                </tr>
              ) : (
                filteredData.map((row) => (
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
                      {activeTab === 'need-repair' && row.status === 'approved' ? (
                        <Button type="link" size="small" onClick={() => openRepairStart(row)}>
                          Bắt đầu SC
                        </Button>
                      ) : null}
                      {activeTab === 'in-repair' && row.status === 'inProgress' ? (
                        <Button type="link" size="small" onClick={() => openRepairComplete(row)}>
                          Hoàn thành SC
                        </Button>
                      ) : null}
                      {activeTab === 'need-repair' ? (
                        <Button danger type="text" onClick={() => handleDeleteDamageReport(row)}>
                          Xóa
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

      <Modal
        open={detailOpen}
        title={selected ? `Chi tiết yêu cầu SC - YC-${selected.assetRequestId}` : 'Chi tiết yêu cầu sửa chữa'}
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
              <b>Tình trạng:</b> {selected.condition || '—'}
            </div>
            <div>
              <b>Ngày hỏng:</b> {selected.brokenDate || '—'}
            </div>
            <div>
              <b>Phòng ban:</b> {selected.department || '—'}
            </div>
            <div>
              <b>Trạng thái:</b> {getStatusLabel(selected.status)}
            </div>
          </div>
        )}
      </Modal>

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


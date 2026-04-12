import { useState, useEffect, useCallback, useMemo } from 'react';
import { useNavigate } from 'react-router-dom';
import { Button, Input, Select, Tag, Spin, Space, message, Modal, Form, DatePicker, InputNumber } from 'antd';
import { SearchOutlined, PlayCircleOutlined, EditOutlined, DeleteOutlined } from '@ant-design/icons';
import type { Dayjs } from 'dayjs';
import dayjs from 'dayjs';
import { SchedulePeriodicModal } from '../components/SchedulePeriodicModal';
import { ScheduleIndividualModal } from '../components/ScheduleIndividualModal';
import {
  inventoryService,
  inventorySessionDateToUtcIso,
  inventorySessionEndOfDayUtcIso,
  SESSION_STATUS,
  type DropdownItem,
  type InventorySessionListItem,
} from '../services/inventoryService';
import '../../maintenance/pages/MaintenancePage.css';
import './InventoryPage.css';

const { Option } = Select;
const { TextArea } = Input;

const STATUS_COLOR: Record<number, string> = {
  0: 'blue',        // Đã lên lịch
  1: 'processing',  // Đang thực hiện
  2: 'warning',     // Chờ xử lý (legacy DB Completed)
  3: 'error',       // Đã hủy
  4: 'success',     // Đã xử lý
  5: 'orange',      // Đến lịch
  6: 'purple',      // Chờ xử lý
};

/** Progress bar fill: red (low) → yellow → green (100%). */
function inventoryProgressFillColor(percent: number | null | undefined): string {
  const p = Math.max(0, Math.min(100, Number(percent) || 0));
  if (p === 0) return '#bfbfbf';
  const hue = (p / 100) * 120;
  return `hsl(${hue}, 72%, 44%)`;
}

function formatDate(dateStr: string | null | undefined): string {
  if (!dateStr) return '-';
  const d = new Date(dateStr);
  if (Number.isNaN(d.getTime())) return '-';
  return `${String(d.getDate()).padStart(2, '0')}/${String(d.getMonth() + 1).padStart(2, '0')}/${d.getFullYear()}`;
}

function execDurationLabel(startStr: string | null | undefined, endStr: string | null | undefined): string {
  if (!startStr || !endStr) return '-';
  const start = new Date(startStr);
  const end = new Date(endStr);
  if (Number.isNaN(start.getTime()) || Number.isNaN(end.getTime())) return '-';
  const su = Date.UTC(start.getUTCFullYear(), start.getUTCMonth(), start.getUTCDate());
  const eu = Date.UTC(end.getUTCFullYear(), end.getUTCMonth(), end.getUTCDate());
  const days = Math.max(1, Math.round((eu - su) / 86_400_000));
  return `${days} ngày`;
}

interface EditFormValues {
  purpose: string;
  startDate: Dayjs;
  executionDays: number;
  periodDays?: number;
}

export function InventoryPage() {
  const navigate = useNavigate();
  const [isPeriodicModalOpen, setIsPeriodicModalOpen] = useState(false);
  const [isIndividualModalOpen, setIsIndividualModalOpen] = useState(false);

  const [sessions, setSessions] = useState<InventorySessionListItem[]>([]);
  const [loading, setLoading] = useState(false);

  const [searchText, setSearchText] = useState('');
  const [departmentFilter, setDepartmentFilter] = useState<number | undefined>(undefined);
  const [departmentOptions, setDepartmentOptions] = useState<DropdownItem[]>([]);
  const [statusFilter, setStatusFilter] = useState<number | undefined>(undefined);

  // Execute confirmation
  const [executeTarget, setExecuteTarget] = useState<InventorySessionListItem | null>(null);
  const [executing, setExecuting] = useState(false);

  // Edit modal
  const [editTarget, setEditTarget] = useState<InventorySessionListItem | null>(null);
  const [editSubmitting, setEditSubmitting] = useState(false);
  const [editForm] = Form.useForm<EditFormValues>();

  // Cancel (delete) confirmation
  const [cancelTarget, setCancelTarget] = useState<InventorySessionListItem | null>(null);
  const [cancelNote, setCancelNote] = useState('');
  const [cancelling, setCancelling] = useState(false);

  const [pageSize, setPageSize] = useState(25);
  const [currentPage, setCurrentPage] = useState(1);

  const fetchSessions = useCallback(async () => {
    setLoading(true);
    try {
      const data = await inventoryService.getSessions({
        keyword: searchText || undefined,
        status: statusFilter,
        departmentId: departmentFilter,
      });
      setSessions(data);
    } catch {
      message.error('Không thể tải danh sách kiểm kê.');
    } finally {
      setLoading(false);
    }
  }, [searchText, statusFilter, departmentFilter]);

  useEffect(() => {
    void inventoryService
      .getDepartments()
      .then(setDepartmentOptions)
      .catch(() => {
        setDepartmentOptions([]);
      });
  }, []);

  useEffect(() => {
    fetchSessions();
  }, [fetchSessions]);

  useEffect(() => {
    setCurrentPage(1);
  }, [searchText, statusFilter, departmentFilter]);

  const totalFiltered = sessions.length;
  const totalPages = Math.max(1, Math.ceil(totalFiltered / pageSize));

  useEffect(() => {
    setCurrentPage((p) => Math.min(p, totalPages));
  }, [totalPages]);

  const safePage = Math.min(currentPage, totalPages);
  const rangeStart = totalFiltered === 0 ? 0 : (safePage - 1) * pageSize + 1;
  const rangeEnd = Math.min(safePage * pageSize, totalFiltered);
  const paginatedSessions = useMemo(
    () => sessions.slice((safePage - 1) * pageSize, safePage * pageSize),
    [sessions, safePage, pageSize],
  );

  const handleSubmitPeriodic = () => {
    fetchSessions();
  };

  const handleSubmitIndividual = () => {
    fetchSessions();
  };

  // --- Execute (Thực hiện kiểm kê) ---
  const handleExecuteConfirm = async () => {
    if (!executeTarget) return;
    setExecuting(true);
    try {
      await inventoryService.activateSession(executeTarget.sessionId);
      message.success('Phiên kiểm kê đã được bắt đầu.');
      setExecuteTarget(null);
      fetchSessions();
    } catch (err: unknown) {
      const axiosErr = err as { response?: { data?: { message?: string } } };
      message.error(axiosErr?.response?.data?.message ?? 'Thao tác thất bại. Vui lòng thử lại.');
    } finally {
      setExecuting(false);
    }
  };

  // --- Edit ---
  const openEditModal = (row: InventorySessionListItem) => {
    setEditTarget(row);
    const start = dayjs(row.startDate);
    const end = dayjs(row.endDate);
    const execDays = Math.max(1, end.diff(start, 'day'));
    editForm.setFieldsValue({
      purpose: row.purpose,
      startDate: start,
      executionDays: execDays,
      periodDays: row.periodDays ?? undefined,
    });
  };

  const handleEditSubmit = async () => {
    if (!editTarget) return;
    const values = await editForm.validateFields();
    setEditSubmitting(true);
    try {
      const endDate = values.startDate.add(values.executionDays, 'day');
      await inventoryService.updateSession(editTarget.sessionId, {
        purpose: values.purpose,
        startDate: inventorySessionDateToUtcIso(values.startDate),
        endDate: inventorySessionEndOfDayUtcIso(endDate),
        periodDays: editTarget.isPeriodic ? values.periodDays : undefined,
      });
      message.success('Đã cập nhật thông tin phiên kiểm kê.');
      setEditTarget(null);
      fetchSessions();
    } catch (err: unknown) {
      const axiosErr = err as { response?: { data?: { message?: string } } };
      message.error(axiosErr?.response?.data?.message ?? 'Cập nhật thất bại. Vui lòng thử lại.');
    } finally {
      setEditSubmitting(false);
    }
  };

  // --- Cancel (Delete → Đã hủy) ---
  const handleCancelConfirm = async () => {
    if (!cancelTarget) return;
    setCancelling(true);
    try {
      await inventoryService.cancelSession(cancelTarget.sessionId, {
        reviewedBy: 0,
        reviewerRoleId: 2,
        reviewNotes: cancelNote || undefined,
      });
      message.success('Lịch kiểm kê đã được hủy.');
      setCancelTarget(null);
      setCancelNote('');
      fetchSessions();
    } catch (err: unknown) {
      const axiosErr = err as { response?: { data?: { message?: string } } };
      message.error(axiosErr?.response?.data?.message ?? 'Hủy thất bại. Vui lòng thử lại.');
    } finally {
      setCancelling(false);
    }
  };

  return (
    <div className="maintenance-page">
      <div className="maintenance-header">
        <h1 className="maintenance-page__title">Kiểm kê</h1>
        <div className="inventory-maintenance-header-actions">
          <Button
            type="primary"
            className="maintenance-btn-add"
            onClick={() => setIsIndividualModalOpen(true)}
          >
            Hẹn lịch kiểm kê
          </Button>
          <Button
            type="primary"
            className="maintenance-btn-add"
            onClick={() => setIsPeriodicModalOpen(true)}
          >
            Lập lịch kiểm kê
          </Button>
        </div>
      </div>

      <div className="maintenance-card">
        <div className="maintenance-filters">
          <Input
            placeholder="Tìm kiếm nội dung kiểm kê"
            prefix={<SearchOutlined />}
            className="maintenance-search"
            value={searchText}
            onChange={(e) => setSearchText(e.target.value)}
            allowClear
          />
          <Select
            placeholder="Phòng ban"
            className="maintenance-select"
            value={departmentFilter}
            onChange={(v) => setDepartmentFilter(v)}
            allowClear
            showSearch
            optionFilterProp="children"
          >
            {departmentOptions.map((d) => (
              <Option key={d.id} value={d.id}>{d.name}</Option>
            ))}
          </Select>
          <Select
            placeholder="Trạng thái"
            className="maintenance-select"
            value={statusFilter}
            onChange={(v) => setStatusFilter(v)}
            allowClear
          >
            <Option value={0}>Đã lên lịch</Option>
            <Option value={5}>Đến lịch</Option>
            <Option value={1}>Đang thực hiện</Option>
            <Option value={3}>Đã hủy</Option>
            <Option value={4}>Đã xử lý</Option>
            <Option value={6}>Chờ xử lý</Option>
          </Select>
        </div>

        <div className="asset-table-wrapper maintenance-table-wrapper">
          <table className="asset-table maintenance-table">
            <thead>
              <tr>
                <th>NGÀY BẮT ĐẦU</th>
                <th>MỤC ĐÍCH</th>
                <th>THỜI GIAN THỰC HIỆN</th>
                <th>PHÒNG BAN</th>
                <th>TRẠNG THÁI</th>
                <th>TIẾN ĐỘ</th>
                <th className="asset-table__cell asset-table__cell--actions" />
              </tr>
            </thead>
            <tbody>
              {loading ? (
                <tr>
                  <td colSpan={7} className="maintenance-table-empty">
                    <Spin size="large" />
                  </td>
                </tr>
              ) : paginatedSessions.length === 0 ? (
                <tr>
                  <td colSpan={7} className="maintenance-table-empty">
                    Không có dữ liệu.
                  </td>
                </tr>
              ) : (
                paginatedSessions.map((row) => {
                  const isScheduled = row.status === SESSION_STATUS.Scheduled || row.status === SESSION_STATUS.Due;
                  const canStartInWindow = row.status === SESSION_STATUS.Due;
                  const openSession = () => {
                    if (isScheduled) return;
                    if (row.status === SESSION_STATUS.InProgress) {
                      navigate(`/inventory/${row.sessionId}`);
                      return;
                    }
                    navigate(`/inventory-review/${row.sessionId}`);
                  };
                  return (
                    <tr
                      key={row.sessionId}
                      className={`asset-row${isScheduled ? '' : ' inventory-table__row--clickable'}`}
                      onClick={isScheduled ? undefined : openSession}
                    >
                      <td>{formatDate(row.startDate)}</td>
                      <td>{row.purpose}</td>
                      <td>{execDurationLabel(row.startDate, row.endDate)}</td>
                      <td>{row.departmentName}</td>
                      <td>
                        <Tag
                          color={STATUS_COLOR[row.status] ?? 'default'}
                          className="inventory-status-tag"
                        >
                          {row.statusName}
                        </Tag>
                      </td>
                      <td className="inventory-table__progress-cell">
                        <div className="inventory-progress">
                          <div className="inventory-progress__bar">
                            <div
                              className="inventory-progress__fill"
                              style={{
                                width: `${row.progressPercent ?? 0}%`,
                                backgroundColor: inventoryProgressFillColor(row.progressPercent),
                              }}
                            />
                          </div>
                          <span className="inventory-progress__label">
                            {row.progressPercent ?? 0}%
                          </span>
                        </div>
                      </td>
                      <td className="asset-table__cell asset-table__cell--actions">
                        {isScheduled ? (
                          <Space size="small" wrap>
                            {canStartInWindow && (
                              <Button
                                type="text"
                                icon={<PlayCircleOutlined />}
                                title="Thực hiện kiểm kê (Đến lịch)"
                                onClick={(e) => {
                                  e.stopPropagation();
                                  setExecuteTarget(row);
                                }}
                              />
                            )}
                            <Button
                              type="text"
                              icon={<EditOutlined />}
                              title="Chỉnh sửa"
                              onClick={(e) => {
                                e.stopPropagation();
                                openEditModal(row);
                              }}
                            />
                            <Button
                              type="text"
                              danger
                              icon={<DeleteOutlined />}
                              title="Hủy lịch"
                              onClick={(e) => {
                                e.stopPropagation();
                                setCancelTarget(row);
                                setCancelNote('');
                              }}
                            />
                          </Space>
                        ) : null}
                      </td>
                    </tr>
                  );
                })
              )}
            </tbody>
          </table>
        </div>

        <div className="maintenance-card__footer">
          <div className="maintenance-footer__left">
            Số lượng trên trang:
            <select
              className="maintenance-footer__select"
              value={pageSize}
              onChange={(e) => {
                setPageSize(Number(e.target.value));
                setCurrentPage(1);
              }}
            >
              <option value={10}>10</option>
              <option value={25}>25</option>
              <option value={50}>50</option>
              <option value={100}>100</option>
            </select>
          </div>
          <div className="maintenance-footer__center">
            {totalFiltered === 0 ? '0 trên 0' : `${rangeStart}-${rangeEnd} trên ${totalFiltered}`}
          </div>
          <div className="maintenance-footer__right">
            <button
              type="button"
              className="maintenance-footer__pager"
              disabled={safePage <= 1}
              onClick={() => setCurrentPage((p) => Math.max(1, p - 1))}
              aria-label="Trang trước"
            >
              ⟨
            </button>
            <button type="button" className="maintenance-footer__pager maintenance-footer__pager--active" tabIndex={-1} aria-current="page">
              {safePage}
            </button>
            <button
              type="button"
              className="maintenance-footer__pager"
              disabled={safePage >= totalPages}
              onClick={() => setCurrentPage((p) => Math.min(totalPages, p + 1))}
              aria-label="Trang sau"
            >
              ⟩
            </button>
          </div>
        </div>
      </div>

      {/* Execute confirmation modal */}
      <Modal
        open={!!executeTarget}
        title="Thực hiện kiểm kê"
        okText="Xác nhận"
        cancelText="Hủy bỏ"
        okButtonProps={{ loading: executing }}
        onOk={handleExecuteConfirm}
        onCancel={() => setExecuteTarget(null)}
        centered
        width={420}
      >
        {executeTarget && (
          <p>
            Bắt đầu thực hiện kiểm kê cho phiên <strong>{executeTarget.purpose}</strong>?
          </p>
        )}
      </Modal>

      {/* Edit modal */}
      <Modal
        open={!!editTarget}
        title="Chỉnh sửa lịch kiểm kê"
        okText="Lưu thay đổi"
        cancelText="Hủy bỏ"
        okButtonProps={{ loading: editSubmitting }}
        onOk={handleEditSubmit}
        onCancel={() => setEditTarget(null)}
        centered
        width={480}
      >
        <Form form={editForm} layout="vertical" style={{ marginTop: 16 }}>
          <Form.Item
            label="Mục đích"
            name="purpose"
            rules={[{ required: true, message: 'Vui lòng nhập mục đích kiểm kê' }]}
          >
            <TextArea rows={3} placeholder="Nhập mục đích kiểm kê" />
          </Form.Item>

          <div style={{ display: 'flex', gap: 12 }}>
            <Form.Item
              label="Ngày bắt đầu"
              name="startDate"
              style={{ flex: 1 }}
              rules={[{ required: true, message: 'Vui lòng chọn ngày bắt đầu' }]}
            >
              <DatePicker format="DD/MM/YYYY" placeholder="Chọn ngày" style={{ width: '100%' }} />
            </Form.Item>
            <Form.Item
              label="Thời gian thực hiện (ngày)"
              name="executionDays"
              style={{ flex: 1 }}
              rules={[
                { required: true, message: 'Vui lòng nhập số ngày' },
                { type: 'number', min: 1, message: 'Phải ít nhất 1 ngày' },
              ]}
            >
              <InputNumber min={1} max={365} style={{ width: '100%' }} placeholder="Ví dụ: 7" />
            </Form.Item>
          </div>

          {/* Live end-date preview */}
          <Form.Item shouldUpdate noStyle>
            {() => {
              const startDate = editForm.getFieldValue('startDate') as Dayjs | undefined;
              const execDays = editForm.getFieldValue('executionDays') as number | undefined;
              if (!startDate || !execDays || execDays <= 0) return null;
              return (
                <div style={{ marginBottom: 16, color: '#595959', fontSize: 13 }}>
                  Hạn hoàn thành:{' '}
                  <strong>{startDate.add(execDays, 'day').format('DD/MM/YYYY')}</strong>
                </div>
              );
            }}
          </Form.Item>

          {editTarget?.isPeriodic && (
            <Form.Item
              label="Chu kỳ kiểm kê (ngày)"
              name="periodDays"
              rules={[
                { required: true, message: 'Vui lòng nhập chu kỳ' },
                { type: 'number', min: 1, message: 'Chu kỳ phải ít nhất 1 ngày' },
              ]}
            >
              <InputNumber min={1} max={3650} style={{ width: '100%' }} placeholder="Ví dụ: 90" />
            </Form.Item>
          )}
        </Form>
      </Modal>

      {/* Cancel (delete) confirmation modal */}
      <Modal
        open={!!cancelTarget}
        title="Hủy lịch kiểm kê"
        okText="Hủy lịch"
        cancelText="Đóng"
        okButtonProps={{ danger: true, loading: cancelling }}
        onOk={handleCancelConfirm}
        onCancel={() => { setCancelTarget(null); setCancelNote(''); }}
        centered
        width={420}
      >
        {cancelTarget && (
          <div>
            <p style={{ marginBottom: 8 }}>
              Bạn có chắc muốn hủy lịch kiểm kê <strong>{cancelTarget.purpose}</strong>?
              Trạng thái sẽ chuyển sang <strong>Đã hủy</strong>.
            </p>
            {cancelTarget.isPeriodic && (
              <p style={{ marginBottom: 12, color: '#cf1322', fontSize: 13 }}>
                Đây là lịch kiểm kê định kỳ. Tất cả các phiên định kỳ tiếp theo chưa bắt đầu
                của phòng ban này cũng sẽ bị hủy.
              </p>
            )}
            <TextArea
              rows={3}
              value={cancelNote}
              onChange={(e) => setCancelNote(e.target.value)}
              placeholder="Lý do hủy (không bắt buộc)"
            />
          </div>
        )}
      </Modal>

      <SchedulePeriodicModal
        open={isPeriodicModalOpen}
        onClose={() => setIsPeriodicModalOpen(false)}
        onSubmit={handleSubmitPeriodic}
      />

      <ScheduleIndividualModal
        open={isIndividualModalOpen}
        onClose={() => setIsIndividualModalOpen(false)}
        onSubmit={handleSubmitIndividual}
      />
    </div>
  );
}

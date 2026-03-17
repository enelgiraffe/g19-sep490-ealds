import { useState, useEffect, useCallback } from 'react';
import { useNavigate } from 'react-router-dom';
import { Button, Input, Select, Tag, Spin, message, Modal, Form, DatePicker } from 'antd';
import { SearchOutlined, PlayCircleOutlined, EditOutlined, DeleteOutlined } from '@ant-design/icons';
import type { Dayjs } from 'dayjs';
import dayjs from 'dayjs';
import { SchedulePeriodicModal } from '../components/SchedulePeriodicModal';
import { ScheduleIndividualModal } from '../components/ScheduleIndividualModal';
import {
  inventoryService,
  SESSION_STATUS,
  type InventorySessionListItem,
} from '../services/inventoryService';
import './InventoryPage.css';

const { Option } = Select;
const { TextArea } = Input;

const STATUS_COLOR: Record<number, string> = {
  0: 'blue',        // Đã lên lịch
  1: 'processing',  // Đang thực hiện
  2: 'warning',     // Chờ xác nhận
  3: 'error',       // Đã hủy
  4: 'success',     // Đã xác nhận
};

function formatDate(dateStr: string | null | undefined): string {
  if (!dateStr) return '-';
  const d = new Date(dateStr);
  if (Number.isNaN(d.getTime())) return '-';
  return `${String(d.getDate()).padStart(2, '0')}/${String(d.getMonth() + 1).padStart(2, '0')}/${d.getFullYear()}`;
}

interface EditFormValues {
  purpose: string;
  startDate: Dayjs;
  endDate: Dayjs;
}

export function InventoryPage() {
  const navigate = useNavigate();
  const [isPeriodicModalOpen, setIsPeriodicModalOpen] = useState(false);
  const [isIndividualModalOpen, setIsIndividualModalOpen] = useState(false);

  const [sessions, setSessions] = useState<InventorySessionListItem[]>([]);
  const [loading, setLoading] = useState(false);

  const [searchText, setSearchText] = useState('');
  const [departmentFilter, setDepartmentFilter] = useState<string | undefined>(undefined);
  const [groupFilter, setGroupFilter] = useState<string | undefined>(undefined);
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

  const fetchSessions = useCallback(async () => {
    setLoading(true);
    try {
      const data = await inventoryService.getSessions({
        keyword: searchText || undefined,
        status: statusFilter,
      });
      setSessions(data);
    } catch {
      message.error('Không thể tải danh sách kiểm kê.');
    } finally {
      setLoading(false);
    }
  }, [searchText, statusFilter]);

  useEffect(() => {
    fetchSessions();
  }, [fetchSessions]);

  const uniqueDepartments = Array.from(new Set(sessions.map((s) => s.departmentName)));
  const uniqueGroups = Array.from(
    new Set(sessions.map((s) => `${s.assetCategoryName} - ${s.assetTypeName}`))
  );

  const filteredSessions = sessions.filter((s) => {
    const matchDept = !departmentFilter || s.departmentName === departmentFilter;
    const matchGroup =
      !groupFilter || `${s.assetCategoryName} - ${s.assetTypeName}` === groupFilter;
    return matchDept && matchGroup;
  });

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
    editForm.setFieldsValue({
      purpose: row.purpose,
      startDate: dayjs(row.startDate),
      endDate: dayjs(row.endDate),
    });
  };

  const handleEditSubmit = async () => {
    if (!editTarget) return;
    const values = await editForm.validateFields();
    setEditSubmitting(true);
    try {
      await inventoryService.updateSession(editTarget.sessionId, {
        purpose: values.purpose,
        startDate: values.startDate.toISOString(),
        endDate: values.endDate.toISOString(),
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
    <div className="inventory-page">
      <div className="inventory-page__header">
        <h1 className="inventory-page__title">Kiểm kê</h1>
        <div className="inventory-page__actions">
          <Button
            className="inventory-btn inventory-btn--outline"
            onClick={() => setIsIndividualModalOpen(true)}
          >
            Hẹn lịch kiểm kê
          </Button>
          <Button
            type="primary"
            className="inventory-btn inventory-btn--primary"
            onClick={() => setIsPeriodicModalOpen(true)}
          >
            Lập lịch kiểm kê
          </Button>
        </div>
      </div>

      <div className="inventory-card">
        <div className="inventory-card__filters">
          <Input
            placeholder="Tìm kiếm nội dung kiểm kê"
            prefix={<SearchOutlined className="inventory-search-icon" />}
            className="inventory-search"
            value={searchText}
            onChange={(e) => setSearchText(e.target.value)}
            allowClear
          />
          <Select
            placeholder="Phòng ban"
            className="inventory-filter-select"
            value={departmentFilter}
            onChange={(v) => setDepartmentFilter(v)}
            allowClear
          >
            {uniqueDepartments.map((d) => (
              <Option key={d} value={d}>{d}</Option>
            ))}
          </Select>
          <Select
            placeholder="Nhóm tài sản"
            className="inventory-filter-select"
            value={groupFilter}
            onChange={(v) => setGroupFilter(v)}
            allowClear
          >
            {uniqueGroups.map((g) => (
              <Option key={g} value={g}>{g}</Option>
            ))}
          </Select>
          <Select
            placeholder="Trạng thái"
            className="inventory-filter-select"
            value={statusFilter}
            onChange={(v) => setStatusFilter(v)}
            allowClear
          >
            <Option value={0}>Đã lên lịch</Option>
            <Option value={1}>Đang thực hiện</Option>
            <Option value={2}>Chờ xác nhận</Option>
            <Option value={3}>Đã hủy</Option>
            <Option value={4}>Đã xác nhận</Option>
          </Select>
        </div>

        <div className="inventory-table-wrapper">
          {loading ? (
            <div className="inventory-table__loading">
              <Spin size="large" />
            </div>
          ) : (
            <table className="inventory-table">
              <thead>
                <tr>
                  <th>NGÀY TẠO LỊCH</th>
                  <th>MỤC ĐÍCH</th>
                  <th>ĐẾN NGÀY</th>
                  <th>PHÒNG BAN</th>
                  <th>NHÓM TÀI SẢN</th>
                  <th>TRẠNG THÁI</th>
                  <th>TIẾN ĐỘ</th>
                  <th />
                </tr>
              </thead>
              <tbody>
                {filteredSessions.length === 0 ? (
                  <tr>
                    <td colSpan={8} className="inventory-table__empty">
                      Không có dữ liệu
                    </td>
                  </tr>
                ) : (
                  filteredSessions.map((row) => {
                    const isScheduled = row.status === SESSION_STATUS.Scheduled;
                    return (
                      <tr
                        key={row.sessionId}
                        className={`inventory-table__row${isScheduled ? '' : ' inventory-table__row--clickable'}`}
                        onClick={isScheduled ? undefined : () => navigate(`/inventory/${row.sessionId}`)}
                      >
                        <td>{formatDate(row.createDate)}</td>
                        <td>{row.purpose}</td>
                        <td>{formatDate(row.endDate)}</td>
                        <td>{row.departmentName}</td>
                        <td>{`${row.assetCategoryName} - ${row.assetTypeName}`}</td>
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
                                style={{ width: `${row.progressPercent ?? 0}%` }}
                              />
                            </div>
                            <span className="inventory-progress__label">
                              {row.progressPercent ?? 0}%
                            </span>
                          </div>
                        </td>
                        <td className="inventory-table__actions-cell">
                          <div className="inventory-row-actions">
                            {isScheduled && (
                              <>
                                <button
                                  type="button"
                                  className="inventory-action-btn inventory-action-btn--execute"
                                  title="Thực hiện kiểm kê"
                                  onClick={(e) => { e.stopPropagation(); setExecuteTarget(row); }}
                                >
                                  <PlayCircleOutlined />
                                </button>
                                <button
                                  type="button"
                                  className="inventory-action-btn"
                                  title="Chỉnh sửa"
                                  onClick={(e) => { e.stopPropagation(); openEditModal(row); }}
                                >
                                  <EditOutlined />
                                </button>
                                <button
                                  type="button"
                                  className="inventory-action-btn inventory-action-btn--danger"
                                  title="Hủy lịch"
                                  onClick={(e) => { e.stopPropagation(); setCancelTarget(row); setCancelNote(''); }}
                                >
                                  <DeleteOutlined />
                                </button>
                              </>
                            )}
                          </div>
                        </td>
                      </tr>
                    );
                  })
                )}
              </tbody>
            </table>
          )}
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
            Trạng thái sẽ chuyển sang <strong>Đang thực hiện</strong>.
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
              label="Ngày kết thúc"
              name="endDate"
              style={{ flex: 1 }}
              rules={[
                { required: true, message: 'Vui lòng chọn ngày kết thúc' },
                ({ getFieldValue }) => ({
                  validator(_, value) {
                    if (!value || !getFieldValue('startDate') || value.isAfter(getFieldValue('startDate'))) {
                      return Promise.resolve();
                    }
                    return Promise.reject(new Error('Ngày kết thúc phải sau ngày bắt đầu'));
                  },
                }),
              ]}
            >
              <DatePicker format="DD/MM/YYYY" placeholder="Chọn ngày" style={{ width: '100%' }} />
            </Form.Item>
          </div>
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
            <p style={{ marginBottom: 12 }}>
              Bạn có chắc muốn hủy lịch kiểm kê <strong>{cancelTarget.purpose}</strong>?
              Trạng thái sẽ chuyển sang <strong>Đã hủy</strong>.
            </p>
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

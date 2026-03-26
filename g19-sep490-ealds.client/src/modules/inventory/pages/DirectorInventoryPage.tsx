import { useState, useEffect, useCallback, type MouseEvent } from 'react';
import { useNavigate } from 'react-router-dom';
import { Button, Input, Select, Tag, Spin, message, Modal, Input as AntInput } from 'antd';
import {
  CheckOutlined,
  FileTextOutlined,
  ReloadOutlined,
  SearchOutlined,
} from '@ant-design/icons';
import {
  inventoryService,
  getCurrentUserId,
  SESSION_STATUS,
  SESSION_STATUS_LABEL,
  type InventorySessionListItem,
} from '../services/inventoryService';
import './DirectorInventoryPage.css';

const { Option } = Select;
const { TextArea } = AntInput;

/** Giám đốc chỉ xem: Chờ xác nhận (2), Đã xử lý (4) — không hiển thị Chờ xử lý (6). */
const DIRECTOR_STATUS_FILTER: { value: number; label: string }[] = [
  { value: SESSION_STATUS.Completed, label: SESSION_STATUS_LABEL[SESSION_STATUS.Completed] },
  { value: SESSION_STATUS.Confirmed, label: SESSION_STATUS_LABEL[SESSION_STATUS.Confirmed] },
];

const STATUS_COLOR: Record<number, string> = {
  0: 'blue',
  1: 'processing',
  2: 'warning',
  3: 'error',
  4: 'success',
  5: 'orange',
  6: 'purple',
};

type DirectorActionType = 'approve' | 'recheck';

interface DirectorActionMeta {
  type: DirectorActionType;
  session: InventorySessionListItem;
}

const DIRECTOR_ACTION_CONFIG: Record<
  DirectorActionType,
  { title: string; okText: string; okDanger?: boolean }
> = {
  approve: {
    title: 'Xác nhận kiểm kê',
    okText: 'Xác nhận',
  },
  recheck: {
    title: 'Yêu cầu kiểm kê lại',
    okText: 'Gửi yêu cầu',
    okDanger: true,
  },
};

function formatDate(dateStr: string | null | undefined): string {
  if (!dateStr) return '-';
  const d = new Date(dateStr);
  if (Number.isNaN(d.getTime())) return '-';
  return `${String(d.getDate()).padStart(2, '0')}/${String(d.getMonth() + 1).padStart(2, '0')}/${d.getFullYear()}`;
}

export function DirectorInventoryPage() {
  const navigate = useNavigate();
  const [sessions, setSessions] = useState<InventorySessionListItem[]>([]);
  const [loading, setLoading] = useState(false);
  const [submitting, setSubmitting] = useState(false);

  const [searchText, setSearchText] = useState('');
  const [departmentFilter, setDepartmentFilter] = useState<string | undefined>(undefined);
  const [statusFilter, setStatusFilter] = useState<number | undefined>(undefined);

  const [actionMeta, setActionMeta] = useState<DirectorActionMeta | null>(null);
  const [actionNotes, setActionNotes] = useState('');

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

  const filteredSessions = sessions
    .filter((s) => !departmentFilter || s.departmentName === departmentFilter)
    .filter(
      (s) =>
        s.status === SESSION_STATUS.Completed /* Chờ xác nhận */
        || s.status === SESSION_STATUS.Confirmed /* Đã xử lý */,
    );

  const openDirectorAction = (type: DirectorActionType, session: InventorySessionListItem) => {
    setActionMeta({ type, session });
    setActionNotes('');
  };

  const closeDirectorAction = () => {
    setActionMeta(null);
    setActionNotes('');
  };

  const handleDirectorActionOk = async () => {
    if (!actionMeta) return;
    setSubmitting(true);
    const directorId = getCurrentUserId();
    const payload = {
      reviewedBy: directorId,
      reviewerRoleId: 3,
      reviewNotes: actionNotes || undefined,
      applyCorrections: false,
    };
    try {
      if (actionMeta.type === 'approve') {
        const res = await inventoryService.directorApproveSession(
          actionMeta.session.sessionId,
          payload,
        );
        message.success(
          res.message
            ?? 'Đã xác nhận.',
        );
      } else {
        const res = await inventoryService.rejectSession(actionMeta.session.sessionId, payload);
        message.success(res.message ?? 'Đã gửi yêu cầu kiểm kê lại.');
      }
      closeDirectorAction();
      fetchSessions();
    } catch (err: unknown) {
      const axiosErr = err as { response?: { data?: { message?: string } } };
      message.error(
        axiosErr?.response?.data?.message ?? 'Thao tác thất bại. Vui lòng thử lại.',
      );
    } finally {
      setSubmitting(false);
    }
  };

  const goReport = (e: MouseEvent, sessionId: number) => {
    e.stopPropagation();
    navigate(`/inventory-review/${sessionId}`);
  };

  const renderActions = (row: InventorySessionListItem) => {
    if (row.status === SESSION_STATUS.Completed) {
      return (
        <div className="dir-inv-row-actions">
          <Button
            size="small"
            type="primary"
            icon={<FileTextOutlined />}
            onClick={(e) => goReport(e, row.sessionId)}
          >
            Xem báo cáo
          </Button>
          <Button
            size="small"
            type="primary"
            icon={<CheckOutlined />}
            onClick={(e) => {
              e.stopPropagation();
              openDirectorAction('approve', row);
            }}
          >
            Xác nhận
          </Button>
          <Button
            size="small"
            icon={<ReloadOutlined />}
            onClick={(e) => {
              e.stopPropagation();
              openDirectorAction('recheck', row);
            }}
          >
            Yêu cầu kiểm kê lại
          </Button>
        </div>
      );
    }

    if (row.status === SESSION_STATUS.Confirmed) {
      return (
        <div className="dir-inv-row-actions">
          <Button
            size="small"
            type="primary"
            icon={<FileTextOutlined />}
            onClick={(e) => goReport(e, row.sessionId)}
          >
            Xem báo cáo
          </Button>
        </div>
      );
    }

    return null;
  };

  const directorModalConfig = actionMeta ? DIRECTOR_ACTION_CONFIG[actionMeta.type] : null;

  return (
    <div className="dir-inv-page">
      <div className="dir-inv-page__header">
        <h1 className="dir-inv-page__title">Kiểm kê tài sản</h1>
      </div>

      <div className="dir-inv-card">
        <div className="dir-inv-card__filters">
          <Input
            placeholder="Tìm kiếm mã / mục đích kiểm kê"
            prefix={<SearchOutlined className="dir-inv-search-icon" />}
            className="dir-inv-search"
            value={searchText}
            onChange={(e) => setSearchText(e.target.value)}
            allowClear
          />
          <Select
            placeholder="Phòng ban"
            className="dir-inv-filter-select"
            value={departmentFilter}
            onChange={(v) => setDepartmentFilter(v)}
            allowClear
          >
            {uniqueDepartments.map((d) => (
              <Option key={d} value={d}>
                {d}
              </Option>
            ))}
          </Select>
          <Select
            placeholder="Trạng thái"
            className="dir-inv-filter-select"
            value={statusFilter}
            onChange={(v) => setStatusFilter(v)}
            allowClear
          >
            {DIRECTOR_STATUS_FILTER.map(({ value, label }) => (
              <Option key={value} value={value}>
                {label}
              </Option>
            ))}
          </Select>
        </div>

        <div className="dir-inv-table-wrapper">
          {loading ? (
            <div className="dir-inv-table__loading">
              <Spin size="large" />
            </div>
          ) : (
            <table className="dir-inv-table">
              <thead>
                <tr>
                  <th>NGÀY TẠO</th>
                  <th>MÃ PHIÊN</th>
                  <th>MỤC ĐÍCH</th>
                  <th>ĐẾN NGÀY</th>
                  <th>PHÒNG BAN</th>
                  <th>TRẠNG THÁI</th>
                  <th>THAO TÁC</th>
                </tr>
              </thead>
              <tbody>
                {filteredSessions.length === 0 ? (
                  <tr>
                    <td colSpan={7} className="dir-inv-table__empty">
                      Không có dữ liệu
                    </td>
                  </tr>
                ) : (
                  filteredSessions.map((row) => {
                    const isViewable =
                      row.status === SESSION_STATUS.Completed
                      || row.status === SESSION_STATUS.Confirmed;
                    return (
                      <tr
                        key={row.sessionId}
                        className={`dir-inv-table__row${isViewable ? ' dir-inv-table__row--clickable' : ''}`}
                        onClick={
                          isViewable
                            ? () => navigate(`/inventory-review/${row.sessionId}`)
                            : undefined
                        }
                      >
                        <td>{formatDate(row.createDate)}</td>
                        <td className="dir-inv-code">{row.code}</td>
                        <td>{row.purpose}</td>
                        <td>{formatDate(row.endDate)}</td>
                        <td>{row.departmentName}</td>
                        <td>
                          <Tag
                            color={STATUS_COLOR[row.status] ?? 'default'}
                            className="dir-inv-status-tag"
                          >
                            {row.statusName}
                          </Tag>
                        </td>
                        <td className="dir-inv-table__actions-cell">{renderActions(row)}</td>
                      </tr>
                    );
                  })
                )}
              </tbody>
            </table>
          )}
        </div>
      </div>

      <Modal
        open={!!actionMeta}
        title={directorModalConfig?.title}
        okText={directorModalConfig?.okText}
        cancelText="Hủy bỏ"
        okButtonProps={{
          danger: directorModalConfig?.okDanger,
          loading: submitting,
        }}
        onOk={handleDirectorActionOk}
        onCancel={closeDirectorAction}
        centered
        width={480}
      >
        {actionMeta && (
          <div className="dir-inv-modal-body">
            <p className="dir-inv-modal-session-info">
              Phiên kiểm kê: <strong>{actionMeta.session.code}</strong> —{' '}
              {actionMeta.session.departmentName}
            </p>
            <div className="dir-inv-modal-notes">
              <label htmlFor="dir-inv-action-notes" className="dir-inv-modal-label">
                {actionMeta.type === 'approve' ? 'Ghi chú (tuỳ chọn)' : 'Lý do yêu cầu kiểm kê lại'}
              </label>
              <TextArea
                id="dir-inv-action-notes"
                rows={3}
                value={actionNotes}
                onChange={(e) => setActionNotes(e.target.value)}
                placeholder={
                  actionMeta.type === 'approve'
                    ? 'Nhập ghi chú...'
                    : 'Nhập lý do yêu cầu kiểm kê lại...'
                }
              />
            </div>
          </div>
        )}
      </Modal>
    </div>
  );
}

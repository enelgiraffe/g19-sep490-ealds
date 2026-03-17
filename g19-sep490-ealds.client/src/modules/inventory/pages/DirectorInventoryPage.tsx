import { useState, useEffect, useCallback } from 'react';
import { useNavigate } from 'react-router-dom';
import { Button, Input, Select, Tag, Spin, message, Modal, Input as AntInput } from 'antd';
import {
  SearchOutlined,
  StopOutlined,
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

const STATUS_COLOR: Record<number, string> = {
  0: 'blue',        // Đã lên lịch
  1: 'processing',  // Đang thực hiện
  2: 'warning',     // Chờ xác nhận
  3: 'error',       // Đã hủy
  4: 'success',     // Đã xác nhận
};

type ActionType = 'cancel';

interface ActionMeta {
  type: ActionType;
  session: InventorySessionListItem;
}

const ACTION_CONFIG: Record<ActionType, { title: string; okText: string; okDanger?: boolean }> = {
  cancel: {
    title: 'Hủy lịch kiểm kê',
    okText: 'Hủy lịch',
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

  const [actionMeta, setActionMeta] = useState<ActionMeta | null>(null);
  const [notes, setNotes] = useState('');

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

  const filteredSessions = sessions.filter(
    (s) => !departmentFilter || s.departmentName === departmentFilter,
  );

  const openActionModal = (type: ActionType, session: InventorySessionListItem) => {
    setActionMeta({ type, session });
    setNotes('');
  };

  const closeActionModal = () => {
    setActionMeta(null);
    setNotes('');
  };

  const handleConfirmAction = async () => {
    if (!actionMeta) return;
    setSubmitting(true);

    const directorId = getCurrentUserId();
    const payload = {
      reviewedBy: directorId,
      reviewerRoleId: 3,
      reviewNotes: notes || undefined,
    };

    try {
      await inventoryService.cancelSession(actionMeta.session.sessionId, payload);
      message.success('Lịch kiểm kê đã được hủy.');
      closeActionModal();
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

  const renderActions = (row: InventorySessionListItem) => {
    if (row.status === SESSION_STATUS.Scheduled) {
      return (
        <div className="dir-inv-row-actions">
          <Button
            size="small"
            danger
            icon={<StopOutlined />}
            onClick={(e) => { e.stopPropagation(); openActionModal('cancel', row); }}
          >
            Hủy
          </Button>
        </div>
      );
    }

    return null;
  };

  const modalConfig = actionMeta ? ACTION_CONFIG[actionMeta.type] : null;

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
              <Option key={d} value={d}>{d}</Option>
            ))}
          </Select>
          <Select
            placeholder="Trạng thái"
            className="dir-inv-filter-select"
            value={statusFilter}
            onChange={(v) => setStatusFilter(v)}
            allowClear
          >
            {Object.entries(SESSION_STATUS_LABEL).map(([val, label]) => (
              <Option key={val} value={Number(val)}>{label}</Option>
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
                  <th>NHÓM TÀI SẢN</th>
                  <th>TRẠNG THÁI</th>
                  <th>TIẾN ĐỘ</th>
                  <th>THAO TÁC</th>
                </tr>
              </thead>
              <tbody>
                {filteredSessions.length === 0 ? (
                  <tr>
                    <td colSpan={9} className="dir-inv-table__empty">
                      Không có dữ liệu
                    </td>
                  </tr>
                ) : (
                  filteredSessions.map((row) => {
                    const isViewable = row.status === SESSION_STATUS.Completed
                      || row.status === SESSION_STATUS.Confirmed;
                    return (
                    <tr
                      key={row.sessionId}
                      className={`dir-inv-table__row${isViewable ? ' dir-inv-table__row--clickable' : ''}`}
                      onClick={isViewable ? () => navigate(`/inventory/${row.sessionId}`) : undefined}
                    >
                      <td>{formatDate(row.createDate)}</td>
                      <td className="dir-inv-code">{row.code}</td>
                      <td>{row.purpose}</td>
                      <td>{formatDate(row.endDate)}</td>
                      <td>{row.departmentName}</td>
                      <td>{`${row.assetCategoryName} - ${row.assetTypeName}`}</td>
                      <td>
                        <Tag
                          color={STATUS_COLOR[row.status] ?? 'default'}
                          className="dir-inv-status-tag"
                        >
                          {row.statusName}
                        </Tag>
                      </td>
                      <td className="dir-inv-table__progress-cell">
                        <div className="dir-inv-progress">
                          <div className="dir-inv-progress__bar">
                            <div
                              className="dir-inv-progress__fill"
                              style={{ width: `${row.progressPercent ?? 0}%` }}
                            />
                          </div>
                          <span className="dir-inv-progress__label">
                            {row.progressPercent ?? 0}%
                          </span>
                        </div>
                      </td>
                      <td className="dir-inv-table__actions-cell">
                        {renderActions(row)}
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

      <Modal
        open={!!actionMeta}
        title={modalConfig?.title}
        okText={modalConfig?.okText}
        cancelText="Hủy bỏ"
        okButtonProps={{
          danger: modalConfig?.okDanger,
          loading: submitting,
        }}
        onOk={handleConfirmAction}
        onCancel={closeActionModal}
        centered
        width={480}
      >
        {actionMeta && (
          <div className="dir-inv-modal-body">
            <p className="dir-inv-modal-session-info">
              Phiên kiểm kê: <strong>{actionMeta.session.code}</strong>
              {' '}— {actionMeta.session.departmentName}
            </p>

            <div className="dir-inv-modal-notes">
              <label htmlFor="dir-inv-cancel-notes" className="dir-inv-modal-label">Lý do hủy</label>
              <TextArea
                id="dir-inv-cancel-notes"
                rows={3}
                value={notes}
                onChange={(e) => setNotes(e.target.value)}
                placeholder="Nhập lý do hủy lịch kiểm kê..."
              />
            </div>
          </div>
        )}
      </Modal>
    </div>
  );
}

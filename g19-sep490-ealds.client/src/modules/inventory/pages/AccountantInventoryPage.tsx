import { useState, useEffect, useCallback, type MouseEvent } from 'react';
import { useNavigate } from 'react-router-dom';
import { Button, Input, Select, Tag, Spin, message, Modal, Tooltip, Input as AntInput } from 'antd';
import {
  CheckOutlined,
  FileTextOutlined,
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

const ACCOUNTANT_STATUS_FILTER: { value: number; label: string }[] = [
  { value: SESSION_STATUS.PendingAccountant, label: SESSION_STATUS_LABEL[SESSION_STATUS.PendingAccountant] },
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

interface ConfirmMeta {
  session: InventorySessionListItem;
}

function ConfirmModalBody({
  session,
  notes,
  onNotesChange,
}: {
  readonly session: InventorySessionListItem;
  readonly notes: string;
  readonly onNotesChange: (v: string) => void;
}) {
  return (
    <div className="dir-inv-modal-body">
      <p className="dir-inv-modal-session-info">
        Phiên kiểm kê: <strong>{session.code}</strong>
        {' '}
        —
        {session.departmentName}
      </p>

      {(session.unresolvedDiscrepancyCount ?? 0) > 0 && (
        <p className="dir-inv-modal-warning">
          Còn
          {' '}
          {session.unresolvedDiscrepancyCount}
          {' '}
          chênh lệch chưa cập nhật lên sổ. Vui lòng xử lý trong báo cáo trước khi hoàn tất.
        </p>
      )}

      <div className="dir-inv-modal-notes">
        <label htmlFor="acc-inv-action-notes" className="dir-inv-modal-label">
          Ghi chú xác nhận
        </label>
        <TextArea
          id="acc-inv-action-notes"
          rows={3}
          value={notes}
          onChange={(e) => onNotesChange(e.target.value)}
          placeholder="Nhập ghi chú xác nhận (nếu có)..."
        />
      </div>
    </div>
  );
}

function formatDate(dateStr: string | null | undefined): string {
  if (!dateStr) return '-';
  const d = new Date(dateStr);
  if (Number.isNaN(d.getTime())) return '-';
  return `${String(d.getDate()).padStart(2, '0')}/${String(d.getMonth() + 1).padStart(2, '0')}/${d.getFullYear()}`;
}

export function AccountantInventoryPage() {
  const navigate = useNavigate();
  const [sessions, setSessions] = useState<InventorySessionListItem[]>([]);
  const [loading, setLoading] = useState(false);
  const [submitting, setSubmitting] = useState(false);

  const [searchText, setSearchText] = useState('');
  const [departmentFilter, setDepartmentFilter] = useState<string | undefined>(undefined);
  const [statusFilter, setStatusFilter] = useState<number | undefined>(undefined);

  const [confirmMeta, setConfirmMeta] = useState<ConfirmMeta | null>(null);
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

  const filteredSessions = sessions
    .filter((s) => !departmentFilter || s.departmentName === departmentFilter)
    .filter(
      (s) =>
        s.status === SESSION_STATUS.PendingAccountant
        || s.status === SESSION_STATUS.Confirmed,
    );

  const openConfirmModal = (session: InventorySessionListItem) => {
    setConfirmMeta({ session });
    setNotes('');
  };

  const closeConfirmModal = () => {
    setConfirmMeta(null);
    setNotes('');
  };

  const handleConfirmAction = async () => {
    if (!confirmMeta) return;
    setSubmitting(true);

    const accountantId = getCurrentUserId();
    const payload = {
      reviewedBy: accountantId,
      reviewerRoleId: 3,
      reviewNotes: notes || undefined,
      applyCorrections: false,
    };

    try {
      const res = await inventoryService.confirmSession(confirmMeta.session.sessionId, payload);
      message.success(res.message ?? 'Đã chuyển trạng thái sang Đã xử lý.');
      closeConfirmModal();
      fetchSessions();
    } catch (err: unknown) {
      const axiosErr = err as { response?: { data?: { message?: string } } };
      message.error(
        axiosErr?.response?.data?.message ?? 'Thao tác thất bại. Vui lòng thử lại.',
      );
      throw err;
    } finally {
      setSubmitting(false);
    }
  };

  const goReport = (e: MouseEvent, sessionId: number) => {
    e.stopPropagation();
    navigate(`/inventory-review/${sessionId}`);
  };

  const renderActions = (row: InventorySessionListItem) => {
    const viewReport = (
      <Button
        size="small"
        icon={<FileTextOutlined />}
        onClick={(e) => goReport(e, row.sessionId)}
      >
        Báo cáo
      </Button>
    );

    if (row.status === SESSION_STATUS.PendingAccountant) {
      const unresolved = row.unresolvedDiscrepancyCount ?? 0;
      const canFinish = unresolved === 0;
      return (
        <div className="dir-inv-row-actions">
          {viewReport}
          <Tooltip
            title={
              canFinish
                ? undefined
                : `Còn ${unresolved} chênh lệch chưa cập nhật lên sổ. Mở báo cáo và dùng «Cập nhật sổ» cho từng dòng.`
            }
          >
            <span>
              <Button
                size="small"
                type="primary"
                icon={<CheckOutlined />}
                disabled={!canFinish}
                onClick={(e) => {
                  e.stopPropagation();
                  openConfirmModal(row);
                }}
              >
                Hoàn tất
              </Button>
            </span>
          </Tooltip>
        </div>
      );
    }

    if (row.status === SESSION_STATUS.Confirmed) {
      return (
        <div className="dir-inv-row-actions">
          {viewReport}
        </div>
      );
    }

    return null;
  };

  return (
    <div className="dir-inv-page">
      <div className="dir-inv-page__header">
        <h1 className="dir-inv-page__title">Xử lý chênh lệch kiểm kê</h1>
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
            {ACCOUNTANT_STATUS_FILTER.map(({ value, label }) => (
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
                  <th>TIẾN ĐỘ</th>
                  <th>THAO TÁC</th>
                </tr>
              </thead>
              <tbody>
                {filteredSessions.length === 0 ? (
                  <tr>
                    <td colSpan={8} className="dir-inv-table__empty">
                      Không có dữ liệu
                    </td>
                  </tr>
                ) : (
                  filteredSessions.map((row) => {
                    const isViewable =
                      row.status === SESSION_STATUS.PendingAccountant
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
                        <td className="dir-inv-table__progress-cell">
                          <div className="dir-inv-progress">
                            <div className="dir-inv-progress__bar">
                              <div
                                className="dir-inv-progress__fill"
                                style={{ width: `${row.progressPercent ?? 0}%` }}
                              />
                            </div>
                            <span className="dir-inv-progress__label">
                              {row.progressPercent ?? 0}
                              %
                            </span>
                          </div>
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
        open={!!confirmMeta}
        title="Hoàn tất xử lý chênh lệch"
        okText="Hoàn tất"
        cancelText="Hủy bỏ"
        okButtonProps={{
          loading: submitting,
          disabled: (confirmMeta?.session.unresolvedDiscrepancyCount ?? 0) > 0,
        }}
        onOk={handleConfirmAction}
        onCancel={closeConfirmModal}
        centered
        width={480}
      >
        {confirmMeta && (
          <ConfirmModalBody
            session={confirmMeta.session}
            notes={notes}
            onNotesChange={setNotes}
          />
        )}
      </Modal>
    </div>
  );
}

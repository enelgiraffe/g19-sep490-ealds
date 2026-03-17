import { useState, useEffect, useCallback } from 'react';
import { useNavigate } from 'react-router-dom';
import { Button, Input, Select, Tag, Spin, message } from 'antd';
import { SearchOutlined, EyeOutlined, EditOutlined, DeleteOutlined, CopyOutlined, PrinterOutlined, ReloadOutlined } from '@ant-design/icons';
import { SchedulePeriodicModal } from '../components/SchedulePeriodicModal';
import { ScheduleIndividualModal } from '../components/ScheduleIndividualModal';
import type { SchedulePeriodicFormValues } from '../components/SchedulePeriodicModal';
import type { ScheduleIndividualFormValues } from '../components/ScheduleIndividualModal';
import {
  inventoryService,
  type InventorySessionListItem,
} from '../services/inventoryService';
import './InventoryPage.css';

const { Option } = Select;

const STATUS_COLOR: Record<number, string> = {
  0: 'default',     // Nháp
  1: 'processing',  // Đang thực hiện
  2: 'warning',     // Chờ xác nhận
  3: 'error',       // Đã hủy
  4: 'success',     // Đã xác nhận
};

function formatDate(dateStr: string | null | undefined): string {
  if (!dateStr) return '-';
  const d = new Date(dateStr);
  if (isNaN(d.getTime())) return '-';
  return `${String(d.getDate()).padStart(2, '0')}/${String(d.getMonth() + 1).padStart(2, '0')}/${d.getFullYear()}`;
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

  const handleSubmitPeriodic = (_values: SchedulePeriodicFormValues) => {
    fetchSessions();
  };

  const handleSubmitIndividual = (_values: ScheduleIndividualFormValues) => {
    fetchSessions();
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
            <Option value={0}>Nháp</Option>
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
                  filteredSessions.map((row) => (
                    <tr
                      key={row.sessionId}
                      className="inventory-table__row inventory-table__row--clickable"
                      onClick={() => navigate(`/inventory/${row.sessionId}`)}
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
                          {row.status === 0 ? (
                            <>
                              <button type="button" className="inventory-action-btn" title="Xem">
                                <EyeOutlined />
                              </button>
                              <button type="button" className="inventory-action-btn" title="Chỉnh sửa">
                                <EditOutlined />
                              </button>
                              <button
                                type="button"
                                className="inventory-action-btn inventory-action-btn--danger"
                                title="Xóa"
                              >
                                <DeleteOutlined />
                              </button>
                            </>
                          ) : (
                            <>
                              <button type="button" className="inventory-action-btn" title="Sao chép">
                                <CopyOutlined />
                              </button>
                              <button type="button" className="inventory-action-btn" title="In">
                                <PrinterOutlined />
                              </button>
                              <button type="button" className="inventory-action-btn" title="Làm mới">
                                <ReloadOutlined />
                              </button>
                            </>
                          )}
                        </div>
                      </td>
                    </tr>
                  ))
                )}
              </tbody>
            </table>
          )}
        </div>
      </div>

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

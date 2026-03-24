import { useEffect, useMemo, useRef, useState } from 'react';
import { Button, Input, Select, message } from 'antd';
import { EyeOutlined, SearchOutlined, UserAddOutlined } from '@ant-design/icons';
import { isAxiosError } from 'axios';
import { Navigate, useNavigate } from 'react-router-dom';
import { mapBackendRoleToAppRole } from '../../auth/types/auth.types';
import { userService, type CreateUserPayload, type UserItem, type UserMetadata } from '../services/userService';
import './UsersPage.css';

type UserStatus = 'active' | 'inactive';

interface UserRow {
  key: number;
  userId: number;
  employeeCode: string;
  fullName: string;
  departmentName: string;
  roleText: string;
  primaryRole: string;
  status: UserStatus;
}

const STATUS_META: Record<UserStatus, { label: string; className: string }> = {
  active: { label: 'Kích hoạt', className: 'users-status-pill users-status-pill--active' },
  inactive: { label: 'Không kích hoạt', className: 'users-status-pill users-status-pill--inactive' },
};

const mapUserToRow = (item: UserItem): UserRow => {
  const roles = Array.isArray(item.roles) ? item.roles : [];
  const roleText = roles.length > 0 ? roles.join(', ') : 'Chưa phân quyền';
  return {
    key: item.userId,
    userId: item.userId,
    employeeCode: item.employeeCode ?? `NV${String(item.userId).padStart(3, '0')}`,
    fullName: item.fullName ?? item.email,
    departmentName: item.departmentName ?? '—',
    roleText,
    primaryRole: roles[0] ?? 'Chưa phân quyền',
    status: item.status === 1 ? 'active' : 'inactive',
  };
};

function getStoredAppRole() {
  try {
    const raw = localStorage.getItem('user');
    if (!raw) return null;
    const user = JSON.parse(raw) as { role?: string };
    return mapBackendRoleToAppRole(user.role);
  } catch {
    return null;
  }
}

export function UsersPage() {
  const navigate = useNavigate();
  const [rows, setRows] = useState<UserRow[]>([]);
  const [metadata, setMetadata] = useState<UserMetadata>({ roles: [], departments: [] });
  const [isLoading, setIsLoading] = useState(false);
  const [searchText, setSearchText] = useState('');
  const [roleFilter, setRoleFilter] = useState<string | 'all'>('all');
  const [isCreateModalOpen, setIsCreateModalOpen] = useState(false);
  const [isSaving, setIsSaving] = useState(false);
  const [isDeletingId, setIsDeletingId] = useState<number | null>(null);
  const [deleteTarget, setDeleteTarget] = useState<UserRow | null>(null);
  const hasLoadedRef = useRef(false);
  const [createDraft, setCreateDraft] = useState({
    fullName: '',
    employeeCode: '',
    email: '',
    phone: '',
    departmentId: 0,
    roleId: 0,
    password: '',
    status: 1,
  });

  const appRole = getStoredAppRole();
  if (appRole !== 'admin') {
    return <Navigate to="/" replace />;
  }

  const loadUsers = async () => {
    try {
      setIsLoading(true);
      const data = await userService.getAll();
      setRows(data.map(mapUserToRow));
    } catch (error) {
      // eslint-disable-next-line no-console
      console.error('Failed to load users', error);
      message.error('Không tải được danh sách người dùng.');
    } finally {
      setIsLoading(false);
    }
  };

  const roleFilterOptions = useMemo(() => {
    const fromMetadata = metadata.roles.map((role) => role.name);
    const fromRows = rows.map((row) => row.primaryRole);
    const unique = Array.from(new Set([...fromMetadata, ...fromRows].filter(Boolean)));
    return unique;
  }, [metadata.roles, rows]);

  useEffect(() => {
    if (hasLoadedRef.current) return;
    hasLoadedRef.current = true;

    loadUsers();
    Promise.all([userService.getRoles(), userService.getDepartments()])
      .then(([roles, departments]) => {
        setMetadata({ roles, departments });
      })
      .catch(() => {
        // Keep page usable by falling back to roles from loaded users.
        // User can still see list even when lookup APIs are unavailable.
      });
  }, []);

  const filteredRows = useMemo(() => {
    const keyword = searchText.trim().toLowerCase();
    return rows.filter((row) => {
      const matchRole = roleFilter === 'all' || row.roleText.toLowerCase().includes(roleFilter.toLowerCase());
      const matchSearch =
        !keyword ||
        row.employeeCode.toLowerCase().includes(keyword) ||
        row.fullName.toLowerCase().includes(keyword);
      return matchRole && matchSearch;
    });
  }, [rows, searchText, roleFilter]);

  const handleCreateUser = async () => {
    const fullName = createDraft.fullName.trim();
    const employeeCode = createDraft.employeeCode.trim();
    const email = createDraft.email.trim();
    const phone = createDraft.phone.trim();
    const password = createDraft.password.trim();
    if (!fullName || !employeeCode || !email || !phone || !createDraft.departmentId || !createDraft.roleId) {
      message.error('Vui lòng nhập đầy đủ thông tin bắt buộc.');
      return;
    }

    const payload: CreateUserPayload = {
      fullName,
      employeeCode,
      email,
      phone,
      departmentId: createDraft.departmentId,
      password,
      status: createDraft.status,
      roleIds: [createDraft.roleId],
    };

    try {
      setIsSaving(true);
      await userService.create(payload);
      message.success('Tạo người dùng thành công.');
      setIsCreateModalOpen(false);
      setCreateDraft({
        fullName: '',
        employeeCode: '',
        email: '',
        phone: '',
        departmentId: 0,
        roleId: 0,
        password: '',
        status: 1,
      });
      await loadUsers();
    } catch (error) {
      if (isAxiosError(error)) {
        const data = error.response?.data as
          | { title?: string; detail?: string; errors?: Record<string, string[]> }
          | string
          | undefined;
        if (typeof data === 'string') {
          message.error(data);
        } else if (data?.errors) {
          const firstError = Object.values(data.errors).flat()[0];
          message.error(firstError || data.title || 'Không thể tạo người dùng.');
        } else {
          message.error(data?.title || data?.detail || 'Không thể tạo người dùng.');
        }
      } else {
        message.error('Không thể tạo người dùng.');
      }
    } finally {
      setIsSaving(false);
    }
  };

  const handleConfirmDelete = async () => {
    if (!deleteTarget) return;
    try {
      setIsDeletingId(deleteTarget.userId);
      await userService.delete(deleteTarget.userId);
      message.success('Xóa người dùng thành công.');
      setDeleteTarget(null);
      await loadUsers();
    } catch (error) {
      if (isAxiosError(error)) {
        const data = error.response?.data;
        if (typeof data === 'string' && data) {
          message.error(data);
        } else {
          message.error('Không thể xóa người dùng.');
        }
      } else {
        message.error('Không thể xóa người dùng.');
      }
    } finally {
      setIsDeletingId(null);
    }
  };

  return (
    <div className="users-page">
      <div className="users-header">
        <h1 className="users-title">Người dùng</h1>
        <Button
          type="primary"
          danger
          className="users-btn-add"
          icon={<UserAddOutlined />}
          onClick={() => setIsCreateModalOpen(true)}
        >
          Thêm người dùng
        </Button>
      </div>

      <div className="users-card">
        <div className="users-filters">
          <Input
            className="users-search"
            placeholder="Tìm kiếm tên / mã nhân viên"
            prefix={<SearchOutlined />}
            value={searchText}
            onChange={(event) => setSearchText(event.target.value)}
          />
          <Select
            className="users-select"
            value={roleFilter}
            onChange={(value) => setRoleFilter(value as string | 'all')}
            options={[
              { value: 'all', label: 'Vai trò' },
              ...roleFilterOptions.map((roleName) => ({ value: roleName, label: roleName })),
            ]}
          />
        </div>

        <div className="asset-table-wrapper users-table-wrapper">
          <table className="asset-table users-table">
            <thead>
              <tr>
                <th>MÃ NHÂN VIÊN</th>
                <th>HỌ VÀ TÊN</th>
                <th>PHÒNG BAN</th>
                <th>VAI TRÒ</th>
                <th>TRẠNG THÁI</th>
                <th className="asset-table__cell asset-table__cell--actions" />
              </tr>
            </thead>
            <tbody>
              {isLoading ? (
                <tr>
                  <td colSpan={6} className="users-empty">
                    Đang tải dữ liệu...
                  </td>
                </tr>
              ) : filteredRows.length === 0 ? (
                <tr>
                  <td colSpan={6} className="users-empty">
                    Không có dữ liệu.
                  </td>
                </tr>
              ) : (
                filteredRows.map((row) => (
                  <tr key={row.key}>
                    <td>{row.employeeCode}</td>
                    <td>{row.fullName}</td>
                    <td>{row.departmentName}</td>
                    <td>{row.roleText}</td>
                    <td>
                      <span className={STATUS_META[row.status].className}>{STATUS_META[row.status].label}</span>
                    </td>
                    <td className="asset-table__cell asset-table__cell--actions">
                      <button
                        type="button"
                        className="users-action-btn"
                        aria-label="Xem chi tiết"
                        onClick={() => navigate(`/users/${row.userId}`)}
                      >
                        <EyeOutlined />
                      </button>
                      <button
                        type="button"
                        className="users-action-btn users-action-btn--danger"
                        disabled={isDeletingId === row.userId}
                        onClick={() => setDeleteTarget(row)}
                        aria-label="Xóa người dùng"
                      >
                        {isDeletingId === row.userId ? '...' : '🗑'}
                      </button>
                    </td>
                  </tr>
                ))
              )}
            </tbody>
          </table>
        </div>

        <div className="users-footer">
          <div>Số lượng trên trang: 25</div>
          <div>
            {filteredRows.length === 0 ? '0' : `1-${Math.min(filteredRows.length, 25)}`} trên {filteredRows.length}
          </div>
        </div>
      </div>

      {isCreateModalOpen && (
        <div className="users-create-overlay" role="dialog" aria-modal="true">
          <div className="users-create-modal">
            <div className="users-create-header">Thêm người dùng</div>
            <div className="users-create-body">
              <div className="users-field">
                <label>Tên nhân viên*</label>
                <Input
                  value={createDraft.fullName}
                  onChange={(event) => setCreateDraft((prev) => ({ ...prev, fullName: event.target.value }))}
                  placeholder="Nhân sự"
                />
              </div>
              <div className="users-field">
                <label>Mã nhân viên*</label>
                <Input
                  value={createDraft.employeeCode}
                  onChange={(event) => setCreateDraft((prev) => ({ ...prev, employeeCode: event.target.value }))}
                  placeholder="CR650"
                />
              </div>
              <div className="users-field">
                <label>Email*</label>
                <Input
                  value={createDraft.email}
                  onChange={(event) => setCreateDraft((prev) => ({ ...prev, email: event.target.value }))}
                  placeholder="email@example.com"
                />
              </div>
              <div className="users-field">
                <label>Số điện thoại*</label>
                <Input
                  value={createDraft.phone}
                  onChange={(event) => setCreateDraft((prev) => ({ ...prev, phone: event.target.value }))}
                  placeholder="09..."
                />
              </div>
              <div className="users-field">
                <label>Thuộc phòng ban*</label>
                <Select
                  value={createDraft.departmentId || undefined}
                  onChange={(value) => setCreateDraft((prev) => ({ ...prev, departmentId: value }))}
                  getPopupContainer={(trigger) => trigger.parentElement ?? document.body}
                  options={metadata.departments.map((department) => ({
                    value: department.departmentId,
                    label: department.name,
                  }))}
                  placeholder="Chọn phòng ban"
                />
              </div>
              <div className="users-field">
                <label>Vai trò*</label>
                <Select
                  value={createDraft.roleId || undefined}
                  onChange={(value) => setCreateDraft((prev) => ({ ...prev, roleId: value }))}
                  getPopupContainer={(trigger) => trigger.parentElement ?? document.body}
                  options={metadata.roles.map((role) => ({ value: role.roleId, label: role.name }))}
                  placeholder="Chọn vai trò"
                />
              </div>
              <div className="users-field">
                <label>Mật khẩu*</label>
                <Input.Password
                  value={createDraft.password}
                  onChange={(event) => setCreateDraft((prev) => ({ ...prev, password: event.target.value }))}
                  placeholder="Tối thiểu 6 ký tự"
                />
              </div>
              <div className="users-field">
                <label>Trạng thái</label>
                <Select
                  value={createDraft.status}
                  onChange={(value) => setCreateDraft((prev) => ({ ...prev, status: value }))}
                  getPopupContainer={(trigger) => trigger.parentElement ?? document.body}
                  options={[
                    { value: 1, label: 'Kích hoạt' },
                    { value: 0, label: 'Không kích hoạt' },
                  ]}
                />
              </div>
            </div>
            <div className="users-create-footer">
              <button type="button" className="users-create-btn users-create-btn--primary" onClick={handleCreateUser}>
                {isSaving ? 'Đang tạo...' : '✓ Tạo'}
              </button>
              <button
                type="button"
                className="users-create-btn users-create-btn--secondary"
                onClick={() => setIsCreateModalOpen(false)}
              >
                ✕ Đóng
              </button>
            </div>
          </div>
        </div>
      )}

      {deleteTarget && (
        <div className="users-confirm-overlay" role="dialog" aria-modal="true">
          <div className="users-confirm-modal">
            <div className="users-confirm-header">Xóa người dùng</div>
            <div className="users-confirm-body">
              Bạn có chắc chắn muốn xóa người dùng <strong>{deleteTarget.fullName}</strong> ({deleteTarget.employeeCode})?
            </div>
            <div className="users-confirm-footer">
              <button
                type="button"
                className="users-confirm-btn users-confirm-btn--danger"
                onClick={handleConfirmDelete}
                disabled={isDeletingId === deleteTarget.userId}
              >
                {isDeletingId === deleteTarget.userId ? 'Đang xóa...' : 'Xóa'}
              </button>
              <button
                type="button"
                className="users-confirm-btn users-confirm-btn--cancel"
                onClick={() => setDeleteTarget(null)}
              >
                Hủy
              </button>
            </div>
          </div>
        </div>
      )}
    </div>
  );
}


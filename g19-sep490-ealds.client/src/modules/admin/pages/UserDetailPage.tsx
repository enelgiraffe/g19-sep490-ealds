import { useEffect, useMemo, useState } from 'react';
import { Input, Select, message, Switch } from 'antd';
import { isAxiosError } from 'axios';
import { LeftOutlined } from '@ant-design/icons';
import { Navigate, useNavigate, useParams } from 'react-router-dom';
import { mapBackendRoleToAppRole } from '../../auth/types/auth.types';
import { userService, type UserItem, type UserMetadata } from '../services/userService';
import './UserDetailPage.css';

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

export function UserDetailPage() {
  const navigate = useNavigate();
  const { id } = useParams();
  const [activeTab, setActiveTab] = useState<'profile' | 'history'>('profile');
  const [user, setUser] = useState<UserItem | null>(null);
  const [loading, setLoading] = useState(false);
  const [metadata, setMetadata] = useState<UserMetadata>({ roles: [], departments: [] });
  const [isEditing, setIsEditing] = useState(false);
  const [isPasswordOpen, setIsPasswordOpen] = useState(false);
  const [isDeleteOpen, setIsDeleteOpen] = useState(false);
  const [isSavingEdit, setIsSavingEdit] = useState(false);
  const [isSavingPassword, setIsSavingPassword] = useState(false);
  const [isDeleting, setIsDeleting] = useState(false);
  const [editDraft, setEditDraft] = useState({
    fullName: '',
    email: '',
    phone: '',
    departmentId: 0,
    roleId: 0,
    status: 1,
  });
  const [newPassword, setNewPassword] = useState('');
  const [confirmPassword, setConfirmPassword] = useState('');

  const appRole = getStoredAppRole();
  if (appRole !== 'admin') {
    return <Navigate to="/" replace />;
  }

  useEffect(() => {
    const userId = Number(id);
    if (!Number.isInteger(userId) || userId <= 0) {
      message.error('ID người dùng không hợp lệ.');
      navigate('/users');
      return;
    }

    setLoading(true);
    userService
      .getById(userId)
      .then((res) => {
        setUser(res);
        setEditDraft({
          fullName: res.fullName ?? '',
          email: res.email ?? '',
          phone: res.phone ?? '',
          departmentId: res.departmentId ?? 0,
          roleId: res.roleIds?.[0] ?? 0,
          status: res.status ?? 1,
        });
      })
      .catch(() => {
        message.error('Không tải được chi tiết người dùng.');
      })
      .finally(() => setLoading(false));

    Promise.all([userService.getRoles(), userService.getDepartments()])
      .then(([roles, departments]) => setMetadata({ roles, departments }))
      .catch(() => {
        // keep detail page usable even when lookup API unavailable
      });
  }, [id, navigate]);

  const departmentSelectOptions = useMemo(() => {
    const base = metadata.departments.map((d) => ({
      value: d.departmentId,
      label: d.name,
    }));
    const uid = user?.departmentId;
    if (uid != null && uid > 0 && !base.some((o) => o.value === uid)) {
      base.push({
        value: uid,
        label: `${user?.departmentName ?? 'Phòng ban'} (không hoạt động)`,
      });
    }
    return base;
  }, [metadata.departments, user?.departmentId, user?.departmentName]);

  const handleSaveEdit = async () => {
    if (!user) return;
    if (!editDraft.fullName.trim() || !editDraft.email.trim() || !editDraft.phone.trim() || !editDraft.departmentId || !editDraft.roleId) {
      message.error('Vui lòng nhập đầy đủ thông tin bắt buộc.');
      return;
    }

    try {
      setIsSavingEdit(true);
      await userService.update(user.userId, {
        fullName: editDraft.fullName.trim(),
        email: editDraft.email.trim(),
        phone: editDraft.phone.trim(),
        departmentId: editDraft.departmentId,
        status: editDraft.status,
        roleIds: [editDraft.roleId],
      });
      const refreshed = await userService.getById(user.userId);
      setUser(refreshed);
      setIsEditing(false);
      message.success('Cập nhật người dùng thành công.');
    } catch {
      message.error('Không thể cập nhật người dùng.');
    } finally {
      setIsSavingEdit(false);
    }
  };

  const handleChangePassword = async () => {
    if (!user) return;
    if (!newPassword || !confirmPassword) {
      message.error('Vui lòng nhập mật khẩu mới và xác nhận.');
      return;
    }
    if (newPassword !== confirmPassword) {
      message.error('Xác nhận mật khẩu không khớp.');
      return;
    }

    try {
      setIsSavingPassword(true);
      await userService.changePassword(user.userId, {
        newPassword,
        confirmNewPassword: confirmPassword,
      });
      setIsPasswordOpen(false);
      setNewPassword('');
      setConfirmPassword('');
      message.success('Đổi mật khẩu thành công.');
    } catch {
      message.error('Không thể đổi mật khẩu.');
    } finally {
      setIsSavingPassword(false);
    }
  };

  const handleDeleteUser = async () => {
    if (!user) return;
    try {
      setIsDeleting(true);
      await userService.delete(user.userId);
      message.success('Xóa người dùng thành công.');
      navigate('/users');
    } catch (error) {
      if (isAxiosError(error) && typeof error.response?.data === 'string') {
        message.error(error.response.data);
      } else {
        message.error('Không thể xóa người dùng.');
      }
    } finally {
      setIsDeleting(false);
      setIsDeleteOpen(false);
    }
  };

  return (
    <div className="user-detail-page">
      <div className="user-detail-header">
        <button type="button" className="user-detail-back" onClick={() => navigate('/users')}>
          <LeftOutlined />
        </button>
        <h1>Quản lý người dùng</h1>
      </div>

      <div className="user-detail-tabs">
        <button
          type="button"
          className={activeTab === 'profile' ? 'user-detail-tab user-detail-tab--active' : 'user-detail-tab'}
          onClick={() => setActiveTab('profile')}
        >
          Hồ sơ
        </button>
        <button
          type="button"
          className={activeTab === 'history' ? 'user-detail-tab user-detail-tab--active' : 'user-detail-tab'}
          onClick={() => setActiveTab('history')}
        >
          Lịch sử hoạt động
        </button>
      </div>

      {loading ? (
        <div className="user-detail-loading">Đang tải dữ liệu...</div>
      ) : activeTab === 'history' ? (
        <div className="user-detail-loading">Lịch sử hoạt động sẽ được bổ sung sau.</div>
      ) : (
        <div className="user-detail-content">
          <div className="user-detail-top">
            <div>
              <div className="user-detail-label">Mã nhân viên</div>
              <div className="user-detail-value">{user?.employeeCode || '—'}</div>
            </div>
          </div>

          <div className="user-detail-grid">
            <div>
              <div className="user-detail-label">Họ tên</div>
              {isEditing ? (
                <Input
                  value={editDraft.fullName}
                  onChange={(e) => setEditDraft((d) => ({ ...d, fullName: e.target.value }))}
                />
              ) : (
                <div className="user-detail-value">{user?.fullName || '—'}</div>
              )}
            </div>
            <div>
              <div className="user-detail-label">Email</div>
              {isEditing ? (
                <Input
                  value={editDraft.email}
                  onChange={(e) => setEditDraft((d) => ({ ...d, email: e.target.value }))}
                />
              ) : (
                <div className="user-detail-value">{user?.email || '—'}</div>
              )}
            </div>
            <div>
              <div className="user-detail-label">Vị trí công việc</div>
              {isEditing ? (
                <Select
                  value={editDraft.roleId || undefined}
                  onChange={(value) => setEditDraft((d) => ({ ...d, roleId: value }))}
                  options={metadata.roles.map((r) => ({ value: r.roleId, label: r.name }))}
                  getPopupContainer={(trigger) => trigger.parentElement ?? document.body}
                />
              ) : (
                <div className="user-detail-value">
                  {Array.isArray(user?.roles) && user.roles.length > 0 ? user.roles.join(', ') : '—'}
                </div>
              )}
            </div>
            <div>
              <div className="user-detail-label">Phòng ban</div>
              {isEditing ? (
                <Select
                  value={editDraft.departmentId || undefined}
                  onChange={(value) => setEditDraft((d) => ({ ...d, departmentId: value }))}
                  options={departmentSelectOptions}
                  getPopupContainer={(trigger) => trigger.parentElement ?? document.body}
                />
              ) : (
                <div className="user-detail-value">{user?.departmentName || '—'}</div>
              )}
            </div>
            <div>
              <div className="user-detail-label">Số điện thoại</div>
              {isEditing ? (
                <Input
                  value={editDraft.phone}
                  onChange={(e) => setEditDraft((d) => ({ ...d, phone: e.target.value }))}
                />
              ) : (
                <div className="user-detail-value">{user?.phone || '—'}</div>
              )}
            </div>
            <div>
              <div className="user-detail-label">Trạng thái</div>
              {isEditing ? (
                <Select
                  value={editDraft.status}
                  onChange={(value) => setEditDraft((d) => ({ ...d, status: value }))}
                  options={[
                    { value: 1, label: 'Kích hoạt' },
                    { value: 0, label: 'Không kích hoạt' },
                  ]}
                  getPopupContainer={(trigger) => trigger.parentElement ?? document.body}
                />
              ) : (
                <div className="user-detail-value">{user?.status === 1 ? 'Kích hoạt' : 'Không kích hoạt'}</div>
              )}
            </div>
          </div>

          {!isEditing && (
            <div className="user-detail-status">
            <Switch checked={user?.status === 1} disabled />
            <span>Kích hoạt</span>
            </div>
          )}
        </div>
      )}

      <div className="user-detail-footer">
        {isEditing ? (
          <>
            <button type="button" className="user-detail-btn user-detail-btn--primary" onClick={handleSaveEdit} disabled={isSavingEdit}>
              {isSavingEdit ? 'Đang lưu...' : '✓ Lưu'}
            </button>
            <button
              type="button"
              className="user-detail-btn user-detail-btn--secondary"
              onClick={() => {
                if (user) {
                  setEditDraft({
                    fullName: user.fullName ?? '',
                    email: user.email ?? '',
                    phone: user.phone ?? '',
                    departmentId: user.departmentId ?? 0,
                    roleId: user.roleIds?.[0] ?? 0,
                    status: user.status ?? 1,
                  });
                }
                setIsEditing(false);
              }}
            >
              ✕ Hủy
            </button>
          </>
        ) : (
          <button type="button" className="user-detail-btn user-detail-btn--primary" onClick={() => setIsEditing(true)}>
            ✎ Chỉnh sửa
          </button>
        )}
        <button type="button" className="user-detail-btn user-detail-btn--secondary" onClick={() => setIsPasswordOpen(true)}>
          🔒 Đổi mật khẩu
        </button>
        <button type="button" className="user-detail-btn user-detail-btn--danger" onClick={() => setIsDeleteOpen(true)}>
          🗑 Xóa
        </button>
      </div>

      {isPasswordOpen && (
        <div className="user-password-modal-overlay" role="dialog" aria-modal="true">
          <div className="user-password-modal">
            <button
              type="button"
              className="user-password-modal__close-btn"
              onClick={() => setIsPasswordOpen(false)}
              aria-label="Đóng"
            >
              <span className="user-password-modal__close">×</span>
            </button>

            <div className="user-password-modal__header">
              <h2 className="user-password-modal__title">Đổi mật khẩu người dùng</h2>
            </div>

            <div className="user-password-modal__body">
              <div className="user-detail-form">
                <label>Mật khẩu mới</label>
                <Input.Password value={newPassword} onChange={(e) => setNewPassword(e.target.value)} />
                <label>Xác nhận mật khẩu</label>
                <Input.Password value={confirmPassword} onChange={(e) => setConfirmPassword(e.target.value)} />
              </div>
            </div>

            <div className="user-password-modal__footer">
              <button
                type="button"
                className="user-password-btn-submit"
                onClick={handleChangePassword}
                disabled={isSavingPassword}
              >
                {isSavingPassword ? 'Đang cập nhật...' : 'Cập nhật'}
              </button>
              <button
                type="button"
                className="user-password-btn-cancel"
                onClick={() => setIsPasswordOpen(false)}
              >
                Hủy
              </button>
            </div>
          </div>
        </div>
      )}

      {isDeleteOpen && (
        <div className="user-password-modal-overlay" role="dialog" aria-modal="true">
          <div className="user-password-modal">
            <button
              type="button"
              className="user-password-modal__close-btn"
              onClick={() => setIsDeleteOpen(false)}
              aria-label="Đóng"
            >
              <span className="user-password-modal__close">×</span>
            </button>

            <div className="user-password-modal__header">
              <h2 className="user-password-modal__title">Xóa người dùng</h2>
            </div>

            <div className="user-password-modal__body">
              Bạn có chắc chắn muốn xóa người dùng <strong>{user?.fullName ?? user?.email ?? ''}</strong> không?
            </div>

            <div className="user-password-modal__footer">
              <button
                type="button"
                className="user-password-btn-submit user-password-btn-submit--danger"
                onClick={handleDeleteUser}
                disabled={isDeleting}
              >
                {isDeleting ? 'Đang xóa...' : 'Xóa'}
              </button>
              <button
                type="button"
                className="user-password-btn-cancel"
                onClick={() => setIsDeleteOpen(false)}
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


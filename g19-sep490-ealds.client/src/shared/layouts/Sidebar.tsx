import { NavLink, useNavigate } from 'react-router-dom';
import { Dropdown } from 'antd';
import type { MenuProps } from 'antd';
import { useCallback, useEffect, useState } from 'react';
import { useAppStore } from '../../stores/appStore';
import { authService } from '../../modules/auth/services/authService';
import { COMMON_MENU, ROLE_MENU, ROLE_OPTIONS } from '../constants/sidebarConfig';
import { notificationService } from '../services/notificationService';
import type { AppRole } from '../types/layout.types';
import './Sidebar.css';

export function Sidebar() {
  const navigate = useNavigate();
  const { currentRole, setCurrentRole } = useAppStore();
  const roleMenu = ROLE_MENU[currentRole];
  const [notificationsUnread, setNotificationsUnread] = useState(0);

  const refreshNotificationsUnread = useCallback(() => {
    notificationService
      .getUnreadCount()
      .then(setNotificationsUnread)
      .catch(() => setNotificationsUnread(0));
  }, []);

  useEffect(() => {
    void refreshNotificationsUnread();
  }, [refreshNotificationsUnread]);

  useEffect(() => {
    const onChanged = () => void refreshNotificationsUnread();
    window.addEventListener('ealds-notifications-changed', onChanged);
    return () => window.removeEventListener('ealds-notifications-changed', onChanged);
  }, [refreshNotificationsUnread]);

  const roleDropdownItems: MenuProps['items'] = ROLE_OPTIONS.map((opt) => ({
    key: opt.value,
    label: opt.label,
    onClick: () => setCurrentRole(opt.value as AppRole),
  }));

  const handleLogout = async () => {
    try {
      await authService.logout();
    } catch {
      // ignore (e.g. network error) – still clear local state
    } finally {
      localStorage.removeItem('accessToken');
      localStorage.removeItem('refreshToken');
      localStorage.removeItem('user');
      navigate('/login');
    }
  };

  const userDropdownItems: MenuProps['items'] = [
    {
      key: 'profile',
      label: 'Hồ sơ',
      onClick: () => navigate('/profile'),
    },
    {
      key: 'logout',
      label: 'Đăng xuất',
      onClick: handleLogout,
    },
  ];

  const currentRoleLabel = ROLE_OPTIONS.find((o) => o.value === currentRole)?.label ?? currentRole;

  return (
    <aside className="sidebar">
      <div className="sidebar__logo">
        <img
          src="/images/logoCompany.png"
          alt="Logo"
          className="sidebar__logo-img"
        />
      </div>
      <nav className="sidebar__nav">
        {COMMON_MENU.length > 0 && (
          <>
            <ul className="sidebar__list sidebar__list--common">
              {COMMON_MENU.map((item) => (
                <li key={item.key}>
                  <NavLink
                    to={item.path}
                    className={({ isActive }) =>
                      `sidebar__link ${isActive ? 'sidebar__link--active' : ''}`
                    }
                  >
                    {item.icon && (
                      <img
                        src={item.icon}
                        alt=""
                        className="sidebar__link-icon"
                        aria-hidden="true"
                      />
                    )}
                    <span className="sidebar__link-label">{item.label}</span>
                    {item.key === 'notifications' && notificationsUnread > 0 && (
                      <span className="sidebar__notifications-badge" aria-label={`${notificationsUnread} chưa đọc`}>
                        {notificationsUnread > 99 ? '99+' : notificationsUnread}
                      </span>
                    )}
                  </NavLink>
                </li>
              ))}
            </ul>
          </>
        )}
        <div className="sidebar__group-label">QUẢN TRỊ</div>
        <ul className="sidebar__list">
          {roleMenu.map((item) => (
            <li key={item.key}>
              <NavLink
                to={item.path}
                className={({ isActive }) =>
                  `sidebar__link ${isActive ? 'sidebar__link--active' : ''}`
                }
              >
                {item.icon && (
                  <img
                    src={item.icon}
                    alt=""
                    className="sidebar__link-icon"
                    aria-hidden="true"
                  />
                )}
                <span className="sidebar__link-label">{item.label}</span>
              </NavLink>
            </li>
          ))}
        </ul>
      </nav>
      <div className="sidebar__user-section">
        <Dropdown 
          menu={{ items: roleDropdownItems }} 
          trigger={['click']} 
          placement="topRight"
          getPopupContainer={(trigger) => trigger.parentElement || document.body}
        >
          <div className="sidebar__role-selector" role="button" tabIndex={0}>
            <span className="sidebar__role-label">Vai trò:</span>
            <span className="sidebar__role-name">{currentRoleLabel}</span>
            <span className="sidebar__role-chevron">▼</span>
          </div>
        </Dropdown>
        <Dropdown 
          menu={{ items: userDropdownItems }} 
          trigger={['click']} 
          placement="topRight"
          getPopupContainer={(trigger) => trigger.parentElement || document.body}
        >
          <div className="sidebar__user" role="button" tabIndex={0}>
            <div className="sidebar__user-avatar">A</div>
            <span className="sidebar__user-name">{currentRoleLabel}</span>
            <span className="sidebar__user-chevron">▼</span>
          </div>
        </Dropdown>
      </div>
    </aside>
  );
}

import { NavLink, useNavigate } from 'react-router-dom';
import { Dropdown } from 'antd';
import type { MenuProps } from 'antd';
import { useCallback, useEffect, useState } from 'react';
import { useAppStore } from '../../stores/appStore';
import { authService } from '../../modules/auth/services/authService';
import { COMMON_MENU, ROLE_MENU } from '../constants/sidebarConfig';
import { notificationService } from '../services/notificationService';
import './Sidebar.css';

function readStoredUserLabel(): { displayName: string; avatarInitial: string } {
  try {
    const raw = localStorage.getItem('user');
    if (!raw) return { displayName: '', avatarInitial: '?' };
    const u = JSON.parse(raw) as { name?: string; email?: string };
    const name = u.name?.trim() ?? '';
    const email = u.email?.trim() ?? '';
    const displayName = name || email || '';
    const initialSource = name || email || '?';
    const avatarInitial = initialSource.charAt(0).toUpperCase();
    return { displayName, avatarInitial };
  } catch {
    return { displayName: '', avatarInitial: '?' };
  }
}

/** Đường dẫn public (icons, logo) — tôn trọng Vite `base` khi deploy subpath. */
function publicAssetUrl(path: string): string {
  if (!path) return '';
  if (/^https?:\/\//i.test(path)) return path;
  const base = import.meta.env.BASE_URL ?? '/';
  const withSlash = base.endsWith('/') ? base : `${base}/`;
  const rel = path.startsWith('/') ? path.slice(1) : path;
  return `${withSlash}${rel}`.replace(/([^:]\/)\/+/g, '$1');
}

export function Sidebar() {
  const navigate = useNavigate();
  const currentRole = useAppStore((s) => s.currentRole);
  const roleMenu = ROLE_MENU[currentRole];
  const [notificationsUnread, setNotificationsUnread] = useState(0);
  const [{ displayName, avatarInitial }, setUserLabel] = useState(() => readStoredUserLabel());

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

  useEffect(() => {
    setUserLabel(readStoredUserLabel());
    const onStorage = (e: StorageEvent) => {
      if (e.key === 'user') setUserLabel(readStoredUserLabel());
    };
    window.addEventListener('storage', onStorage);
    return () => window.removeEventListener('storage', onStorage);
  }, []);

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

  const sidebarUserTitle = displayName || 'Người dùng';

  return (
    <aside className="sidebar">
      <div className="sidebar__logo">
        <img
          src={publicAssetUrl('/images/logoCompany.png')}
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
                        src={publicAssetUrl(item.icon)}
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
                    src={publicAssetUrl(item.icon)}
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
          menu={{ items: userDropdownItems }} 
          trigger={['click']} 
          placement="topRight"
          getPopupContainer={(trigger) => trigger.parentElement || document.body}
        >
          <div className="sidebar__user" role="button" tabIndex={0}>
            <div className="sidebar__user-avatar" aria-hidden="true">
              {avatarInitial}
            </div>
            <span className="sidebar__user-name">{sidebarUserTitle}</span>
            <span className="sidebar__user-chevron">▼</span>
          </div>
        </Dropdown>
      </div>
    </aside>
  );
}

import { NavLink } from 'react-router-dom';
import { Dropdown } from 'antd';
import type { MenuProps } from 'antd';
import { useAppStore } from '../../stores/appStore';
import { COMMON_MENU, ROLE_MENU, ROLE_OPTIONS } from '../constants/sidebarConfig';
import type { AppRole } from '../types/layout.types';
import './Sidebar.css';

export function Sidebar() {
  const { currentRole, setCurrentRole } = useAppStore();
  const roleMenu = ROLE_MENU[currentRole];

  const roleDropdownItems: MenuProps['items'] = ROLE_OPTIONS.map((opt) => ({
    key: opt.value,
    label: opt.label,
    onClick: () => setCurrentRole(opt.value as AppRole),
  }));

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
                    {item.label}
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
                {item.label}
              </NavLink>
            </li>
          ))}
        </ul>
      </nav>
      <Dropdown menu={{ items: roleDropdownItems }} trigger={['click']} placement="topRight">
        <div className="sidebar__user" role="button" tabIndex={0}>
          <div className="sidebar__user-avatar">A</div>
          <span className="sidebar__user-name">{currentRoleLabel}</span>
          <span className="sidebar__user-chevron">▼</span>
        </div>
      </Dropdown>
    </aside>
  );
}

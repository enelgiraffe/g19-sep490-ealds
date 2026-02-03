import { Dropdown } from 'antd';
import type { DropdownProps } from 'antd';
import { useState } from 'react';
import { NotificationPopover } from './NotificationPopover';
import { MOCK_NOTIFICATIONS } from '../data/notificationsMockData';
import './Topbar.css';

const unreadCount = () => MOCK_NOTIFICATIONS.filter((n) => !n.read).length;

export function Topbar() {
  const [open, setOpen] = useState(false);
  const count = unreadCount();

  const dropdownRender: DropdownProps['dropdownRender'] = () => (
    <div onClick={() => setOpen(false)} onKeyDown={() => {}} role="presentation">
      <NotificationPopover onClose={() => setOpen(false)} />
    </div>
  );

  return (
    <header className="topbar">
      <div className="topbar__spacer" />
      <Dropdown
        open={open}
        onOpenChange={setOpen}
        trigger={['click']}
        placement="bottomRight"
        dropdownRender={dropdownRender}
      >
        <button type="button" className="topbar__bell" aria-label="Notifications">
          <span className="topbar__bell-icon">🔔</span>
          {count > 0 && (
            <span className="topbar__bell-badge">{count > 99 ? '99+' : count}</span>
          )}
        </button>
      </Dropdown>
    </header>
  );
}

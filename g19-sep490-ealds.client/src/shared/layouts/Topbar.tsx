import { Dropdown } from 'antd';
import type { DropdownProps } from 'antd';
import { useState, useEffect, useCallback } from 'react';
import { NotificationPopover } from './NotificationPopover';
import { notificationService } from '../services/notificationService';
import './Topbar.css';

export function Topbar() {
  const [open, setOpen] = useState(false);
  const [unreadCount, setUnreadCount] = useState(0);

  const refreshUnread = useCallback(() => {
    notificationService
      .getUnreadCount()
      .then(setUnreadCount)
      .catch(() => setUnreadCount(0));
  }, []);

  useEffect(() => {
    refreshUnread();
  }, [refreshUnread]);

  useEffect(() => {
    const onChanged = () => refreshUnread();
    window.addEventListener('ealds-notifications-changed', onChanged);
    return () => window.removeEventListener('ealds-notifications-changed', onChanged);
  }, [refreshUnread]);

  useEffect(() => {
    if (open) {
      refreshUnread();
    }
  }, [open, refreshUnread]);

  const dropdownRender: DropdownProps['dropdownRender'] = () => (
    <div role="presentation">
      <NotificationPopover onClose={() => setOpen(false)} onUnreadChange={refreshUnread} />
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
          {unreadCount > 0 && (
            <span className="topbar__bell-badge">{unreadCount > 99 ? '99+' : unreadCount}</span>
          )}
        </button>
      </Dropdown>
    </header>
  );
}

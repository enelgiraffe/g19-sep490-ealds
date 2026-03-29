import { useCallback, useEffect, useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { Spin } from 'antd';
import { NotificationRow } from '../components/NotificationRow';
import {
  markNotificationRead,
  notificationService,
} from '../services/notificationService';
import type { NotificationItem } from '../types/notification.types';
import './NotificationPopover.css';

interface NotificationPopoverProps {
  onClose?: () => void;
  /** Called when unread count may have changed */
  onUnreadChange?: () => void;
}

export function NotificationPopover({ onClose, onUnreadChange }: NotificationPopoverProps) {
  const navigate = useNavigate();
  const [items, setItems] = useState<NotificationItem[]>([]);
  const [loading, setLoading] = useState(true);

  const load = useCallback(async () => {
    setLoading(true);
    try {
      const list = await notificationService.list(15);
      setItems(list);
    } catch {
      setItems([]);
    } finally {
      setLoading(false);
    }
  }, []);

  useEffect(() => {
    void load();
  }, [load]);

  useEffect(() => {
    const onChanged = () => {
      void load();
    };
    window.addEventListener('ealds-notifications-changed', onChanged);
    return () => window.removeEventListener('ealds-notifications-changed', onChanged);
  }, [load]);

  const unread = items.filter((i) => !i.read).length;

  const handleClick = (item: NotificationItem) => {
    markNotificationRead(Number(item.id));
    void load();
    onUnreadChange?.();
    navigate(item.link);
    onClose?.();
  };

  return (
    <div className="notification-popover">
      <div className="notification-popover__header">
        🔔 Thông báo ({unread > 0 ? `${unread} chưa đọc` : `${items.length}`})
      </div>
      <Spin spinning={loading}>
        <div className="notification-popover__list">
          {!loading && items.length === 0 ? (
            <div className="notification-popover__empty" style={{ padding: '12px 16px', color: '#888' }}>
              Không có thông báo
            </div>
          ) : (
            items.map((item) => (
              <NotificationRow
                key={item.id}
                item={item}
                onClick={() => handleClick(item)}
                className="notification-popover__row"
              />
            ))
          )}
        </div>
      </Spin>
    </div>
  );
}

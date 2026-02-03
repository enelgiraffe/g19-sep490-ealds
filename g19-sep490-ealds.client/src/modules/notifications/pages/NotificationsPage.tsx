import { useState, useMemo } from 'react';
import { useNavigate } from 'react-router-dom';
import { Card, Dropdown } from 'antd';
import type { MenuProps } from 'antd';
import { NotificationRow } from '../../../shared/components/NotificationRow';
import {
  MOCK_NOTIFICATIONS,
  NOTIFICATION_CATEGORY_TABS,
} from '../../../shared/data/notificationsMockData';
import type { NotificationItem, NotificationType } from '../../../shared/types/notification.types';
import './NotificationsPage.css';

export function NotificationsPage() {
  const navigate = useNavigate();
  const [notifications, setNotifications] = useState<NotificationItem[]>(() =>
    MOCK_NOTIFICATIONS.map((n) => ({ ...n }))
  );
  const [categoryFilter, setCategoryFilter] = useState<'all' | NotificationType>('all');
  const [readFilter, setReadFilter] = useState<'all' | 'unread'>('all');

  const unreadCount = useMemo(
    () => notifications.filter((n) => !n.read).length,
    [notifications]
  );

  const filteredList = useMemo(() => {
    return notifications.filter((item) => {
      const matchCategory =
        categoryFilter === 'all' || item.type === categoryFilter;
      const matchRead =
        readFilter === 'all' || (readFilter === 'unread' && !item.read);
      return matchCategory && matchRead;
    });
  }, [notifications, categoryFilter, readFilter]);

  const handleClick = (link: string) => {
    navigate(link);
  };

  const markAllRead = () => {
    setNotifications((prev) => prev.map((n) => ({ ...n, read: true })));
  };

  const markReadItems: MenuProps['items'] = [
    { key: 'all', label: 'Đánh dấu tất cả đã đọc', onClick: markAllRead },
  ];

  return (
    <div className="notifications-page">
      <header className="notifications-page__header">
        <div className="notifications-page__title-row">
          <h1 className="notifications-page__title">Thông báo</h1>
          <div className="notifications-page__badge-wrap">
            <span className="notifications-page__mini-icon" aria-hidden>🔔</span>
            {unreadCount > 0 && (
              <span className="notifications-page__unread-badge">
                {unreadCount > 99 ? '99+' : unreadCount}
              </span>
            )}
          </div>
        </div>

        <div className="notifications-page__filters">
          <div className="notifications-page__category-tabs">
            {NOTIFICATION_CATEGORY_TABS.map((tab) => (
              <button
                key={tab.value}
                type="button"
                className={`notifications-page__tab ${
                  categoryFilter === tab.value ? 'notifications-page__tab--active' : ''
                }`}
                onClick={() => setCategoryFilter(tab.value)}
              >
                {tab.label}
              </button>
            ))}
          </div>
          <div className="notifications-page__right-actions">
            <div className="notifications-page__read-tabs">
              <button
                type="button"
                className={`notifications-page__read-tab ${
                  readFilter === 'all' ? 'notifications-page__read-tab--active' : ''
                }`}
                onClick={() => setReadFilter('all')}
              >
                Tất cả
              </button>
              <button
                type="button"
                className={`notifications-page__read-tab ${
                  readFilter === 'unread' ? 'notifications-page__read-tab--active' : ''
                }`}
                onClick={() => setReadFilter('unread')}
              >
                Chưa đọc
              </button>
            </div>
            <Dropdown menu={{ items: markReadItems }} trigger={['click']} placement="bottomRight">
              <button type="button" className="notifications-page__mark-read">
                Đánh dấu đã đọc <span className="notifications-page__chevron">▼</span>
              </button>
            </Dropdown>
          </div>
        </div>
      </header>

      <Card className="notifications-page__card">
        <div className="notifications-page__list">
          {filteredList.length === 0 ? (
            <div className="notifications-page__empty">Không có thông báo</div>
          ) : (
            filteredList.map((item) => (
              <NotificationRow
                key={item.id}
                item={item}
                onClick={() => handleClick(item.link)}
                showChevron
              />
            ))
          )}
        </div>
      </Card>
    </div>
  );
}

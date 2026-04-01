import { useState, useMemo, useEffect, useCallback } from 'react';
import { useNavigate } from 'react-router-dom';
import { Card, Spin, message } from 'antd';
import { NotificationRow } from '../../../shared/components/NotificationRow';
import { NOTIFICATION_CATEGORY_TABS } from '../../../shared/data/notificationsMockData';
import {
  markAllNotificationsRead,
  markNotificationRead,
  notificationService,
} from '../../../shared/services/notificationService';
import type { NotificationItem, NotificationType } from '../../../shared/types/notification.types';
import './NotificationsPage.css';

export function NotificationsPage() {
  const navigate = useNavigate();
  const [notifications, setNotifications] = useState<NotificationItem[]>([]);
  const [loading, setLoading] = useState(true);
  const [categoryFilter, setCategoryFilter] = useState<'all' | NotificationType>('all');
  const [readFilter, setReadFilter] = useState<'all' | 'unread'>('all');

  const load = useCallback(async () => {
    setLoading(true);
    try {
      const list = await notificationService.list();
      setNotifications(list);
    } catch {
      message.error('Không tải được thông báo. Vui lòng đăng nhập lại hoặc thử sau.');
      setNotifications([]);
    } finally {
      setLoading(false);
    }
  }, []);

  useEffect(() => {
    void load();
  }, [load]);

  useEffect(() => {
    const onChanged = () => void load();
    window.addEventListener('ealds-notifications-changed', onChanged);
    return () => window.removeEventListener('ealds-notifications-changed', onChanged);
  }, [load]);

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

  const handleClick = (item: NotificationItem) => {
    markNotificationRead(Number(item.id));
    void load();
    navigate(item.link);
  };

  const markAllRead = () => {
    const ids = notifications.map((n) => Number(n.id)).filter((x) => !Number.isNaN(x));
    markAllNotificationsRead(ids);
    void load();
  };

  return (
    <div className="notifications-page">
      <header className="notifications-page__header">
        <div className="notifications-page__title-row">
          <h1 className="notifications-page__title">Thông báo</h1>
          <div className="notifications-page__badge-wrap">
            <span className="notifications-page__mini-icon" aria-hidden>
              🔔
            </span>
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
            <button
              type="button"
              className="notifications-page__mark-read"
              onClick={markAllRead}
              disabled={unreadCount === 0}
            >
              Đánh dấu đã đọc
            </button>
          </div>
        </div>
      </header>

      <Card className="notifications-page__card">
        <Spin spinning={loading}>
          <div className="notifications-page__list">
            {!loading && filteredList.length === 0 ? (
              <div className="notifications-page__empty">Không có thông báo</div>
            ) : (
              filteredList.map((item) => (
                <NotificationRow
                  key={item.id}
                  item={item}
                  onClick={() => handleClick(item)}
                  showChevron
                />
              ))
            )}
          </div>
        </Spin>
      </Card>
    </div>
  );
}

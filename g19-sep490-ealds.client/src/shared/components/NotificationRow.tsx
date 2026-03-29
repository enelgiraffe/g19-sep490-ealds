import { getNotificationTypeConfig } from '../services/notificationService';
import type { NotificationItem } from '../types/notification.types';
import './NotificationRow.css';

interface NotificationRowProps {
  item: NotificationItem;
  onClick: () => void;
  /** Optional class for container (e.g. popover vs page list) */
  className?: string;
  /** Show chevron on the right (for module page) */
  showChevron?: boolean;
}

export function NotificationRow({ item, onClick, className = '', showChevron = false }: NotificationRowProps) {
  const config = getNotificationTypeConfig(item.type);
  return (
    <button
      type="button"
      className={`notification-row ${item.read ? 'notification-row--read' : ''} ${className}`.trim()}
      onClick={onClick}
    >
      <span
        className="notification-row__icon"
        style={{ color: config.color }}
        title={config.label}
      >
        {config.icon}
      </span>
      <div className="notification-row__body">
        <div className="notification-row__title">{item.title}</div>
        <div className="notification-row__desc">{item.description}</div>
        <div className="notification-row__time">{item.time}</div>
      </div>
      {showChevron && <span className="notification-row__chevron" aria-hidden>›</span>}
    </button>
  );
}

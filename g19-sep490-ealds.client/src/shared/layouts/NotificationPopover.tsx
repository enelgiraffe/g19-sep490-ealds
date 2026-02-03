import { useNavigate } from 'react-router-dom';
import { NotificationRow } from '../components/NotificationRow';
import { MOCK_NOTIFICATIONS } from '../data/notificationsMockData';
import './NotificationPopover.css';

interface NotificationPopoverProps {
  onClose?: () => void;
}

export function NotificationPopover({ onClose }: NotificationPopoverProps) {
  const navigate = useNavigate();

  const handleClick = (link: string) => {
    navigate(link);
    onClose?.();
  };

  return (
    <div className="notification-popover">
      <div className="notification-popover__header">
        🔔 Notifications ({MOCK_NOTIFICATIONS.length})
      </div>
      <div className="notification-popover__list">
        {MOCK_NOTIFICATIONS.map((item) => (
          <NotificationRow
            key={item.id}
            item={item}
            onClick={() => handleClick(item.link)}
            className="notification-popover__row"
          />
        ))}
      </div>
    </div>
  );
}

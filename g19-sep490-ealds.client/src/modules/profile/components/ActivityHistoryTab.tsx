import { DatePicker, Empty } from 'antd';
import './ActivityHistoryTab.css';

export function ActivityHistoryTab() {
  const activities: { id: number; action: string; time: string }[] = [];

  return (
    <div className="activity-history-tab">
      <div className="activity-history-tab__header">
        <h3 className="activity-history-tab__title">Lịch sử hoạt động</h3>
        <DatePicker placeholder="Thời gian" className="activity-history-tab__date-picker" />
      </div>

      <div className="activity-history-timeline">
        {activities.length === 0 ? (
          <Empty description="Chưa có lịch sử hoạt động" />
        ) : (
          activities.map((activity) => (
            <div key={activity.id} className="timeline-item">
              <div className="timeline-item__marker"></div>
              <div className="timeline-item__content">
                <div className="timeline-item__action">{activity.action}</div>
                <div className="timeline-item__time">{activity.time}</div>
              </div>
            </div>
          ))
        )}
      </div>
    </div>
  );
}

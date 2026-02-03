import type { NotificationItem, NotificationType } from '../types/notification.types';

export const MOCK_NOTIFICATIONS: NotificationItem[] = [
  {
    id: '1',
    type: 'approval_request',
    title: 'Yêu cầu mua sắm từ phòng IT',
    description: 'Chờ phê duyệt',
    time: '2 giờ trước',
    read: false,
    link: '/approval-detail/1',
  },
  {
    id: '2',
    type: 'approval_request',
    title: 'Điều chuyển tài sản',
    description: 'Phòng HR',
    time: '1 ngày trước',
    read: false,
    link: '/approval-detail/2',
  },
  {
    id: '3',
    type: 'maintenance_due',
    title: 'Bảo trì đến hạn',
    description: 'Tài sản: LAP-001',
    time: '3 giờ trước',
    read: false,
    link: '/maintenance',
  },
  {
    id: '4',
    type: 'inventory_confirmation',
    title: 'Xác nhận kiểm kê',
    description: 'Đợt kiểm kê Q1 - Phòng Kế toán',
    time: '5 giờ trước',
    read: true,
    link: '/assets',
  },
  {
    id: '5',
    type: 'overdue_critical',
    title: 'Tài sản quá hạn bảo trì',
    description: 'PC-102 - Cần xử lý gấp',
    time: '1 ngày trước',
    read: false,
    link: '/assets',
  },
];

export const NOTIFICATION_TYPE_CONFIG: Record<
  NotificationType,
  { icon: string; color: string; label: string; labelVi: string }
> = {
  approval_request: { icon: '🟡', color: '#faad14', label: 'Approval request', labelVi: 'Phê duyệt' },
  inventory_confirmation: { icon: '🔵', color: '#1677ff', label: 'Inventory confirmation', labelVi: 'Kiểm kê' },
  maintenance_due: { icon: '🟠', color: '#fa8c16', label: 'Maintenance due', labelVi: 'Bảo trì' },
  overdue_critical: { icon: '🔴', color: '#ff4d4f', label: 'Overdue / critical', labelVi: 'Quá hạn' },
};

/** For category filter tabs */
export const NOTIFICATION_CATEGORY_TABS: { value: 'all' | NotificationType; label: string }[] = [
  { value: 'all', label: 'Tất cả' },
  { value: 'approval_request', label: 'Phê duyệt' },
  { value: 'inventory_confirmation', label: 'Kiểm kê' },
  { value: 'maintenance_due', label: 'Bảo trì' },
  { value: 'overdue_critical', label: 'Quá hạn' },
];

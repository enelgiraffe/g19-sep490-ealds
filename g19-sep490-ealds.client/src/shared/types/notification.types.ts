/** Director-focused types: approval, inventory, maintenance, overdue */
export type NotificationType =
  | 'approval_request'      // 🟡
  | 'inventory_confirmation' // 🔵
  | 'maintenance_due'       // 🟠
  | 'overdue_critical';     // 🔴

export interface NotificationItem {
  id: string;
  type: NotificationType;
  title: string;
  description: string;
  time: string;
  read: boolean;
  /** Path to redirect on click (e.g. /approval-detail/1, /assets/xxx) */
  link: string;
}

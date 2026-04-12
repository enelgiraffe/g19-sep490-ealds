import type { AppRole, SidebarItem } from '../types/layout.types';

/** Common menu items for all users */
export const COMMON_MENU: SidebarItem[] = [
  { key: 'notifications', path: '/notifications', label: 'Thông báo', icon: '/icons/sidebar-notifications.svg' },
];

/** Menu items per role (paths in English) */
export const ROLE_MENU: Record<AppRole, SidebarItem[]> = {
  department_head: [
    { key: 'assets', path: '/assets', label: 'Tài sản', icon: '/icons/sidebar-assets.svg' },
    {
      key: 'purchase-requisitions',
      path: '/purchase-requisitions',
      label: 'Yêu cầu mua',
      icon: '/icons/sidebar-purchase-orders.svg',
    },
    { key: 'transfers', path: '/transfers', label: 'Điều chuyển', icon: '/icons/sidebar-transfers.svg' },
    { key: 'repairs', path: '/repairs', label: 'Sửa chữa', icon: '/icons/sidebar-repairs.svg' },
    { key: 'maintenance', path: '/maintenance', label: 'Bảo trì', icon: '/icons/sidebar-maintenance.svg' },
    { key: 'liquidation', path: '/liquidation', label: 'Thanh lý', icon: '/icons/sidebar-liquidation.svg' },
    { key: 'allocation-requests', path: '/allocation-requests', label: 'Cấp phát & Hoàn trả', icon: '/icons/sidebar-allocations.svg' },
    { key: 'inventory', path: '/inventory', label: 'Kiểm kê', icon: '/icons/sidebar-inventories.svg' },
  ],
  accountant: [
    // Kế toán dùng màn tài sản riêng
    { key: 'assets', path: '/accountant-assets', label: 'Tài sản', icon: '/icons/sidebar-assets.svg' },
    { key: 'liquidation', path: '/liquidation', label: 'Thanh lý', icon: '/icons/sidebar-liquidation.svg' },
    { key: 'cost-recording', path: '/cost-recording', label: 'Ghi nhận chi phí', icon: '/icons/sidebar-cost-recording.svg' },
    { key: 'requests', path: '/requests', label: 'Yêu cầu', icon: '/icons/sidebar-requests.svg' },
    { key: 'purchase-orders', path: '/purchase-orders', label: 'Đơn mua', icon: '/icons/sidebar-purchase-orders.svg' },
    {
      key: 'goods-receipts',
      path: '/goods-receipts',
      label: 'Biên nhận hàng',
      icon: '/icons/sidebar-purchase-orders.svg',
    },
    {
      key: 'supplier-invoices',
      path: '/supplier-invoices',
      label: 'Hóa đơn NCC',
      icon: '/icons/sidebar-purchase-orders.svg',
    },
  ],
  director: [
    { key: 'dashboard', path: '/dashboard', label: 'Dashboard', icon: '/icons/sidebar-dashboard.svg' },
    { key: 'assets', path: '/assets', label: 'Tài sản', icon: '/icons/sidebar-assets.svg' },
    { key: 'requests', path: '/requests', label: 'Yêu cầu', icon: '/icons/sidebar-requests.svg' },
    { key: 'reports', path: '/reports', label: 'Báo cáo', icon: '/icons/sidebar-reports.svg' },
  ],
  admin: [
    { key: 'users', path: '/users', label: 'Người dùng', icon: '/icons/sidebar-users.svg' },
    { key: 'roles', path: '/roles', label: 'Vai trò', icon: '/icons/sidebar-roles.svg' },
    { key: 'departments', path: '/departments', label: 'Phòng ban', icon: '/icons/sidebar-departments.svg' },
    { key: 'categories', path: '/categories', label: 'Danh mục', icon: '/icons/sidebar-categories.svg' },
    { key: 'approval-workflows', path: '/approval-workflows', label: 'Quy trình phê duyệt', icon: '/icons/sidebar-approval-workflows.svg' },
    { key: 'extended-fields', path: '/extended-fields', label: 'Trường mở rộng', icon: '/icons/sidebar-extended-fields.svg' },
    { key: 'system-settings', path: '/system-settings', label: 'Cấu hình hệ thống', icon: '/icons/sidebar-system-settings.svg' },
  ],
};

export const ROLE_OPTIONS = [
  { value: 'department_head' as const, label: 'Trường phòng ban' },
  { value: 'accountant' as const, label: 'Kế toán' },
  { value: 'director' as const, label: 'Giám đốc' },
  { value: 'admin' as const, label: 'Admin' },
];

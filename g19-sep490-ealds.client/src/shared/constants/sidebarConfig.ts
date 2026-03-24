import type { AppRole, SidebarItem } from '../types/layout.types';

/** Common menu items for all users */
export const COMMON_MENU: SidebarItem[] = [
  { key: 'notifications', path: '/notifications', label: 'Thông báo' },
];

/** Menu items per role (paths in English) */
export const ROLE_MENU: Record<AppRole, SidebarItem[]> = {
  department_head: [
    { key: 'assets', path: '/assets', label: 'Tài sản' },
    { key: 'purchase-orders', path: '/purchase-orders', label: 'Đơn mua' },
    { key: 'transfers', path: '/transfers', label: 'Điều chuyển' },
    { key: 'repairs', path: '/repairs', label: 'Sửa chữa' },
    { key: 'maintenance', path: '/maintenance', label: 'Bảo trì' },
    { key: 'liquidation', path: '/liquidation', label: 'Thanh lý' },
    { key: 'inventory', path: '/inventory', label: 'Kiểm kê' },
  ],
  accountant: [
    // Kế toán dùng màn tài sản riêng
    { key: 'assets', path: '/accountant-assets', label: 'Tài sản' },
    { key: 'accountant-inventory', path: '/accountant-inventory', label: 'Kiểm kê' },
    { key: 'transfers', path: '/transfers', label: 'Điều chuyển' },
    { key: 'allocations', path: '/allocations', label: 'Cấp phát-Thu hồi' },
    { key: 'liquidation', path: '/liquidation', label: 'Thanh lý' },
    { key: 'cost-recording', path: '/cost-recording', label: 'Ghi nhận chi phí' },
    { key: 'requests', path: '/requests', label: 'Yêu cầu' },
  ],
  director: [
    { key: 'dashboard', path: '/dashboard', label: 'Dashboard' },
    { key: 'assets', path: '/assets', label: 'Tài sản' },
    { key: 'requests', path: '/requests', label: 'Yêu cầu' },
    { key: 'reports', path: '/reports', label: 'Báo cáo' },
  ],
  admin: [
    { key: 'users', path: '/users', label: 'Người dùng' },
    { key: 'roles', path: '/roles', label: 'Vai trò' },
    { key: 'departments', path: '/departments', label: 'Phòng ban' },
    { key: 'categories', path: '/categories', label: 'Danh mục' },
    { key: 'approval-workflows', path: '/approval-workflows', label: 'Quy trình phê duyệt' },
    { key: 'extended-fields', path: '/extended-fields', label: 'Trường mở rộng' },
    { key: 'system-settings', path: '/system-settings', label: 'Cấu hình hệ thống' },
  ],
};

export const ROLE_OPTIONS = [
  { value: 'department_head' as const, label: 'Trường phòng ban' },
  { value: 'accountant' as const, label: 'Kế toán' },
  { value: 'director' as const, label: 'Giám đốc' },
  { value: 'admin' as const, label: 'Admin' },
];

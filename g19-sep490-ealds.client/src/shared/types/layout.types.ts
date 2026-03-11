export type AppRole =
  | 'department_head'
  | 'accountant'
  | 'director'
  | 'admin';

export interface SidebarItem {
  key: string;
  path: string;
  label: string;
  /** Optional icon path, e.g. '/icons/sidebar-assets.svg' */
  icon?: string;
}

export interface RoleOption {
  value: AppRole;
  label: string;
}

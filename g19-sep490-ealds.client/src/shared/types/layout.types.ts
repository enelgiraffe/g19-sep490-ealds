export type AppRole =
  | 'department_head'
  | 'accountant'
  | 'director'
  | 'admin';

export interface SidebarItem {
  key: string;
  path: string;
  label: string;
}

export interface RoleOption {
  value: AppRole;
  label: string;
}

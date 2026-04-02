import type { AppRole } from '../../../shared/types/layout.types';

export interface LoginFormData {
  email: string;
  password: string;
}

export interface LoginResponse {
  accessToken: string;
  refreshToken?: string;
  user: User;
}

export interface User {
  id: string;
  email: string;
  name: string;
  role: string;
}

export interface AuthError {
  message: string;
  code?: string;
}

/** Map backend role code (e.g. from JWT / login response) to app sidebar role */
export function mapBackendRoleToAppRole(role: string | undefined | null): AppRole {
  const r = (role ?? '').toLowerCase().replace(/\s+/g, '_');
  if (r === 'admin') return 'admin';
  if (r === 'director' || r === 'giám_đốc') return 'director';
  if (r === 'accountant' || r === 'kế_toán') return 'accountant';
  if (r === 'department_head' || r === 'departmenthead' || r === 'dept_head' || r === 'trưởng_phòng')
    return 'department_head';
  return 'department_head';
}

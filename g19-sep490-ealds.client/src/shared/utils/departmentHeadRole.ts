/**
 * Khớp Role.Code trên backend (ví dụ DEPARTMENT_HEAD, DEPARTMENTHEAD — InventoryNotificationService).
 */
export function isDepartmentHeadRoleCode(role: string | null | undefined): boolean {
  const r = String(role ?? '')
    .trim()
    .toUpperCase()
    .replace(/\s+/g, '_');
  return (
    r === 'DEPARTMENT_MANAGER' ||
    r === 'DEPARTMENTMANAGER' ||
    r === 'DEPARTMENT_HEAD' ||
    r === 'DEPARTMENTHEAD' ||
    r === 'HEAD_OF_DEPARTMENT' ||
    r === 'HEADOFDEPARTMENT' ||
    r === 'DEPT_MANAGER' ||
    r === 'DEPTMANAGER' ||
    r === 'DEPT_HEAD' ||
    r === 'DEPTHEAD' ||
    r === 'TRUONG_PHONG' ||
    r === 'TRUONGPHONG'
  );
}

/** Lọc yêu cầu thanh lý cho trưởng phòng: do mình tạo hoặc tài sản thuộc phòng ban mình. */
export function filterDisposalListForDepartmentHead<T extends { createdBy: number; fromDepartmentId: number }>(
  list: T[],
  userId: number,
  departmentId: number | null | undefined,
): T[] {
  const uid = Number(userId);
  const did =
    departmentId != null && departmentId !== undefined && !Number.isNaN(Number(departmentId))
      ? Number(departmentId)
      : null;
  return list.filter((x) => {
    if (Number(x.createdBy) === uid) return true;
    if (did != null && Number(x.fromDepartmentId) === did) return true;
    return false;
  });
}

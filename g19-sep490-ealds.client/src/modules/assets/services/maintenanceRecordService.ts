import { apiClient } from '../../../shared/services/apiClient';

const maintenanceRecordApi = apiClient;

export interface MaintenanceRecordResponse {
  recordId: number;
  taskId: number;
  assetInstanceId: number;
  instanceCode: string;
  executionDate: string;
  totalCost: number;
  workPerformed: string;
  conditionBefore: string;
  conditionAfter: string;
  technicalNote?: string | null;
  status: number;
  /** maintenance (API MaintenanceRecord) hoặc repair (ghép từ API RepairRecord trên client) */
  recordSource?: string | null;
  /** Chỉ lịch sử sửa chữa: tên đơn vị sửa chữa (Supplier). */
  repairUnitName?: string | null;
  /** Bảo hành theo lần sửa chữa (không phải bảo hành tài sản). API: yyyy-MM-dd hoặc null. */
  repairWarrantyStartDate?: string | null;
  repairWarrantyEndDate?: string | null;
  repairWarrantyPeriodValue?: number | null;
  repairWarrantyPeriodUnit?: string | null;
  repairWarrantyConditions?: string | null;
  repairWarrantyNote?: string | null;
}

export function isRepairMaintenanceRecord(record: MaintenanceRecordResponse): boolean {
  return String(record.recordSource ?? '').toLowerCase() === 'repair';
}

/** Ghép lịch sử bảo dưỡng (MaintenanceRecord) và sửa chữa (RepairRecord) cho cùng một bảng trên UI. */
export function mergeMaintenanceAndRepairHistory(
  maintenance: MaintenanceRecordResponse[],
  repairs: Omit<MaintenanceRecordResponse, 'recordSource'>[]
): MaintenanceRecordResponse[] {
  const rows: MaintenanceRecordResponse[] = [
    ...maintenance.map((r) => ({ ...r, recordSource: r.recordSource ?? 'maintenance' })),
    ...repairs.map((r) => ({
      ...r,
      recordSource: 'repair',
    })),
  ];
  rows.sort((a, b) => String(b.executionDate).localeCompare(String(a.executionDate)));
  return rows;
}

export function getMaintenanceRecordStatusLabel(status: number): string {
  switch (status) {
    case 1:
      return 'Hoàn thành';
    case 2:
      return 'Thất bại';
    case 3:
      return 'Một phần';
    default:
      return '—';
  }
}

export const maintenanceRecordService = {
  async getByAssetId(assetId: number): Promise<MaintenanceRecordResponse[]> {
    const response = await maintenanceRecordApi.get<MaintenanceRecordResponse[]>(
      `/api/MaintenanceRecord/asset/${assetId}`
    );
    return response.data;
  },

  async getByInstanceId(instanceId: number): Promise<MaintenanceRecordResponse[]> {
    const response = await maintenanceRecordApi.get<MaintenanceRecordResponse[]>(
      `/api/MaintenanceRecord/instance/${instanceId}`
    );
    return response.data;
  },
};

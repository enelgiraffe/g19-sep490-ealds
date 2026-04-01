import { directorApi } from '../../requests/services/directorRequestService';
import type { AssetStatusItem, KPISummary, PendingApprovalRow } from '../types/dashboard.types';

export interface DirectorDashboardSummary {
  kpi: KPISummary;
  pendingPreview: PendingPreviewRow[];
  assetStatusBreakdown: AssetStatusItem[];
}

/** API row before mapping display date */
export interface PendingPreviewRow {
  id: string;
  requestType: string;
  department: string;
  createDate: string;
  status: string;
}

function mapPendingRow(row: PendingPreviewRow): PendingApprovalRow {
  const d = new Date(row.createDate);
  const date = Number.isNaN(d.getTime()) ? '—' : d.toLocaleDateString('vi-VN');
  return {
    id: row.id,
    requestType: row.requestType,
    department: row.department,
    date,
    status: row.status,
  };
}

export const directorDashboardService = {
  async getSummary(): Promise<{
    kpi: KPISummary;
    pendingApprovals: PendingApprovalRow[];
    assetStatus: AssetStatusItem[];
  }> {
    const { data } = await directorApi.get<DirectorDashboardSummary>('/api/dashboard/director');
    return {
      kpi: data.kpi,
      pendingApprovals: (data.pendingPreview ?? []).map(mapPendingRow),
      assetStatus: data.assetStatusBreakdown ?? [],
    };
  },
};

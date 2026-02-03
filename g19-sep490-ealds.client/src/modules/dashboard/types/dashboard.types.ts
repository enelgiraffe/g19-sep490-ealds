export interface KPISummary {
  totalAssets: number;
  totalAssetValue: number; // billion VND or unit
  pendingApprovals: number;
  assetsDueMaintenance: number;
}

export interface PendingApprovalRow {
  id: string;
  requestType: string;
  department: string;
  date: string;
  status: string;
}

export interface AssetStatusItem {
  name: string;
  value: number;
  color?: string;
}

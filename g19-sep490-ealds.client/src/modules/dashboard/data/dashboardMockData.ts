import type { KPISummary, PendingApprovalRow, AssetStatusItem } from '../types/dashboard.types';

export const MOCK_KPI: KPISummary = {
  totalAssets: 1240,
  totalAssetValue: 45.2,
  pendingApprovals: 8,
  assetsDueMaintenance: 23,
};

export const MOCK_PENDING_APPROVALS: PendingApprovalRow[] = [
  { id: '1', requestType: 'Mua sắm', department: 'IT', date: '01/02', status: 'Pending' },
  { id: '2', requestType: 'Điều chuyển', department: 'HR', date: '30/01', status: 'Pending' },
  { id: '3', requestType: 'Thanh lý', department: 'Kế toán', date: '29/01', status: 'Pending' },
  { id: '4', requestType: 'Mua sắm', department: 'Hành chính', date: '28/01', status: 'Pending' },
  { id: '5', requestType: 'Điều chuyển', department: 'IT', date: '27/01', status: 'Pending' },
  { id: '6', requestType: 'Sửa chữa', department: 'Vận hành', date: '26/01', status: 'Pending' },
  { id: '7', requestType: 'Mua sắm', department: 'Kế toán', date: '25/01', status: 'Pending' },
];

export const MOCK_ASSET_STATUS: AssetStatusItem[] = [
  { name: 'Đang sử dụng', value: 620, color: '#1677ff' },
  { name: 'Nhàn rỗi', value: 310, color: '#52c41a' },
  { name: 'Đang sửa chữa', value: 186, color: '#faad14' },
  { name: 'Thanh lý', value: 124, color: '#ff4d4f' },
];

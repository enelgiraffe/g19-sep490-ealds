import type { MaintenanceRecordResponse } from './maintenanceRecordService';
import { apiClient } from '../../../shared/services/apiClient';

const repairRecordApi = apiClient;

/** Cùng dạng với một dòng lịch sử bảo dưỡng trên UI; nguồn là bảng RepairRecord. */
export type RepairHistoryRecordResponse = Omit<MaintenanceRecordResponse, 'recordSource'>;

export const repairRecordService = {
  async getByAssetId(assetId: number): Promise<RepairHistoryRecordResponse[]> {
    const response = await repairRecordApi.get<RepairHistoryRecordResponse[]>(
      `/api/RepairRecord/asset/${assetId}`
    );
    return response.data;
  },

  async getByInstanceId(instanceId: number): Promise<RepairHistoryRecordResponse[]> {
    const response = await repairRecordApi.get<RepairHistoryRecordResponse[]>(
      `/api/RepairRecord/instance/${instanceId}`
    );
    return response.data;
  },
};

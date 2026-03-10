import axios from 'axios';

const API_BASE_URL = import.meta.env.VITE_API_BASE_URL || '';

const maintenanceApi = axios.create({
  baseURL: API_BASE_URL,
  headers: {
    'Content-Type': 'application/json',
  },
});

maintenanceApi.interceptors.request.use((config) => {
  const token = localStorage.getItem('accessToken');
  if (token) {
    config.headers = config.headers ?? {};
    config.headers.Authorization = `Bearer ${token}`;
  }
  return config;
});

/** Payload gửi đề xuất bảo dưỡng (Trưởng phòng ban) */
export interface MaintenanceRequestPayload {
  assetId: number;
  requestTypeId: number;
  createdBy: number;
  title?: string | null;
  description?: string | null;
  scheduleId?: number | null;
  plannedDate?: string | null;
  assignTo?: number;
  address?: string | null;
}

export interface MaintenanceRequestResponse {
  assetRequestId: number;
  taskId: number;
}

/** RequestTypeId cho loại "Bảo dưỡng" - cần trùng với bản ghi trong bảng RequestType */
const MAINTENANCE_REQUEST_TYPE_ID = 2;

export const maintenanceRequestService = {
  /**
   * Gửi đề xuất bảo dưỡng máy móc (POST api/Assets/Requests/maintenance).
   * ScheduleId: gửi 0 nếu không theo lịch định kỳ (tránh lỗi validate nếu backend đang dùng int).
   */
  async create(payload: MaintenanceRequestPayload): Promise<MaintenanceRequestResponse> {
    const body = {
      assetId: payload.assetId,
      requestTypeId: payload.requestTypeId ?? MAINTENANCE_REQUEST_TYPE_ID,
      createdBy: payload.createdBy,
      title: payload.title ?? null,
      description: payload.description ?? null,
      scheduleId: payload.scheduleId ?? 0,
      plannedDate: payload.plannedDate ?? null,
      assignTo: payload.assignTo ?? 0,
      address: payload.address ?? null,
    };
    const response = await maintenanceApi.post<MaintenanceRequestResponse>(
      '/api/Assets/Requests/maintenance',
      body
    );
    return response.data;
  },
};

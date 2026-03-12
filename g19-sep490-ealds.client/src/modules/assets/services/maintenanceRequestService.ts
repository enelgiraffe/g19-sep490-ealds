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

export interface MaintenanceRequestListItemDTO {
  recordId: number;
  assetRequestId: number;
  code: string;
  transferDate: string;
  assetCode: string;
  assetName: string;
  fromDepartment: string;
  toDepartment: string;
  quantity: number;
  status: number;
  statusName: string;
  reason?: string | null;
}

/** RequestTypeId cho loại "Bảo dưỡng" - cần trùng với bản ghi trong bảng RequestType */
const MAINTENANCE_REQUEST_TYPE_ID = 2;

export const maintenanceRequestService = {
  /**
   * Gửi đề xuất bảo dưỡng máy móc (POST api/Assets/Requests/maintenance).
   * ScheduleId: để null/bỏ trống nếu không theo lịch định kỳ.
   */
  async create(payload: MaintenanceRequestPayload): Promise<MaintenanceRequestResponse> {
    const body = {
      assetId: payload.assetId,
      requestTypeId: payload.requestTypeId ?? MAINTENANCE_REQUEST_TYPE_ID,
      createdBy: payload.createdBy,
      title: payload.title ?? null,
      description: payload.description ?? null,
      // Nếu không có lịch định kỳ thì để null để backend hiểu là ad-hoc, tránh FK lỗi với ScheduleId = 0.
      scheduleId: payload.scheduleId ?? null,
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

  /**
   * Lấy danh sách yêu cầu bảo dưỡng cho "Tài sản cần bảo dưỡng".
   * Backend trả dạng TransferRequestListItemDTO.
   */
  async list(): Promise<MaintenanceRequestListItemDTO[]> {
    const response = await maintenanceApi.get<MaintenanceRequestListItemDTO[]>(
      '/api/Assets/Requests/maintenance/list'
    );
    return response.data;
  },
};

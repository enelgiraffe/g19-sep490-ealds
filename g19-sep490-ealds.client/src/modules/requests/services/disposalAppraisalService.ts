import axios from 'axios';

const API_BASE_URL = import.meta.env.VITE_API_BASE_URL || '';

const appraisalApi = axios.create({
  baseURL: API_BASE_URL,
  headers: {
    'Content-Type': 'application/json',
  },
});

appraisalApi.interceptors.request.use((config) => {
  const token = localStorage.getItem('accessToken');
  if (token) {
    config.headers = config.headers ?? {};
    config.headers.Authorization = `Bearer ${token}`;
  }
  return config;
});

export interface DisposalAppraisalListItem {
  appraisalId: number;
  assetRequestId: number;
  requestTitle: string;
  requestStatus: number;
  requestCreateDate: string;
  scheduledAt?: string | null;
  meetingLocation?: string | null;
  meetingDepartmentId?: number | null;
  meetingDepartmentName?: string | null;
  status: number;
  isReporter: boolean;
  isRelatedMember: boolean;
  hasReport: boolean;
}

export interface DisposalAppraisalMemberItem {
  appraisalMemberId: number;
  userId: number;
  memberName: string;
  memberRole?: string | null;
  isReporter: boolean;
  decision: number;
  rejectReason?: string | null;
  decisionDate?: string | null;
}

export interface DisposalAppraisalReport {
  appraisalReportId?: number | null;
  minutesNo?: string | null;
  meetingDate?: string | null;
  appraisedValue?: number | null;
  marketReferenceValue?: number | null;
  appraisalMethod?: string | null;
  appraisedValueInWords?: string | null;
  appraisalOutcome?: string | null;
  summary?: string | null;
  recommendation?: string | null;
  attachmentUrls?: string | null;
  submittedBy?: number | null;
  submittedDate?: string | null;
}

export interface DisposalAppraisalDetail {
  appraisalId: number;
  assetRequestId: number;
  requestTitle: string;
  requestStatus: number;
  requestCreateDate: string;
  scheduledAt?: string | null;
  meetingLocation?: string | null;
  meetingDepartmentId?: number | null;
  meetingDepartmentName?: string | null;
  status: number;
  reporterUserId?: number | null;
  isReporter: boolean;
  isRelatedMember: boolean;
  canManageCommittee: boolean;
  report?: DisposalAppraisalReport | null;
  members: DisposalAppraisalMemberItem[];
}

export const disposalAppraisalService = {
  async getMyAppraisals(userId: number): Promise<DisposalAppraisalListItem[]> {
    const response = await appraisalApi.get<DisposalAppraisalListItem[]>(
      '/api/Assets/Requests/disposal/appraisals/my',
      { params: { userId } },
    );
    return response.data;
  },

  /** Danh sách toàn bộ đợt thẩm định (chỉ tài khoản giám đốc). */
  async getDirectorAppraisals(userId: number): Promise<DisposalAppraisalListItem[]> {
    const response = await appraisalApi.get<DisposalAppraisalListItem[]>(
      '/api/Assets/Requests/disposal/appraisals/director',
      { params: { userId } },
    );
    return response.data;
  },

  async getDetail(appraisalId: number, userId: number): Promise<DisposalAppraisalDetail> {
    const response = await appraisalApi.get<DisposalAppraisalDetail>(
      `/api/Assets/Requests/disposal/appraisals/${appraisalId}`,
      { params: { userId } },
    );
    return response.data;
  },

  async getByAssetRequest(assetRequestId: number, userId: number): Promise<DisposalAppraisalDetail> {
    const response = await appraisalApi.get<DisposalAppraisalDetail>(
      `/api/Assets/Requests/disposal/appraisals/by-request/${assetRequestId}`,
      { params: { userId } },
    );
    return response.data;
  },

  async create(payload: {
    userId: number;
    assetRequestId: number;
    scheduledAt?: string | null;
    meetingLocation?: string | null;
    meetingDepartmentId?: number | null;
    reporterUserId?: number | null;
  }): Promise<DisposalAppraisalDetail> {
    const response = await appraisalApi.post<DisposalAppraisalDetail>(
      '/api/Assets/Requests/disposal/appraisals',
      payload,
    );
    return response.data;
  },

  async update(
    appraisalId: number,
    payload: {
      userId: number;
      scheduledAt?: string | null;
      meetingLocation?: string | null;
      meetingDepartmentId?: number | null;
      reporterUserId?: number | null;
    },
  ): Promise<DisposalAppraisalDetail> {
    const response = await appraisalApi.put<DisposalAppraisalDetail>(
      `/api/Assets/Requests/disposal/appraisals/${appraisalId}`,
      payload,
    );
    return response.data;
  },

  async addMember(
    appraisalId: number,
    payload: {
      userId: number;
      memberUserId: number;
      memberRole?: string | null;
      setAsReporter?: boolean;
    },
  ): Promise<DisposalAppraisalDetail> {
    const response = await appraisalApi.post<DisposalAppraisalDetail>(
      `/api/Assets/Requests/disposal/appraisals/${appraisalId}/members`,
      payload,
    );
    return response.data;
  },

  async removeMember(appraisalId: number, appraisalMemberId: number, userId: number): Promise<DisposalAppraisalDetail> {
    const response = await appraisalApi.delete<DisposalAppraisalDetail>(
      `/api/Assets/Requests/disposal/appraisals/${appraisalId}/members/${appraisalMemberId}`,
      { params: { userId } },
    );
    return response.data;
  },

  async saveReport(
    appraisalId: number,
    payload: {
      userId: number;
      minutesNo?: string | null;
      meetingDate?: string | null;
      appraisedValue?: number | null;
      marketReferenceValue?: number | null;
      appraisalMethod?: string | null;
      appraisedValueInWords?: string | null;
      appraisalOutcome?: string | null;
      summary?: string | null;
      recommendation?: string | null;
      attachmentUrls?: string | null;
    },
  ): Promise<{ appraisalId: number; status: number }> {
    const response = await appraisalApi.post<{ appraisalId: number; status: number }>(
      `/api/Assets/Requests/disposal/appraisals/${appraisalId}/report`,
      payload,
    );
    return response.data;
  },

  async saveDecision(
    appraisalId: number,
    payload: { userId: number; decision: 1 | 2; rejectReason?: string | null },
  ): Promise<{ appraisalId: number; decision: number; status: number }> {
    const response = await appraisalApi.post<{ appraisalId: number; decision: number; status: number }>(
      `/api/Assets/Requests/disposal/appraisals/${appraisalId}/decision`,
      payload,
    );
    return response.data;
  },
};


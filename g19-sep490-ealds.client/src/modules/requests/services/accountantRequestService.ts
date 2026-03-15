import axios from 'axios';

const API_BASE_URL = import.meta.env.VITE_API_BASE_URL || '';

const accountantApi = axios.create({
  baseURL: API_BASE_URL,
  headers: {
    'Content-Type': 'application/json',
  },
});

accountantApi.interceptors.request.use((config) => {
  const token = localStorage.getItem('accessToken');
  if (token) {
    config.headers = config.headers ?? {};
    config.headers.Authorization = `Bearer ${token}`;
  }
  return config;
});

// These IDs must match App:PurchaseRequestTypeId and App:TransferRequestTypeId on the backend
export const PURCHASE_REQUEST_TYPE_ID = 1;

export interface AccountantRequestListItem {
  assetRequestId: number;
  title: string;
  status: number;
  requestTypeId: number;
  userId: number;
  createDate: string;
}

interface AccountantRequestListResponse {
  items: AccountantRequestListItem[];
  total: number;
  page: number;
  pageSize: number;
  totalPages: number;
}

export const accountantRequestService = {
  async getPurchaseRequests(): Promise<AccountantRequestListItem[]> {
    const response = await accountantApi.get<AccountantRequestListResponse>(
      '/api/Assets/Requests/accountant/view',
      {
        params: {
          requestTypeIds: PURCHASE_REQUEST_TYPE_ID,
          page: 1,
          pageSize: 200,
        },
      },
    );
    return response.data.items;
  },
};


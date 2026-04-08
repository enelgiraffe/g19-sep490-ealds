import axios from 'axios';
import type { NotificationItem, NotificationType } from '../types/notification.types';
import { NOTIFICATION_TYPE_CONFIG } from '../data/notificationsMockData';

const API_BASE_URL = import.meta.env.VITE_API_BASE_URL || '';

const notificationsApi = axios.create({
  baseURL: API_BASE_URL,
  headers: { 'Content-Type': 'application/json' },
});

notificationsApi.interceptors.request.use((config) => {
  const token = localStorage.getItem('accessToken');
  if (token) {
    config.headers = config.headers ?? {};
    config.headers.Authorization = `Bearer ${token}`;
  }
  return config;
});

export interface ApiNotification {
  notificationId: number;
  title: string;
  content: string | null;
  refId: number | null;
  sentDate: string;
  isSend: boolean;
}

const READ_IDS_KEY = 'ealds-read-notification-ids';

function getReadIdSet(): Set<string> {
  try {
    const raw = localStorage.getItem(READ_IDS_KEY);
    if (!raw) return new Set();
    const arr = JSON.parse(raw) as string[];
    return new Set(Array.isArray(arr) ? arr : []);
  } catch {
    return new Set();
  }
}

function saveReadIdSet(set: Set<string>) {
  localStorage.setItem(READ_IDS_KEY, JSON.stringify([...set]));
}

export function markNotificationRead(notificationId: number) {
  const s = getReadIdSet();
  s.add(String(notificationId));
  saveReadIdSet(s);
  window.dispatchEvent(new Event('ealds-notifications-changed'));
}

export function markAllNotificationsRead(ids: number[]) {
  const s = getReadIdSet();
  ids.forEach((id) => s.add(String(id)));
  saveReadIdSet(s);
  window.dispatchEvent(new Event('ealds-notifications-changed'));
}

function inferType(title: string): NotificationType {
  const t = title.toLowerCase();
  if (t.includes('phê duyệt') || t.includes('yeu cau') || t.includes('yêu cầu')) return 'approval_request';
  if (t.includes('sửa chữa') || t.includes('sua chua')) return 'approval_request';
  if (t.includes('bảo trì') || t.includes('bao tri')) return 'maintenance_due';
  if (t.includes('quá hạn') || t.includes('qua han')) return 'overdue_critical';
  return 'inventory_confirmation';
}

/** Map server row → UI row (read state from localStorage until DB supports IsRead). */
export function mapApiToNotificationItem(n: ApiNotification): NotificationItem {
  const read = getReadIdSet().has(String(n.notificationId));
  return {
    id: String(n.notificationId),
    type: inferType(n.title),
    title: n.title,
    description: n.content ?? '',
    time: formatSentDate(n.sentDate),
    read,
    link: inferNotificationLink(n.title, n.content, n.refId),
  };
}

/** Matches backend RequestTypeId → tab key on <RequestsPage /> */
const REQUEST_TAB_BY_TYPE_ID: Record<number, string> = {
  1: 'purchase',
  2: 'maintenance',
  3: 'transfer',
  4: 'repair',
  5: 'liquidation',
  6: 'allocation',
  7: 'handover',
};

function normalizeNotificationContent(content: string | null): string {
  if (content == null) return '';
  return content.replace(/^\uFEFF/, '').trim();
}

/** Asset-request rows use "Loại #typeId" in content; RefId is the sender user id — never use it as inventory session id. */
function requestTabFromLoaiContent(contentNorm: string): string | null {
  const m = contentNorm.match(/Loại\s*#\s*(\d+)/i);
  if (!m) return null;
  const typeId = Number.parseInt(m[1], 10);
  if (!Number.isFinite(typeId)) return null;
  return REQUEST_TAB_BY_TYPE_ID[typeId] ?? 'purchase';
}

/**
 * RefId is only a session id for inventory notifications. For asset requests it points at User (creator).
 */
function isInventoryNotification(title: string, contentNorm: string): boolean {
  const t = title.toLowerCase();
  const c = contentNorm.toLowerCase();
  if (t.includes('kiểm kê') || t.includes('kiem ke')) return true;
  if (c.startsWith('kk ') || /\bkk\s+/i.test(contentNorm)) return true;
  if (t.includes('phiên') && (t.includes('kiểm') || t.includes('kiem'))) return true;
  if (t.includes('lịch kiểm') || t.includes('lich kiem')) return true;
  return false;
}

function inferNotificationLink(title: string, content: string | null, refId: number | null): string {
  const contentNorm = normalizeNotificationContent(content);

  // Asset-request workflow (server: AssetRequestNotificationService) — match Loại # anywhere (not only line start).
  const tabFromLoai = requestTabFromLoaiContent(contentNorm);
  if (tabFromLoai != null) {
    if (tabFromLoai === 'repair') return '/repairs';
    return `/requests?tab=${tabFromLoai}`;
  }

  // Legacy notifications whose title still contains "YC #123"
  const ycMatch = title.match(/YC\s*#\s*(\d+)/i);
  if (ycMatch) {
    const typeIdMatch = contentNorm.match(/Loại\s*#\s*(\d+)/i);
    const typeId = typeIdMatch ? Number.parseInt(typeIdMatch[1], 10) : Number.NaN;
    const tab =
      Number.isFinite(typeId) && REQUEST_TAB_BY_TYPE_ID[typeId]
        ? REQUEST_TAB_BY_TYPE_ID[typeId]
        : 'purchase';
    if (tab === 'repair') return '/repairs';
    return `/requests?tab=${tab}`;
  }

  const t = title.toLowerCase();
  const c = contentNorm.toLowerCase();

  // Inventory cancel: RefId intentionally null (server); still link to inventory list.
  if (refId == null) {
    if (isInventoryNotification(title, contentNorm) || (t.includes('hủy') && (t.includes('kiểm') || t.includes('kiem')))) {
      return '/inventory';
    }
    return '/notifications';
  }

  if (!isInventoryNotification(title, contentNorm)) {
    // refId present but not inventory — likely legacy/misclassified; avoid /inventory/:userId
    return '/notifications';
  }

  // Director: confirm session after department head report
  if (t.includes('chờ xác nhận') || t.includes('cho xac nhan')) {
    return `/inventory-review/${refId}`;
  }

  // Department head: director confirmed with book discrepancies → review / sổ
  const headNeedsReviewAfterDirector =
    (t.includes('đã xác nhận kiểm kê') || t.includes('da xac nhan kiem ke')) &&
    (c.includes('chênh lệch') ||
      c.includes('chenh lech') ||
      c.includes('chờ xử lý') ||
      c.includes('cho xu ly') ||
      c.includes('trên sổ') ||
      c.includes('tren so'));
  if (headNeedsReviewAfterDirector) {
    return `/inventory-review/${refId}`;
  }

  if (t.includes('xử lý chênh lệch') || t.includes('xu ly chenh lech')) {
    return `/inventory-review/${refId}`;
  }

  // Scheduled arrival, execution, recheck, or confirmed with no further book work
  return `/inventory/${refId}`;
}

function formatSentDate(iso: string): string {
  try {
    const d = new Date(iso);
    if (Number.isNaN(d.getTime())) return iso;
    return d.toLocaleString('vi-VN', { dateStyle: 'short', timeStyle: 'short' });
  } catch {
    return iso;
  }
}

async function fetchNotifications(take: number): Promise<NotificationItem[]> {
  const res = await notificationsApi.get<ApiNotification[]>('/api/notifications', {
    params: { take },
  });
  const rows = res.data ?? [];
  return rows.map(mapApiToNotificationItem);
}

export const notificationService = {
  list: (take = 200) => fetchNotifications(take),

  async getUnreadCount(): Promise<number> {
    const items = await fetchNotifications(200);
    return items.filter((i) => !i.read).length;
  },
};

/** Safe config lookup for NotificationRow when type is narrowed */
export function getNotificationTypeConfig(type: NotificationType) {
  return NOTIFICATION_TYPE_CONFIG[type] ?? NOTIFICATION_TYPE_CONFIG.inventory_confirmation;
}

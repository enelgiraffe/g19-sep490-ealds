import type {
  AssetInstanceResponse,
  DepreciationPolicyItem,
  DepreciationRecordItem,
} from '../services/assetService';

export function monthDiff(fromDate: Date, toDate: Date): number {
  const diff =
    (toDate.getFullYear() - fromDate.getFullYear()) * 12 + (toDate.getMonth() - fromDate.getMonth());
  return Math.max(0, diff);
}

export function roundAwayFromZero(value: number): number {
  if (!Number.isFinite(value)) return 0;
  if (value === 0) return 0;
  return Math.sign(value) * Math.round(Math.abs(value));
}

/** Ngày đầu tháng của kỳ khấu hao (cùng logic trang chỉnh sửa cá thể). */
export function getDepreciationPeriodDate(value: string): Date | null {
  if (!value.trim()) return null;
  const date = new Date(value);
  if (Number.isNaN(date.getTime())) return null;
  return new Date(date.getFullYear(), date.getMonth(), 1);
}

export function toDepreciationPeriodInput(value?: string | null): string {
  if (!value) return '';
  const raw = String(value);
  if (/^\d{4}-\d{2}-\d{2}$/.test(raw)) return raw;
  const d = new Date(raw);
  if (Number.isNaN(d.getTime())) return '';
  const pad = (n: number) => String(n).padStart(2, '0');
  return `${d.getFullYear()}-${pad(d.getMonth() + 1)}-${pad(d.getDate())}`;
}

export type DepreciationPreviewResult = {
  amount: number;
  accumulated: number;
  remainingValue: number;
  remainingLifeMonths: number;
};

/**
 * Xem trước khấu hao đường thẳng (đồng bộ với useMemo depreciationPreview ở AssetInstanceEditPage).
 */
export function computeDepreciationPreview(args: {
  policyUsefulLifeMonths: number;
  policySalvageValue: number;
  depreciationPeriodYyyyMmDd: string;
  originalPrice: number;
  inUseDate?: string | null;
  purchaseDate?: string | null;
  depreciationRecords: DepreciationRecordItem[];
}): DepreciationPreviewResult | null {
  const policy = args.policyUsefulLifeMonths;
  const periodDate = getDepreciationPeriodDate(args.depreciationPeriodYyyyMmDd.trim());
  if (!periodDate || policy <= 0) return null;

  const records = [...args.depreciationRecords]
    .filter((r) => {
      const recordDate = getDepreciationPeriodDate(String(r.period));
      return recordDate != null && recordDate < periodDate;
    })
    .sort((a, b) => String(a.period).localeCompare(String(b.period)));
  const lastRecord = records.length > 0 ? records[records.length - 1] : null;

  const openingValue = lastRecord?.remainingValue ?? Number(args.originalPrice || 0);
  const salvageValue = Number(args.policySalvageValue || 0);
  const inUseBase =
    getDepreciationPeriodDate(String(args.inUseDate ?? '')) ??
    getDepreciationPeriodDate(String(args.purchaseDate ?? ''));
  const elapsedMonths = inUseBase ? monthDiff(inUseBase, periodDate) : 0;
  const remainingMonths = Math.max(1, policy - elapsedMonths);

  const depreciableBase = openingValue - salvageValue;
  const monthly = depreciableBase > 0 ? roundAwayFromZero(depreciableBase / remainingMonths) : 0;
  const maxAllowed = Math.max(0, openingValue - salvageValue);
  const amount = Math.min(monthly, maxAllowed);
  const accumulated = (lastRecord?.accumulatedDepreciation ?? 0) + amount;
  const remainingValue = openingValue - amount;
  const remainingLifeMonths = Math.max(0, policy - elapsedMonths - 1);

  return {
    amount,
    accumulated,
    remainingValue,
    remainingLifeMonths,
  };
}

/**
 * Giá trị hiển thị cho "Giá trị hiện tại" (ưu tiên snapshot API, sau đó preview khấu hao / bản ghi mới nhất).
 * Payload instances từ GET /api/assets/{id} cần đủ policy + depreciationRecords như AssetInstanceService.ToDto để khớp chi tiết.
 */
export function resolveInstanceDisplayedRemainingValue(
  instance: AssetInstanceResponse,
  depreciationPolicies?: readonly DepreciationPolicyItem[] | null
): number | null {
  if (instance.remainingValue != null) return instance.remainingValue;

  const embedded = instance.depreciationRecords ?? [];
  const sortedAsc = [...embedded].sort((a, b) => String(a.period).localeCompare(String(b.period)));
  const earliestDepRecord = sortedAsc[0];
  const latest = embedded.length === 0 ? null : sortedAsc[sortedAsc.length - 1];

  const periodRaw =
    earliestDepRecord?.period ??
    instance.depreciationPeriod ??
    instance.inUseDate ??
    instance.purchaseDate ??
    null;
  const periodInput = toDepreciationPeriodInput(periodRaw);

  const policyRow =
    depreciationPolicies?.find((p) => p.policyId === instance.depreciationPolicyId) ?? null;
  const usefulLife = policyRow?.usefullLifeMonths ?? instance.depreciationUsefulLifeMonths ?? 0;
  const salvage = Number(policyRow?.salvageValue ?? instance.depreciationSalvageValue ?? 0);

  const preview =
    instance.isFixedAsset === true &&
    usefulLife > 0 &&
    periodInput.trim().length > 0
      ? computeDepreciationPreview({
          policyUsefulLifeMonths: usefulLife,
          policySalvageValue: salvage,
          depreciationPeriodYyyyMmDd: periodInput,
          originalPrice: Number(instance.originalPrice ?? 0),
          inUseDate: instance.inUseDate,
          purchaseDate: instance.purchaseDate,
          depreciationRecords: embedded,
        })
      : null;

  return preview?.remainingValue ?? latest?.remainingValue ?? null;
}

export function formatDepreciationPolicySelectLabel(
  name: string,
  usefullLifeMonths: number
): string {
  return `${name} (${usefullLifeMonths} tháng)`;
}

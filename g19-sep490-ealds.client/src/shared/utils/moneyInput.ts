/** Giống RepairCompleteModal: chỉ giữ chữ số → số nguyên (VND). */
export function parseIntegerMoneyInput(value: string): number | undefined {
  const normalized = value.replace(/[^\d]/g, '');
  if (!normalized) return undefined;
  const parsed = Number(normalized);
  return Number.isFinite(parsed) ? parsed : undefined;
}

import { useEffect, useMemo, useState, type FormEvent } from 'react';
import { Link, useNavigate, useParams } from 'react-router-dom';
import { message } from 'antd';
import {
  assetService,
  assetInstanceService,
  type AssetInstanceResponse,
  type GuaranteeItem,
  type UpdateAssetInstancePayload,
} from '../services/assetService';
import { profileService } from '../../profile/services/profileService';
import { mapBackendRoleToAppRole } from '../../auth/types/auth.types';
import './AssetCreatePage.css';

function toDateOnly(value: string): string | undefined {
  const trimmed = value.trim();
  if (!trimmed) return undefined;
  return trimmed.split('T')[0] ?? undefined;
}

function toDateInput(value?: string | null): string {
  if (!value) return '';
  const raw = String(value);
  if (/^\d{4}-\d{2}-\d{2}$/.test(raw)) return raw;
  const d = new Date(raw);
  if (Number.isNaN(d.getTime())) return '';
  const pad = (n: number) => String(n).padStart(2, '0');
  return `${d.getFullYear()}-${pad(d.getMonth() + 1)}-${pad(d.getDate())}`;
}

function computeWarrantyEndDate(startDate: string, periodValue: string, periodUnit: string): string {
  if (!startDate.trim() || !periodValue.trim() || !periodUnit.trim()) return '';
  const period = Number(periodValue);
  if (!Number.isFinite(period) || period <= 0) return '';
  const base = new Date(startDate);
  if (Number.isNaN(base.getTime())) return '';
  const result = new Date(base);
  if (periodUnit === 'day') result.setDate(result.getDate() + period);
  else if (periodUnit === 'week') result.setDate(result.getDate() + period * 7);
  else if (periodUnit === 'month') result.setMonth(result.getMonth() + period);
  else if (periodUnit === 'year') result.setFullYear(result.getFullYear() + period);
  else return '';
  const pad = (n: number) => String(n).padStart(2, '0');
  return `${result.getFullYear()}-${pad(result.getMonth() + 1)}-${pad(result.getDate())}`;
}

function parseNumberInput(value: string): number | undefined {
  const normalized = value.replace(/[^\d]/g, '');
  if (!normalized) return undefined;
  const parsed = Number(normalized);
  return Number.isFinite(parsed) ? parsed : undefined;
}

function formatMoneyInput(value?: number | null): string {
  if (value == null) return '';
  return value.toLocaleString('en-US');
}

const EXTERNAL_WARRANTY_CODE_PREFIX = 'Mã BH ngoài:';

function splitWarrantyConditions(raw?: string | null): { externalCode: string; details: string } {
  const source = String(raw ?? '').trim();
  if (!source) return { externalCode: '', details: '' };
  const [firstLine, ...rest] = source.split('\n');
  if (!firstLine?.trim().startsWith(EXTERNAL_WARRANTY_CODE_PREFIX)) {
    return { externalCode: '', details: source };
  }
  const externalCode = firstLine.replace(EXTERNAL_WARRANTY_CODE_PREFIX, '').trim();
  const details = rest.join('\n').trim();
  return { externalCode, details };
}

function buildWarrantyConditions(externalCode: string, details: string): string | null {
  const code = externalCode.trim();
  const content = details.trim();
  if (!code && !content) return null;
  if (code && content) return `${EXTERNAL_WARRANTY_CODE_PREFIX} ${code}\n${content}`;
  if (code) return `${EXTERNAL_WARRANTY_CODE_PREFIX} ${code}`;
  return content;
}

function getLatestGuarantee(instance: AssetInstanceResponse): GuaranteeItem | null {
  if (!instance.guarantees || instance.guarantees.length === 0) return null;
  return [...instance.guarantees].sort((a, b) =>
    String(a.warrantyEndDate ?? '').localeCompare(String(b.warrantyEndDate ?? ''))
  )[instance.guarantees.length - 1] ?? null;
}

export function AssetInstanceEditPage() {
  const navigate = useNavigate();
  const { instanceId } = useParams<{ instanceId: string }>();
  const parsedInstanceId = Number(instanceId);

  const [loading, setLoading] = useState(true);
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [instance, setInstance] = useState<AssetInstanceResponse | null>(null);
  const [isAccountant, setIsAccountant] = useState(false);

  const [serialNumber, setSerialNumber] = useState('');
  const [contractNo, setContractNo] = useState('');
  const [purchaseDate, setPurchaseDate] = useState('');
  const [originalPriceInput, setOriginalPriceInput] = useState('');
  const [currentValueInput, setCurrentValueInput] = useState('');
  const [condition, setCondition] = useState('');
  const [note, setNote] = useState('');

  const [warrantyPeriodValue, setWarrantyPeriodValue] = useState('');
  const [warrantyPeriodUnit, setWarrantyPeriodUnit] = useState('month');
  const [warrantyExternalCode, setWarrantyExternalCode] = useState('');
  const [warrantyConditions, setWarrantyConditions] = useState('');
  const [warrantyStartDate, setWarrantyStartDate] = useState('');
  const [warrantyEndDate, setWarrantyEndDate] = useState('');

  const [depreciationPeriod, setDepreciationPeriod] = useState('');
  const [depreciationAmountInput, setDepreciationAmountInput] = useState('');
  const [accumulatedDepInput, setAccumulatedDepInput] = useState('');
  const [remainingValueInput, setRemainingValueInput] = useState('');
  const [advancedEditing, setAdvancedEditing] = useState(false);

  useEffect(() => {
    if (!parsedInstanceId || Number.isNaN(parsedInstanceId)) {
      setError('ID cá thể không hợp lệ.');
      setLoading(false);
      return;
    }

    let cancelled = false;
    async function loadData() {
      setLoading(true);
      setError(null);
      try {
        const [inst, profile] = await Promise.all([
          assetInstanceService.getById(parsedInstanceId),
          profileService.getProfile().catch(() => null),
        ]);
        if (cancelled) return;

        const storedRole = (() => {
          const raw = localStorage.getItem('user');
          if (!raw) return null;
          try {
            const parsed = JSON.parse(raw) as { role?: string | null };
            return parsed.role ?? null;
          } catch {
            return null;
          }
        })();
        const allowed = mapBackendRoleToAppRole(profile?.role ?? storedRole) === 'accountant';
        setIsAccountant(allowed);

        const latestGuarantee = getLatestGuarantee(inst);
        setInstance(inst);
        setSerialNumber(inst.serialNumber ?? '');
        setContractNo(inst.contractNo ?? '');
        setPurchaseDate(toDateInput(inst.purchaseDate));
        setOriginalPriceInput(formatMoneyInput(inst.originalPrice));
        setCurrentValueInput(formatMoneyInput(inst.currentValue));
        setCondition(inst.condition ?? '');
        setNote(inst.note ?? '');

        setWarrantyPeriodValue(
          String(latestGuarantee?.warrantyPeriodValue ?? inst.warrantyPeriodValue ?? '')
        );
        setWarrantyPeriodUnit(
          String(latestGuarantee?.warrantyPeriodUnit ?? inst.warrantyPeriodUnit ?? 'month').toLowerCase()
        );
        const parsedWarranty = splitWarrantyConditions(
          latestGuarantee?.warrantyConditions ?? inst.warrantyConditions ?? '',
        );
        setWarrantyExternalCode(parsedWarranty.externalCode);
        setWarrantyConditions(parsedWarranty.details);
        setWarrantyStartDate(toDateInput(latestGuarantee?.startDate ?? inst.warrantyStartDate));
        setWarrantyEndDate(toDateInput(latestGuarantee?.warrantyEndDate ?? inst.warrantyEndDate));

        setDepreciationPeriod(toDateInput(inst.depreciationPeriod));
        setDepreciationAmountInput(formatMoneyInput(inst.depreciationAmount));
        setAccumulatedDepInput(formatMoneyInput(inst.accumulatedDepreciation));
        setRemainingValueInput(formatMoneyInput(inst.remainingValue));
      } catch {
        if (!cancelled) setError('Không tải được thông tin cá thể.');
      } finally {
        if (!cancelled) setLoading(false);
      }
    }
    loadData();

    return () => {
      cancelled = true;
    };
  }, [parsedInstanceId]);

  useEffect(() => {
    const computed = computeWarrantyEndDate(warrantyStartDate, warrantyPeriodValue, warrantyPeriodUnit);
    setWarrantyEndDate(computed);
  }, [warrantyStartDate, warrantyPeriodValue, warrantyPeriodUnit]);

  const displayGuaranteeId = useMemo(() => {
    if (!instance) return '—';
    const latest = getLatestGuarantee(instance);
    const id = latest?.guaranteeId ?? instance.guaranteeId;
    return id != null ? `BH-${id}` : '—';
  }, [instance]);

  const validateForm = (): string | null => {
    const originalPrice = parseNumberInput(originalPriceInput);
    const currentValue = parseNumberInput(currentValueInput);
    const depAmount = parseNumberInput(depreciationAmountInput);
    const depAccumulated = parseNumberInput(accumulatedDepInput);
    const depRemaining = parseNumberInput(remainingValueInput);
    const warrantyValue = Number(warrantyPeriodValue || 0);

    if (!purchaseDate) return 'Vui lòng chọn ngày mua.';
    if (originalPrice == null) return 'Vui lòng nhập giá gốc hợp lệ.';
    if (currentValue == null) return 'Vui lòng nhập giá trị hiện tại hợp lệ.';
    if (currentValue > originalPrice) return 'Giá trị hiện tại không được lớn hơn giá gốc.';

    const hasAnyWarranty =
      warrantyPeriodValue.trim() ||
      warrantyConditions.trim() ||
      warrantyStartDate.trim() ||
      warrantyEndDate.trim();
    if (hasAnyWarranty) {
      if (!warrantyPeriodValue.trim() || !warrantyStartDate || !warrantyEndDate) {
        return 'Vui lòng nhập đủ thời hạn bảo hành, ngày bắt đầu và ngày kết thúc.';
      }
      if (!Number.isFinite(warrantyValue) || warrantyValue <= 0) {
        return 'Thời hạn bảo hành phải lớn hơn 0.';
      }
      if (new Date(warrantyEndDate) < new Date(warrantyStartDate)) {
        return 'Ngày kết thúc bảo hành phải lớn hơn hoặc bằng ngày bắt đầu.';
      }
    }

    if (depAmount != null && depAmount < 0) return 'Mức khấu hao kỳ gần nhất không hợp lệ.';
    if (depAccumulated != null && depAccumulated < 0) return 'Khấu hao lũy kế không hợp lệ.';
    if (depRemaining != null && depRemaining < 0) return 'Giá trị còn lại không hợp lệ.';
    return null;
  };

  const handleSubmit = async (e: FormEvent) => {
    e.preventDefault();
    if (!instance || !isAccountant) return;

    const validationError = validateForm();
    if (validationError) {
      setError(validationError);
      return;
    }

    const hasWarrantyGroup =
      !!warrantyPeriodValue.trim() &&
      !!warrantyStartDate.trim() &&
      !!warrantyEndDate.trim();
    const hasDepreciationGroup =
      !!depreciationPeriod.trim() ||
      !!depreciationAmountInput.trim() ||
      !!accumulatedDepInput.trim() ||
      !!remainingValueInput.trim();

    const sharedAdvancedPayload: UpdateAssetInstancePayload = {
      contractNo: contractNo.trim() || null,
      purchaseDate: toDateOnly(purchaseDate),
      originalPrice: parseNumberInput(originalPriceInput),
      currentValue: parseNumberInput(currentValueInput),
      condition: condition.trim() || null,
      note: note.trim() || null,
      ...(hasWarrantyGroup
        ? {
            warrantyPeriodValue: Number(warrantyPeriodValue),
            warrantyPeriodUnit: warrantyPeriodUnit.trim() || 'month',
            warrantyConditions: buildWarrantyConditions(warrantyExternalCode, warrantyConditions),
            warrantyStartDate: toDateOnly(warrantyStartDate),
            warrantyEndDate: toDateOnly(warrantyEndDate),
          }
        : {}),
      ...(hasDepreciationGroup
        ? {
            depreciationPeriod: toDateOnly(depreciationPeriod),
            depreciationAmount: parseNumberInput(depreciationAmountInput),
            accumulatedDepreciation: parseNumberInput(accumulatedDepInput),
            remainingValue: parseNumberInput(remainingValueInput),
          }
        : {}),
    };

    const payload: UpdateAssetInstancePayload = {
      serialNumber: serialNumber.trim() || null,
      ...sharedAdvancedPayload,
    };

    setSaving(true);
    setError(null);
    try {
      await assetInstanceService.update(instance.assetInstanceId, payload);
      if (advancedEditing) {
        const assetDetail = await assetService.getById(instance.assetId);
        const siblingIds = (assetDetail.instances ?? [])
          .map((row) => row.assetInstanceId)
          .filter((id) => id !== instance.assetInstanceId);
        if (siblingIds.length > 0) {
          const syncResults = await Promise.allSettled(
            siblingIds.map((id) =>
              assetInstanceService.update(id, {
                ...sharedAdvancedPayload,
              }),
            ),
          );
          const failedCount = syncResults.filter((r) => r.status === 'rejected').length;
          const successCount = siblingIds.length - failedCount;
          if (failedCount === 0) {
            message.success(`Đã đồng bộ thông tin chung cho ${successCount} cá thể cùng tài sản.`);
          } else {
            message.warning(`Đồng bộ thành công ${successCount}/${siblingIds.length} cá thể.`);
          }
        }
      }
      message.success('Cập nhật thông tin cá thể thành công.');
      navigate(`/asset-instances/${instance.assetInstanceId}`, {
        state: {
          backToPath: '/accountant-assets',
          backLabel: '← Quay lại danh sách tài sản',
        },
      });
    } catch (e: unknown) {
      const err = e as { response?: { data?: { message?: string } | string } };
      const msg = err?.response?.data && typeof err.response.data === 'object'
        ? err.response.data.message
        : err?.response?.data ?? 'Cập nhật thông tin cá thể thất bại.';
      setError(typeof msg === 'string' ? msg : 'Cập nhật thông tin cá thể thất bại.');
    } finally {
      setSaving(false);
    }
  };

  if (loading) return <div className="asset-create-page">Đang tải thông tin cá thể...</div>;
  if (error && !instance) return <div className="asset-create-page">{error}</div>;

  if (!isAccountant) {
    return (
      <div className="asset-create-page">
        <div className="asset-create__error">
          Bạn không có quyền sửa thông tin cá thể. Chỉ kế toán được phép thực hiện.
        </div>
        <div className="asset-create__header-actions">
          <button
            type="button"
            className="asset-create__btn asset-create__btn--secondary"
            onClick={() => navigate('/assets')}
          >
            Quay lại
          </button>
        </div>
      </div>
    );
  }

  return (
    <div className="asset-create-page">
      <div className="asset-create__header">
        <Link to="/accountant-assets" className="asset-create__back">
          ← Tất cả tài sản
        </Link>
        <div className="asset-create__title-row">
          <h1 className="asset-create__title">Sửa thông tin cá thể</h1>
          <div className="asset-create__header-actions">
            <button
              type="button"
              className="asset-create__btn asset-create__btn--secondary"
              onClick={() => navigate(-1)}
            >
              Hủy
            </button>
            <button
              type="submit"
              form="asset-instance-edit-form"
              className="asset-create__btn asset-create__btn--primary"
              disabled={saving}
            >
              {saving ? 'Đang lưu...' : 'Lưu'}
            </button>
          </div>
        </div>
      </div>

      <form id="asset-instance-edit-form" className="asset-create__card" onSubmit={handleSubmit}>
        {error && <div className="asset-create__error">{error}</div>}

        <section className="asset-create__section">
          <h2 className="asset-create__section-title">Thông tin cá thể</h2>
          <label className="asset-create__checkbox-row" style={{ marginBottom: 12 }}>
            <input
              type="checkbox"
              checked={advancedEditing}
              onChange={(e) => setAdvancedEditing(e.target.checked)}
            />
            <span>Bật đồng bộ thông tin chung sang các cá thể cùng tài sản khi lưu</span>
          </label>
          <div className="asset-create__grid asset-create__grid--two">
            <div className="asset-create__column">
              <div className="asset-create__field">
                <label className="asset-create__label">Mã cá thể</label>
                <input className="asset-create__input" value={instance?.instanceCode ?? ''} disabled />
              </div>
              <div className="asset-create__field">
                <label className="asset-create__label">Số serial</label>
                <input
                  className="asset-create__input"
                  value={serialNumber}
                  onChange={(e) => setSerialNumber(e.target.value)}
                />
              </div>
              <div className="asset-create__field">
                <label className="asset-create__label">Số hợp đồng</label>
                <input
                  className="asset-create__input"
                  value={contractNo}
                  onChange={(e) => setContractNo(e.target.value)}
                />
              </div>
              <div className="asset-create__field">
                <label className="asset-create__label">Ngày mua</label>
                <input
                  type="date"
                  className="asset-create__input"
                  value={purchaseDate}
                  onChange={(e) => setPurchaseDate(e.target.value)}
                />
              </div>
            </div>
            <div className="asset-create__column">
              <div className="asset-create__field">
                <label className="asset-create__label">Giá gốc</label>
                <input
                  type="text"
                  inputMode="numeric"
                  className="asset-create__input"
                  value={originalPriceInput}
                  onChange={(e) => setOriginalPriceInput(formatMoneyInput(parseNumberInput(e.target.value)))}
                  placeholder="Nhập giá gốc"
                />
              </div>
              <div className="asset-create__field">
                <label className="asset-create__label">Giá trị hiện tại</label>
                <input
                  type="text"
                  inputMode="numeric"
                  className="asset-create__input"
                  value={currentValueInput}
                  onChange={(e) => setCurrentValueInput(formatMoneyInput(parseNumberInput(e.target.value)))}
                  placeholder="Nhập giá trị hiện tại"
                />
              </div>
              <div className="asset-create__field">
                <label className="asset-create__label">Tình trạng (mô tả thực tế)</label>
                <input
                  className="asset-create__input"
                  value={condition}
                  onChange={(e) => setCondition(e.target.value)}
                  placeholder="Ví dụ: Hoạt động tốt, trầy xước nhẹ..."
                />
              </div>
              <div className="asset-create__field">
                <label className="asset-create__label">Ghi chú</label>
                <textarea
                  className="asset-create__textarea"
                  rows={3}
                  value={note}
                  onChange={(e) => setNote(e.target.value)}
                />
              </div>
            </div>
          </div>
        </section>

        <section className="asset-create__section">
          <h2 className="asset-create__section-title">Bảo hành và khấu hao</h2>
          <div className="asset-create__grid asset-create__grid--two">
            <div className="asset-create__column">
              <h3 className="asset-create__section-title" style={{ fontSize: 16 }}>Thông tin bảo hành</h3>
              <div className="asset-create__field">
                <label className="asset-create__label">Mã bảo hành nội bộ</label>
                <input className="asset-create__input" value={displayGuaranteeId} disabled />
              </div>
              <div className="asset-create__field">
                <label className="asset-create__label">Mã bảo hành ngoài (theo giấy tờ)</label>
                <input
                  className="asset-create__input"
                  value={warrantyExternalCode}
                  onChange={(e) => setWarrantyExternalCode(e.target.value)}
                  placeholder="Nhập mã từ nhà cung cấp"
                />
              </div>
              <div className="asset-create__field asset-create__field--quantity-unit-row">
                <div className="asset-create__quantity-unit-cell">
                  <label className="asset-create__label">Thời hạn bảo hành</label>
                  <input
                    type="number"
                    min={1}
                    className="asset-create__input"
                    value={warrantyPeriodValue}
                    onChange={(e) => setWarrantyPeriodValue(e.target.value)}
                  />
                </div>
                <div className="asset-create__quantity-unit-cell">
                  <label className="asset-create__label">Đơn vị</label>
                  <select
                    className="asset-create__select"
                    value={warrantyPeriodUnit}
                    onChange={(e) => setWarrantyPeriodUnit(e.target.value)}
                  >
                    <option value="day">Ngày</option>
                    <option value="week">Tuần</option>
                    <option value="month">Tháng</option>
                    <option value="year">Năm</option>
                  </select>
                </div>
              </div>
              <div className="asset-create__field">
                <label className="asset-create__label">Ngày bắt đầu</label>
                <input
                  type="date"
                  className="asset-create__input"
                  value={warrantyStartDate}
                  onChange={(e) => setWarrantyStartDate(e.target.value)}
                />
              </div>
              <div className="asset-create__field">
                <label className="asset-create__label">Ngày kết thúc</label>
                <input
                  type="date"
                  className="asset-create__input"
                  value={warrantyEndDate}
                  readOnly
                  disabled
                />
              </div>
              <div className="asset-create__field">
                <label className="asset-create__label">Điều kiện bảo hành</label>
                <textarea
                  className="asset-create__textarea"
                  rows={2}
                  value={warrantyConditions}
                  onChange={(e) => setWarrantyConditions(e.target.value)}
                />
              </div>
            </div>

            <div className="asset-create__column">
              <h3 className="asset-create__section-title" style={{ fontSize: 16 }}>Thông tin khấu hao</h3>
              <div className="asset-create__field">
                <label className="asset-create__label">Chính sách</label>
                <input className="asset-create__input" value={instance?.depreciationPolicyName ?? '—'} disabled />
              </div>
              <div className="asset-create__field">
                <label className="asset-create__label">Thời gian hữu ích (tháng)</label>
                <input
                  className="asset-create__input"
                  value={
                    instance?.depreciationUsefulLifeMonths != null
                      ? String(instance.depreciationUsefulLifeMonths)
                      : '—'
                  }
                  disabled
                />
              </div>
              <div className="asset-create__field">
                <label className="asset-create__label">Giá trị hoàn trả ước tính</label>
                <input
                  className="asset-create__input"
                  value={formatMoneyInput(instance?.depreciationSalvageValue)}
                  disabled
                />
              </div>
              <div className="asset-create__field">
                <label className="asset-create__label">Kỳ khấu hao gần nhất</label>
                <input
                  type="date"
                  className="asset-create__input"
                  value={depreciationPeriod}
                  onChange={(e) => setDepreciationPeriod(e.target.value)}
                />
              </div>
              <div className="asset-create__field">
                <label className="asset-create__label">Mức khấu hao kỳ gần nhất</label>
                <input
                  type="text"
                  inputMode="numeric"
                  className="asset-create__input"
                  value={depreciationAmountInput}
                  onChange={(e) =>
                    setDepreciationAmountInput(formatMoneyInput(parseNumberInput(e.target.value)))
                  }
                />
              </div>
              <div className="asset-create__field">
                <label className="asset-create__label">Lũy kế</label>
                <input
                  type="text"
                  inputMode="numeric"
                  className="asset-create__input"
                  value={accumulatedDepInput}
                  onChange={(e) => setAccumulatedDepInput(formatMoneyInput(parseNumberInput(e.target.value)))}
                />
              </div>
              <div className="asset-create__field">
                <label className="asset-create__label">Giá trị còn lại (KH)</label>
                <input
                  type="text"
                  inputMode="numeric"
                  className="asset-create__input"
                  value={remainingValueInput}
                  onChange={(e) => setRemainingValueInput(formatMoneyInput(parseNumberInput(e.target.value)))}
                />
              </div>
            </div>
          </div>
        </section>
      </form>
    </div>
  );
}

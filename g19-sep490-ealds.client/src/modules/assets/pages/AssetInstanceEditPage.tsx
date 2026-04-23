import { useEffect, useMemo, useRef, useState, type FormEvent } from 'react';
import { Link, useNavigate, useParams } from 'react-router-dom';
import { message } from 'antd';
import {
  assetService,
  assetInstanceService,
  isAssetInstanceNonEditableStatus,
  type AssetInstanceResponse,
  type GuaranteeItem,
  type UpdateAssetInstancePayload,
  type DepreciationPolicyItem,
} from '../services/assetService';
import { transferRequestService, type AssetLocationOption } from '../services/transferRequestService';
import { profileService } from '../../profile/services/profileService';
import { mapBackendRoleToAppRole } from '../../auth/types/auth.types';
import { useAppStore } from '../../../stores/appStore';
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

function toDisplayDate(iso: string): string {
  if (!iso) return '';
  const m = iso.match(/^(\d{4})-(\d{2})-(\d{2})/);
  if (!m) return '';
  return `${m[3]}/${m[2]}/${m[1]}`;
}

function fromDisplayDate(display: string): string {
  const m = display.match(/^(\d{1,2})\/(\d{1,2})\/(\d{4})$/);
  if (!m) return '';
  const d = m[1].padStart(2, '0');
  const mo = m[2].padStart(2, '0');
  return `${m[3]}-${mo}-${d}`;
}

function monthDiff(fromDate: Date, toDate: Date): number {
  const diff = (toDate.getFullYear() - fromDate.getFullYear()) * 12 + (toDate.getMonth() - fromDate.getMonth());
  return Math.max(0, diff);
}

function roundAwayFromZero(value: number): number {
  if (!Number.isFinite(value)) return 0;
  if (value === 0) return 0;
  return Math.sign(value) * Math.round(Math.abs(value));
}

function getPeriodDate(value: string): Date | null {
  if (!value.trim()) return null;
  const date = new Date(value);
  if (Number.isNaN(date.getTime())) return null;
  return new Date(date.getFullYear(), date.getMonth(), 1);
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
  const currentRole = useAppStore((s) => s.currentRole);
  const backToListPath = currentRole === 'accountant' ? '/accountant-assets' : '/assets';
  const prevDeptRef = useRef<string | null>(null);

  const [loading, setLoading] = useState(true);
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [instance, setInstance] = useState<AssetInstanceResponse | null>(null);
  const [isAccountant, setIsAccountant] = useState(false);

  const [departments, setDepartments] = useState<AssetLocationOption[]>([]);
  const [deptEmployees, setDeptEmployees] = useState<
    { employeeId: number; name: string; code: string }[]
  >([]);
  const [warehouses, setWarehouses] = useState<{ warehouseId: number; name: string }[]>([]);
  const [suppliers, setSuppliers] = useState<{ supplierId: number; name: string; code: string }[]>(
    []
  );
  const [loadMetaError, setLoadMetaError] = useState<string | null>(null);

  const [departmentId, setDepartmentId] = useState('');
  const [managerEmployeeId, setManagerEmployeeId] = useState('');
  const [warehouseId, setWarehouseId] = useState('');
  const [supplierId, setSupplierId] = useState('');

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
  const [warrantyStartDateText, setWarrantyStartDateText] = useState('');

  const [depreciationPeriod, setDepreciationPeriod] = useState('');
  const [depreciationAmountInput, setDepreciationAmountInput] = useState('');
  const [accumulatedDepInput, setAccumulatedDepInput] = useState('');
  const [remainingValueInput, setRemainingValueInput] = useState('');
  const [depreciationPolicyId, setDepreciationPolicyId] = useState('');
  const [depreciationPolicies, setDepreciationPolicies] = useState<DepreciationPolicyItem[]>([]);
  const [advancedEditing, setAdvancedEditing] = useState(false);

  const [isCapitalized, setIsCapitalized] = useState(false);
  const [wasAlreadyCapitalized, setWasAlreadyCapitalized] = useState(false);

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
      prevDeptRef.current = null;
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

        if (isAssetInstanceNonEditableStatus(inst.status)) {
          if (!cancelled) {
            message.warning(
              'Không thể chỉnh sửa cá thể ở trạng thái đã loại bỏ, mất, đã thanh lý hoặc đã vốn hóa.'
            );
            navigate(`/asset-instances/${parsedInstanceId}`, { replace: true });
          }
          return;
        }

        const latestGuarantee = getLatestGuarantee(inst);
        setInstance(inst);
        setWarehouseId(String(inst.warehouseId));
        setDepartmentId(
          inst.currentDepartmentId != null && inst.currentDepartmentId > 0
            ? String(inst.currentDepartmentId)
            : ''
        );
        setManagerEmployeeId(
          inst.currentResponsibleEmployeeId != null && inst.currentResponsibleEmployeeId > 0
            ? String(inst.currentResponsibleEmployeeId)
            : ''
        );
        setSupplierId(inst.supplierId != null && inst.supplierId > 0 ? String(inst.supplierId) : '');

        try {
          const [deptList, wh, sup, policies] = await Promise.all([
            transferRequestService.getAssetLocations(),
            assetService.getWarehouses(),
            assetService.getSuppliers(),
            assetService.getDepreciationPolicies(),
          ]);
          if (cancelled) return;
          setDepartments(deptList);
          setWarehouses(wh);
          setSuppliers(sup.map((s) => ({ supplierId: s.supplierId, name: s.name, code: s.code })));
          setDepreciationPolicies(policies);
          setLoadMetaError(null);
        } catch {
          if (!cancelled) {
            setLoadMetaError(
              'Không tải được danh mục (phòng ban, kho, nhà cung cấp, chính sách khấu hao).'
            );
          }
        }

        const capitalized = inst.status === 7;
        setIsCapitalized(capitalized);
        setWasAlreadyCapitalized(capitalized);

        setSerialNumber(inst.serialNumber ?? '');
        setContractNo(inst.contractNo ?? '');
        setPurchaseDate(toDateInput(inst.purchaseDate));
        setOriginalPriceInput(inst.originalPrice != null ? String(inst.originalPrice) : '');
        setCurrentValueInput(inst.currentValue != null ? String(inst.currentValue) : '');
        setCondition(inst.condition ?? '');
        setNote(inst.note ?? '');

        setWarrantyPeriodValue(
          String(latestGuarantee?.warrantyPeriodValue ?? inst.warrantyPeriodValue ?? '')
        );
        setWarrantyPeriodUnit('month');
        const parsedWarranty = splitWarrantyConditions(
          latestGuarantee?.warrantyConditions ?? inst.warrantyConditions ?? '',
        );
        setWarrantyExternalCode(parsedWarranty.externalCode);
        setWarrantyConditions(parsedWarranty.details);
        const initWarrantyStart = toDateInput(latestGuarantee?.startDate ?? inst.warrantyStartDate);
        setWarrantyStartDate(initWarrantyStart);
        setWarrantyStartDateText(toDisplayDate(initWarrantyStart));
        setWarrantyEndDate(toDateInput(latestGuarantee?.warrantyEndDate ?? inst.warrantyEndDate));

        const sortedDepRecords = [...(inst.depreciationRecords ?? [])].sort((a, b) =>
          String(a.period).localeCompare(String(b.period))
        );
        const earliestDepRecord = sortedDepRecords[0];
        setDepreciationPeriod(toDateInput(earliestDepRecord?.period ?? inst.depreciationPeriod));
        setDepreciationAmountInput(
          inst.depreciationAmount != null ? String(inst.depreciationAmount) : ''
        );
        setAccumulatedDepInput(
          inst.accumulatedDepreciation != null ? String(inst.accumulatedDepreciation) : ''
        );
        setRemainingValueInput(inst.remainingValue != null ? String(inst.remainingValue) : '');
        setDepreciationPolicyId(
          inst.depreciationPolicyId != null ? String(inst.depreciationPolicyId) : ''
        );
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

  useEffect(() => {
    const deptId = departmentId;
    if (!deptId || Number(deptId) <= 0) {
      setDeptEmployees([]);
      setManagerEmployeeId('');
      prevDeptRef.current = null;
      return;
    }

    const isUserDeptChange =
      prevDeptRef.current !== null && prevDeptRef.current !== deptId;
    prevDeptRef.current = deptId;

    let cancelled = false;
    setDeptEmployees([]);
    (async () => {
      try {
        const list = await assetService.getEmployeesByDepartment(Number(deptId));
        if (cancelled) return;
        const sorted = [...list].sort((a, b) => {
          const ua = a.userId ?? Number.MAX_SAFE_INTEGER;
          const ub = b.userId ?? Number.MAX_SAFE_INTEGER;
          if (ua !== ub) return ua - ub;
          return a.employeeId - b.employeeId;
        });
        setDeptEmployees(sorted);
        setManagerEmployeeId((prev) => {
          if (sorted.some((e) => String(e.employeeId) === prev)) return prev;
          if (isUserDeptChange && sorted.length > 0) return String(sorted[0].employeeId);
          return '';
        });
      } catch {
        if (!cancelled) {
          setDeptEmployees([]);
        }
      }
    })();

    return () => {
      cancelled = true;
    };
  }, [departmentId]);

  const displayGuaranteeId = useMemo(() => {
    if (!instance) return '—';
    const latest = getLatestGuarantee(instance);
    const id = latest?.guaranteeId ?? instance.guaranteeId;
    return id != null ? `BH-${id}` : '—';
  }, [instance]);

  const selectedDepreciationPolicy = useMemo(() => {
    if (!depreciationPolicyId) return null;
    return depreciationPolicies.find((p) => String(p.policyId) === depreciationPolicyId) ?? null;
  }, [depreciationPolicies, depreciationPolicyId]);

  const depreciationPreview = useMemo(() => {
    if (!instance) return null;
    const policy = selectedDepreciationPolicy;
    const periodDate = getPeriodDate(depreciationPeriod);
    if (!policy || !periodDate || policy.usefullLifeMonths <= 0) return null;

    const records = [...(instance.depreciationRecords ?? [])]
      .filter((r) => {
        const recordDate = getPeriodDate(String(r.period));
        return recordDate != null && recordDate < periodDate;
      })
      .sort((a, b) => String(a.period).localeCompare(String(b.period)));
    const lastRecord = records.length > 0 ? records[records.length - 1] : null;

    const openingValue = lastRecord?.remainingValue ?? Number(originalPriceInput || instance.originalPrice || 0);
    const salvageValue = Number(policy.salvageValue || 0);
    const inUseBase = getPeriodDate(instance.inUseDate ?? purchaseDate) ?? getPeriodDate(purchaseDate);
    const elapsedMonths = inUseBase ? monthDiff(inUseBase, periodDate) : 0;
    const remainingMonths = Math.max(1, policy.usefullLifeMonths - elapsedMonths);

    const depreciableBase = openingValue - salvageValue;
    const monthly = depreciableBase > 0 ? roundAwayFromZero(depreciableBase / remainingMonths) : 0;
    const maxAllowed = Math.max(0, openingValue - salvageValue);
    const amount = Math.min(monthly, maxAllowed);
    const accumulated = (lastRecord?.accumulatedDepreciation ?? 0) + amount;
    const remainingValue = openingValue - amount;
    const remainingLifeMonths = Math.max(0, policy.usefullLifeMonths - elapsedMonths - 1);

    return {
      amount,
      accumulated,
      remainingValue,
      remainingLifeMonths,
    };
  }, [
    depreciationPeriod,
    depreciationPolicyId,
    instance,
    originalPriceInput,
    purchaseDate,
    selectedDepreciationPolicy,
  ]);

  const remainingDepreciationMonths = useMemo(() => {
    if (depreciationPreview != null) return depreciationPreview.remainingLifeMonths;
    const totalMonths = instance?.depreciationUsefulLifeMonths;
    if (totalMonths == null || totalMonths <= 0) return null;
    const completedPeriods = instance?.depreciationRecords?.length ?? 0;
    return Math.max(0, totalMonths - completedPeriods);
  }, [depreciationPreview, instance?.depreciationUsefulLifeMonths, instance?.depreciationRecords]);

  const validateForm = (): string | null => {
    const originalPrice =
      originalPriceInput.trim() === '' ? NaN : Number(originalPriceInput);
    const currentValue =
      currentValueInput.trim() === '' ? NaN : Number(currentValueInput);
    const depAmount =
      depreciationPreview?.amount ??
      (depreciationAmountInput.trim() === '' ? undefined : Number(depreciationAmountInput));
    const depAccumulated =
      depreciationPreview?.accumulated ??
      (accumulatedDepInput.trim() === '' ? undefined : Number(accumulatedDepInput));
    const depRemaining =
      depreciationPreview?.remainingValue ??
      (remainingValueInput.trim() === '' ? undefined : Number(remainingValueInput));
    const warrantyValue = Number(warrantyPeriodValue || 0);

    if (!purchaseDate) return 'Vui lòng chọn ngày mua.';
    if (!Number.isFinite(originalPrice)) return 'Vui lòng nhập giá gốc hợp lệ.';
    if (!Number.isFinite(currentValue)) return 'Vui lòng nhập giá trị hiện tại hợp lệ.';
    if (currentValue > originalPrice) return 'Giá trị hiện tại không được lớn hơn giá gốc.';
    if (!warehouseId || Number(warehouseId) <= 0) return 'Vui lòng chọn kho.';
    if (isAccountant && (!departmentId || Number(departmentId) <= 0)) {
      return 'Vui lòng chọn vị trí tài sản (phòng ban).';
    }

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

    if (depAmount != null && !Number.isFinite(depAmount)) {
      return 'Mức khấu hao kỳ gần nhất không hợp lệ.';
    }
    if (depAmount != null && depAmount < 0) {
      return 'Mức khấu hao kỳ gần nhất không hợp lệ.';
    }
    if (depAccumulated != null && !Number.isFinite(depAccumulated)) {
      return 'Khấu hao lũy kế không hợp lệ.';
    }
    if (depAccumulated != null && depAccumulated < 0) {
      return 'Khấu hao lũy kế không hợp lệ.';
    }
    if (depRemaining != null && !Number.isFinite(depRemaining)) {
      return 'Giá trị còn lại không hợp lệ.';
    }
    if (depRemaining != null && depRemaining < 0) {
      return 'Giá trị còn lại không hợp lệ.';
    }
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
      !!depreciationPolicyId.trim() ||
      !!depreciationPeriod.trim() ||
      !!depreciationAmountInput.trim() ||
      !!accumulatedDepInput.trim() ||
      !!remainingValueInput.trim();

    const deptId =
      isAccountant && departmentId && Number(departmentId) > 0 ? Number(departmentId) : null;
    const managerId =
      isAccountant && managerEmployeeId && Number(managerEmployeeId) > 0
        ? Number(managerEmployeeId)
        : null;
    const supId = supplierId && Number(supplierId) > 0 ? Number(supplierId) : null;
    const purchaseOnly = toDateOnly(purchaseDate);

    const sharedAdvancedPayload: UpdateAssetInstancePayload = {
      warehouseId: Number(warehouseId),
      supplierId: supId,
      assignedDepartmentId: deptId,
      responsibleEmployeeId: managerId,
      assignmentEffectiveDate:
        isAccountant && (deptId != null || managerId != null) ? purchaseOnly ?? null : null,
      contractNo: contractNo.trim() || null,
      purchaseDate: purchaseOnly,
      originalPrice: Number(originalPriceInput),
      currentValue: Number(currentValueInput),
      condition: condition.trim() || null,
      note: note.trim() || null,
    };

    const selectedPolicyId = depreciationPolicyId ? Number(depreciationPolicyId) : null;
    const effectiveDepAmount =
      depreciationPreview?.amount ??
      (depreciationAmountInput.trim() ? Number(depreciationAmountInput) : undefined);
    const effectiveAccumulated =
      depreciationPreview?.accumulated ??
      (accumulatedDepInput.trim() ? Number(accumulatedDepInput) : undefined);
    const effectiveRemainingValue =
      depreciationPreview?.remainingValue ??
      (remainingValueInput.trim() ? Number(remainingValueInput) : undefined);

    const detailOnlyPayload: UpdateAssetInstancePayload = {
      depreciationPolicyId: selectedPolicyId,
      ...(hasWarrantyGroup
        ? {
            warrantyPeriodValue: Number(warrantyPeriodValue),
            warrantyPeriodUnit: 'month',
            warrantyConditions: buildWarrantyConditions(warrantyExternalCode, warrantyConditions),
            warrantyStartDate: toDateOnly(warrantyStartDate),
            warrantyEndDate: toDateOnly(warrantyEndDate),
          }
        : {}),
      ...(hasDepreciationGroup
        ? {
            depreciationPeriod: toDateOnly(depreciationPeriod),
            depreciationAmount: effectiveDepAmount,
            accumulatedDepreciation: effectiveAccumulated,
            remainingValue: effectiveRemainingValue,
          }
        : {}),
    };

    const payload: UpdateAssetInstancePayload = {
      serialNumber: serialNumber.trim() || null,
      ...sharedAdvancedPayload,
      ...detailOnlyPayload,
    };

    setSaving(true);
    setError(null);
    try {
      await assetInstanceService.update(instance.assetInstanceId, payload);
      if (isCapitalized && !wasAlreadyCapitalized) {
        try {
          await assetInstanceService.capitalize(instance.assetInstanceId);
        } catch {
          message.warning('Cập nhật thành công nhưng không thể vốn hoá tài sản. Vui lòng thử lại.');
        }
      }
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
          backToPath: backToListPath,
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
        <Link to={backToListPath} className="asset-create__back">
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
        {loadMetaError && (
          <div className="asset-create__error" role="alert">
            {loadMetaError}
          </div>
        )}
        {error && <div className="asset-create__error">{error}</div>}

        <section className="asset-create__section">
          <h2 className="asset-create__section-title">Thông tin cá thể</h2>
          <label className="asset-create__checkbox-row" style={{ marginBottom: 8 }}>
            <input
              type="checkbox"
              checked={isCapitalized}
              disabled={wasAlreadyCapitalized}
              onChange={(e) => setIsCapitalized(e.target.checked)}
            />
            <span>
              Là tài sản cố định (vốn hoá)
              {wasAlreadyCapitalized && (
                <span style={{ marginLeft: 6, color: '#888', fontStyle: 'italic' }}>
                  — đã được vốn hoá
                </span>
              )}
            </span>
          </label>
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
                <label className="asset-create__label">Tên tài sản (danh mục)</label>
                <input
                  className="asset-create__input"
                  value={instance?.assetName ?? '—'}
                  disabled
                />
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
                <label className="asset-create__label">Giá trị</label>
                <input
                  type="number"
                  className="asset-create__input"
                  value={originalPriceInput}
                  readOnly
                  disabled
                />
              </div>
              <div className="asset-create__field">
                <label className="asset-create__label">Giá trị hiện tại</label>
                <input
                  type="number"
                  className="asset-create__input"
                  value={currentValueInput}
                  readOnly
                  disabled
                />
              </div>
              <div className="asset-create__field">
                <label className="asset-create__label">Quy cách tài sản</label>
                <input
                  className="asset-create__input"
                  value={condition}
                  onChange={(e) => setCondition(e.target.value)}
                />
              </div>
            </div>
            <div className="asset-create__column">
              <div className="asset-create__field">
                <label className="asset-create__label">
                  Vị trí tài sản
                  <span className="asset-create__required">*</span>
                </label>
                <select
                  className="asset-create__select"
                  value={departmentId}
                  onChange={(e) => {
                    setDepartmentId(e.target.value);
                    setManagerEmployeeId('');
                  }}
                >
                  <option value="">Chọn phòng ban</option>
                  {departments.map((d) => (
                    <option key={d.locationId} value={d.locationId}>
                      {d.displayName}
                    </option>
                  ))}
                </select>
              </div>
              <div className="asset-create__field">
                <label className="asset-create__label">Người quản lý</label>
                <select
                  className="asset-create__select"
                  value={
                    !departmentId || deptEmployees.length === 0
                      ? ''
                      : deptEmployees.some(
                            (e) => String(e.employeeId) === managerEmployeeId
                          )
                        ? managerEmployeeId
                        : String(deptEmployees[0].employeeId)
                  }
                  disabled={!departmentId || deptEmployees.length === 0}
                  onChange={(e) => setManagerEmployeeId(e.target.value)}
                >
                  {!departmentId && <option value="">Chọn phòng ban trước</option>}
                  {departmentId && deptEmployees.length === 0 && (
                    <option value="">Không có nhân viên trong phòng ban</option>
                  )}
                  {deptEmployees.map((emp) => (
                    <option key={emp.employeeId} value={emp.employeeId}>
                      {emp.name}
                    </option>
                  ))}
                </select>
              </div>
              <div className="asset-create__field">
                <label className="asset-create__label">
                  Kho<span className="asset-create__required">*</span>
                </label>
                <select
                  className="asset-create__select"
                  value={warehouseId}
                  onChange={(e) => setWarehouseId(e.target.value)}
                >
                  <option value="">Chọn kho</option>
                  {warehouses.map((w) => (
                    <option key={w.warehouseId} value={w.warehouseId}>
                      {w.name}
                    </option>
                  ))}
                </select>
              </div>
              <div className="asset-create__field">
                <label className="asset-create__label">
                  Ngày mua<span className="asset-create__required">*</span>
                </label>
                <input
                  type="date"
                  className="asset-create__input"
                  value={purchaseDate}
                  onChange={(e) => setPurchaseDate(e.target.value)}
                />
              </div>
              <div className="asset-create__field">
                <label className="asset-create__label">Nhà cung cấp</label>
                <select
                  className="asset-create__select"
                  value={supplierId}
                  onChange={(e) => setSupplierId(e.target.value)}
                >
                  <option value="">Chọn nhà cung cấp</option>
                  {suppliers.map((s) => (
                    <option key={s.supplierId} value={s.supplierId}>
                      {s.name}
                      {s.code ? ` (${s.code})` : ''}
                    </option>
                  ))}
                </select>
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
                <label className="asset-create__label">Ghi chú</label>
                <textarea
                  className="asset-create__textarea"
                  rows={2}
                  value={note}
                  onChange={(e) => setNote(e.target.value)}
                />
              </div>
            </div>
          </div>
        </section>

        <section className="asset-create__section">
          <h2 className="asset-create__section-title">Bảo hành</h2>
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
          <div className="asset-create__grid asset-create__grid--three">
            <div className="asset-create__field">
              <label className="asset-create__label">Thời gian bảo hành</label>
              <div className="asset-create__field--inline">
                <input
                  type="number"
                  min={0}
                  className="asset-create__input"
                  value={warrantyPeriodValue}
                  onChange={(e) => setWarrantyPeriodValue(e.target.value)}
                />
                <span className="asset-create__suffix">Tháng</span>
              </div>
            </div>
            <div className="asset-create__field">
              <label className="asset-create__label">Điều kiện bảo hành</label>
              <input
                className="asset-create__input"
                value={warrantyConditions}
                onChange={(e) => setWarrantyConditions(e.target.value)}
              />
            </div>
            <div className="asset-create__field">
              <label className="asset-create__label">Hạn bảo hành</label>
              <input
                type="text"
                className="asset-create__input"
                value={toDisplayDate(warrantyEndDate)}
                readOnly
                disabled
                placeholder="dd/mm/yyyy"
              />
            </div>
          </div>
          <div className="asset-create__field">
            <label className="asset-create__label">Ngày bắt đầu bảo hành</label>
            <input
              type="text"
              className="asset-create__input"
              placeholder="dd/mm/yyyy"
              value={warrantyStartDateText}
              onChange={(e) => {
                const val = e.target.value;
                setWarrantyStartDateText(val);
                const iso = fromDisplayDate(val);
                if (iso) setWarrantyStartDate(iso);
                else if (!val.trim()) setWarrantyStartDate('');
              }}
            />
            <p className="asset-create__hint">
              Hạn bảo hành được tính từ ngày bắt đầu và thời gian bảo hành (tháng).
            </p>
          </div>
        </section>

        <section className="asset-create__section">
          <h2 className="asset-create__section-title">Thông tin khấu hao</h2>
          <div className="asset-create__grid asset-create__grid--three">
            <div className="asset-create__field">
              <label className="asset-create__label">Mức khấu hao kỳ gần nhất</label>
              <input
                type="number"
                className="asset-create__input"
                value={depreciationPreview != null ? String(depreciationPreview.amount) : depreciationAmountInput}
                readOnly
                disabled
              />
            </div>
            <div className="asset-create__field">
              <label className="asset-create__label">Thời gian còn lại</label>
              <div className="asset-create__field--inline">
                <input
                  className="asset-create__input"
                  value={remainingDepreciationMonths != null ? String(remainingDepreciationMonths) : '—'}
                  disabled
                  readOnly
                />
                <span className="asset-create__suffix">Tháng</span>
              </div>
            </div>
            <div className="asset-create__field">
              <label className="asset-create__label">Giá trị còn lại</label>
              <input
                type="number"
                className="asset-create__input"
                value={
                  depreciationPreview != null
                    ? String(depreciationPreview.remainingValue)
                    : remainingValueInput
                }
                readOnly
                disabled
              />
            </div>
            <div className="asset-create__field">
              <label className="asset-create__label">Ngày bắt đầu khấu hao</label>
              <input
                type="date"
                className="asset-create__input"
                value={depreciationPeriod}
                onChange={(e) => setDepreciationPeriod(e.target.value)}
              />
            </div>
            <div className="asset-create__field">
              <label className="asset-create__label">Khấu hao lũy kế</label>
              <input
                type="number"
                className="asset-create__input"
                value={
                  depreciationPreview != null
                    ? String(depreciationPreview.accumulated)
                    : accumulatedDepInput
                }
                readOnly
                disabled
              />
            </div>
            <div className="asset-create__field">
              <label className="asset-create__label">Thời gian khấu hao</label>
              <div className="asset-create__field--inline">
                <input
                  className="asset-create__input"
                  value={
                    instance?.depreciationUsefulLifeMonths != null
                      ? String(instance.depreciationUsefulLifeMonths)
                      : ''
                  }
                  disabled
                />
                <span className="asset-create__suffix">Tháng</span>
              </div>
            </div>
          </div>
          <div className="asset-create__grid asset-create__grid--two">
            <div className="asset-create__field">
              <label className="asset-create__label">Chính sách khấu hao</label>
              <select
                className="asset-create__select"
                value={depreciationPolicyId}
                onChange={(e) => setDepreciationPolicyId(e.target.value)}
              >
                <option value="">— Chưa chọn chính sách —</option>
                {depreciationPolicies.map((p) => (
                  <option key={p.policyId} value={p.policyId}>
                    {p.name} ({p.usefullLifeMonths} tháng)
                  </option>
                ))}
              </select>
            </div>
          </div>
        </section>
      </form>
    </div>
  );
}

import { useEffect, useRef, useState, type FormEvent } from 'react';
import { message } from 'antd';
import { Link, useNavigate, useParams } from 'react-router-dom';
import {
  assetInstanceService,
  assetService,
  ASSET_MEASUREMENT_UNITS,
  type AssetDetailResponse,
  type UpdateAssetPayload,
} from '../services/assetService';
import { transferRequestService, type AssetLocationOption } from '../services/transferRequestService';
import {
  maintenanceTemplateService,
  type MaintenanceTemplateItem,
} from '../../maintenance/services/maintenanceTemplateService';
import { useAppStore } from '../../../stores/appStore';
import '../../maintenance/pages/MaintenancePage.css';
import './AssetCreatePage.css';

function getApiErrorMessage(error: unknown, fallback: string): string {
  const axiosErr = error as { response?: { data?: unknown } };
  const data = axiosErr?.response?.data;
  if (typeof data === 'string' && data.trim()) return data;
  if (data && typeof data === 'object' && 'message' in (data as Record<string, unknown>)) {
    const msg = (data as Record<string, unknown>).message;
    if (typeof msg === 'string' && msg.trim()) return msg;
  }
  return fallback;
}

function parseEnumNumber(value: number | string | null | undefined): number {
  if (typeof value === 'number') return value;
  if (!value) return 0;
  const parsed = Number(value);
  if (Number.isFinite(parsed)) return parsed;
  const normalized = String(value).toLowerCase();
  if (normalized === 'onetime') return 1;
  if (normalized === 'periodic') return 2;
  if (normalized === 'day') return 1;
  if (normalized === 'week') return 2;
  if (normalized === 'month') return 3;
  if (normalized === 'year') return 4;
  return 0;
}

function getRepeatUnitLabel(value?: number | string | null): string {
  const parsed = parseEnumNumber(value);
  if (parsed === 1) return 'Ngày';
  if (parsed === 2) return 'Tuần';
  if (parsed === 3) return 'Tháng';
  if (parsed === 4) return 'Năm';
  return '—';
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

function toDateOnly(value: string): string | undefined {
  const trimmed = value.trim();
  if (!trimmed) return undefined;
  return trimmed.split('T')[0] ?? undefined;
}

type MaintenanceTemplateForm = {
  assetTypeId: number | null;
  name: string;
  content: string;
  frequencyType: 1 | 2;
  repeatIntervalValue: number;
  repeatIntervalUnit: 1 | 2 | 3 | 4;
  isActive: boolean;
};

function getFrequencyLabel(value: number | string): string {
  return parseEnumNumber(value) === 1 ? 'Một lần' : 'Định kỳ';
}

export function AssetEditPage() {
  const { id } = useParams<{ id: string }>();
  const navigate = useNavigate();
  const assetId = Number(id);
  const currentRole = useAppStore((s) => s.currentRole);
  const backToListPath = currentRole === 'accountant' ? '/accountant-assets' : '/assets';
  const prevDeptRef = useRef<string | null>(null);

  const [loading, setLoading] = useState(true);
  const [saving, setSaving] = useState(false);
  const [asset, setAsset] = useState<AssetDetailResponse | null>(null);
  const [error, setError] = useState<string | null>(null);

  const [code, setCode] = useState('');
  const [name, setName] = useState('');
  const [assetTypeId, setAssetTypeId] = useState('');
  const [purchaseDate, setPurchaseDate] = useState('');
  const [originalPrice, setOriginalPrice] = useState('');
  const [currentValue, setCurrentValue] = useState('');
  const [unit, setUnit] = useState('');
  const [quantity, setQuantity] = useState('');
  const [warehouseId, setWarehouseId] = useState('');
  const [departmentId, setDepartmentId] = useState('');
  const [managerEmployeeId, setManagerEmployeeId] = useState('');
  const [supplierId, setSupplierId] = useState('');
  const [warrantyEndDate, setWarrantyEndDate] = useState('');

  const [departments, setDepartments] = useState<AssetLocationOption[]>([]);
  const [deptEmployees, setDeptEmployees] = useState<
    { employeeId: number; name: string; code: string }[]
  >([]);
  const [assetTypes, setAssetTypes] = useState<{ assetTypeId: number; name: string }[]>([]);
  const [warehouses, setWarehouses] = useState<{ warehouseId: number; name: string }[]>([]);
  const [suppliers, setSuppliers] = useState<{ supplierId: number; name: string; code: string }[]>(
    []
  );
  const [loadMetaError, setLoadMetaError] = useState<string | null>(null);

  const [contractNumber, setContractNumber] = useState('');
  const [serialNumber, setSerialNumber] = useState('');
  const [specification, setSpecification] = useState('');
  const [note, setNote] = useState('');
  const [origin, setOrigin] = useState('');
  const [isFixedAsset, setIsFixedAsset] = useState(true);
  const [warrantyMonths, setWarrantyMonths] = useState('');
  const [warrantyCondition, setWarrantyCondition] = useState('');
  /** Loaded for API; create form has no field — fallback to ngày mua when saving. */
  const [warrantyStartDate, setWarrantyStartDate] = useState('');
  const [depreciationBaseValue, setDepreciationBaseValue] = useState('');
  const [depreciationStartDate, setDepreciationStartDate] = useState('');
  const [depreciationRemainingMonths, setDepreciationRemainingMonths] = useState('');
  const [depreciationAccumulated, setDepreciationAccumulated] = useState('');
  const [depreciationRemainingValue, setDepreciationRemainingValue] = useState('');
  const [maintenanceTemplates, setMaintenanceTemplates] = useState<MaintenanceTemplateItem[]>([]);
  const [templateFormOpen, setTemplateFormOpen] = useState(false);
  const [templateFormSubmitting, setTemplateFormSubmitting] = useState(false);
  const [templateForm, setTemplateForm] = useState<MaintenanceTemplateForm>({
    assetTypeId: null,
    name: '',
    content: '',
    frequencyType: 2,
    repeatIntervalValue: 1,
    repeatIntervalUnit: 3,
    isActive: true,
  });

  useEffect(() => {
    if (!assetId || Number.isNaN(assetId)) {
      setError('ID tài sản không hợp lệ.');
      setLoading(false);
      return;
    }

    let isMounted = true;
    setLoading(true);
    prevDeptRef.current = null;

    assetService
      .getById(assetId)
      .then(async (data) => {
        if (!isMounted) return;
        setAsset(data);
        const primary = data.instances?.[0];
        setCode(data.code);
        setName(data.name);
        setAssetTypeId(String(data.assetTypeId));
        setPurchaseDate(toDateInput(primary?.purchaseDate ?? data.inUseDate));
        setOriginalPrice(primary != null ? String(primary.originalPrice) : '');
        setCurrentValue(primary != null ? String(primary.currentValue) : '');
        setUnit(data.unit || 'Cái');
        setQuantity(String(data.quantity ?? ''));
        setWarehouseId(primary != null ? String(primary.warehouseId) : '');
        setDepartmentId(
          primary?.currentDepartmentId != null && primary.currentDepartmentId > 0
            ? String(primary.currentDepartmentId)
            : ''
        );
        setManagerEmployeeId(
          primary?.currentResponsibleEmployeeId != null && primary.currentResponsibleEmployeeId > 0
            ? String(primary.currentResponsibleEmployeeId)
            : ''
        );
        setSupplierId(
          primary?.supplierId != null && primary.supplierId > 0 ? String(primary.supplierId) : ''
        );
        setSpecification(data.specification ?? '');
        setNote(data.note ?? '');
        setContractNumber(primary?.contractNo ?? '');
        setSerialNumber(primary?.serialNumber ?? '');
        setOrigin('');
        setIsFixedAsset(true);

        setWarrantyMonths(
          primary?.warrantyPeriodValue != null ? String(primary.warrantyPeriodValue) : ''
        );
        setWarrantyCondition(primary?.warrantyConditions ?? '');
        setWarrantyStartDate(toDateInput(primary?.warrantyStartDate));
        setWarrantyEndDate(toDateInput(primary?.warrantyEndDate));

        setDepreciationBaseValue(
          primary?.depreciationAmount != null ? String(primary.depreciationAmount) : ''
        );
        setDepreciationStartDate(toDateInput(primary?.depreciationPeriod));
        setDepreciationRemainingMonths('');
        setDepreciationAccumulated(
          primary?.accumulatedDepreciation != null ? String(primary.accumulatedDepreciation) : ''
        );
        setDepreciationRemainingValue(
          primary?.remainingValue != null ? String(primary.remainingValue) : ''
        );

        try {
          const [deptList, types, wh, sup] = await Promise.all([
            transferRequestService.getAssetLocations(),
            assetService.getAssetTypes(),
            assetService.getWarehouses(),
            assetService.getSuppliers(),
          ]);
          if (!isMounted) return;
          setDepartments(deptList);
          setAssetTypes(types);
          setWarehouses(wh);
          setSuppliers(sup.map((s) => ({ supplierId: s.supplierId, name: s.name, code: s.code })));
          setLoadMetaError(null);
        } catch {
          if (!isMounted) return;
          setLoadMetaError(
            'Không tải được danh mục (phòng ban, loại tài sản, kho, nhà cung cấp).'
          );
        }

        const templates = await maintenanceTemplateService.getAll().catch(() => []);
        if (!isMounted) return;
        setMaintenanceTemplates(
          (Array.isArray(templates) ? templates : []).filter(
            (t) => t.assetTypeId === data.assetTypeId
          )
        );
      })
      .catch(() => {
        if (!isMounted) return;
        setError('Không tải được thông tin tài sản.');
      })
      .finally(() => {
        if (isMounted) setLoading(false);
      });

    return () => {
      isMounted = false;
    };
  }, [assetId]);

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

  const resetTemplateForm = () => {
    setTemplateForm({
      assetTypeId: asset?.assetTypeId ?? null,
      name: '',
      content: '',
      frequencyType: 2,
      repeatIntervalValue: 1,
      repeatIntervalUnit: 3,
      isActive: true,
    });
  };

  const openTemplateFormModal = () => {
    if (!asset?.assetTypeId) {
      message.warning('Không xác định được loại tài sản.');
      return;
    }
    setTemplateForm({
      assetTypeId: asset.assetTypeId,
      name: '',
      content: '',
      frequencyType: 2,
      repeatIntervalValue: 1,
      repeatIntervalUnit: 3,
      isActive: true,
    });
    setTemplateFormOpen(true);
  };

  const reloadTemplatesForAssetType = async () => {
    if (!asset?.assetTypeId) return;
    try {
      const all = await maintenanceTemplateService.getAll();
      setMaintenanceTemplates(
        (Array.isArray(all) ? all : []).filter((t) => t.assetTypeId === asset.assetTypeId)
      );
    } catch {
      setMaintenanceTemplates([]);
    }
  };

  const submitTemplateForm = async () => {
    if (!templateForm.assetTypeId) {
      message.warning('Vui lòng chọn loại tài sản.');
      return;
    }
    if (!templateForm.name.trim()) {
      message.warning('Vui lòng nhập tên quy định.');
      return;
    }
    if (!templateForm.content.trim()) {
      message.warning('Vui lòng nhập nội dung bảo dưỡng.');
      return;
    }

    const isOneTime = templateForm.frequencyType === 1;
    const payload = {
      assetTypeId: templateForm.assetTypeId,
      name: templateForm.name.trim(),
      content: templateForm.content.trim(),
      frequencyType: templateForm.frequencyType,
      repeatIntervalValue: isOneTime ? 0 : Math.max(1, Number(templateForm.repeatIntervalValue || 1)),
      repeatIntervalUnit: isOneTime ? 0 : templateForm.repeatIntervalUnit,
      isActive: templateForm.isActive,
    } as const;

    setTemplateFormSubmitting(true);
    try {
      await maintenanceTemplateService.create(payload);
      message.success('Thêm quy định bảo dưỡng thành công.');
      setTemplateFormOpen(false);
      resetTemplateForm();
      await reloadTemplatesForAssetType();
    } catch (error) {
      message.error(getApiErrorMessage(error, 'Lưu quy định bảo dưỡng thất bại.'));
    } finally {
      setTemplateFormSubmitting(false);
    }
  };

  const handleSubmit = async (e: FormEvent) => {
    e.preventDefault();
    if (!assetId || Number.isNaN(assetId)) return;

    if (!name.trim() || !assetTypeId || !purchaseDate) {
      alert('Vui lòng nhập đầy đủ các trường bắt buộc được đánh dấu *.');
      return;
    }
    const isAccountant = currentRole === 'accountant';
    const primary = asset?.instances?.[0];
    if (primary) {
      if (!warehouseId || Number(warehouseId) <= 0) {
        alert('Vui lòng chọn kho.');
        return;
      }
      if (isAccountant && (!departmentId || Number(departmentId) <= 0)) {
        alert('Vui lòng chọn vị trí tài sản (phòng ban).');
        return;
      }
    }
    const qty = Math.max(1, Number(quantity || 1));
    const purchaseIso = toDateOnly(purchaseDate);
    const catalogPayload: UpdateAssetPayload = {
      code: code.trim(),
      name: name.trim(),
      assetTypeId: assetTypeId ? Number(assetTypeId) : undefined,
      unit: unit || undefined,
      quantity: quantity ? Number(quantity) : undefined,
      inUseDate: purchaseIso ?? null,
      specification: specification.trim() || null,
      note: note.trim() || null,
    };

    const deptId =
      isAccountant && departmentId && Number(departmentId) > 0
        ? Number(departmentId)
        : null;
    const managerId =
      isAccountant && managerEmployeeId && Number(managerEmployeeId) > 0
        ? Number(managerEmployeeId)
        : null;
    const supId = supplierId && Number(supplierId) > 0 ? Number(supplierId) : null;

    const hasWarrantyGroup =
      warrantyMonths.trim().length > 0 ||
      warrantyCondition.trim().length > 0 ||
      warrantyEndDate.trim().length > 0;
    const warrantyStart = toDateOnly(warrantyStartDate) ?? purchaseIso;
    const warrantyEnd = toDateOnly(warrantyEndDate);

    let instancePayload: Parameters<typeof assetInstanceService.update>[1] | null = null;
    if (primary) {
      instancePayload = {
        warehouseId: Number(warehouseId),
        purchaseDate: purchaseIso,
        originalPrice: Number(originalPrice || 0),
        currentValue: Number(currentValue || 0),
        serialNumber: qty > 1 ? null : serialNumber.trim() || null,
        contractNo: contractNumber.trim() || null,
        condition: specification.trim() || null,
        supplierId: supId,
        assignedDepartmentId: deptId,
        responsibleEmployeeId: managerId,
        assignmentEffectiveDate:
          isAccountant && (deptId != null || managerId != null) ? purchaseIso ?? null : null,
      };

      if (hasWarrantyGroup) {
        if (!warrantyMonths.trim() || !warrantyEnd || !warrantyStart) {
          alert(
            'Bảo hành: nhập thời gian bảo hành (tháng), và chọn hạn bảo hành (hoặc đảm bảo có ngày mua để dùng làm ngày bắt đầu).'
          );
          return;
        }
        const wm = Number(warrantyMonths);
        if (!Number.isFinite(wm) || wm <= 0) {
          alert('Thời gian bảo hành phải là số dương.');
          return;
        }
        instancePayload.warrantyPeriodValue = wm;
        instancePayload.warrantyPeriodUnit = 'month';
        instancePayload.warrantyConditions = warrantyCondition.trim() || null;
        instancePayload.warrantyStartDate = warrantyStart;
        instancePayload.warrantyEndDate = warrantyEnd;
      }

      const hasDepreciationGroup =
        depreciationBaseValue.trim().length > 0 ||
        depreciationStartDate.trim().length > 0 ||
        depreciationRemainingMonths.trim().length > 0 ||
        depreciationAccumulated.trim().length > 0 ||
        depreciationRemainingValue.trim().length > 0;

      if (hasDepreciationGroup) {
        instancePayload.depreciationPeriod = toDateOnly(depreciationStartDate) ?? null;
        instancePayload.depreciationAmount = depreciationBaseValue.trim()
          ? Number(depreciationBaseValue)
          : undefined;
        instancePayload.accumulatedDepreciation = depreciationAccumulated.trim()
          ? Number(depreciationAccumulated)
          : undefined;
        instancePayload.remainingValue = depreciationRemainingValue.trim()
          ? Number(depreciationRemainingValue)
          : undefined;
      }
    }

    setSaving(true);
    setError(null);

    try {
      await assetService.update(assetId, catalogPayload);
      if (primary && instancePayload) {
        await assetInstanceService.update(primary.assetInstanceId, instancePayload);
      }
      navigate(backToListPath);
    } catch (err: unknown) {
      const msg =
        err && typeof err === 'object' && 'response' in err
          ? (err as { response?: { data?: { message?: string } } }).response?.data?.message
          : null;
      setError(msg || 'Cập nhật tài sản thất bại. Vui lòng thử lại.');
    } finally {
      setSaving(false);
    }
  };

  if (loading) {
    return <div className="asset-create-page">Đang tải thông tin tài sản...</div>;
  }

  if (error && !asset) {
    return <div className="asset-create-page">{error}</div>;
  }

  return (
    <div className="asset-create-page">
      <div className="asset-create__header">
        <Link to={backToListPath} className="asset-create__back">
          ← Tất cả tài sản
        </Link>
        <div className="asset-create__title-row">
          <h1 className="asset-create__title">Sửa tài sản</h1>
          <span className="asset-create__status">{asset?.statusName ?? 'Đang sử dụng'}</span>
          <div className="asset-create__header-actions">
            <button
              type="button"
              className="asset-create__btn asset-create__btn--secondary"
              onClick={() => navigate(backToListPath)}
            >
              Hủy
            </button>
            <button
              type="submit"
              form="asset-edit-form"
              className="asset-create__btn asset-create__btn--primary"
              disabled={saving}
            >
              {saving ? 'Đang lưu...' : 'Lưu'}
            </button>
          </div>
        </div>
      </div>

      <form id="asset-edit-form" onSubmit={handleSubmit} className="asset-create__card">
        {loadMetaError && (
          <div className="asset-create__error" role="alert">
            {loadMetaError}
          </div>
        )}
        {error && <div className="asset-create__error">{error}</div>}

        <section className="asset-create__section">
          <h2 className="asset-create__section-title">Thông tin chung</h2>
          <div className="asset-create__grid asset-create__grid--two">
            <div className="asset-create__column">
              <div className="asset-create__field">
                <label className="asset-create__label">
                  Mã tài sản<span className="asset-create__required">*</span>
                </label>
                <input
                  className="asset-create__input"
                  value={code}
                  onChange={(e) => setCode(e.target.value)}
                />
              </div>

              <div className="asset-create__field">
                <label className="asset-create__label">
                  Loại tài sản<span className="asset-create__required">*</span>
                </label>
                <select
                  className="asset-create__select"
                  value={assetTypeId}
                  onChange={(e) => setAssetTypeId(e.target.value)}
                >
                  <option value="">Chọn loại tài sản</option>
                  {assetTypes.map((t) => (
                    <option key={t.assetTypeId} value={t.assetTypeId}>
                      {t.name}
                    </option>
                  ))}
                </select>
              </div>

              <div className="asset-create__field">
                <label className="asset-create__label">
                  Tên tài sản<span className="asset-create__required">*</span>
                </label>
                <input
                  className="asset-create__input"
                  value={name}
                  onChange={(e) => setName(e.target.value)}
                />
              </div>

              <div className="asset-create__field asset-create__field--quantity-unit-row">
                <div className="asset-create__quantity-unit-cell">
                  <label className="asset-create__label">Số lượng</label>
                  <input
                    type="number"
                    min={1}
                    className="asset-create__input"
                    value={quantity}
                    onChange={(e) => setQuantity(e.target.value)}
                  />
                </div>
                <div className="asset-create__quantity-unit-cell">
                  <label className="asset-create__label">Đơn vị tính</label>
                  <select
                    className="asset-create__select"
                    value={unit}
                    onChange={(e) => setUnit(e.target.value)}
                  >
                    <option value="">Chọn đơn vị tính</option>
                    {ASSET_MEASUREMENT_UNITS.map((u) => (
                      <option key={u} value={u}>
                        {u}
                      </option>
                    ))}
                  </select>
                </div>
              </div>

              <div className="asset-create__field">
                <label className="asset-create__label">Giá trị</label>
                <input
                  type="number"
                  min={0}
                  className="asset-create__input"
                  value={originalPrice}
                  onChange={(e) => setOriginalPrice(e.target.value)}
                />
              </div>

              <div className="asset-create__field">
                <label className="asset-create__label">Giá trị hiện tại</label>
                <input
                  type="number"
                  min={0}
                  className="asset-create__input"
                  value={currentValue}
                  onChange={(e) => setCurrentValue(e.target.value)}
                />
              </div>

              <div className="asset-create__field">
                <label className="asset-create__label">Nguồn gốc</label>
                <input
                  className="asset-create__input"
                  value={origin}
                  onChange={(e) => setOrigin(e.target.value)}
                />
              </div>

              <label className="asset-create__checkbox-row">
                <input
                  type="checkbox"
                  checked={isFixedAsset}
                  onChange={(e) => setIsFixedAsset(e.target.checked)}
                />
                <span>Là tài sản cố định</span>
              </label>
            </div>

            <div className="asset-create__column">
              <div className="asset-create__field">
                <label className="asset-create__label">
                  Vị trí tài sản
                  {currentRole === 'accountant' && (
                    <span className="asset-create__required">*</span>
                  )}
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
                  value={contractNumber}
                  onChange={(e) => setContractNumber(e.target.value)}
                />
              </div>

              <div className="asset-create__field">
                <label className="asset-create__label">Số serial</label>
                <input
                  className="asset-create__input"
                  value={serialNumber}
                  onChange={(e) => setSerialNumber(e.target.value)}
                  disabled={Number(quantity || 1) > 1}
                />
                {Number(quantity || 1) > 1 && (
                  <p className="asset-create__hint">Chỉ áp dụng khi số lượng là 1.</p>
                )}
              </div>

              <div className="asset-create__field">
                <label className="asset-create__label">Quy cách tài sản</label>
                <input
                  className="asset-create__input"
                  value={specification}
                  onChange={(e) => setSpecification(e.target.value)}
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
          <div className="asset-create__section-header">
            <h2 className="asset-create__section-title">Quy định bảo dưỡng</h2>
            <button
              type="button"
              className="asset-create__btn asset-create__btn--danger"
              onClick={openTemplateFormModal}
            >
              + Thêm quy định bảo dưỡng
            </button>
          </div>
          <table className="asset-create__maintenance-table">
            <thead>
              <tr>
                <th>Tên quy định</th>
                <th>Nội dung bảo dưỡng</th>
                <th>Tần suất bảo dưỡng</th>
                <th>Lặp lại theo</th>
                <th>Trạng thái</th>
              </tr>
            </thead>
            <tbody>
              {maintenanceTemplates.length > 0 ? (
                maintenanceTemplates.map((t) => (
                  <tr key={t.templateId}>
                    <td>{t.name?.trim() || '—'}</td>
                    <td>{t.content?.trim() || '—'}</td>
                    <td>{getFrequencyLabel(t.frequencyType)}</td>
                    <td>
                      {Number(t.repeatIntervalValue || 0) > 0
                        ? `${t.repeatIntervalValue} ${getRepeatUnitLabel(t.repeatIntervalUnit)}`
                        : '—'}
                    </td>
                    <td>{t.isActive ? 'Đang áp dụng' : 'Ngưng áp dụng'}</td>
                  </tr>
                ))
              ) : (
                <tr>
                  <td colSpan={5}>Chưa có quy định bảo dưỡng cho loại tài sản này.</td>
                </tr>
              )}
            </tbody>
          </table>
        </section>

        <section className="asset-create__section">
          <h2 className="asset-create__section-title">Bảo hành</h2>
          <div className="asset-create__grid asset-create__grid--three">
            <div className="asset-create__field">
              <label className="asset-create__label">Thời gian bảo hành</label>
              <div className="asset-create__field--inline">
                <input
                  type="number"
                  min={0}
                  className="asset-create__input"
                  value={warrantyMonths}
                  onChange={(e) => setWarrantyMonths(e.target.value)}
                />
                <span className="asset-create__suffix">Tháng</span>
              </div>
            </div>
            <div className="asset-create__field">
              <label className="asset-create__label">Điều kiện bảo hành</label>
              <input
                className="asset-create__input"
                value={warrantyCondition}
                onChange={(e) => setWarrantyCondition(e.target.value)}
              />
            </div>
            <div className="asset-create__field">
              <label className="asset-create__label">Hạn bảo hành</label>
              <input
                type="date"
                className="asset-create__input"
                value={warrantyEndDate}
                onChange={(e) => setWarrantyEndDate(e.target.value)}
              />
            </div>
          </div>
        </section>

        <section className="asset-create__section">
          <h2 className="asset-create__section-title">Thông tin khấu hao</h2>
          <div className="asset-create__grid asset-create__grid--three">
            <div className="asset-create__field">
              <label className="asset-create__label">Giá trị tính khấu hao</label>
              <input
                type="number"
                min={0}
                className="asset-create__input"
                value={depreciationBaseValue}
                onChange={(e) => setDepreciationBaseValue(e.target.value)}
              />
            </div>
            <div className="asset-create__field">
              <label className="asset-create__label">Thời gian còn lại</label>
              <div className="asset-create__field--inline">
                <input
                  type="number"
                  min={0}
                  className="asset-create__input"
                  value={depreciationRemainingMonths}
                  onChange={(e) => setDepreciationRemainingMonths(e.target.value)}
                />
                <span className="asset-create__suffix">Tháng</span>
              </div>
            </div>
            <div className="asset-create__field">
              <label className="asset-create__label">Giá trị còn lại</label>
              <input
                type="number"
                min={0}
                className="asset-create__input"
                value={depreciationRemainingValue}
                onChange={(e) => setDepreciationRemainingValue(e.target.value)}
              />
            </div>
            <div className="asset-create__field">
              <label className="asset-create__label">Ngày bắt đầu khấu hao</label>
              <input
                type="date"
                className="asset-create__input"
                value={depreciationStartDate}
                onChange={(e) => setDepreciationStartDate(e.target.value)}
              />
            </div>
            <div className="asset-create__field">
              <label className="asset-create__label">Khấu hao lũy kế</label>
              <input
                type="number"
                min={0}
                className="asset-create__input"
                value={depreciationAccumulated}
                onChange={(e) => setDepreciationAccumulated(e.target.value)}
              />
            </div>
            <div className="asset-create__field">
              <label className="asset-create__label">Thời gian khấu hao</label>
              <div className="asset-create__field--inline">
                <input
                  className="asset-create__input"
                  value={
                    asset?.instances?.[0]?.depreciationUsefulLifeMonths != null
                      ? String(asset.instances[0].depreciationUsefulLifeMonths)
                      : ''
                  }
                  disabled
                />
                <span className="asset-create__suffix">Tháng</span>
              </div>
            </div>
          </div>
        </section>

        <section className="asset-create__section">
          <h2 className="asset-create__section-title">Tài liệu</h2>
          <div className="asset-create__files">
            <div className="asset-create__file">
              <span className="asset-create__file-index">#1</span>
              <span className="asset-create__file-name">Tài liệu đính kèm</span>
            </div>
            <div className="asset-create__file">
              <span className="asset-create__file-index">#2</span>
              <span className="asset-create__file-name">Tài liệu đính kèm</span>
            </div>
          </div>
          <div className="asset-create__file-actions">
            <button type="button" className="asset-create__btn asset-create__btn--danger">
              Tải toàn bộ
            </button>
            <button type="button" className="asset-create__btn asset-create__btn--danger">
              Thêm tài liệu
            </button>
          </div>
        </section>
      </form>

      {templateFormOpen && asset ? (
        <div className="template-form-modal-overlay" role="dialog" aria-modal="true">
          <div className="template-form-modal">
            <button
              type="button"
              className="template-form-modal__close-btn"
              onClick={() => {
                setTemplateFormOpen(false);
                resetTemplateForm();
              }}
              aria-label="Đóng"
            >
              <span className="template-form-modal__close">×</span>
            </button>
            <div className="template-form-modal__header">
              <h2 className="template-form-modal__title">Thêm quy định bảo dưỡng</h2>
            </div>
            <div className="template-form-modal__body">
              <div className="template-form-modal__grid">
                <div className="template-form-modal__item">
                  <label htmlFor="asset-edit-template-asset-type">Loại tài sản</label>
                  <select
                    id="asset-edit-template-asset-type"
                    className="template-form-modal__input"
                    value={templateForm.assetTypeId ?? ''}
                    disabled
                  >
                    <option value={templateForm.assetTypeId ?? ''}>
                      {assetTypes.find((t) => t.assetTypeId === templateForm.assetTypeId)?.name ?? '—'}
                    </option>
                  </select>
                </div>
                <div className="template-form-modal__item">
                  <label htmlFor="asset-edit-template-name">Tên quy định</label>
                  <input
                    id="asset-edit-template-name"
                    className="template-form-modal__input"
                    value={templateForm.name}
                    onChange={(e) => setTemplateForm((prev) => ({ ...prev, name: e.target.value }))}
                    placeholder="Nhập tên quy định"
                  />
                </div>
                <div className="template-form-modal__item template-form-modal__item--full">
                  <label htmlFor="asset-edit-template-content">Nội dung bảo dưỡng</label>
                  <textarea
                    id="asset-edit-template-content"
                    className="template-form-modal__textarea"
                    rows={4}
                    value={templateForm.content}
                    onChange={(e) =>
                      setTemplateForm((prev) => ({ ...prev, content: e.target.value }))
                    }
                    placeholder="Nhập nội dung bảo dưỡng"
                  />
                </div>
                <div className="template-form-modal__item template-form-modal__item--full">
                  <label>Tần suất bảo dưỡng</label>
                  <div className="template-form-modal__radio-group">
                    <label>
                      <input
                        type="radio"
                        checked={templateForm.frequencyType === 1}
                        onChange={() => setTemplateForm((prev) => ({ ...prev, frequencyType: 1 }))}
                      />{' '}
                      Một lần
                    </label>
                    <label>
                      <input
                        type="radio"
                        checked={templateForm.frequencyType === 2}
                        onChange={() => setTemplateForm((prev) => ({ ...prev, frequencyType: 2 }))}
                      />{' '}
                      Định kỳ
                    </label>
                  </div>
                </div>
                <div className="template-form-modal__item">
                  <label htmlFor="asset-edit-template-repeat-value">Giá trị lặp lại</label>
                  <input
                    id="asset-edit-template-repeat-value"
                    className="template-form-modal__input"
                    type="number"
                    min={1}
                    value={templateForm.repeatIntervalValue}
                    disabled={templateForm.frequencyType === 1}
                    onChange={(e) =>
                      setTemplateForm((prev) => ({
                        ...prev,
                        repeatIntervalValue: Math.max(1, Number(e.target.value || 1)),
                      }))
                    }
                  />
                </div>
                <div className="template-form-modal__item">
                  <label htmlFor="asset-edit-template-repeat-unit">Lặp lại theo</label>
                  <select
                    id="asset-edit-template-repeat-unit"
                    className="template-form-modal__input"
                    value={templateForm.repeatIntervalUnit}
                    disabled={templateForm.frequencyType === 1}
                    onChange={(e) =>
                      setTemplateForm((prev) => ({
                        ...prev,
                        repeatIntervalUnit: Number(e.target.value) as 1 | 2 | 3 | 4,
                      }))
                    }
                  >
                    <option value={1}>Ngày</option>
                    <option value={2}>Tuần</option>
                    <option value={3}>Tháng</option>
                    <option value={4}>Năm</option>
                  </select>
                </div>
              </div>
            </div>
            <div className="template-form-modal__footer">
              <button
                type="button"
                className="template-form-modal__btn-secondary"
                disabled={templateFormSubmitting}
                onClick={() => {
                  setTemplateFormOpen(false);
                  resetTemplateForm();
                }}
              >
                Hủy
              </button>
              <button
                type="button"
                className="template-form-modal__btn-primary"
                disabled={templateFormSubmitting}
                onClick={submitTemplateForm}
              >
                {templateFormSubmitting ? 'Đang lưu...' : 'Lưu'}
              </button>
            </div>
          </div>
        </div>
      ) : null}
    </div>
  );
}


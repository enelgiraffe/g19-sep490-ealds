import { useEffect, useRef, useState } from 'react';
import type { FormEvent } from 'react';
import { Link, useNavigate } from 'react-router-dom';
import {
  assetService,
  assetInstanceService,
  ASSET_CATALOG_DOCUMENT_TYPE,
  ASSET_MEASUREMENT_UNITS,
  type CreateAssetPayload,
} from '../services/assetService';
import { ASSET_DOCUMENT_FILE_ACCEPT, isAllowedAssetDocumentFile, uploadAssetFile } from '../services/assetDocumentUploadService';
import { DISALLOWED_DOCUMENT_TYPE_MESSAGE } from '../../../shared/utils/allowedDocumentFiles';
import { message } from 'antd';
import { transferRequestService, type AssetLocationOption } from '../services/transferRequestService';
import { useAppStore } from '../../../stores/appStore';
import { profileService } from '../../profile/services/profileService';
import './AssetCreatePage.css';

interface GeneralInfoForm {
  assetCodePrefix: string;
  name: string;
  assetTypeId: string;
  departmentId: string;
  managerEmployeeId: string;
  warehouseId: string;
  purchaseDate: string;
  supplierId: string;
  contractNumber: string;
  serialNumber: string;
  specification: string;
  quantity: string;
  unit: string;
  instanceCodePrefix: string;
  value: string;
  origin: string;
  note: string;
  isFixedAsset: boolean;
}

interface WarrantyForm {
  durationMonths: string;
  conditions: string;
  expiryDate: string;
}

interface DepreciationForm {
  baseValue: string;
  startDate: string;
  lifeTimeMonths: string;
  accumulatedDepreciation: string;
  remainingValue: string;
  depreciationPolicyId: string;
}

interface AllocationForm {
  allocateNow: boolean;
  usageTarget: string;
}

type AssetFormDocRow = {
  id: string;
  fileName: string;
  url?: string;
  uploading?: boolean;
  error?: string;
};

function fromDisplayDate(display: string): string {
  const m = display.match(/^(\d{1,2})\/(\d{1,2})\/(\d{4})$/);
  if (!m) return '';
  const d = m[1].padStart(2, '0');
  const mo = m[2].padStart(2, '0');
  return `${m[3]}-${mo}-${d}`;
}

export function AssetCreatePage() {
  const navigate = useNavigate();
  const currentRole = useAppStore((s) => s.currentRole);

  const backToListPath = currentRole === 'accountant' ? '/accountant-assets' : '/assets';

  /** Chỉ tạo bản ghi tài sản (Asset), chưa tạo cá thể / nhập kho — phù hợp luồng đơn mua. */
  const [onlyMasterAsset, setOnlyMasterAsset] = useState(currentRole === 'accountant');
  const [actorUserId, setActorUserId] = useState(0);

  const [general, setGeneral] = useState<GeneralInfoForm>({
    assetCodePrefix: '',
    name: '',
    assetTypeId: '',
    departmentId: '',
    managerEmployeeId: '',
    warehouseId: '',
    purchaseDate: '',
    supplierId: '',
    contractNumber: '',
    serialNumber: '',
    specification: '',
    quantity: '1',
    unit: 'Cái',
    instanceCodePrefix: '',
    value: '',
    origin: '',
    note: '',
    isFixedAsset: true,
  });

  const [warranty, setWarranty] = useState<WarrantyForm>({
    durationMonths: '',
    conditions: '',
    expiryDate: '',
  });
  const [warrantyExpiryText, setWarrantyExpiryText] = useState('');

  const [depreciation, setDepreciation] = useState<DepreciationForm>({
    baseValue: '',
    startDate: '',
    lifeTimeMonths: '',
    accumulatedDepreciation: '',
    remainingValue: '',
    depreciationPolicyId: '',
  });

  const [allocation, setAllocation] = useState<AllocationForm>({
    allocateNow: false,
    usageTarget: '',
  });

  const [isSubmitting, setIsSubmitting] = useState(false);
  const [submitError, setSubmitError] = useState<string | null>(null);

  const [departments, setDepartments] = useState<AssetLocationOption[]>([]);
  const [assetTypes, setAssetTypes] = useState<{ assetTypeId: number; name: string }[]>([]);
  const [warehouses, setWarehouses] = useState<{ warehouseId: number; name: string }[]>([]);
  const [suppliers, setSuppliers] = useState<{ supplierId: number; name: string; code: string }[]>(
    []
  );
  const [instancePrefixes, setInstancePrefixes] = useState<string[]>([]);
  const [assetCatalogPrefixes, setAssetCatalogPrefixes] = useState<string[]>([]);
  const [deptEmployees, setDeptEmployees] = useState<
    { employeeId: number; name: string; code: string }[]
  >([]);
  const [loadMetaError, setLoadMetaError] = useState<string | null>(null);

  const [documents, setDocuments] = useState<AssetFormDocRow[]>([]);
  const docFileInputRef = useRef<HTMLInputElement | null>(null);

  const pickDocument = () => docFileInputRef.current?.click();

  const onDocFileSelected = async (e: React.ChangeEvent<HTMLInputElement>) => {
    const file = e.target.files?.[0];
    e.target.value = '';
    if (!file) return;
    if (!isAllowedAssetDocumentFile(file)) {
      message.error(DISALLOWED_DOCUMENT_TYPE_MESSAGE);
      return;
    }
    const rowId = crypto.randomUUID();
    setDocuments((prev) => [...prev, { id: rowId, fileName: file.name, uploading: true }]);
    try {
      const { url, fileName } = await uploadAssetFile(file);
      setDocuments((prev) =>
        prev.map((d) =>
          d.id === rowId
            ? { ...d, url, fileName: fileName || file.name, uploading: false, error: undefined }
            : d
        )
      );
    } catch {
      setDocuments((prev) =>
        prev.map((d) =>
          d.id === rowId ? { ...d, uploading: false, error: 'Tải lên thất bại.' } : d
        )
      );
    }
  };

  const removeDocumentRow = (id: string) => {
    setDocuments((prev) => prev.filter((d) => d.id !== id));
  };

  const downloadAllDocumentUrls = () => {
    const urls = documents.filter((d) => d.url).map((d) => d.url as string);
    urls.forEach((u) => window.open(u, '_blank', 'noopener,noreferrer'));
  };

  useEffect(() => {
    let cancelled = false;
    (async () => {
      try {
        const p = await profileService.getProfile();
        if (!cancelled) setActorUserId(p.id);
      } catch {
        if (!cancelled) setActorUserId(0);
      }
    })();
    return () => {
      cancelled = true;
    };
  }, []);

  useEffect(() => {
    let cancelled = false;
    (async () => {
      try {
        const [deptList, types, wh, sup, instPrefixes, catPrefixes] = await Promise.all([
          transferRequestService.getAssetLocations(),
          assetService.getAssetTypes(),
          assetService.getWarehouses(),
          assetService.getSuppliers(),
          assetService.getInstanceCodePrefixes(),
          assetService.getAssetCodePrefixes(),
        ]);
        if (cancelled) return;
        setDepartments(deptList);
        setAssetTypes(types);
        setWarehouses(wh);
        setGeneral((g) =>
          g.warehouseId === '' && wh.length > 0
            ? { ...g, warehouseId: String(wh[0].warehouseId) }
            : g
        );
        setSuppliers(sup.map((s) => ({ supplierId: s.supplierId, name: s.name, code: s.code })));
        setInstancePrefixes(instPrefixes);
        setAssetCatalogPrefixes(catPrefixes);
        setLoadMetaError(null);
      } catch {
        if (!cancelled) {
          setLoadMetaError(
            'Không tải được dữ liệu tham chiếu (phòng ban, loại tài sản, kho, nhà cung cấp).'
          );
        }
      }
    })();
    return () => {
      cancelled = true;
    };
  }, []);

  useEffect(() => {
    const deptId = general.departmentId;
    if (!deptId || Number(deptId) <= 0) {
      setDeptEmployees([]);
      setGeneral((g) => ({ ...g, managerEmployeeId: '' }));
      return;
    }

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
        setGeneral((g) => ({
          ...g,
          managerEmployeeId:
            sorted.length > 0 ? String(sorted[0].employeeId) : '',
        }));
      } catch {
        if (!cancelled) {
          setDeptEmployees([]);
          setGeneral((g) => ({ ...g, managerEmployeeId: '' }));
        }
      }
    })();

    return () => {
      cancelled = true;
    };
  }, [general.departmentId]);

  const handleSubmit = async (e: FormEvent) => {
    e.preventDefault();
    if (documents.some((d) => d.uploading)) {
      alert('Đang tải tài liệu lên, vui lòng đợi.');
      return;
    }
    if (documents.some((d) => d.error)) {
      alert('Có tài liệu tải lên lỗi. Xóa dòng đó hoặc thêm tài liệu khác.');
      return;
    }
    const documentPayload = documents
      .filter((d) => d.url)
      .map((d) => ({ fileUrl: d.url as string, documentType: ASSET_CATALOG_DOCUMENT_TYPE }));

    if (onlyMasterAsset) {
      if (!general.name?.trim() || !general.assetTypeId || !general.assetCodePrefix.trim()) {
        alert('Vui lòng nhập mã tài sản, loại và tên tài sản.');
        return;
      }
      if (documentPayload.length > 0 && actorUserId <= 0) {
        alert('Cần đăng nhập hợp lệ để đính kèm tài liệu (upload yêu cầu người dùng).');
        return;
      }
      setSubmitError(null);
      setIsSubmitting(true);
      try {
        await assetService.create({
          assetCodePrefix: general.assetCodePrefix.trim(),
          name: general.name.trim(),
          assetTypeId: Number(general.assetTypeId),
          unit: general.unit || 'Cái',
          quantity: null,
          createdBy: actorUserId > 0 ? actorUserId : 0,
          specification: general.specification?.trim() || null,
          note: general.note?.trim() || null,
          inUseDate: null,
          documents: documentPayload.length > 0 ? documentPayload : undefined,
        });
        navigate(backToListPath);
      } catch (err: unknown) {
        const msg =
          err && typeof err === 'object' && 'response' in err
            ? (err as { response?: { data?: { message?: string } } }).response?.data?.message
            : null;
        setSubmitError(msg || 'Tạo bản ghi tài sản (chưa kèm cá thể) thất bại.');
      } finally {
        setIsSubmitting(false);
      }
      return;
    }

    if (!general.name || !general.assetTypeId || !general.purchaseDate) {
      alert('Vui lòng nhập đầy đủ các trường bắt buộc được đánh dấu *.');
      return;
    }
    const isAccountant = currentRole === 'accountant';
    if (isAccountant && (!general.departmentId || Number(general.departmentId) <= 0)) {
      alert('Vui lòng chọn vị trí tài sản (phòng ban).');
      return;
    }
    const assetPrefix = general.assetCodePrefix.trim();
    if (!assetPrefix) {
      alert('Vui lòng nhập mã tài sản (tiền tố mã).');
      return;
    }

    const prefix = general.instanceCodePrefix.trim();
    const qty = Math.max(1, Number(general.quantity || 1));
    if (!prefix) {
      alert('Vui lòng nhập mã cá thể (tiền tố mã).');
      return;
    }

    if (!general.warehouseId || Number(general.warehouseId) <= 0) {
      alert('Vui lòng chọn kho.');
      return;
    }
    const selectedWarehouseId = Number(general.warehouseId);

    setSubmitError(null);
    setIsSubmitting(true);

    if (documentPayload.length > 0 && actorUserId <= 0) {
      alert('Cần đăng nhập hợp lệ để đính kèm tài liệu.');
      setIsSubmitting(false);
      return;
    }

    const val = Number(general.value || 0);
    const deptId =
      isAccountant && general.departmentId && Number(general.departmentId) > 0
        ? Number(general.departmentId)
        : null;
    const managerId =
      isAccountant && general.managerEmployeeId
        ? Number(general.managerEmployeeId)
        : null;
    const supplierId = general.supplierId ? Number(general.supplierId) : null;

    const payload: CreateAssetPayload = {
      assetCodePrefix: assetPrefix,
      code: '_',
      name: general.name.trim(),
      assetTypeId: Number(general.assetTypeId),
      unit: general.unit || 'Cái',
      quantity: qty,
      instanceCodePrefix: prefix,
      createdBy: actorUserId > 0 ? actorUserId : 0,
      inUseDate: general.purchaseDate || null,
      specification: general.specification?.trim() || null,
      note: general.note?.trim() || null,
      initialInstance: {
        instanceCode: '_',
        serialNumber: qty === 1 ? general.serialNumber?.trim() || null : null,
        warehouseId: selectedWarehouseId,
        purchaseDate: general.purchaseDate,
        originalPrice: val,
        currentValue: val,
        inUseDate: general.purchaseDate || null,
        depreciationPolicyId: depreciation.depreciationPolicyId
          ? Number(depreciation.depreciationPolicyId)
          : null,
        contractNo: general.contractNumber?.trim() || null,
        condition: general.specification?.trim() || null,
        supplierId: supplierId && supplierId > 0 ? supplierId : null,
        assignedDepartmentId: deptId,
        responsibleEmployeeId: managerId,
        assignmentEffectiveDate:
          isAccountant && (deptId != null || managerId != null)
            ? general.purchaseDate || null
            : null,
      },
      documents: documentPayload.length > 0 ? documentPayload : undefined,
    };

    try {
      const created = await assetService.create(payload);
      if (general.isFixedAsset && created.instances && created.instances.length > 0) {
        await Promise.allSettled(
          created.instances.map((inst) => assetInstanceService.capitalize(inst.assetInstanceId))
        );
      }
      navigate(backToListPath);
    } catch (err: unknown) {
      const msg =
        err && typeof err === 'object' && 'response' in err
          ? (err as { response?: { data?: { message?: string } } }).response?.data?.message
          : null;
      setSubmitError(msg || 'Tạo tài sản thất bại. Kiểm tra kết nối backend hoặc dữ liệu.');
    } finally {
      setIsSubmitting(false);
    }
  };

  return (
    <div className="asset-create-page">
      <div className="asset-create__header">
        <Link to={backToListPath} className="asset-create__back">
          ← Tất cả tài sản
        </Link>
        <div className="asset-create__title-row">
          <h1 className="asset-create__title">Thêm tài sản</h1>
          <span className="asset-create__status">Đang sử dụng</span>
          <label className="asset-create__checkbox-row" style={{ marginLeft: 12 }}>
            <input
              type="checkbox"
              checked={onlyMasterAsset}
              onChange={(e) => setOnlyMasterAsset(e.target.checked)}
            />
            <span>Chỉ tạo bản ghi tài sản — chưa tạo cá thể / nhập kho</span>
          </label>
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
              form="asset-create-form"
              className="asset-create__btn asset-create__btn--primary"
              disabled={isSubmitting}
            >
              {isSubmitting ? 'Đang lưu...' : 'Lưu'}
            </button>
          </div>
        </div>
      </div>

      <form id="asset-create-form" onSubmit={handleSubmit} className="asset-create__card">
        {loadMetaError && (
          <div className="asset-create__error" role="alert">
            {loadMetaError}
          </div>
        )}
        {submitError && (
          <div className="asset-create__error" role="alert">
            {submitError}
          </div>
        )}
        {/* Thông tin chung */}
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
                  list="asset-catalog-prefix-options"
                  placeholder="Ví dụ: TS"
                  value={general.assetCodePrefix}
                  onChange={(e) =>
                    setGeneral({ ...general, assetCodePrefix: e.target.value })
                  }
                />
                <datalist id="asset-catalog-prefix-options">
                  {assetCatalogPrefixes.map((p) => (
                    <option key={p} value={p} />
                  ))}
                </datalist>
                <p className="asset-create__hint">
                  Hệ thống gắn số thứ tự tự động cho mã tài sản (ví dụ TS → TS01).
                </p>
              </div>

              <div className="asset-create__field">
                <label className="asset-create__label">
                  Loại tài sản<span className="asset-create__required">*</span>
                </label>
                <select
                  className="asset-create__select"
                  value={general.assetTypeId}
                  onChange={(e) => setGeneral({ ...general, assetTypeId: e.target.value })}
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
                  value={general.name}
                  onChange={(e) => setGeneral({ ...general, name: e.target.value })}
                />
              </div>

              {!onlyMasterAsset && (
                <div className="asset-create__field">
                  <label className="asset-create__label">
                    Mã cá thể<span className="asset-create__required">*</span>
                  </label>
                  <input
                    className="asset-create__input"
                    list="asset-instance-prefix-options"
                    placeholder="Ví dụ: LAP"
                    value={general.instanceCodePrefix}
                    onChange={(e) =>
                      setGeneral({ ...general, instanceCodePrefix: e.target.value })
                    }
                  />
                  <datalist id="asset-instance-prefix-options">
                    {instancePrefixes.map((p) => (
                      <option key={p} value={p} />
                    ))}
                  </datalist>
                  <p className="asset-create__hint">
                    Hệ thống gắn số thứ tự tự động (ví dụ LAP → LAP01, LAP02…).
                  </p>
                </div>
              )}

              {onlyMasterAsset ? (
                <div className="asset-create__field">
                  <label className="asset-create__label">Đơn vị tính</label>
                  <select
                    className="asset-create__select"
                    value={general.unit}
                    onChange={(e) => setGeneral({ ...general, unit: e.target.value })}
                  >
                    <option value="">Chọn đơn vị tính</option>
                    {ASSET_MEASUREMENT_UNITS.map((u) => (
                      <option key={u} value={u}>
                        {u}
                      </option>
                    ))}
                  </select>
                </div>
              ) : (
                <div className="asset-create__field asset-create__field--quantity-unit-row">
                  <div className="asset-create__quantity-unit-cell">
                    <label className="asset-create__label">Số lượng (số cá thể tạo)</label>
                    <input
                      type="number"
                      min={1}
                      className="asset-create__input"
                      value={general.quantity}
                      onChange={(e) => setGeneral({ ...general, quantity: e.target.value })}
                    />
                  </div>
                  <div className="asset-create__quantity-unit-cell">
                    <label className="asset-create__label">Đơn vị tính</label>
                    <select
                      className="asset-create__select"
                      value={general.unit}
                      onChange={(e) => setGeneral({ ...general, unit: e.target.value })}
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
              )}

              {!onlyMasterAsset && (
                <div className="asset-create__field">
                  <label className="asset-create__label">Giá trị</label>
                  <input
                    type="number"
                    min={0}
                    className="asset-create__input"
                    value={general.value}
                    onChange={(e) => setGeneral({ ...general, value: e.target.value })}
                  />
                </div>
              )}

              <div className="asset-create__field">
                <label className="asset-create__label">Nguồn gốc</label>
                <input
                  className="asset-create__input"
                  value={general.origin}
                  onChange={(e) => setGeneral({ ...general, origin: e.target.value })}
                />
              </div>

              <label className="asset-create__checkbox-row">
                <input
                  type="checkbox"
                  checked={general.isFixedAsset}
                  onChange={(e) => setGeneral({ ...general, isFixedAsset: e.target.checked })}
                />
                <span>Là tài sản cố định</span>
              </label>

              <div className="asset-create__field">
                <label className="asset-create__label">Quy cách tài sản</label>
                <input
                  className="asset-create__input"
                  value={general.specification}
                  onChange={(e) => setGeneral({ ...general, specification: e.target.value })}
                />
              </div>

              <div className="asset-create__field">
                <label className="asset-create__label">Ghi chú</label>
                <textarea
                  className="asset-create__textarea"
                  rows={2}
                  value={general.note}
                  onChange={(e) => setGeneral({ ...general, note: e.target.value })}
                />
              </div>
            </div>

            {!onlyMasterAsset && (
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
                  value={general.departmentId}
                  onChange={(e) =>
                    setGeneral({
                      ...general,
                      departmentId: e.target.value,
                      managerEmployeeId: '',
                    })
                  }
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
                    !general.departmentId || deptEmployees.length === 0
                      ? ''
                      : deptEmployees.some(
                            (e) => String(e.employeeId) === general.managerEmployeeId
                          )
                        ? general.managerEmployeeId
                        : String(deptEmployees[0].employeeId)
                  }
                  disabled={!general.departmentId || deptEmployees.length === 0}
                  onChange={(e) =>
                    setGeneral({ ...general, managerEmployeeId: e.target.value })
                  }
                >
                  {!general.departmentId && (
                    <option value="">Chọn phòng ban trước</option>
                  )}
                  {general.departmentId && deptEmployees.length === 0 && (
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
                  value={general.warehouseId}
                  onChange={(e) => setGeneral({ ...general, warehouseId: e.target.value })}
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
                  value={general.purchaseDate}
                  onChange={(e) => setGeneral({ ...general, purchaseDate: e.target.value })}
                />
              </div>

              <div className="asset-create__field">
                <label className="asset-create__label">Nhà cung cấp</label>
                <select
                  className="asset-create__select"
                  value={general.supplierId}
                  onChange={(e) => setGeneral({ ...general, supplierId: e.target.value })}
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
                  value={general.contractNumber}
                  onChange={(e) => setGeneral({ ...general, contractNumber: e.target.value })}
                />
              </div>

              <div className="asset-create__field">
                <label className="asset-create__label">Số serial</label>
                <input
                  className="asset-create__input"
                  value={general.serialNumber}
                  onChange={(e) => setGeneral({ ...general, serialNumber: e.target.value })}
                  disabled={Number(general.quantity || 1) > 1}
                />
                {Number(general.quantity || 1) > 1 && (
                  <p className="asset-create__hint">Chỉ áp dụng khi số lượng là 1.</p>
                )}
              </div>
            </div>
            )}
          </div>
        </section>

        {/* Bảo hành */}
        {!onlyMasterAsset && (
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
                  value={warranty.durationMonths}
                  onChange={(e) =>
                    setWarranty({ ...warranty, durationMonths: e.target.value })
                  }
                />
                <span className="asset-create__suffix">Tháng</span>
              </div>
            </div>
            <div className="asset-create__field">
              <label className="asset-create__label">Điều kiện bảo hành</label>
              <input
                className="asset-create__input"
                value={warranty.conditions}
                onChange={(e) => setWarranty({ ...warranty, conditions: e.target.value })}
              />
            </div>
            <div className="asset-create__field">
              <label className="asset-create__label">Hạn bảo hành</label>
              <input
                type="text"
                className="asset-create__input"
                placeholder="dd/mm/yyyy"
                value={warrantyExpiryText}
                onChange={(e) => {
                  const val = e.target.value;
                  setWarrantyExpiryText(val);
                  const iso = fromDisplayDate(val);
                  if (iso) setWarranty({ ...warranty, expiryDate: iso });
                  else if (!val.trim()) setWarranty({ ...warranty, expiryDate: '' });
                }}
              />
            </div>
          </div>
        </section>
        )}

        {/* Thông tin khấu hao */}
        {!onlyMasterAsset && (
        <section className="asset-create__section">
          <h2 className="asset-create__section-title">Thông tin khấu hao</h2>
          <div className="asset-create__grid asset-create__grid--three">
            <div className="asset-create__field">
              <label className="asset-create__label">Mức khấu hao kỳ gần nhất</label>
              <input
                type="number"
                min={0}
                className="asset-create__input"
                value={depreciation.baseValue}
                onChange={(e) => setDepreciation({ ...depreciation, baseValue: e.target.value })}
              />
            </div>

            <div className="asset-create__field">
              <label className="asset-create__label">Thời gian còn lại</label>
              <div className="asset-create__field--inline">
                <input
                  className="asset-create__input"
                  value="—"
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
                min={0}
                className="asset-create__input"
                value={depreciation.remainingValue}
                onChange={(e) =>
                  setDepreciation({ ...depreciation, remainingValue: e.target.value })
                }
              />
            </div>

            <div className="asset-create__field">
              <label className="asset-create__label">Ngày bắt đầu khấu hao</label>
              <input
                type="date"
                className="asset-create__input"
                value={depreciation.startDate}
                onChange={(e) =>
                  setDepreciation({ ...depreciation, startDate: e.target.value })
                }
              />
            </div>

            <div className="asset-create__field">
              <label className="asset-create__label">Khấu hao lũy kế</label>
              <input
                type="number"
                min={0}
                className="asset-create__input"
                value={depreciation.accumulatedDepreciation}
                onChange={(e) =>
                  setDepreciation({
                    ...depreciation,
                    accumulatedDepreciation: e.target.value,
                  })
                }
              />
            </div>

            <div className="asset-create__field">
              <label className="asset-create__label">Thời gian khấu hao</label>
              <div className="asset-create__field--inline">
                <input
                  type="number"
                  min={0}
                  className="asset-create__input"
                  value={depreciation.lifeTimeMonths}
                  onChange={(e) =>
                    setDepreciation({ ...depreciation, lifeTimeMonths: e.target.value })
                  }
                />
                <span className="asset-create__suffix">Tháng</span>
              </div>
            </div>
          </div>
        </section>
        )}

        {/* Đã cấp phát */}
        {!onlyMasterAsset && (
        <section className="asset-create__section">
          <h2 className="asset-create__section-title">Đã cấp phát</h2>
          <div className="asset-create__grid asset-create__grid--two">
            <label className="asset-create__checkbox-row">
              <input
                type="checkbox"
                checked={allocation.allocateNow}
                onChange={(e) =>
                  setAllocation({ ...allocation, allocateNow: e.target.checked })
                }
              />
              <span>Có cấp phát ngay</span>
            </label>

            <div className="asset-create__field">
              <label className="asset-create__label">Đối tượng sử dụng</label>
              <input
                className="asset-create__input"
                value={allocation.usageTarget}
                onChange={(e) =>
                  setAllocation({ ...allocation, usageTarget: e.target.value })
                }
              />
            </div>
          </div>
        </section>
        )}

        {/* Tài liệu */}
        <section className="asset-create__section">
          <input
            ref={docFileInputRef}
            type="file"
            accept={ASSET_DOCUMENT_FILE_ACCEPT}
            style={{ display: 'none' }}
            onChange={onDocFileSelected}
          />
          <h2 className="asset-create__section-title">Tài liệu</h2>
          <div className="asset-create__files">
            {documents.length === 0 ? (
              <p className="asset-create__hint">Chưa có tài liệu. Chọn &quot;Thêm tài liệu&quot; để tải lên.</p>
            ) : (
              documents.map((d, idx) => (
                <div key={d.id} className="asset-create__file">
                  <span className="asset-create__file-index">#{idx + 1}</span>
                  <span className="asset-create__file-name asset-create__file-name--grow">
                    {d.uploading
                      ? `Đang tải: ${d.fileName}…`
                      : d.error
                        ? `${d.fileName} — ${d.error}`
                        : d.fileName}
                  </span>
                  {d.url && !d.uploading && (
                    <a
                      href={d.url}
                      target="_blank"
                      rel="noopener noreferrer"
                      className="asset-create__btn asset-create__btn--secondary"
                      style={{ padding: '4px 10px', fontSize: 12 }}
                    >
                      Mở
                    </a>
                  )}
                  <button
                    type="button"
                    className="asset-create__btn asset-create__btn--secondary"
                    style={{ padding: '4px 10px', fontSize: 12 }}
                    onClick={() => removeDocumentRow(d.id)}
                  >
                    Xóa
                  </button>
                </div>
              ))
            )}
          </div>
          <div className="asset-create__file-actions">
            <button
              type="button"
              className="asset-create__btn asset-create__btn--secondary"
              onClick={downloadAllDocumentUrls}
              disabled={!documents.some((d) => d.url)}
            >
              Tải toàn bộ
            </button>
            <button type="button" className="asset-create__btn asset-create__btn--primary" onClick={pickDocument}>
              Thêm tài liệu
            </button>
          </div>
        </section>

      </form>
    </div>
  );
}

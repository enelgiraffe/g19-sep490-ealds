import { useEffect, useMemo, useState } from 'react';
import { Button, Form, Tabs, message } from 'antd';
import { isAxiosError } from 'axios';
import './CategoriesPage.css';
import { assetCategoryService, type AssetCategoryItem } from '../services/assetCategoryService';
import { assetTypeService, type AssetTypeListItem } from '../services/assetTypeService';
import { assetLocationService, type AssetLocationItem } from '../services/assetLocationService';
import { supplierService, type SupplierItem } from '../services/supplierService';
import { AssetTypesSection } from '../components/AssetTypesSection';
import { AssetGroupsSection } from '../components/AssetGroupsSection';
import { AssetLocationsSection } from '../components/AssetLocationsSection';
import { CategoriesModals, type AssetManagementMethod } from '../components/CategoriesModals';
import {
  SuppliersSection,
  type SupplierDraft,
  type SupplierFormErrors,
  type SupplierRow,
  type SupplierStatus,
} from '../components/SuppliersSection';

type CategoryStatus = 'tracking' | 'stopped';

interface CategoryRow {
  key: number;
  code: string;
  name: string;
  group: string;
  managementMethod: string;
  quantityTracking: number;
  displayStatus: CategoryStatus;
}

interface AssetGroupRow {
  key: number;
  code: number;
  name: string;
  parentCode: string | null;
}

interface AssetLocationRow {
  key: number;
  index: number;
  name: string;
  parentName: string | null;
  note: string | null;
  status: CategoryStatus;
}

const STATUS_LABELS: Record<CategoryStatus, { label: string; className: string }> = {
  tracking: { label: 'Đang theo dõi', className: 'categories-status-pill categories-status-pill--active' },
  stopped: { label: 'Ngừng theo dõi', className: 'categories-status-pill categories-status-pill--inactive' },
};

const SUPPLIER_CODE_MAX_LENGTH = 50;
const SUPPLIER_NAME_MAX_LENGTH = 200;
const SUPPLIER_ADDRESS_MAX_LENGTH = 255;
const SUPPLIER_TAX_CODE_REGEX = /^(\d{10}|\d{13})$/;
const SUPPLIER_PHONE_REGEX = /^(?:\+84|0)(?:3|5|7|8|9)\d{8}$/;

const mapAssetTypeToCategoryRow = (item: AssetTypeListItem): CategoryRow => ({
  key: item.assetTypeId,
  code: String(item.assetTypeId),
  name: item.name,
  group: item.categoryName,
  managementMethod: 'Quản lý theo mã',
  quantityTracking: item.assetCount,
  displayStatus: 'tracking',
});

const mapCategoryToGroupRow = (item: AssetCategoryItem): AssetGroupRow => ({
  key: item.categoryId,
  code: item.categoryId,
  name: item.name,
  parentCode: null,
});

const mapLocationToRow = (item: AssetLocationItem, index: number): AssetLocationRow => ({
  key: item.locationId,
  index: index + 1,
  name: item.departmentName,
  parentName: item.assetName,
  note: item.note ?? null,
  status: item.isCurrent ? 'tracking' : 'stopped',
});

const mapSupplierToRow = (item: SupplierItem, index: number): SupplierRow => ({
  key: item.supplierId,
  supplierId: item.supplierId,
  index: index + 1,
  code: item.code,
  name: item.name,
  taxCode: item.taxCode ?? null,
  phone: item.phone ?? null,
  address: item.address ?? null,
  email: item.email ?? null,
  status: item.status === 1 ? 'active' : 'inactive',
});

export function CategoriesPage() {
  const [activeCatalogTab, setActiveCatalogTab] = useState('asset-types');
  const [activeSubTab, setActiveSubTab] = useState('type');
  const [statusFilter, setStatusFilter] = useState<'all' | CategoryStatus>('all');
  const [searchText, setSearchText] = useState('');
  const [assetTypeRows, setAssetTypeRows] = useState<CategoryRow[]>([]);
  const [expandedGroupCodes, setExpandedGroupCodes] = useState<string[]>(['PTVT']);
  const [categoryRows, setCategoryRows] = useState<AssetGroupRow[]>([]);
  const [isLoadingCategories, setIsLoadingCategories] = useState(false);
  const [isLoadingAssetTypes, setIsLoadingAssetTypes] = useState(false);
  const [locationRows, setLocationRows] = useState<AssetLocationRow[]>([]);
  const [supplierRows, setSupplierRows] = useState<SupplierRow[]>([]);
  const [isLoadingSuppliers, setIsLoadingSuppliers] = useState(false);
  const [supplierStatusFilter, setSupplierStatusFilter] = useState<'all' | SupplierStatus>('all');
  const [isCreateAssetTypeOpen, setIsCreateAssetTypeOpen] = useState(false);
  const [isCreateAssetGroupOpen, setIsCreateAssetGroupOpen] = useState(false);
  const [isLocationModalOpen, setIsLocationModalOpen] = useState(false);
  const [locationModalMode, setLocationModalMode] = useState<'create' | 'edit'>('create');
  const [editingLocation, setEditingLocation] = useState<AssetLocationRow | null>(null);
  const [isSupplierModalOpen, setIsSupplierModalOpen] = useState(false);
  const [supplierModalMode, setSupplierModalMode] = useState<'create' | 'edit'>('create');
  const [editingSupplier, setEditingSupplier] = useState<SupplierRow | null>(null);
  const [isSavingSupplier, setIsSavingSupplier] = useState(false);
  const [supplierDraft, setSupplierDraft] = useState<SupplierDraft>({
    code: '',
    name: '',
    taxCode: '',
    address: '',
    phone: '',
    email: '',
    status: 'active',
  });
  const [supplierFormErrors, setSupplierFormErrors] = useState<SupplierFormErrors>({});
  const [isSupplierDeleteConfirmOpen, setIsSupplierDeleteConfirmOpen] = useState(false);
  const [supplierDeleteTarget, setSupplierDeleteTarget] = useState<SupplierRow | null>(null);
  const [isDeletingSupplier, setIsDeletingSupplier] = useState(false);
  const [createForm] = Form.useForm<{
    name: string;
    code: string;
    groupCode: string;
    note?: string;
    managementMethod: AssetManagementMethod;
  }>();
  const [createGroupForm] = Form.useForm<{
    name: string;
    code: string;
    parentCode?: string | null;
  }>();

  useEffect(() => {
    if (activeCatalogTab !== 'asset-types' || activeSubTab !== 'type') {
      return;
    }

    const fetchAssetTypes = async () => {
      try {
        setIsLoadingAssetTypes(true);
        const data = await assetTypeService.getAll(searchText.trim() || undefined);
        setAssetTypeRows(data.map(mapAssetTypeToCategoryRow));
      } catch (error) {
        // eslint-disable-next-line no-console
        console.error('Failed to load asset types', error);
        message.error('Không tải được danh sách loại tài sản từ hệ thống.');
      } finally {
        setIsLoadingAssetTypes(false);
      }
    };

    fetchAssetTypes();
  }, [activeCatalogTab, activeSubTab, searchText]);

  useEffect(() => {
    if (activeCatalogTab !== 'asset-types' || activeSubTab !== 'group') {
      return;
    }

    const fetchCategories = async () => {
      try {
        setIsLoadingCategories(true);
        const data = await assetCategoryService.getAll(searchText.trim() || undefined);
        setCategoryRows(data.map(mapCategoryToGroupRow));
      } catch (error) {
        // eslint-disable-next-line no-console
        console.error('Failed to load asset categories', error);
        message.error('Không tải được danh sách nhóm tài sản từ hệ thống.');
      } finally {
        setIsLoadingCategories(false);
      }
    };

    fetchCategories();
  }, [activeCatalogTab, activeSubTab, searchText]);

  useEffect(() => {
    if (activeCatalogTab !== 'asset-locations') {
      return;
    }

    const fetchLocations = async () => {
      try {
        const data = await assetLocationService.getAll();
        setLocationRows(data.map((item, index) => mapLocationToRow(item, index)));
      } catch (error) {
        // eslint-disable-next-line no-console
        console.error('Failed to load asset locations', error);
        message.error('Không tải được danh sách vị trí tài sản từ hệ thống.');
      }
    };

    fetchLocations();
  }, [activeCatalogTab]);

  const loadSuppliers = async (keyword?: string) => {
    try {
      setIsLoadingSuppliers(true);
      const data = await supplierService.getAll(keyword);
      setSupplierRows(data.map((item, index) => mapSupplierToRow(item, index)));
    } catch (error) {
      // eslint-disable-next-line no-console
      console.error('Failed to load suppliers', error);
      message.error('Không tải được danh sách nhà cung cấp từ hệ thống.');
    } finally {
      setIsLoadingSuppliers(false);
    }
  };

  useEffect(() => {
    if (activeCatalogTab !== 'suppliers') {
      return;
    }

    loadSuppliers(searchText.trim() || undefined);
  }, [activeCatalogTab, searchText]);

  const filteredRows = useMemo(() => {
    const kw = searchText.trim().toLowerCase();
    return assetTypeRows.filter((row) => {
      const matchStatus = statusFilter === 'all' || row.displayStatus === statusFilter;
      const matchSearch =
        !kw ||
        row.code.toLowerCase().includes(kw) ||
        row.name.toLowerCase().includes(kw) ||
        row.group.toLowerCase().includes(kw);
      return matchStatus && matchSearch;
    });
  }, [assetTypeRows, statusFilter, searchText]);

  const visibleGroupRows = useMemo(() => {
    const kw = searchText.trim().toLowerCase();
    const rows = categoryRows.filter((row) => {
      if (!kw) return true;
      return row.name.toLowerCase().includes(kw) || String(row.code).toLowerCase().includes(kw);
    });

    return { rows, childrenByParent: {} as Record<string, AssetGroupRow[]> };
  }, [categoryRows, searchText]);

  const filteredLocationRows = useMemo(() => {
    const kw = searchText.trim().toLowerCase();
    return locationRows.filter((row) => {
      const matchStatus = statusFilter === 'all' || row.status === statusFilter;
      const matchSearch =
        !kw ||
        row.name.toLowerCase().includes(kw) ||
        (row.parentName ?? '').toLowerCase().includes(kw) ||
        (row.note ?? '').toLowerCase().includes(kw);
      return matchStatus && matchSearch;
    });
  }, [locationRows, searchText, statusFilter]);

  const filteredSupplierRows = useMemo(() => {
    return supplierRows.filter((row) => {
      const matchStatus = supplierStatusFilter === 'all' || row.status === supplierStatusFilter;
      return matchStatus;
    });
  }, [supplierRows, supplierStatusFilter]);

  const handleOpenCreateLocation = () => {
    setLocationModalMode('create');
    setEditingLocation(null);
    setIsLocationModalOpen(true);
  };

  const handleOpenEditLocation = (row: AssetLocationRow) => {
    setLocationModalMode('edit');
    setEditingLocation(row);
    setIsLocationModalOpen(true);
  };

  const handleOpenCreateSupplier = () => {
    setSupplierModalMode('create');
    setEditingSupplier(null);
    setIsSupplierModalOpen(true);
    setSupplierFormErrors({});
    setSupplierDraft({
      code: '',
      name: '',
      taxCode: '',
      address: '',
      phone: '',
      email: '',
      status: 'active',
    });
  };

  const handleOpenEditSupplier = (row: SupplierRow) => {
    setSupplierModalMode('edit');
    setEditingSupplier(row);
    setIsSupplierModalOpen(true);
    setSupplierFormErrors({});
    setSupplierDraft({
      code: row.code,
      name: row.name,
      taxCode: row.taxCode ?? '',
      address: row.address ?? '',
      phone: row.phone ?? '',
      email: row.email ?? '',
      status: row.status,
    });
  };

  const handleDeleteSupplier = (row: SupplierRow) => {
    setSupplierDeleteTarget(row);
    setIsSupplierDeleteConfirmOpen(true);
  };

  const handleConfirmDeleteSupplier = async () => {
    if (!supplierDeleteTarget) return;
    setIsDeletingSupplier(true);
    try {
      await supplierService.delete(supplierDeleteTarget.supplierId);
      message.success('Xóa nhà cung cấp thành công.');
      setIsSupplierDeleteConfirmOpen(false);
      setSupplierDeleteTarget(null);
      await loadSuppliers(searchText.trim() || undefined);
    } catch (error) {
      // eslint-disable-next-line no-console
      console.error('Failed to delete supplier', error);
      message.error('Không thể xóa nhà cung cấp.');
    } finally {
      setIsDeletingSupplier(false);
    }
  };

  const handleSubmitSupplier = async () => {
    const nextErrors: typeof supplierFormErrors = {};
    const code = supplierDraft.code.trim();
    const name = supplierDraft.name.trim();
    const taxCode = supplierDraft.taxCode.trim();
    const address = supplierDraft.address.trim();
    const phone = supplierDraft.phone.trim();
    const email = supplierDraft.email.trim();

    if (!code) nextErrors.code = 'Vui lòng nhập mã nhà cung cấp.';
    else if (code.length > SUPPLIER_CODE_MAX_LENGTH) {
      nextErrors.code = `Mã nhà cung cấp tối đa ${SUPPLIER_CODE_MAX_LENGTH} ký tự.`;
    }

    if (!name) nextErrors.name = 'Vui lòng nhập tên nhà cung cấp.';
    else if (name.length > SUPPLIER_NAME_MAX_LENGTH) {
      nextErrors.name = `Tên nhà cung cấp tối đa ${SUPPLIER_NAME_MAX_LENGTH} ký tự.`;
    }

    if (taxCode && !SUPPLIER_TAX_CODE_REGEX.test(taxCode)) {
      nextErrors.taxCode = 'MST phải gồm đúng 10 hoặc 13 chữ số.';
    }

    if (address.length > SUPPLIER_ADDRESS_MAX_LENGTH) {
      nextErrors.address = `Địa chỉ tối đa ${SUPPLIER_ADDRESS_MAX_LENGTH} ký tự.`;
    }

    if (phone && !SUPPLIER_PHONE_REGEX.test(phone)) {
      nextErrors.phone = 'Số điện thoại phải có 10 số (ví dụ: 0912345678) hoặc dạng +84 (ví dụ: +84912345678).';
    }

    if (email && !/^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(email)) {
      nextErrors.email = 'Email không đúng định dạng.';
    }

    if (!supplierDraft.status) nextErrors.status = 'Vui lòng chọn trạng thái.';

    if (Object.keys(nextErrors).length > 0) {
      setSupplierFormErrors(nextErrors);
      return;
    }

    setSupplierFormErrors({});
    setIsSavingSupplier(true);
    try {
      const payloadBase = {
        code,
        name,
        taxCode: taxCode || undefined,
        address: address || undefined,
        phone: phone || undefined,
        email: email || undefined,
        status: supplierDraft.status === 'active' ? 1 : 0,
      };

      if (supplierModalMode === 'create') {
        await supplierService.create(payloadBase);
        message.success('Tạo nhà cung cấp thành công.');
      } else {
        if (!editingSupplier) {
          message.error('Không tìm thấy dữ liệu nhà cung cấp.');
          return;
        }
        await supplierService.update(editingSupplier.supplierId, payloadBase);
        message.success('Cập nhật nhà cung cấp thành công.');
      }

      setIsSupplierModalOpen(false);
      await loadSuppliers(searchText.trim() || undefined);
    } catch (error) {
      if (isAxiosError(error) && error.response?.status === 400) {
        const responseData = error.response.data as
          | { errors?: Record<string, string[]>; title?: string; detail?: string }
          | undefined;
        const apiErrors = responseData?.errors ?? {};

        const mapFieldError = (fieldName: string): string | undefined => {
          const key = Object.keys(apiErrors).find((k) => k.toLowerCase() === fieldName.toLowerCase());
          return key && apiErrors[key]?.length ? apiErrors[key][0] : undefined;
        };

        const nextErrors: typeof supplierFormErrors = {
          code: mapFieldError('Code'),
          name: mapFieldError('Name'),
          taxCode: mapFieldError('TaxCode'),
          address: mapFieldError('Address'),
          phone: mapFieldError('Phone'),
          email: mapFieldError('Email'),
          status: mapFieldError('Status'),
        };
        setSupplierFormErrors(nextErrors);

        const allMessages = Object.values(apiErrors).flat().filter(Boolean);
        if (allMessages.length > 0) {
          message.error(allMessages.join(' | '));
        } else {
          message.error(responseData?.title || 'Dữ liệu không hợp lệ.');
        }
        return;
      }

      // eslint-disable-next-line no-console
      console.error('Failed to save supplier', error);
      message.error('Không thể lưu nhà cung cấp.');
    } finally {
      setIsSavingSupplier(false);
    }
  };

  const handleSupplierDraftFieldChange = (field: keyof SupplierDraft, value: string) => {
    setSupplierDraft((current) => ({ ...current, [field]: value }));
  };

  const handleClearSupplierFieldError = (field: keyof SupplierFormErrors) => {
    setSupplierFormErrors((current) => ({ ...current, [field]: undefined }));
  };

  const handleCloseSupplierDeleteConfirm = () => {
    setIsSupplierDeleteConfirmOpen(false);
    setSupplierDeleteTarget(null);
  };

  return (
    <div className="categories-page">
      <div className="categories-header">
        <h1 className="categories-title">Danh mục tài sản</h1>
        <Button
          type="primary"
          danger
          className="categories-btn-add"
          onClick={() => {
            if (activeCatalogTab === 'asset-types' && activeSubTab === 'type') {
              setIsCreateAssetTypeOpen(true);
              queueMicrotask(() => {
                createForm.setFieldsValue({
                  name: '',
                  code: '',
                  groupCode: 'MM',
                  note: '',
                  managementMethod: 'code',
                });
              });
              return;
            }
            if (activeCatalogTab === 'asset-locations') {
              handleOpenCreateLocation();
              return;
            }
            if (activeCatalogTab === 'suppliers') {
              handleOpenCreateSupplier();
              return;
            }
            message.info('Chức năng tạo mới sẽ được bổ sung cho tab này sau.');
          }}
        >
          + Tạo mới
        </Button>
      </div>

      <div className="categories-card">
        <Tabs
          activeKey={activeCatalogTab}
          onChange={(key) => {
            setActiveCatalogTab(key);
            // Reset some filters when đổi tab chính cho dễ nhìn
            setSearchText('');
            setStatusFilter('all');
            setSupplierStatusFilter('all');
          }}
          className="categories-tabs categories-tabs--primary"
          items={[
            { key: 'asset-types', label: 'Danh mục tài sản' },
            { key: 'asset-locations', label: 'Vị trí tài sản' },
            { key: 'work-locations', label: 'Vị trí công việc' },
            { key: 'suppliers', label: 'Nhà cung cấp' },
          ]}
        />

        {activeCatalogTab === 'asset-types' && (
          <div className="categories-subtabs">
            <button
              type="button"
              className={
                activeSubTab === 'type'
                  ? 'categories-subtab categories-subtab--active'
                  : 'categories-subtab'
              }
              onClick={() => setActiveSubTab('type')}
            >
              <span className="categories-subtab__label">Loại tài sản</span>
              {activeSubTab === 'type' && <span className="categories-subtab__underline" />}
            </button>
            <button
              type="button"
              className={
                activeSubTab === 'group'
                  ? 'categories-subtab categories-subtab--active'
                  : 'categories-subtab'
              }
              onClick={() => setActiveSubTab('group')}
            >
              <span className="categories-subtab__label">Nhóm tài sản</span>
              {activeSubTab === 'group' && <span className="categories-subtab__underline" />}
            </button>
          </div>
        )}

        {activeCatalogTab === 'asset-types' && activeSubTab === 'type' && (
          <AssetTypesSection
            searchText={searchText}
            onSearchTextChange={setSearchText}
            statusFilter={statusFilter}
            onStatusFilterChange={setStatusFilter}
            onResetFilters={() => {
              setSearchText('');
              setStatusFilter('all');
            }}
            isLoadingAssetTypes={isLoadingAssetTypes}
            rows={filteredRows}
            statusLabels={STATUS_LABELS}
          />
        )}

        {activeCatalogTab === 'asset-types' && activeSubTab === 'group' && (
          <AssetGroupsSection
            searchText={searchText}
            onSearchTextChange={setSearchText}
            isLoadingCategories={isLoadingCategories}
            rows={visibleGroupRows.rows}
            expandedGroupCodes={expandedGroupCodes}
            onToggleGroup={(code) => {
              setExpandedGroupCodes((current) =>
                current.includes(code)
                  ? current.filter((c) => c !== code)
                  : [...current, code],
              );
            }}
          />
        )}

        {activeCatalogTab === 'asset-locations' && (
          <AssetLocationsSection
            searchText={searchText}
            onSearchTextChange={setSearchText}
            statusFilter={statusFilter}
            onStatusFilterChange={setStatusFilter}
            rows={filteredLocationRows}
            statusLabels={STATUS_LABELS}
            onOpenEditLocation={handleOpenEditLocation}
          />
        )}

        {activeCatalogTab === 'suppliers' && (
          <SuppliersSection
            searchText={searchText}
            onSearchTextChange={setSearchText}
            supplierStatusFilter={supplierStatusFilter}
            onSupplierStatusFilterChange={setSupplierStatusFilter}
            isLoadingSuppliers={isLoadingSuppliers}
            rows={filteredSupplierRows}
            onOpenEditSupplier={handleOpenEditSupplier}
            onDeleteSupplier={handleDeleteSupplier}
            isSupplierDeleteConfirmOpen={isSupplierDeleteConfirmOpen}
            supplierDeleteTarget={supplierDeleteTarget}
            onCloseSupplierDeleteConfirm={handleCloseSupplierDeleteConfirm}
            onConfirmDeleteSupplier={handleConfirmDeleteSupplier}
            isDeletingSupplier={isDeletingSupplier}
            isSupplierModalOpen={isSupplierModalOpen}
            supplierModalMode={supplierModalMode}
            supplierDraft={supplierDraft}
            supplierFormErrors={supplierFormErrors}
            onSupplierDraftFieldChange={handleSupplierDraftFieldChange}
            onClearSupplierFieldError={handleClearSupplierFieldError}
            onCloseSupplierModal={() => setIsSupplierModalOpen(false)}
            onSubmitSupplier={handleSubmitSupplier}
            isSavingSupplier={isSavingSupplier}
            supplierCodeMaxLength={SUPPLIER_CODE_MAX_LENGTH}
            supplierNameMaxLength={SUPPLIER_NAME_MAX_LENGTH}
            supplierAddressMaxLength={SUPPLIER_ADDRESS_MAX_LENGTH}
          />
        )}

        <div className="categories-card__footer">
          <div className="categories-footer__left">
            Số lượng trên trang:
            <select className="categories-footer__select" defaultValue={25}>
              <option value={10}>10</option>
              <option value={25}>25</option>
              <option value={50}>50</option>
              <option value={100}>100</option>
            </select>
          </div>
          <div className="categories-footer__center">1-25 trên 289</div>
          <div className="categories-footer__right">
            <button className="categories-footer__pager" disabled type="button">
              ⟨
            </button>
            <button
              className="categories-footer__pager categories-footer__pager--active"
              type="button"
            >
              1
            </button>
            <button className="categories-footer__pager" type="button">
              ⟩
            </button>
          </div>
        </div>
      </div>

      <CategoriesModals
        isCreateAssetTypeOpen={isCreateAssetTypeOpen}
        setIsCreateAssetTypeOpen={setIsCreateAssetTypeOpen}
        createForm={createForm}
        isLocationModalOpen={isLocationModalOpen}
        setIsLocationModalOpen={setIsLocationModalOpen}
        locationModalMode={locationModalMode}
        editingLocation={editingLocation}
        isCreateAssetGroupOpen={isCreateAssetGroupOpen}
        setIsCreateAssetGroupOpen={setIsCreateAssetGroupOpen}
        createGroupForm={createGroupForm}
      />
    </div>
  );
}


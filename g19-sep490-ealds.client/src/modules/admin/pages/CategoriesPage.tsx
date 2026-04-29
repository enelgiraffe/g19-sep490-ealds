import { useCallback, useEffect, useMemo, useState } from 'react';
import { Button, Form, Input, Modal, Tabs, message } from 'antd';
import { isAxiosError } from 'axios';
import './CategoriesPage.css';
import { assetCategoryService, type AssetCategoryItem } from '../services/assetCategoryService';
import { assetTypeService, type AssetTypeListItem } from '../services/assetTypeService';
import { supplierService, type SupplierItem } from '../services/supplierService';
import { assetService, type WarehouseItem } from '../../assets/services/assetService';
import { AssetTypesSection } from '../components/AssetTypesSection';
import { AssetGroupsSection } from '../components/AssetGroupsSection';
import { WarehousesSection } from '../components/WarehousesSection';
import { CategoriesModals } from '../components/CategoriesModals';
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
  categoryId: number;
  quantityTracking: number;
  displayStatus: CategoryStatus;
}

interface AssetGroupRow {
  key: number;
  code: number;
  name: string;
  assetTypeCount: number;
}

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
  categoryId: item.categoryId,
  quantityTracking: item.assetCount,
  displayStatus: 'tracking',
});

const mapCategoryToGroupRow = (item: AssetCategoryItem): AssetGroupRow => ({
  key: item.categoryId,
  code: item.categoryId,
  name: item.name,
  assetTypeCount: item.assetTypeCount,
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
  const [categoryRows, setCategoryRows] = useState<AssetGroupRow[]>([]);
  const [isLoadingCategories, setIsLoadingCategories] = useState(false);
  const [isLoadingAssetTypes, setIsLoadingAssetTypes] = useState(false);
  const [warehouses, setWarehouses] = useState<WarehouseItem[]>([]);
  const [supplierRows, setSupplierRows] = useState<SupplierRow[]>([]);
  const [isLoadingSuppliers, setIsLoadingSuppliers] = useState(false);
  const [supplierStatusFilter, setSupplierStatusFilter] = useState<'all' | SupplierStatus>('all');
  const [isAssetTypeModalOpen, setIsAssetTypeModalOpen] = useState(false);
  const [assetTypeModalMode, setAssetTypeModalMode] = useState<'create' | 'edit'>('create');
  const [editingAssetTypeId, setEditingAssetTypeId] = useState<number | null>(null);
  const [isSavingAssetType, setIsSavingAssetType] = useState(false);
  const [isAssetTypeDeleteOpen, setIsAssetTypeDeleteOpen] = useState(false);
  const [assetTypeDeleteTarget, setAssetTypeDeleteTarget] = useState<CategoryRow | null>(null);
  const [isDeletingAssetType, setIsDeletingAssetType] = useState(false);
  const [isAssetCategoryModalOpen, setIsAssetCategoryModalOpen] = useState(false);
  const [assetCategoryModalMode, setAssetCategoryModalMode] = useState<'create' | 'edit'>('create');
  const [editingCategoryId, setEditingCategoryId] = useState<number | null>(null);
  const [isSavingAssetCategory, setIsSavingAssetCategory] = useState(false);
  const [isCategoryDeleteOpen, setIsCategoryDeleteOpen] = useState(false);
  const [categoryDeleteTarget, setCategoryDeleteTarget] = useState<AssetGroupRow | null>(null);
  const [isDeletingCategory, setIsDeletingCategory] = useState(false);
  const [isWarehouseModalOpen, setIsWarehouseModalOpen] = useState(false);
  const [warehouseModalMode, setWarehouseModalMode] = useState<'create' | 'edit'>('create');
  const [editingWarehouseId, setEditingWarehouseId] = useState<number | null>(null);
  const [isSavingWarehouse, setIsSavingWarehouse] = useState(false);
  const [isLoadingWarehouses, setIsLoadingWarehouses] = useState(false);
  const [isWarehouseDeleteOpen, setIsWarehouseDeleteOpen] = useState(false);
  const [warehouseDeleteTarget, setWarehouseDeleteTarget] = useState<WarehouseItem | null>(null);
  const [isDeletingWarehouse, setIsDeletingWarehouse] = useState(false);
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
  const [assetTypeForm] = Form.useForm<{
    name: string;
    categoryId?: number;
  }>();
  const [assetCategoryForm] = Form.useForm<{ name: string }>();
  const [warehouseForm] = Form.useForm<{ name: string; location?: string }>();

  const loadAssetTypes = useCallback(async () => {
    if (activeCatalogTab !== 'asset-types' || activeSubTab !== 'type') {
      return;
    }
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
  }, [activeCatalogTab, activeSubTab, searchText]);

  useEffect(() => {
    void loadAssetTypes();
  }, [loadAssetTypes]);

  const refreshAssetTypesData = useCallback(async () => {
    try {
      const data = await assetTypeService.getAll(searchText.trim() || undefined);
      setAssetTypeRows(data.map(mapAssetTypeToCategoryRow));
    } catch {
      /* bảng loại TS sẽ tải lại khi mở tab */
    }
  }, [searchText]);

  const loadAssetCategories = useCallback(async () => {
    if (activeCatalogTab !== 'asset-types' || activeSubTab !== 'group') {
      return;
    }
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
  }, [activeCatalogTab, activeSubTab, searchText]);

  useEffect(() => {
    void loadAssetCategories();
  }, [loadAssetCategories]);

  const loadWarehouses = useCallback(async () => {
    if (activeCatalogTab !== 'warehouses') {
      return;
    }
    try {
      setIsLoadingWarehouses(true);
      const data = await assetService.getWarehouses();
      setWarehouses(data);
    } catch (error) {
      // eslint-disable-next-line no-console
      console.error('Failed to load warehouses', error);
      message.error('Không tải được danh sách kho.');
    } finally {
      setIsLoadingWarehouses(false);
    }
  }, [activeCatalogTab]);

  useEffect(() => {
    void loadWarehouses();
  }, [loadWarehouses]);

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

  const filteredCategoryRows = useMemo(() => {
    const kw = searchText.trim().toLowerCase();
    return categoryRows.filter((row) => {
      if (!kw) return true;
      return row.name.toLowerCase().includes(kw) || String(row.code).toLowerCase().includes(kw);
    });
  }, [categoryRows, searchText]);

  const filteredWarehouseRows = useMemo(() => {
    const kw = searchText.trim().toLowerCase();
    return warehouses.filter((w) => {
      if (!kw) return true;
      const dn = (w.description ?? '').toLowerCase();
      return (
        String(w.warehouseId).includes(kw) ||
        (w.name ?? '').toLowerCase().includes(kw) ||
        dn.includes(kw)
      );
    });
  }, [warehouses, searchText]);

  const filteredSupplierRows = useMemo(() => {
    return supplierRows.filter((row) => {
      const matchStatus = supplierStatusFilter === 'all' || row.status === supplierStatusFilter;
      return matchStatus;
    });
  }, [supplierRows, supplierStatusFilter]);

  const handleOpenCreateWarehouse = () => {
    setWarehouseModalMode('create');
    setEditingWarehouseId(null);
    warehouseForm.resetFields();
    warehouseForm.setFieldsValue({ name: '', location: '' });
    setIsWarehouseModalOpen(true);
  };

  const handleOpenEditWarehouse = (row: WarehouseItem) => {
    setWarehouseModalMode('edit');
    setEditingWarehouseId(row.warehouseId);
    warehouseForm.resetFields();
    warehouseForm.setFieldsValue({
      name: row.name,
      location: row.description ?? '',
    });
    setIsWarehouseModalOpen(true);
  };

  const handleSubmitWarehouse = async (values: { name: string; location?: string }) => {
    const name = values.name?.trim();
    if (!name) {
      message.error('Vui lòng nhập tên kho.');
      return;
    }
    setIsSavingWarehouse(true);
    try {
      const payload = {
        name,
        description: values.location?.trim() || undefined,
      };
      if (warehouseModalMode === 'edit' && editingWarehouseId != null) {
        await assetService.updateWarehouse(editingWarehouseId, payload);
        message.success('Đã cập nhật kho.');
      } else {
        await assetService.createWarehouse(payload);
        message.success('Đã tạo kho mới.');
      }
      setIsWarehouseModalOpen(false);
      setEditingWarehouseId(null);
      setWarehouseModalMode('create');
      warehouseForm.resetFields();
      await loadWarehouses();
    } catch (error) {
      if (isAxiosError(error)) {
        const data = error.response?.data as { message?: string } | undefined;
        if (data?.message) {
          message.error(data.message);
        } else {
          message.error(
            warehouseModalMode === 'edit' ? 'Không thể cập nhật kho.' : 'Không thể tạo kho.'
          );
        }
      } else {
        // eslint-disable-next-line no-console
        console.error('Failed to save warehouse', error);
        message.error(
          warehouseModalMode === 'edit' ? 'Không thể cập nhật kho.' : 'Không thể tạo kho.'
        );
      }
      throw error;
    } finally {
      setIsSavingWarehouse(false);
    }
  };

  const handleWarehouseModalOk = () =>
    warehouseForm.validateFields().then((v) => handleSubmitWarehouse(v as { name: string; location?: string }));

  const handleRequestDeleteWarehouse = (row: WarehouseItem) => {
    setWarehouseDeleteTarget(row);
    setIsWarehouseDeleteOpen(true);
  };

  const handleCloseWarehouseDeleteConfirm = () => {
    setIsWarehouseDeleteOpen(false);
    setWarehouseDeleteTarget(null);
  };

  const handleConfirmDeleteWarehouse = async () => {
    if (!warehouseDeleteTarget) return;
    setIsDeletingWarehouse(true);
    try {
      await assetService.deleteWarehouse(warehouseDeleteTarget.warehouseId);
      message.success('Đã xóa kho.');
      handleCloseWarehouseDeleteConfirm();
      await loadWarehouses();
    } catch (error) {
      if (isAxiosError(error)) {
        const data = error.response?.data as { message?: string } | undefined;
        if (data?.message) {
          message.error(data.message);
          return;
        }
      }
      // eslint-disable-next-line no-console
      console.error('Failed to delete warehouse', error);
      message.error('Không thể xóa kho.');
    } finally {
      setIsDeletingWarehouse(false);
    }
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
      nextErrors.phone = 'Số điện thoại phải có 10 số ';
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

  const handleOpenCreateAssetType = () => {
    setAssetTypeModalMode('create');
    setEditingAssetTypeId(null);
    assetTypeForm.setFieldsValue({ name: '', categoryId: undefined });
    setIsAssetTypeModalOpen(true);
  };

  const handleOpenEditAssetType = (row: CategoryRow) => {
    setAssetTypeModalMode('edit');
    setEditingAssetTypeId(row.key);
    assetTypeForm.setFieldsValue({
      name: row.name,
      categoryId: row.categoryId,
    });
    setIsAssetTypeModalOpen(true);
  };

  const handleSubmitAssetType = async (values: { name: string; categoryId: number }) => {
    setIsSavingAssetType(true);
    try {
      if (assetTypeModalMode === 'create') {
        await assetTypeService.create({
          name: values.name.trim(),
          categoryId: values.categoryId,
        });
        message.success('Tạo loại tài sản thành công.');
      } else if (editingAssetTypeId != null) {
        await assetTypeService.update(editingAssetTypeId, {
          name: values.name.trim(),
          categoryId: values.categoryId,
        });
        message.success('Cập nhật loại tài sản thành công.');
      }
      setIsAssetTypeModalOpen(false);
      setEditingAssetTypeId(null);
      await loadAssetTypes();
    } catch (error) {
      if (isAxiosError(error)) {
        const data = error.response?.data as { message?: string } | undefined;
        if (data?.message) {
          message.error(data.message);
          return;
        }
      }
      // eslint-disable-next-line no-console
      console.error('Failed to save asset type', error);
      message.error('Không thể lưu loại tài sản.');
    } finally {
      setIsSavingAssetType(false);
    }
  };

  const handleDeleteAssetType = (row: CategoryRow) => {
    setAssetTypeDeleteTarget(row);
    setIsAssetTypeDeleteOpen(true);
  };

  const handleCloseAssetTypeDeleteConfirm = () => {
    setIsAssetTypeDeleteOpen(false);
    setAssetTypeDeleteTarget(null);
  };

  const handleConfirmDeleteAssetType = async () => {
    if (!assetTypeDeleteTarget) return;
    setIsDeletingAssetType(true);
    try {
      await assetTypeService.delete(assetTypeDeleteTarget.key);
      message.success('Đã xóa loại tài sản.');
      handleCloseAssetTypeDeleteConfirm();
      await loadAssetTypes();
    } catch (error) {
      if (isAxiosError(error)) {
        const data = error.response?.data as { message?: string } | undefined;
        if (data?.message) {
          message.error(data.message);
          return;
        }
      }
      // eslint-disable-next-line no-console
      console.error('Failed to delete asset type', error);
      message.error('Không thể xóa loại tài sản.');
    } finally {
      setIsDeletingAssetType(false);
    }
  };

  const handleOpenCreateAssetCategory = () => {
    setAssetCategoryModalMode('create');
    setEditingCategoryId(null);
    assetCategoryForm.setFieldsValue({ name: '' });
    setIsAssetCategoryModalOpen(true);
  };

  const handleOpenEditAssetCategory = (row: AssetGroupRow) => {
    setAssetCategoryModalMode('edit');
    setEditingCategoryId(row.key);
    assetCategoryForm.setFieldsValue({ name: row.name });
    setIsAssetCategoryModalOpen(true);
  };

  const handleSubmitAssetCategory = async (values: { name: string }) => {
    const name = values.name.trim();
    if (!name) {
      message.error('Vui lòng nhập tên nhóm tài sản.');
      return;
    }
    setIsSavingAssetCategory(true);
    try {
      if (assetCategoryModalMode === 'create') {
        await assetCategoryService.create({ name });
        message.success('Tạo nhóm tài sản thành công.');
      } else if (editingCategoryId != null) {
        await assetCategoryService.update(editingCategoryId, { name });
        message.success('Cập nhật nhóm tài sản thành công.');
      }
      setIsAssetCategoryModalOpen(false);
      setEditingCategoryId(null);
      await loadAssetCategories();
      await refreshAssetTypesData();
    } catch (error) {
      if (isAxiosError(error)) {
        const data = error.response?.data as { message?: string } | undefined;
        if (data?.message) {
          message.error(data.message);
          return;
        }
      }
      // eslint-disable-next-line no-console
      console.error('Failed to save asset category', error);
      message.error('Không thể lưu nhóm tài sản.');
    } finally {
      setIsSavingAssetCategory(false);
    }
  };

  const handleDeleteAssetCategory = (row: AssetGroupRow) => {
    setCategoryDeleteTarget(row);
    setIsCategoryDeleteOpen(true);
  };

  const handleCloseCategoryDeleteConfirm = () => {
    setIsCategoryDeleteOpen(false);
    setCategoryDeleteTarget(null);
  };

  const handleConfirmDeleteAssetCategory = async () => {
    if (!categoryDeleteTarget) return;
    setIsDeletingCategory(true);
    try {
      await assetCategoryService.delete(categoryDeleteTarget.key);
      message.success('Đã xóa nhóm tài sản.');
      handleCloseCategoryDeleteConfirm();
      await loadAssetCategories();
      await refreshAssetTypesData();
    } catch (error) {
      if (isAxiosError(error)) {
        const data = error.response?.data as { message?: string } | undefined;
        if (data?.message) {
          message.error(data.message);
          return;
        }
      }
      // eslint-disable-next-line no-console
      console.error('Failed to delete asset category', error);
      message.error('Không thể xóa nhóm tài sản.');
    } finally {
      setIsDeletingCategory(false);
    }
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
              handleOpenCreateAssetType();
              return;
            }
            if (activeCatalogTab === 'asset-types' && activeSubTab === 'group') {
              handleOpenCreateAssetCategory();
              return;
            }
            if (activeCatalogTab === 'warehouses') {
              handleOpenCreateWarehouse();
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
            { key: 'warehouses', label: 'Kho' },
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
            onEditAssetType={handleOpenEditAssetType}
            onDeleteAssetType={handleDeleteAssetType}
          />
        )}

        {activeCatalogTab === 'asset-types' && activeSubTab === 'group' && (
          <AssetGroupsSection
            searchText={searchText}
            onSearchTextChange={setSearchText}
            isLoadingCategories={isLoadingCategories}
            rows={filteredCategoryRows}
            onEditAssetCategory={handleOpenEditAssetCategory}
            onDeleteAssetCategory={handleDeleteAssetCategory}
          />
        )}

        {activeCatalogTab === 'warehouses' && (
          <WarehousesSection
            searchText={searchText}
            onSearchTextChange={setSearchText}
            isLoadingWarehouses={isLoadingWarehouses}
            rows={filteredWarehouseRows}
            onEditWarehouse={handleOpenEditWarehouse}
            onRequestDeleteWarehouse={handleRequestDeleteWarehouse}
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

      <Modal
        title="Xóa loại tài sản"
        open={isAssetTypeDeleteOpen}
        onOk={handleConfirmDeleteAssetType}
        onCancel={handleCloseAssetTypeDeleteConfirm}
        confirmLoading={isDeletingAssetType}
        okText="Xóa"
        okButtonProps={{ danger: true }}
        cancelText="Hủy"
      >
        <p>
          Bạn có chắc muốn xóa loại tài sản{' '}
          <strong>{assetTypeDeleteTarget?.name}</strong>?
        </p>
      </Modal>

      <Modal
        title="Xóa nhóm tài sản"
        open={isCategoryDeleteOpen}
        onOk={handleConfirmDeleteAssetCategory}
        onCancel={handleCloseCategoryDeleteConfirm}
        confirmLoading={isDeletingCategory}
        okText="Xóa"
        okButtonProps={{ danger: true }}
        cancelText="Hủy"
      >
        <p>
          Bạn có chắc muốn xóa nhóm tài sản <strong>{categoryDeleteTarget?.name}</strong>?
        </p>
      </Modal>

      <Modal
        title="Xóa kho"
        open={isWarehouseDeleteOpen}
        onOk={handleConfirmDeleteWarehouse}
        onCancel={handleCloseWarehouseDeleteConfirm}
        confirmLoading={isDeletingWarehouse}
        okText="Xóa"
        okButtonProps={{ danger: true }}
        cancelText="Hủy"
      >
        <p>
          Bạn có chắc muốn xóa kho <strong>{warehouseDeleteTarget?.name}</strong>?
        </p>
      </Modal>

      <Modal
        title={
          warehouseModalMode === 'create' ? 'Tạo kho mới' : 'Chỉnh sửa kho'
        }
        open={isWarehouseModalOpen}
        onCancel={() => {
          setIsWarehouseModalOpen(false);
          setEditingWarehouseId(null);
          setWarehouseModalMode('create');
        }}
        onOk={handleWarehouseModalOk}
        confirmLoading={isSavingWarehouse}
        okText={warehouseModalMode === 'create' ? 'Tạo kho' : 'Lưu'}
        cancelText="Hủy"
        destroyOnClose
      >
        <Form form={warehouseForm} layout="vertical">
          <Form.Item
            name="name"
            label="Tên kho"
            rules={[{ required: true, message: 'Vui lòng nhập tên kho.' }]}
          >
            <Input placeholder="Nhập tên kho" maxLength={200} />
          </Form.Item>
          <Form.Item name="location" label="Địa điểm">
            <Input.TextArea placeholder="Địa chỉ / mô tả (tuỳ chọn)" rows={2} maxLength={500} />
          </Form.Item>
        </Form>
      </Modal>

      <CategoriesModals
        isAssetTypeModalOpen={isAssetTypeModalOpen}
        setIsAssetTypeModalOpen={setIsAssetTypeModalOpen}
        assetTypeModalMode={assetTypeModalMode}
        editingAssetTypeId={editingAssetTypeId}
        assetTypeForm={assetTypeForm}
        onSubmitAssetType={handleSubmitAssetType}
        isSavingAssetType={isSavingAssetType}
        isAssetCategoryModalOpen={isAssetCategoryModalOpen}
        setIsAssetCategoryModalOpen={setIsAssetCategoryModalOpen}
        assetCategoryModalMode={assetCategoryModalMode}
        editingCategoryId={editingCategoryId}
        assetCategoryForm={assetCategoryForm}
        onSubmitAssetCategory={handleSubmitAssetCategory}
        isSavingAssetCategory={isSavingAssetCategory}
      />
    </div>
  );
}


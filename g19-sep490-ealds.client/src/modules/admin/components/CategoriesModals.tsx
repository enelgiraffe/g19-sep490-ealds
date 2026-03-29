import { Form, Input, Select, Switch, message } from 'antd';
import { useEffect, useState } from 'react';
import {
  assetInstanceService,
  type AssetInstanceResponse,
} from '../../assets/services/assetService';
import { assetCategoryService, type AssetCategoryItem } from '../services/assetCategoryService';
import {
  assetLocationService,
  type DepartmentLocationOption,
} from '../services/assetLocationService';
import './CategoriesModals.css';

const { Option } = Select;

interface CategoriesModalsProps {
  isAssetTypeModalOpen: boolean;
  setIsAssetTypeModalOpen: (open: boolean) => void;
  assetTypeModalMode: 'create' | 'edit';
  editingAssetTypeId: number | null;
  assetTypeForm: ReturnType<typeof Form.useForm<{ name: string; categoryId?: number }>>[0];
  onSubmitAssetType: (values: { name: string; categoryId: number }) => Promise<void>;
  isSavingAssetType: boolean;
  isAssetCategoryModalOpen: boolean;
  setIsAssetCategoryModalOpen: (open: boolean) => void;
  assetCategoryModalMode: 'create' | 'edit';
  editingCategoryId: number | null;
  assetCategoryForm: ReturnType<typeof Form.useForm<{ name: string }>>[0];
  onSubmitAssetCategory: (values: { name: string }) => Promise<void>;
  isSavingAssetCategory: boolean;
  isLocationModalOpen: boolean;
  setIsLocationModalOpen: (open: boolean) => void;
  locationModalMode: 'create' | 'edit';
  editingLocationId: number | null;
  locationForm: ReturnType<
    typeof Form.useForm<{
      assetInstanceId?: number;
      departmentId?: number;
      startDate: string;
      endDate?: string;
      isCurrent: boolean;
      note?: string;
    }>
  >[0];
  onSubmitLocation: (values: {
    assetInstanceId?: number;
    departmentId?: number;
    startDate: string;
    endDate?: string;
    isCurrent: boolean;
    note?: string;
  }) => Promise<void>;
  isSavingLocation: boolean;
}

export function CategoriesModals({
  isAssetTypeModalOpen,
  setIsAssetTypeModalOpen,
  assetTypeModalMode,
  editingAssetTypeId,
  assetTypeForm,
  onSubmitAssetType,
  isSavingAssetType,
  isAssetCategoryModalOpen,
  setIsAssetCategoryModalOpen,
  assetCategoryModalMode,
  editingCategoryId,
  assetCategoryForm,
  onSubmitAssetCategory,
  isSavingAssetCategory,
  isLocationModalOpen,
  setIsLocationModalOpen,
  locationModalMode,
  editingLocationId,
  locationForm,
  onSubmitLocation,
  isSavingLocation,
}: CategoriesModalsProps) {
  const [assetCategories, setAssetCategories] = useState<AssetCategoryItem[]>([]);
  const [isLoadingAssetCategories, setIsLoadingAssetCategories] = useState(false);
  const [departmentOptions, setDepartmentOptions] = useState<DepartmentLocationOption[]>([]);
  const [assetInstanceOptions, setAssetInstanceOptions] = useState<AssetInstanceResponse[]>([]);
  const [isLoadingLocationPickers, setIsLoadingLocationPickers] = useState(false);

  useEffect(() => {
    if (!isAssetTypeModalOpen) {
      return;
    }

    let cancelled = false;

    const loadCategories = async () => {
      try {
        setIsLoadingAssetCategories(true);
        const data = await assetCategoryService.getAll();
        if (cancelled) return;
        setAssetCategories(data);
        const currentId = assetTypeForm.getFieldValue('categoryId') as number | undefined;
        if (
          assetTypeModalMode === 'create' &&
          data.length > 0 &&
          (currentId === undefined || currentId === null)
        ) {
          assetTypeForm.setFieldValue('categoryId', data[0].categoryId);
        }
      } catch {
        if (!cancelled) {
          message.error('Không tải được danh sách nhóm tài sản.');
          setAssetCategories([]);
        }
      } finally {
        if (!cancelled) {
          setIsLoadingAssetCategories(false);
        }
      }
    };

    void loadCategories();

    return () => {
      cancelled = true;
    };
  }, [isAssetTypeModalOpen, assetTypeModalMode, assetTypeForm]);

  useEffect(() => {
    if (!isLocationModalOpen) {
      return;
    }

    let cancelled = false;

    const loadPickers = async () => {
      try {
        setIsLoadingLocationPickers(true);
        const [depts, instances] = await Promise.all([
          assetLocationService.getDepartments(),
          assetInstanceService.getAll(),
        ]);
        if (cancelled) return;
        setDepartmentOptions(depts);
        setAssetInstanceOptions(instances);
      } catch {
        if (!cancelled) {
          message.error('Không tải được danh sách tài sản / phòng ban.');
          setDepartmentOptions([]);
          setAssetInstanceOptions([]);
        }
      } finally {
        if (!cancelled) {
          setIsLoadingLocationPickers(false);
        }
      }
    };

    void loadPickers();

    return () => {
      cancelled = true;
    };
  }, [isLocationModalOpen]);

  return (
    <>
      {isAssetTypeModalOpen && (
        <div className="categories-modals__overlay" role="presentation">
          <div className="categories-modals__dialog" role="dialog" aria-modal="true">
            <button
              type="button"
              className="categories-modals__close-btn"
              onClick={() => setIsAssetTypeModalOpen(false)}
              disabled={isSavingAssetType}
              aria-label="Đóng"
            >
              <span className="categories-modals__close">×</span>
            </button>

            <div className="categories-modals__header">
              <h2 className="categories-modals__title">
                {assetTypeModalMode === 'create' ? 'Tạo loại tài sản' : 'Chỉnh sửa loại tài sản'}
              </h2>
            </div>

            <div className="categories-modals__body">
              <Form
                form={assetTypeForm}
                layout="vertical"
                className="categories-modals__content categories-modals-form"
                onFinish={async (values) => {
                  await onSubmitAssetType({
                    name: (values.name as string).trim(),
                    categoryId: values.categoryId as number,
                  });
                }}
              >
                <div className="categories-modals-form__section">
                  <h3 className="categories-modals-form__section-title">Thông tin loại tài sản</h3>

                  {assetTypeModalMode === 'edit' && editingAssetTypeId != null && (
                    <div className="categories-modals-form__item">
                      <div className="categories-modals-form__field ant-form-item">
                        <div className="ant-form-item-label">
                          <label>Mã loại tài sản</label>
                        </div>
                        <Input
                          value={String(editingAssetTypeId)}
                          disabled
                          className="categories-modals__input"
                        />
                      </div>
                    </div>
                  )}

                  <div className="categories-modals-form__item">
                    <Form.Item
                      label="Tên loại tài sản"
                      name="name"
                      rules={[{ required: true, message: 'Vui lòng nhập tên loại tài sản.' }]}
                      required
                      className="categories-modals-form__field"
                    >
                      <Input placeholder="-" className="categories-modals__input" />
                    </Form.Item>
                  </div>

                  <div className="categories-modals-form__item">
                    <Form.Item
                      label="Nhóm tài sản"
                      name="categoryId"
                      rules={[{ required: true, message: 'Vui lòng chọn nhóm tài sản.' }]}
                      required
                      className="categories-modals-form__field"
                    >
                      <Select
                        placeholder="Chọn nhóm tài sản"
                        loading={isLoadingAssetCategories}
                        disabled={
                          isSavingAssetType ||
                          isLoadingAssetCategories ||
                          assetCategories.length === 0
                        }
                        className="categories-modals__select"
                        popupMatchSelectWidth={false}
                      >
                        {assetCategories.map((c) => (
                          <Option key={c.categoryId} value={c.categoryId}>
                            {c.name}
                          </Option>
                        ))}
                      </Select>
                    </Form.Item>
                  </div>
                </div>
              </Form>
            </div>

            <div className="categories-modals__footer">
              <button
                type="button"
                className="categories-modals__btn--primary"
                disabled={isSavingAssetType}
                onClick={() => assetTypeForm.submit()}
              >
                {isSavingAssetType ? 'Đang lưu...' : 'Xác nhận'}
              </button>
              <button
                type="button"
                className="categories-modals__btn--secondary"
                disabled={isSavingAssetType}
                onClick={() => setIsAssetTypeModalOpen(false)}
              >
                Hủy
              </button>
            </div>
          </div>
        </div>
      )}

      {isAssetCategoryModalOpen && (
        <div className="categories-modals__overlay" role="presentation">
          <div className="categories-modals__dialog" role="dialog" aria-modal="true">
            <button
              type="button"
              className="categories-modals__close-btn"
              onClick={() => setIsAssetCategoryModalOpen(false)}
              disabled={isSavingAssetCategory}
              aria-label="Đóng"
            >
              <span className="categories-modals__close">×</span>
            </button>

            <div className="categories-modals__header">
              <h2 className="categories-modals__title">
                {assetCategoryModalMode === 'create' ? 'Tạo nhóm tài sản' : 'Chỉnh sửa nhóm tài sản'}
              </h2>
            </div>

            <div className="categories-modals__body">
              <Form
                form={assetCategoryForm}
                layout="vertical"
                className="categories-modals__content categories-modals-form"
                onFinish={async (values) => {
                  await onSubmitAssetCategory({
                    name: (values.name as string).trim(),
                  });
                }}
              >
                <div className="categories-modals-form__section">
                  <h3 className="categories-modals-form__section-title">Thông tin nhóm</h3>

                  {assetCategoryModalMode === 'edit' && editingCategoryId != null && (
                    <div className="categories-modals-form__item">
                      <div className="categories-modals-form__field ant-form-item">
                        <div className="ant-form-item-label">
                          <label>Mã nhóm</label>
                        </div>
                        <Input
                          value={String(editingCategoryId)}
                          disabled
                          className="categories-modals__input"
                        />
                      </div>
                    </div>
                  )}

                  <div className="categories-modals-form__item">
                    <Form.Item
                      label="Tên nhóm tài sản"
                      name="name"
                      rules={[{ required: true, message: 'Vui lòng nhập tên nhóm tài sản.' }]}
                      required
                      className="categories-modals-form__field"
                    >
                      <Input placeholder="-" className="categories-modals__input" />
                    </Form.Item>
                  </div>
                </div>
              </Form>
            </div>

            <div className="categories-modals__footer">
              <button
                type="button"
                className="categories-modals__btn--primary"
                disabled={isSavingAssetCategory}
                onClick={() => assetCategoryForm.submit()}
              >
                {isSavingAssetCategory ? 'Đang lưu...' : 'Xác nhận'}
              </button>
              <button
                type="button"
                className="categories-modals__btn--secondary"
                disabled={isSavingAssetCategory}
                onClick={() => setIsAssetCategoryModalOpen(false)}
              >
                Hủy
              </button>
            </div>
          </div>
        </div>
      )}

      {isLocationModalOpen && (
        <div className="categories-modals__overlay" role="presentation">
          <div className="categories-modals__dialog" role="dialog" aria-modal="true">
            <button
              type="button"
              className="categories-modals__close-btn"
              onClick={() => setIsLocationModalOpen(false)}
              disabled={isSavingLocation}
              aria-label="Đóng"
            >
              <span className="categories-modals__close">×</span>
            </button>

            <div className="categories-modals__header">
              <h2 className="categories-modals__title">
                {locationModalMode === 'create' ? 'Tạo vị trí tài sản' : 'Chỉnh sửa vị trí tài sản'}
              </h2>
            </div>

            <div className="categories-modals__body">
              <Form
                form={locationForm}
                layout="vertical"
                className="categories-modals__content categories-modals-form"
                onFinish={async (values) => {
                  await onSubmitLocation({
                    assetInstanceId: values.assetInstanceId as number | undefined,
                    departmentId: values.departmentId as number | undefined,
                    startDate: values.startDate as string,
                    endDate: (values.endDate as string | undefined)?.trim() || undefined,
                    isCurrent: values.isCurrent as boolean,
                    note: (values.note as string | undefined) ?? '',
                  });
                }}
              >
                <div className="categories-modals-form__section">
                  <h3 className="categories-modals-form__section-title">Thông tin vị trí</h3>

                  {locationModalMode === 'edit' && editingLocationId != null && (
                    <div className="categories-modals-form__item">
                      <div className="categories-modals-form__field ant-form-item">
                        <div className="ant-form-item-label">
                          <label>Mã bản ghi vị trí</label>
                        </div>
                        <Input
                          value={String(editingLocationId)}
                          disabled
                          className="categories-modals__input"
                        />
                      </div>
                    </div>
                  )}

                  <div className="categories-modals-form__item">
                    <Form.Item
                      label="Thiết bị (bản ghi tài sản)"
                      name="assetInstanceId"
                      rules={[{ required: true, message: 'Vui lòng chọn bản ghi tài sản.' }]}
                      required
                      className="categories-modals-form__field"
                    >
                      <Select
                        placeholder="Chọn bản ghi tài sản"
                        showSearch
                        optionFilterProp="children"
                        loading={isLoadingLocationPickers}
                        disabled={
                          isSavingLocation ||
                          locationModalMode === 'edit' ||
                          isLoadingLocationPickers
                        }
                        className="categories-modals__select"
                        popupMatchSelectWidth={false}
                      >
                        {assetInstanceOptions.map((a) => (
                          <Option key={a.assetInstanceId} value={a.assetInstanceId}>
                            {(a.assetCode ?? '').trim()} · {a.instanceCode} — {a.assetName ?? ''}
                          </Option>
                        ))}
                      </Select>
                    </Form.Item>
                  </div>

                  <div className="categories-modals-form__item">
                    <Form.Item
                      label="Phòng ban (vị trí)"
                      name="departmentId"
                      rules={[{ required: true, message: 'Vui lòng chọn phòng ban.' }]}
                      required
                      className="categories-modals-form__field"
                    >
                      <Select
                        placeholder="Chọn phòng ban"
                        showSearch
                        optionFilterProp="children"
                        loading={isLoadingLocationPickers}
                        disabled={isSavingLocation || isLoadingLocationPickers}
                        className="categories-modals__select"
                        popupMatchSelectWidth={false}
                      >
                        {departmentOptions.map((d) => (
                          <Option key={d.locationId} value={d.locationId}>
                            {d.displayName}
                          </Option>
                        ))}
                      </Select>
                    </Form.Item>
                  </div>

                  <div className="categories-modals-form__item categories-modals-form__item--inline-dates">
                    <Form.Item
                      label="Ngày bắt đầu"
                      name="startDate"
                      rules={[{ required: true, message: 'Vui lòng chọn ngày bắt đầu.' }]}
                      required
                      className="categories-modals-form__field"
                    >
                      <Input type="date" className="categories-modals__input" />
                    </Form.Item>
                    <Form.Item label="Ngày kết thúc" name="endDate" className="categories-modals-form__field">
                      <Input type="date" className="categories-modals__input" />
                    </Form.Item>
                  </div>

                  <div className="categories-modals-form__item">
                    <Form.Item
                      label="Vị trí hiện tại"
                      name="isCurrent"
                      valuePropName="checked"
                      className="categories-modals-form__field"
                    >
                      <Switch checkedChildren="Có" unCheckedChildren="Không" />
                    </Form.Item>
                  </div>

                  <div className="categories-modals-form__item">
                    <Form.Item label="Ghi chú" name="note" className="categories-modals-form__field">
                      <Input placeholder="Ghi chú (tuỳ chọn)" className="categories-modals__input" />
                    </Form.Item>
                  </div>
                </div>
              </Form>
            </div>

            <div className="categories-modals__footer">
              <button
                type="button"
                className="categories-modals__btn--primary"
                disabled={isSavingLocation}
                onClick={() => locationForm.submit()}
              >
                {isSavingLocation ? 'Đang lưu...' : 'Xác nhận'}
              </button>
              <button
                type="button"
                className="categories-modals__btn--secondary"
                disabled={isSavingLocation}
                onClick={() => setIsLocationModalOpen(false)}
              >
                Hủy
              </button>
            </div>
          </div>
        </div>
      )}
    </>
  );
}

import { Button, Form, Input, Modal, Radio, Select, message } from 'antd';
import { useMemo } from 'react';
import type { AssetLocationRow } from './AssetLocationsSection';

const { Option } = Select;

export type AssetManagementMethod = 'code' | 'quantity';

interface CategoriesModalsProps {
  isCreateAssetTypeOpen: boolean;
  setIsCreateAssetTypeOpen: (open: boolean) => void;
  createForm: ReturnType<typeof Form.useForm<{
    name: string;
    code: string;
    groupCode: string;
    note?: string;
    managementMethod: AssetManagementMethod;
  }>>[0];
  isLocationModalOpen: boolean;
  setIsLocationModalOpen: (open: boolean) => void;
  locationModalMode: 'create' | 'edit';
  editingLocation: AssetLocationRow | null;
  isCreateAssetGroupOpen: boolean;
  setIsCreateAssetGroupOpen: (open: boolean) => void;
  createGroupForm: ReturnType<typeof Form.useForm<{
    name: string;
    code: string;
    parentCode?: string | null;
  }>>[0];
}

export function CategoriesModals({
  isCreateAssetTypeOpen,
  setIsCreateAssetTypeOpen,
  createForm,
  isLocationModalOpen,
  setIsLocationModalOpen,
  locationModalMode,
  editingLocation,
  isCreateAssetGroupOpen,
  setIsCreateAssetGroupOpen,
  createGroupForm,
}: CategoriesModalsProps) {
  const locationInitialValues = useMemo(
    () => ({
      name: editingLocation?.name ?? '',
      parentName: editingLocation?.parentName ?? 'Kho A',
      status: editingLocation?.status ?? 'tracking',
      note: editingLocation?.note ?? '',
    }),
    [editingLocation],
  );

  return (
    <>
      <Modal
        open={isCreateAssetTypeOpen}
        onCancel={() => setIsCreateAssetTypeOpen(false)}
        footer={null}
        centered
        destroyOnClose
        closeIcon={<span className="categories-modal__close">×</span>}
        className="categories-create-modal"
        title={<span className="categories-modal__title">Tạo loại tài sản</span>}
      >
        <Form
          form={createForm}
          layout="vertical"
          className="categories-modal__form"
          initialValues={{
            managementMethod: 'code' as AssetManagementMethod,
            groupCode: 'MM',
          }}
          onFinish={() => {
            message.success('Tạo loại tài sản thành công (mock).');
            setIsCreateAssetTypeOpen(false);
          }}
        >
          <Form.Item
            label="Tên loại tài sản"
            name="name"
            rules={[{ required: true, message: 'Vui lòng nhập tên loại tài sản.' }]}
            required
          >
            <Input placeholder="-" />
          </Form.Item>

          <Form.Item
            label="Mã loại tài sản"
            name="code"
            rules={[{ required: true, message: 'Vui lòng nhập mã loại tài sản.' }]}
            required
          >
            <Input placeholder="-" />
          </Form.Item>

          <Form.Item label="Nhóm tài sản" name="groupCode">
            <Select disabled placeholder="Nhóm tài sản (backend sẽ bổ sung sau)" />
          </Form.Item>

          <Form.Item label="Ghi chú" name="note">
            <Input.TextArea placeholder="Nội dung Ghi chú" rows={6} />
          </Form.Item>

          <Form.Item
            label="Cách quản lý"
            name="managementMethod"
            rules={[{ required: true, message: 'Vui lòng chọn cách quản lý.' }]}
            required
          >
            <Radio.Group className="categories-management-group">
              <Radio value="code" className="categories-management-option">
                Quản lý theo mã
              </Radio>
              <Radio value="quantity" className="categories-management-option">
                Quản lý theo số lượng
              </Radio>
            </Radio.Group>
          </Form.Item>

          <div className="categories-modal__footer">
            <Button
              type="primary"
              danger
              htmlType="submit"
              className="categories-modal__btn categories-modal__btn--primary"
            >
              ✓ Xác nhận
            </Button>
            <Button
              onClick={() => setIsCreateAssetTypeOpen(false)}
              className="categories-modal__btn categories-modal__btn--secondary"
            >
              ✕ Hủy
            </Button>
          </div>
        </Form>
      </Modal>

      <Modal
        open={isLocationModalOpen}
        onCancel={() => setIsLocationModalOpen(false)}
        footer={null}
        centered
        destroyOnClose
        closeIcon={<span className="categories-modal__close">×</span>}
        className="categories-create-modal"
        title={
          <span className="categories-modal__title">
            {locationModalMode === 'create' ? 'Tạo vị trí' : 'Chỉnh sửa vị trí'}
          </span>
        }
      >
        <Form
          layout="vertical"
          className="categories-modal__form"
          initialValues={locationInitialValues}
          onFinish={() => {
            message.success(
              locationModalMode === 'create'
                ? 'Tạo vị trí tài sản thành công (mock).'
                : 'Cập nhật vị trí tài sản thành công (mock).',
            );
            setIsLocationModalOpen(false);
          }}
        >
          <Form.Item
            label="Tên vị trí"
            name="name"
            rules={[{ required: true, message: 'Vui lòng nhập tên vị trí.' }]}
            required
          >
            <Input placeholder="Tên vị trí" />
          </Form.Item>

          <Form.Item label="Thuộc" name="parentName">
            <Select placeholder="Kho A">
              <Option value="Kho A">Kho A</Option>
              <Option value="Kho B">Kho B</Option>
            </Select>
          </Form.Item>

          <Form.Item
            label="Trạng thái"
            name="status"
            rules={[{ required: true, message: 'Vui lòng chọn trạng thái.' }]}
            required
          >
            <Select>
              <Option value="tracking">Đang theo dõi</Option>
              <Option value="stopped">Không theo dõi</Option>
            </Select>
          </Form.Item>

          <Form.Item label="Ghi chú" name="note">
            <Input placeholder="IDL" />
          </Form.Item>

          <div className="categories-modal__footer">
            <Button
              type="primary"
              danger
              htmlType="submit"
              className="categories-modal__btn categories-modal__btn--primary"
            >
              {locationModalMode === 'create' ? '✓ Tạo' : '✎ Chỉnh sửa'}
            </Button>
            <Button
              onClick={() => setIsLocationModalOpen(false)}
              className="categories-modal__btn categories-modal__btn--secondary"
            >
              ✕ Đóng
            </Button>
          </div>
        </Form>
      </Modal>

      <Modal
        open={isCreateAssetGroupOpen}
        onCancel={() => setIsCreateAssetGroupOpen(false)}
        footer={null}
        centered
        destroyOnClose
        closeIcon={<span className="categories-modal__close">×</span>}
        className="categories-create-modal"
        title={<span className="categories-modal__title">Thêm nhóm tài sản</span>}
      >
        <Form
          form={createGroupForm}
          layout="vertical"
          className="categories-modal__form"
          onFinish={() => {
            message.success('Tạo nhóm tài sản thành công (mock).');
            setIsCreateAssetGroupOpen(false);
          }}
        >
          <Form.Item
            label="Tên nhóm tài sản"
            name="name"
            rules={[{ required: true, message: 'Vui lòng nhập tên nhóm tài sản.' }]}
            required
          >
            <Input placeholder="-" />
          </Form.Item>

          <Form.Item
            label="Mã nhóm tài sản"
            name="code"
            rules={[{ required: true, message: 'Vui lòng nhập mã nhóm tài sản.' }]}
            required
          >
            <Input placeholder="-" />
          </Form.Item>

          <Form.Item label="Thuộc nhóm" name="parentCode">
            <Select allowClear placeholder="Chọn nhóm cha" disabled>
              <Option value="root">root</Option>
            </Select>
          </Form.Item>

          <div className="categories-modal__footer">
            <Button
              type="primary"
              danger
              htmlType="submit"
              className="categories-modal__btn categories-modal__btn--primary"
            >
              ✓ Xác nhận
            </Button>
            <Button
              onClick={() => setIsCreateAssetGroupOpen(false)}
              className="categories-modal__btn categories-modal__btn--secondary"
            >
              ✕ Hủy
            </Button>
          </div>
        </Form>
      </Modal>
    </>
  );
}


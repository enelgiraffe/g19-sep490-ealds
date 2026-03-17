import { useEffect, useState } from 'react';
import { Modal, Form, Input, Select, DatePicker, Button, message } from 'antd';
import type { Dayjs } from 'dayjs';
import {
  inventoryService,
  getCurrentUserId,
  type DropdownItem,
  type AssetDropdownItem,
} from '../services/inventoryService';
import './SchedulePeriodicModal.css';

const { TextArea } = Input;

export interface SchedulePeriodicFormValues {
  checkDate: Dayjs;
  assetId?: number;
  departmentId: number;
  assetCategoryId: number;
  assetTypeId: number;
  purpose: string;
}

interface SchedulePeriodicModalProps {
  open: boolean;
  onClose: () => void;
  onSubmit: (values: SchedulePeriodicFormValues) => void;
}

export function SchedulePeriodicModal({
  open,
  onClose,
  onSubmit,
}: SchedulePeriodicModalProps) {
  const [form] = Form.useForm<SchedulePeriodicFormValues>();
  const [submitting, setSubmitting] = useState(false);

  const [assets, setAssets] = useState<AssetDropdownItem[]>([]);
  const [departments, setDepartments] = useState<DropdownItem[]>([]);
  const [categories, setCategories] = useState<DropdownItem[]>([]);
  const [assetTypes, setAssetTypes] = useState<DropdownItem[]>([]);
  const [loadingMeta, setLoadingMeta] = useState(false);
  const [loadingAssets, setLoadingAssets] = useState(false);

  useEffect(() => {
    if (open) {
      form.resetFields();
      setAssetTypes([]);
      loadMeta();
    }
  }, [open, form]);

  const loadMeta = async () => {
    setLoadingMeta(true);
    setLoadingAssets(true);
    try {
      const [deps, cats, assetList] = await Promise.all([
        inventoryService.getDepartments(),
        inventoryService.getAssetCategories(),
        inventoryService.getAssets(),
      ]);
      setDepartments(deps);
      setCategories(cats);
      setAssets(assetList);
    } catch {
      message.error('Không thể tải dữ liệu. Vui lòng thử lại.');
    } finally {
      setLoadingMeta(false);
      setLoadingAssets(false);
    }
  };

  const handleCategoryChange = async (categoryId: number) => {
    form.setFieldValue('assetTypeId', undefined);
    setAssetTypes([]);
    if (!categoryId) return;
    try {
      const types = await inventoryService.getAssetTypes(categoryId);
      setAssetTypes(types);
    } catch {
      message.error('Không thể tải loại tài sản.');
    }
  };

  const handleSubmit = () => {
    form.validateFields().then(async (values) => {
      setSubmitting(true);
      try {
        const checkDate = values.checkDate.toISOString();
        await inventoryService.createSession({
          purpose: values.purpose,
          startDate: checkDate,
          endDate: checkDate,
          departmentId: values.departmentId,
          assetCategoryId: values.assetCategoryId,
          assetTypeId: values.assetTypeId,
          createdBy: getCurrentUserId(),
        });
        message.success('Đã lập lịch kiểm kê thành công!');
        onSubmit(values);
        form.resetFields();
        onClose();
      } catch (err: any) {
        const msg = err?.response?.data?.message || 'Lập lịch kiểm kê thất bại.';
        message.error(msg);
      } finally {
        setSubmitting(false);
      }
    });
  };

  return (
    <Modal
      open={open}
      onCancel={onClose}
      footer={null}
      width={520}
      centered
      className="schedule-periodic-modal"
      closeIcon={<span className="schedule-modal__close">×</span>}
    >
      <div className="schedule-modal__header">
        <h2 className="schedule-modal__title">Lập lịch kiểm kê</h2>
      </div>

      <Form form={form} layout="vertical" className="schedule-periodic-form">
        <Form.Item
          label="Ngày"
          name="checkDate"
          rules={[{ required: true, message: 'Vui lòng chọn ngày kiểm kê' }]}
        >
          <DatePicker
            format="DD MMM YYYY"
            placeholder="Chọn ngày"
            className="schedule-form__datepicker"
          />
        </Form.Item>

        <Form.Item label="Tài sản" name="assetId">
          <Select
            placeholder="Select"
            loading={loadingAssets}
            showSearch
            allowClear
            optionFilterProp="label"
            options={assets.map((a) => ({
              value: a.assetId,
              label: `${a.code} - ${a.name}`,
            }))}
          />
        </Form.Item>

        <Form.Item
          label="Vị trí tài sản"
          name="departmentId"
          rules={[{ required: true, message: 'Vui lòng chọn vị trí tài sản' }]}
        >
          <Select
            placeholder="Select"
            loading={loadingMeta}
            showSearch
            allowClear
            optionFilterProp="label"
            options={departments.map((d) => ({ value: d.id, label: d.name }))}
          />
        </Form.Item>

        <Form.Item
          label="Nhóm tài sản"
          name="assetCategoryId"
          rules={[{ required: true, message: 'Vui lòng chọn nhóm tài sản' }]}
        >
          <Select
            placeholder="Select"
            loading={loadingMeta}
            showSearch
            allowClear
            optionFilterProp="label"
            options={categories.map((c) => ({ value: c.id, label: c.name }))}
            onChange={handleCategoryChange}
          />
        </Form.Item>

        <Form.Item
          label="Loại tài sản"
          name="assetTypeId"
          rules={[{ required: true, message: 'Vui lòng chọn loại tài sản' }]}
        >
          <Select
            placeholder="Select"
            showSearch
            allowClear
            optionFilterProp="label"
            options={assetTypes.map((t) => ({ value: t.id, label: t.name }))}
            disabled={assetTypes.length === 0}
          />
        </Form.Item>

        <Form.Item label="Mục đích" name="purpose">
          <TextArea rows={4} className="schedule-form__textarea" />
        </Form.Item>

        <div className="schedule-modal__footer">
          <Button onClick={onClose} className="schedule-modal__btn-cancel" disabled={submitting}>
            Hủy
          </Button>
          <Button
            type="primary"
            onClick={handleSubmit}
            className="schedule-modal__btn-submit"
            loading={submitting}
          >
            Lập lịch
          </Button>
        </div>
      </Form>
    </Modal>
  );
}

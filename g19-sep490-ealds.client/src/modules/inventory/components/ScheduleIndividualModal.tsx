import { useEffect, useState } from 'react';
import { Modal, Form, Input, Select, DatePicker, Button, message } from 'antd';
import type { Dayjs } from 'dayjs';
import {
  inventoryService,
  getCurrentUserId,
  type DropdownItem,
} from '../services/inventoryService';
import './ScheduleIndividualModal.css';

const { TextArea } = Input;

export interface ScheduleIndividualFormValues {
  departmentId: number;
  assetCategoryId: number;
  assetTypeId: number;
  purpose: string;
  startDate: Dayjs;
  endDate: Dayjs;
}

interface ScheduleIndividualModalProps {
  open: boolean;
  onClose: () => void;
  onSubmit: (values: ScheduleIndividualFormValues) => void;
}

export function ScheduleIndividualModal({
  open,
  onClose,
  onSubmit,
}: ScheduleIndividualModalProps) {
  const [form] = Form.useForm<ScheduleIndividualFormValues>();
  const [submitting, setSubmitting] = useState(false);

  const [departments, setDepartments] = useState<DropdownItem[]>([]);
  const [categories, setCategories] = useState<DropdownItem[]>([]);
  const [assetTypes, setAssetTypes] = useState<DropdownItem[]>([]);
  const [loadingMeta, setLoadingMeta] = useState(false);

  useEffect(() => {
    if (open) {
      form.resetFields();
      setAssetTypes([]);
      loadMeta();
    }
  }, [open, form]);

  const loadMeta = async () => {
    setLoadingMeta(true);
    try {
      const [deps, cats] = await Promise.all([
        inventoryService.getDepartments(),
        inventoryService.getAssetCategories(),
      ]);
      setDepartments(deps);
      setCategories(cats);
    } catch {
      message.error('Không thể tải dữ liệu. Vui lòng thử lại.');
    } finally {
      setLoadingMeta(false);
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
        await inventoryService.createSession({
          purpose: values.purpose,
          startDate: values.startDate.toISOString(),
          endDate: values.endDate.toISOString(),
          departmentId: values.departmentId,
          assetCategoryId: values.assetCategoryId,
          assetTypeId: values.assetTypeId,
          createdBy: getCurrentUserId(),
        });
        message.success('Đã hẹn lịch kiểm kê thành công!');
        onSubmit(values);
        form.resetFields();
        onClose();
      } catch (err: any) {
        const msg = err?.response?.data?.message || 'Hẹn lịch kiểm kê thất bại.';
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
      className="schedule-individual-modal"
      closeIcon={<span className="schedule-modal__close">×</span>}
    >
      <div className="schedule-modal__header">
        <h2 className="schedule-modal__title">Hẹn lịch kiểm kê</h2>
        <p className="schedule-modal__subtitle">Kiểm kê định kỳ theo nhóm và loại tài sản</p>
      </div>

      <Form form={form} layout="vertical" className="schedule-individual-form">
        <Form.Item
          label="Phòng ban"
          name="departmentId"
          rules={[{ required: true, message: 'Vui lòng chọn phòng ban' }]}
        >
          <Select
            placeholder="Chọn phòng ban"
            loading={loadingMeta}
            showSearch
            optionFilterProp="label"
            options={departments.map((d) => ({ value: d.id, label: d.name }))}
          />
        </Form.Item>

        <Form.Item
          label="Nhóm tài sản (Danh mục)"
          name="assetCategoryId"
          rules={[{ required: true, message: 'Vui lòng chọn nhóm tài sản' }]}
        >
          <Select
            placeholder="Chọn nhóm tài sản"
            loading={loadingMeta}
            showSearch
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
            placeholder="Chọn loại tài sản"
            showSearch
            optionFilterProp="label"
            options={assetTypes.map((t) => ({ value: t.id, label: t.name }))}
            disabled={assetTypes.length === 0}
          />
        </Form.Item>

        <Form.Item
          label="Mục đích"
          name="purpose"
          rules={[{ required: true, message: 'Vui lòng nhập mục đích kiểm kê' }]}
        >
          <TextArea rows={3} placeholder="Ví dụ: Kiểm kê định kỳ quý I năm 2026" />
        </Form.Item>

        <div className="schedule-individual-form__date-row">
          <Form.Item
            label="Ngày bắt đầu"
            name="startDate"
            rules={[{ required: true, message: 'Vui lòng chọn ngày bắt đầu' }]}
          >
            <DatePicker format="DD/MM/YYYY" placeholder="Chọn ngày" style={{ width: '100%' }} />
          </Form.Item>

          <Form.Item
            label="Ngày kết thúc"
            name="endDate"
            rules={[
              { required: true, message: 'Vui lòng chọn ngày kết thúc' },
              ({ getFieldValue }) => ({
                validator(_, value) {
                  if (!value || !getFieldValue('startDate') || value.isAfter(getFieldValue('startDate'))) {
                    return Promise.resolve();
                  }
                  return Promise.reject(new Error('Ngày kết thúc phải sau ngày bắt đầu'));
                },
              }),
            ]}
          >
            <DatePicker format="DD/MM/YYYY" placeholder="Chọn ngày" style={{ width: '100%' }} />
          </Form.Item>
        </div>

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
            Hẹn lịch
          </Button>
        </div>
      </Form>
    </Modal>
  );
}

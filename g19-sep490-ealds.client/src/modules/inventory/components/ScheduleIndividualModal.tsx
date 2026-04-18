import { useEffect, useState } from 'react';
import { Form, Input, Select, DatePicker, message } from 'antd';
import type { Dayjs } from 'dayjs';
import { profileService } from '../../profile/services/profileService';
import {
  inventoryService,
  getCurrentUserId,
  inventorySessionDateToUtcIso,
  inventorySessionEndOfDayUtcIso,
  type DropdownItem,
} from '../services/inventoryService';
import '../../purchase-orders/components/CreatePurchaseOrderModal.css';
import './ScheduleIndividualModal.css';

const { TextArea } = Input;

export interface ScheduleIndividualFormValues {
  checkDate: Dayjs;
  departmentId: number;
  purpose: string;
}

interface ScheduleIndividualModalProps {
  readonly open: boolean;
  readonly onClose: () => void;
  readonly onSubmit: (values: ScheduleIndividualFormValues) => void;
}

export function ScheduleIndividualModal({
  open,
  onClose,
  onSubmit,
}: ScheduleIndividualModalProps) {
  const [form] = Form.useForm<ScheduleIndividualFormValues>();
  const [submitting, setSubmitting] = useState(false);
  const [departments, setDepartments] = useState<DropdownItem[]>([]);
  const [loadingMeta, setLoadingMeta] = useState(false);

  useEffect(() => {
    if (open) {
      form.resetFields();
      loadMeta();
    }
  }, [open, form]);

  const loadMeta = async () => {
    setLoadingMeta(true);
    try {
      const deps = await inventoryService.getDepartments();
      setDepartments(deps);
      const profile = await profileService.getProfile();
      const defaultDeptId = profile.departmentId ?? undefined;
      if (defaultDeptId != null && deps.some((d) => d.id === defaultDeptId)) {
        form.setFieldsValue({ departmentId: defaultDeptId });
      }
    } catch {
      message.error('Không thể tải dữ liệu. Vui lòng thử lại.');
    } finally {
      setLoadingMeta(false);
    }
  };

  const handleSubmit = () => {
    form.validateFields().then(async (values) => {
      const departmentId = values.departmentId;
      if (departmentId == null) {
        message.error('Vui lòng chọn phòng ban.');
        return;
      }
      setSubmitting(true);
      try {
        const startIso = inventorySessionDateToUtcIso(values.checkDate);
        await inventoryService.createSession({
          purpose: values.purpose ?? '',
          startDate: startIso,
          endDate: inventorySessionEndOfDayUtcIso(values.checkDate),
          departmentId,
          createdBy: getCurrentUserId(),
        });
        message.success('Đã lập lịch kiểm kê thành công!');
        onSubmit(values);
        form.resetFields();
        onClose();
      } catch (err: unknown) {
        const data = (err as { response?: { data?: { message?: string; errors?: Record<string, string[]> } } })
          ?.response?.data;
        const msg =
          data?.message ||
          (data?.errors ? Object.values(data.errors).flat().join('; ') : undefined) ||
          'Lập lịch kiểm kê thất bại.';
        message.error(msg);
      } finally {
        setSubmitting(false);
      }
    });
  };

  if (!open) return null;

  return (
    <div className="create-purchase-modal-overlay" role="dialog" aria-modal="true">
      <div className="create-purchase-modal inventory-schedule-modal">
        <button type="button" className="create-purchase-modal__close-btn" onClick={onClose} aria-label="Đóng">
          <span className="create-purchase-modal__close">×</span>
        </button>

        <div className="create-purchase-modal__header">
          <h2 className="create-purchase-modal__title">Lập lịch kiểm kê</h2>
          <p className="inventory-schedule-modal__subtitle">Kiểm kê riêng lẻ</p>
        </div>

        <div className="create-purchase-modal__body">
          <Form form={form} layout="vertical" className="create-purchase-form">
            <Form.Item
              label="Ngày kiểm kê"
              name="checkDate"
              rules={[{ required: true, message: 'Vui lòng chọn ngày kiểm kê' }]}
            >
              <DatePicker
                format="DD/MM/YYYY"
                placeholder="Chọn ngày"
                className="schedule-form__datepicker"
                style={{ width: '100%' }}
              />
            </Form.Item>

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

            <Form.Item label="Mục đích" name="purpose">
              <TextArea
                rows={3}
                placeholder="Ví dụ: Kiểm kê tài sản định kỳ tháng 3"
                className="schedule-form__textarea"
              />
            </Form.Item>
          </Form>
        </div>

        <div className="create-purchase-modal__footer">
          <button
            type="button"
            className="create-purchase-btn-submit"
            disabled={submitting}
            onClick={() => void handleSubmit()}
          >
            {submitting ? 'Đang lưu…' : 'Lập lịch'}
          </button>
          <button type="button" className="create-purchase-btn-cancel" disabled={submitting} onClick={onClose}>
            Hủy
          </button>
        </div>
      </div>
    </div>
  );
}

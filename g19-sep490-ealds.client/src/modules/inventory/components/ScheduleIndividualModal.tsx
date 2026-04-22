import { useEffect, useState } from 'react';
import { Form, Input, Select, DatePicker, InputNumber, message } from 'antd';
import dayjs from 'dayjs';
import type { Dayjs } from 'dayjs';
import { profileService } from '../../profile/services/profileService';
import {
  inventoryService,
  getCurrentUserId,
  inventorySessionDateToUtcIso,
  inventorySessionEndDayForInclusiveDuration,
  inventorySessionEndOfDayUtcIso,
  type DropdownItem,
} from '../services/inventoryService';
import '../../purchase-orders/components/CreatePurchaseOrderModal.css';
import './ScheduleIndividualModal.css';
import './SchedulePeriodicModal.css';

const { TextArea } = Input;

export interface ScheduleIndividualFormValues {
  checkDate: Dayjs;
  departmentId: number;
  purpose: string;
  executionDays: number;
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
      const patch: Partial<ScheduleIndividualFormValues> = { executionDays: 1 };
      if (defaultDeptId != null && deps.some((d) => d.id === defaultDeptId)) {
        patch.departmentId = defaultDeptId;
      }
      form.setFieldsValue(patch);
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
        const endDay = inventorySessionEndDayForInclusiveDuration(values.checkDate, values.executionDays);
        await inventoryService.createSession({
          purpose: values.purpose,
          startDate: inventorySessionDateToUtcIso(values.checkDate),
          endDate: inventorySessionEndOfDayUtcIso(endDay),
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
      <div className="create-purchase-modal inventory-schedule-modal inventory-schedule-modal--wide">
        <button type="button" className="create-purchase-modal__close-btn" onClick={onClose} aria-label="Đóng">
          <span className="create-purchase-modal__close">×</span>
        </button>

        <div className="create-purchase-modal__header">
          <h2 className="create-purchase-modal__title">Tạo lịch kiểm kê bất thường</h2>
        </div>

        <div className="create-purchase-modal__body">
          <Form form={form} layout="vertical" className="create-purchase-form schedule-periodic-form">
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
              label="Mục đích"
              name="purpose"
              rules={[{ required: true, message: 'Vui lòng nhập mục đích kiểm kê' }]}
            >
              <TextArea
                rows={3}
                placeholder="Ví dụ: Kiểm kê tài sản định kỳ tháng 3"
                className="schedule-form__textarea"
              />
            </Form.Item>

            <div className="schedule-periodic-form__date-row">
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
                  disabledDate={(current) =>
                    !!current && current.isBefore(dayjs().startOf('day'))
                  }
                />
              </Form.Item>

              <Form.Item
                label="Thời gian thực hiện (ngày)"
                name="executionDays"
                rules={[
                  { required: true, message: 'Vui lòng nhập số ngày thực hiện' },
                  { type: 'number', min: 1, message: 'Phải ít nhất 1 ngày' },
                ]}
              >
                <InputNumber min={1} max={365} style={{ width: '100%' }} placeholder="Ví dụ: 7" />
              </Form.Item>
            </div>

            <Form.Item shouldUpdate noStyle>
              {() => {
                const checkDate = form.getFieldValue('checkDate') as Dayjs | undefined;
                const execDays = form.getFieldValue('executionDays') as number | undefined;
                if (!checkDate || !execDays || execDays <= 0) return null;
                const endDate = inventorySessionEndDayForInclusiveDuration(checkDate, execDays);
                return (
                  <div className="schedule-periodic-form__enddate-preview">
                    <span className="schedule-periodic-form__enddate-label">Hạn hoàn thành:</span>
                    <strong className="schedule-periodic-form__enddate-value">
                      {endDate.format('DD/MM/YYYY')}
                    </strong>
                  </div>
                );
              }}
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

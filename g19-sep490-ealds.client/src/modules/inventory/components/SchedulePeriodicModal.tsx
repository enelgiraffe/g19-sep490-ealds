import { useEffect, useState } from 'react';
import { Form, Input, Select, DatePicker, message, InputNumber } from 'antd';
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
import './SchedulePeriodicModal.css';

const { TextArea } = Input;

export const INVENTORY_PERIOD_PRESETS = [
  { label: 'Hàng tháng (30 ngày)', value: 30 },
  { label: '2 tháng (60 ngày)', value: 60 },
  { label: 'Hàng quý (90 ngày)', value: 90 },
  { label: 'Nửa năm (180 ngày)', value: 180 },
  { label: 'Hàng năm (365 ngày)', value: 365 },
  { label: 'Tùy chỉnh...', value: -1 },
] as const;

export interface SchedulePeriodicFormValues {
  departmentId: number;
  purpose: string;
  startDate: Dayjs;
  executionDays: number;
  periodPreset: number;
  periodCustomDays?: number;
}

interface SchedulePeriodicModalProps {
  readonly open: boolean;
  readonly onClose: () => void;
  readonly onSubmit: (values: SchedulePeriodicFormValues) => void;
}

export function SchedulePeriodicModal({
  open,
  onClose,
  onSubmit,
}: SchedulePeriodicModalProps) {
  const [form] = Form.useForm<SchedulePeriodicFormValues>();
  const [submitting, setSubmitting] = useState(false);
  const [isCustomPeriod, setIsCustomPeriod] = useState(false);

  const [departments, setDepartments] = useState<DropdownItem[]>([]);
  const [loadingMeta, setLoadingMeta] = useState(false);

  useEffect(() => {
    if (open) {
      form.resetFields();
      setIsCustomPeriod(false);
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

  const handlePeriodPresetChange = (value: number) => {
    setIsCustomPeriod(value === -1);
    if (value !== -1) {
      form.setFieldValue('periodCustomDays', undefined);
    }
  };

  const handleSubmit = () => {
    form.validateFields().then(async (values) => {
      const periodDays = values.periodPreset === -1
        ? values.periodCustomDays
        : values.periodPreset;

      if (!periodDays || periodDays <= 0) {
        message.error('Vui lòng nhập chu kỳ kiểm kê hợp lệ.');
        return;
      }

      const computedEndDate = inventorySessionEndDayForInclusiveDuration(
        values.startDate,
        values.executionDays,
      );

      const departmentId = values.departmentId;
      if (departmentId == null) {
        message.error('Vui lòng chọn phòng ban.');
        return;
      }

      setSubmitting(true);
      try {
        await inventoryService.createSession({
          purpose: values.purpose,
          startDate: inventorySessionDateToUtcIso(values.startDate),
          endDate: inventorySessionEndOfDayUtcIso(computedEndDate),
          departmentId,
          createdBy: getCurrentUserId(),
          isPeriodic: true,
          periodDays,
        });
        message.success('Đã lập lịch kiểm kê định kỳ thành công!');
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

  if (!open) return null;

  return (
    <div className="create-purchase-modal-overlay" role="dialog" aria-modal="true">
      <div className="create-purchase-modal inventory-schedule-modal--wide">
        <button type="button" className="create-purchase-modal__close-btn" onClick={onClose} aria-label="Đóng">
          <span className="create-purchase-modal__close">×</span>
        </button>

        <div className="create-purchase-modal__header">
          <h2 className="create-purchase-modal__title">Tạo lịch kiểm kê định kỳ</h2>
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
              <TextArea rows={3} placeholder="Ví dụ: Kiểm kê định kỳ quý I năm 2026" />
            </Form.Item>

            <Form.Item
              label="Chu kỳ kiểm kê"
              name="periodPreset"
              rules={[{ required: true, message: 'Vui lòng chọn chu kỳ kiểm kê' }]}
            >
              <Select
                placeholder="Chọn chu kỳ"
                options={INVENTORY_PERIOD_PRESETS.map((p) => ({ value: p.value, label: p.label }))}
                onChange={handlePeriodPresetChange}
              />
            </Form.Item>

            {isCustomPeriod && (
              <Form.Item
                label="Số ngày tùy chỉnh (ngày)"
                name="periodCustomDays"
                rules={[
                  { required: true, message: 'Vui lòng nhập số ngày' },
                  { type: 'number', min: 1, message: 'Chu kỳ phải ít nhất 1 ngày' },
                ]}
              >
                <InputNumber
                  min={1}
                  max={3650}
                  style={{ width: '100%' }}
                  placeholder="Nhập số ngày (ví dụ: 45)"
                />
              </Form.Item>
            )}

            <div className="schedule-periodic-form__date-row">
              <Form.Item
                label="Ngày bắt đầu đầu tiên"
                name="startDate"
                rules={[{ required: true, message: 'Vui lòng chọn ngày bắt đầu' }]}
              >
                <DatePicker
                  format="DD/MM/YYYY"
                  placeholder="Chọn ngày"
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
                const startDate = form.getFieldValue('startDate') as Dayjs | undefined;
                const execDays = form.getFieldValue('executionDays') as number | undefined;
                if (!startDate || !execDays || execDays <= 0) return null;
                const endDate = inventorySessionEndDayForInclusiveDuration(startDate, execDays);
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
            {submitting ? 'Đang lưu…' : 'Hẹn lịch'}
          </button>
          <button type="button" className="create-purchase-btn-cancel" disabled={submitting} onClick={onClose}>
            Hủy
          </button>
        </div>
      </div>
    </div>
  );
}

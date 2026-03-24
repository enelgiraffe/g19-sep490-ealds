import { useEffect, useState } from 'react';
import { Modal, Form, Input, Select, DatePicker, Button, message, InputNumber } from 'antd';
import type { Dayjs } from 'dayjs';
import { useAppStore } from '../../../stores/appStore';
import {
  inventoryService,
  getCurrentUserId,
  type DropdownItem,
} from '../services/inventoryService';
import './SchedulePeriodicModal.css';

const { TextArea } = Input;

const PERIOD_PRESETS = [
  { label: 'Hàng tháng (30 ngày)', value: 30 },
  { label: '2 tháng (60 ngày)', value: 60 },
  { label: 'Hàng quý (90 ngày)', value: 90 },
  { label: 'Nửa năm (180 ngày)', value: 180 },
  { label: 'Hàng năm (365 ngày)', value: 365 },
  { label: 'Tùy chỉnh...', value: -1 },
];

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
  const isDeptHead = useAppStore((s) => s.currentRole) === 'department_head';
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
      if (isDeptHead && deps.length === 1) {
        form.setFieldsValue({ departmentId: deps[0].id });
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

      const computedEndDate = values.startDate.add(values.executionDays, 'day');

      const departmentId = isDeptHead ? departments[0]?.id : values.departmentId;
      if (departmentId == null) {
        message.error('Không xác định được phòng ban.');
        return;
      }

      setSubmitting(true);
      try {
        await inventoryService.createSession({
          purpose: values.purpose,
          startDate: values.startDate.toISOString(),
          endDate: computedEndDate.toISOString(),
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
        <h2 className="schedule-modal__title">Hẹn lịch kiểm kê</h2>
        <p className="schedule-modal__subtitle">Kiểm kê định kỳ theo nhóm và loại tài sản</p>
      </div>

      <Form form={form} layout="vertical" className="schedule-periodic-form">
        {!isDeptHead && (
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
        )}
        {isDeptHead && departments[0] && (
          <p className="schedule-modal__dept-hint">
            Phòng ban: <strong>{departments[0].name}</strong>
          </p>
        )}

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
            options={PERIOD_PRESETS.map((p) => ({ value: p.value, label: p.label }))}
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
            <DatePicker format="DD/MM/YYYY" placeholder="Chọn ngày" style={{ width: '100%' }} />
          </Form.Item>

          <Form.Item
            label="Thời gian thực hiện (ngày)"
            name="executionDays"
            rules={[
              { required: true, message: 'Vui lòng nhập số ngày thực hiện' },
              { type: 'number', min: 1, message: 'Phải ít nhất 1 ngày' },
            ]}
          >
            <InputNumber
              min={1}
              max={365}
              style={{ width: '100%' }}
              placeholder="Ví dụ: 7"
            />
          </Form.Item>
        </div>

        <Form.Item shouldUpdate noStyle>
          {() => {
            const startDate = form.getFieldValue('startDate') as Dayjs | undefined;
            const execDays = form.getFieldValue('executionDays') as number | undefined;
            if (!startDate || !execDays || execDays <= 0) return null;
            const endDate = startDate.add(execDays, 'day');
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

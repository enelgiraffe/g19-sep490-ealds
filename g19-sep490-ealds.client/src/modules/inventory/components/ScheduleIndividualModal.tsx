import { useEffect, useState } from 'react';
import { Modal, Form, Input, Select, DatePicker, Button, message } from 'antd';
import type { Dayjs } from 'dayjs';
import { useAppStore } from '../../../stores/appStore';
import {
  inventoryService,
  getCurrentUserId,
  type DropdownItem,
} from '../services/inventoryService';
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
  const isDeptHead = useAppStore((s) => s.currentRole) === 'department_head';
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
      if (isDeptHead && deps.length === 1) {
        form.setFieldsValue({ departmentId: deps[0].id });
      }
    } catch {
      message.error('Không thể tải dữ liệu. Vui lòng thử lại.');
    } finally {
      setLoadingMeta(false);
    }
  };

  const handleSubmit = () => {
    form.validateFields().then(async (values) => {
      const departmentId = isDeptHead ? departments[0]?.id : values.departmentId;
      if (departmentId == null) {
        message.error('Không xác định được phòng ban.');
        return;
      }
      setSubmitting(true);
      try {
        const checkDate = values.checkDate.toISOString();
        await inventoryService.createSession({
          purpose: values.purpose ?? '',
          startDate: checkDate,
          endDate: checkDate,
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

  return (
    <Modal
      open={open}
      onCancel={onClose}
      footer={null}
      width={480}
      centered
      className="schedule-individual-modal"
      closeIcon={<span className="schedule-modal__close">×</span>}
    >
      <div className="schedule-modal__header">
        <h2 className="schedule-modal__title">Lập lịch kiểm kê</h2>
        <p className="schedule-modal__subtitle">Kiểm kê toàn bộ tài sản của phòng ban</p>
      </div>

      <Form form={form} layout="vertical" className="schedule-individual-form">
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

        <Form.Item label="Mục đích" name="purpose">
          <TextArea rows={3} placeholder="Ví dụ: Kiểm kê tài sản định kỳ tháng 3" className="schedule-form__textarea" />
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

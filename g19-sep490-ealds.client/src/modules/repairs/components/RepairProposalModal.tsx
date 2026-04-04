import { useEffect } from 'react';
import { Form, Input, Modal } from 'antd';

export interface RepairProposalFormValues {
  reason: string;
  repairKind: string;
}

export interface RepairProposalModalProps {
  open: boolean;
  loading: boolean;
  assetCode: string;
  assetName: string;
  onClose: () => void;
  onSubmit: (values: RepairProposalFormValues) => void | Promise<void>;
}

export function RepairProposalModal({
  open,
  loading,
  assetCode,
  assetName,
  onClose,
  onSubmit,
}: RepairProposalModalProps) {
  const [form] = Form.useForm<RepairProposalFormValues>();

  useEffect(() => {
    if (!open) form.resetFields();
  }, [open, form]);

  return (
    <Modal
      title="Tạo đơn sửa chữa"
      open={open}
      onCancel={onClose}
      okText="Gửi phê duyệt"
      cancelText="Hủy"
      confirmLoading={loading}
      onOk={() => form.submit()}
    >
      <p style={{ marginBottom: 16, color: 'var(--color-text-secondary, #666)' }}>
        <strong>{assetCode || '—'}</strong>
        {assetName ? ` — ${assetName}` : null}
      </p>
      <Form form={form} layout="vertical" onFinish={(v) => onSubmit(v)}>
        <Form.Item
          name="reason"
          label="Lý do hỏng"
          rules={[{ required: true, message: 'Vui lòng nhập lý do hỏng.' }]}
        >
          <Input.TextArea rows={3} placeholder="Mô tả nguyên nhân / tình trạng hỏng" />
        </Form.Item>
        <Form.Item
          name="repairKind"
          label="Hình thức / nội dung sửa chữa đề xuất"
          rules={[{ required: true, message: 'Vui lòng mô tả hình thức sửa chữa.' }]}
        >
          <Input.TextArea
            rows={3}
            placeholder="Ví dụ: thay linh kiện, sửa chữa nội bộ, gửi bảo hành nhà cung cấp…"
          />
        </Form.Item>
      </Form>
    </Modal>
  );
}

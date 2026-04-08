import { useEffect } from 'react';
import { Form, Input, Modal } from 'antd';

export interface RepairProposalFormValues {
  damageCondition: string;
  repairKind: string;
}

export interface RepairProposalAssetItem {
  assetCode: string;
  assetName: string;
}

export interface RepairProposalModalProps {
  open: boolean;
  loading: boolean;
  items: RepairProposalAssetItem[];
  onClose: () => void;
  onSubmit: (values: RepairProposalFormValues) => void | Promise<void>;
}

export function RepairProposalModal({
  open,
  loading,
  items,
  onClose,
  onSubmit,
}: RepairProposalModalProps) {
  const [form] = Form.useForm<RepairProposalFormValues>();

  useEffect(() => {
    if (!open) form.resetFields();
  }, [open, form]);

  const count = items.length;

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
      {count > 1 ? (
        <ul
          style={{
            margin: '0 0 16px',
            padding: '8px 12px',
            maxHeight: 160,
            overflowY: 'auto',
            listStyle: 'none',
            background: 'var(--color-bg-secondary, #f9fafb)',
            border: '1px solid var(--color-border, #e5e7eb)',
            borderRadius: 8,
            fontSize: 13,
          }}
        >
          {items.map((it, idx) => (
            <li key={`${it.assetCode}-${idx}`} style={{ padding: '4px 0' }}>
              <strong>{it.assetCode || '—'}</strong>
              {it.assetName ? ` — ${it.assetName}` : null}
            </li>
          ))}
        </ul>
      ) : null}
      <Form form={form} layout="vertical" onFinish={(v) => onSubmit(v)}>
        <Form.Item
          name="damageCondition"
          label="Tình trạng hỏng hóc"
          rules={[{ required: true, message: 'Vui lòng nhập tình trạng hỏng hóc.' }]}
        >
          <Input.TextArea rows={3} placeholder="Mô tả chi tiết tình trạng hỏng hóc" />
        </Form.Item>
        <Form.Item
          name="repairKind"
          label="Phương án sửa chữa đề xuất"
          rules={[{ required: true, message: 'Vui lòng mô tả phương án sửa chữa.' }]}
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

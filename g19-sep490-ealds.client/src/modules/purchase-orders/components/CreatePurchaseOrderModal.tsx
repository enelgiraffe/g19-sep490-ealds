import { useEffect } from 'react';
import { Modal, Form, Input, Button, Table, InputNumber, Select } from 'antd';
import { PlusOutlined, DeleteOutlined } from '@ant-design/icons';
import './CreatePurchaseOrderModal.css';

const { TextArea } = Input;
const { Option } = Select;

export interface CreatePurchaseFormValues {
  title: string;
  description?: string;
  reason?: string;
  needDate?: string;
  supplier?: string;
  assetType?: string;
  purpose?: string;
  equipment?: {
    name?: string;
    quantity?: number;
    machineCode?: string;
    unit?: string;
    estimatedPrice?: string;
  }[];
}

interface CreatePurchaseOrderModalProps {
  open: boolean;
  onClose: () => void;
  onSubmit: (values: { title: string; description?: string; proposedData?: string }) => void;
  creatorName?: string | null;
}

export function CreatePurchaseOrderModal({
  open,
  onClose,
  onSubmit,
  creatorName,
}: CreatePurchaseOrderModalProps) {
  const [form] = Form.useForm<CreatePurchaseFormValues>();

  useEffect(() => {
    if (open) {
      form.resetFields();
    }
  }, [open, form]);

  const buildProposedData = (values: CreatePurchaseFormValues): string | undefined => {
    const equipment = values.equipment?.filter(
      (e) => e?.name != null && String(e.name).trim() !== ''
    );
    if (!equipment?.length) return undefined;
    let total = 0;
    const rows = equipment.map((e) => {
      const q = Number(e.quantity) || 1;
      const price = parseFloat(String(e.estimatedPrice || '0').replace(/[^\d.-]/g, '')) || 0;
      const rowTotal = q * price;
      total += rowTotal;
      return {
        name: e.name,
        quantity: q,
        machineCode: e.machineCode ?? '',
        unit: e.unit ?? 'Cái',
        estimatedPrice: e.estimatedPrice ?? '0',
      };
    });
    return JSON.stringify({
      equipment: rows,
      totalPrice: total.toLocaleString('vi-VN') + 'đ',
    });
  };

  const handleSubmit = () => {
    form.validateFields().then((values) => {
      const title = values.title?.trim();
      if (!title) {
        form.setFields([{ name: 'title', errors: ['Vui lòng nhập tiêu đề'] }]);
        return;
      }
      const description = [
        values.reason && `Lý do: ${values.reason}`,
        values.needDate && `Thời gian cần: ${values.needDate}`,
        values.supplier && `Nhà cung cấp đề xuất: ${values.supplier}`,
        values.assetType && `Loại tài sản: ${values.assetType}`,
        values.purpose && `Mục đích: ${values.purpose}`,
      ]
        .filter(Boolean)
        .join('\n');
      const proposedData = buildProposedData(values);
      onSubmit({
        title,
        description: description || undefined,
        proposedData: proposedData || undefined,
      });
      form.resetFields();
      onClose();
    });
  };

  return (
    <Modal
      open={open}
      onCancel={onClose}
      footer={null}
      width={900}
      className="create-purchase-modal"
      closeIcon={<span className="create-purchase-modal__close">×</span>}
    >
      <div className="create-purchase-modal__header">
        <h2 className="create-purchase-modal__title">Tạo yêu cầu mua vật tư</h2>
      </div>

      <Form
        form={form}
        layout="vertical"
        className="create-purchase-form"
        initialValues={{
          equipment: [{ name: '', quantity: 1, machineCode: '', unit: 'Cái', estimatedPrice: '' }],
        }}
      >
        {creatorName != null && (
          <div className="create-purchase-form__row">
            <Form.Item label="Người gửi" className="create-purchase-form__item">
              <Input value={creatorName} disabled />
            </Form.Item>
          </div>
        )}

        <div className="create-purchase-form__row">
          <Form.Item
            label="Tiêu đề"
            name="title"
            className="create-purchase-form__item"
            rules={[{ required: true, message: 'Vui lòng nhập tiêu đề' }]}
          >
            <Input placeholder="VD: Yêu cầu mua máy cắt sắt phục vụ sản xuất" />
          </Form.Item>
        </div>

        <div className="create-purchase-form__row">
          <Form.Item
            label="Lý do đề nghị"
            name="reason"
            className="create-purchase-form__item"
          >
            <Input placeholder="Nhập lý do" />
          </Form.Item>
          <Form.Item
            label="Thời gian cần vật tư"
            name="needDate"
            className="create-purchase-form__item"
          >
            <Input placeholder="VD: 20/02/2026" />
          </Form.Item>
        </div>

        <div className="create-purchase-form__row">
          <Form.Item
            label="Nhà cung cấp đề xuất"
            name="supplier"
            className="create-purchase-form__item"
          >
            <Input placeholder="Nhập nhà cung cấp" />
          </Form.Item>
          <Form.Item
            label="Loại tài sản"
            name="assetType"
            className="create-purchase-form__item"
          >
            <Select placeholder="Chọn loại tài sản" allowClear>
              <Option value="Máy móc">Máy móc</Option>
              <Option value="Thiết bị">Thiết bị</Option>
              <Option value="Công cụ">Công cụ</Option>
              <Option value="Vật tư">Vật tư</Option>
            </Select>
          </Form.Item>
        </div>

        <div className="create-purchase-form__section">
          <h3 className="create-purchase-form__section-title">Danh mục vật tư</h3>
          <Form.List name="equipment">
            {(fields, { add, remove }) => (
              <>
                <Table
                  dataSource={fields}
                  pagination={false}
                  rowKey="key"
                  className="create-purchase-equipment-table"
                  columns={[
                    {
                      title: 'STT',
                      key: 'stt',
                      width: 60,
                      align: 'center',
                      render: (_, __, i) => i + 1,
                    },
                    {
                      title: 'Tên vật tư',
                      key: 'name',
                      width: 180,
                      render: (_, __, i) => (
                        <Form.Item name={[i, 'name']} noStyle>
                          <Input placeholder="Nhập tên vật tư" />
                        </Form.Item>
                      ),
                    },
                    {
                      title: 'Số lượng',
                      key: 'quantity',
                      width: 90,
                      render: (_, __, i) => (
                        <Form.Item name={[i, 'quantity']} noStyle initialValue={1}>
                          <InputNumber min={1} style={{ width: '100%' }} />
                        </Form.Item>
                      ),
                    },
                    {
                      title: 'Mã máy',
                      key: 'machineCode',
                      width: 110,
                      render: (_, __, i) => (
                        <Form.Item name={[i, 'machineCode']} noStyle>
                          <Input placeholder="Mã máy" />
                        </Form.Item>
                      ),
                    },
                    {
                      title: 'Đơn vị',
                      key: 'unit',
                      width: 100,
                      render: (_, __, i) => (
                        <Form.Item name={[i, 'unit']} noStyle initialValue="Cái">
                          <Select style={{ width: '100%' }}>
                            <Option value="Cái">Cái</Option>
                            <Option value="Chiếc">Chiếc</Option>
                            <Option value="Bộ">Bộ</Option>
                            <Option value="Kg">Kg</Option>
                          </Select>
                        </Form.Item>
                      ),
                    },
                    {
                      title: 'Đơn giá dự tính',
                      key: 'estimatedPrice',
                      width: 140,
                      render: (_, __, i) => (
                        <Form.Item name={[i, 'estimatedPrice']} noStyle>
                          <Input placeholder="VD: 1000000" />
                        </Form.Item>
                      ),
                    },
                    {
                      title: '',
                      key: 'action',
                      width: 50,
                      render: (_, __, i) =>
                        fields.length > 1 ? (
                          <Button
                            type="text"
                            icon={<DeleteOutlined />}
                            danger
                            onClick={() => remove(i)}
                          />
                        ) : null,
                    },
                  ]}
                />
                <Button
                  type="dashed"
                  icon={<PlusOutlined />}
                  onClick={() => add({ name: '', quantity: 1, machineCode: '', unit: 'Cái', estimatedPrice: '' })}
                  className="create-purchase-btn-add-equipment"
                >
                  Thêm vật tư
                </Button>
              </>
            )}
          </Form.List>
        </div>

        <div className="create-purchase-form__section">
          <Form.Item
            label="Mục đích sử dụng"
            name="purpose"
            className="create-purchase-form__item-full"
          >
            <TextArea rows={3} placeholder="Mô tả mục đích sử dụng vật tư" />
          </Form.Item>
        </div>

        <div className="create-purchase-modal__footer">
          <Button
            type="primary"
            icon={<span>✉</span>}
            onClick={handleSubmit}
            className="create-purchase-btn-submit"
          >
            Gửi yêu cầu
          </Button>
          <Button icon={<span>✏</span>} onClick={onClose} className="create-purchase-btn-draft">
            Hủy
          </Button>
        </div>
      </Form>
    </Modal>
  );
}

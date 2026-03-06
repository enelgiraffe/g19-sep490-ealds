import { Modal, Form, Input, DatePicker, Select, Button, Table, InputNumber } from 'antd';
import { PlusOutlined, DeleteOutlined, EditOutlined, CloudUploadOutlined } from '@ant-design/icons';
import type { ColumnsType } from 'antd/es/table';
import './CreatePurchaseOrderModal.css';

const { TextArea } = Input;
const { Option } = Select;

interface Equipment {
  key: string;
  stt: number;
  name: string;
  quantity: number;
  machineCode: string;
  unit: string;
  estimatedPrice: string;
}

interface CreatePurchaseOrderModalProps {
  open: boolean;
  onClose: () => void;
  onSubmit: (values: any) => void;
}

export function CreatePurchaseOrderModal({ open, onClose, onSubmit }: CreatePurchaseOrderModalProps) {
  const [form] = Form.useForm();

  const equipmentData: Equipment[] = [
    {
      key: '1',
      stt: 1,
      name: 'Máy cắt sắt',
      quantity: 1,
      machineCode: 'EGHB',
      unit: 'Cái',
      estimatedPrice: '1,000,000,000đ',
    },
  ];

  const equipmentColumns: ColumnsType<Equipment> = [
    {
      title: 'STT',
      dataIndex: 'stt',
      key: 'stt',
      width: 60,
      align: 'center',
    },
    {
      title: 'Tên vật tư',
      dataIndex: 'name',
      key: 'name',
      width: 150,
      render: (text) => <Input defaultValue={text} placeholder="Nhập tên vật tư" />,
    },
    {
      title: 'Số lượng',
      dataIndex: 'quantity',
      key: 'quantity',
      width: 100,
      render: (text) => <InputNumber defaultValue={text} min={1} style={{ width: '100%' }} />,
    },
    {
      title: 'Mã máy',
      dataIndex: 'machineCode',
      key: 'machineCode',
      width: 120,
      render: (text) => <Input defaultValue={text} placeholder="Nhập mã máy" />,
    },
    {
      title: 'Đơn vị tính',
      dataIndex: 'unit',
      key: 'unit',
      width: 120,
      render: (text) => (
        <Select defaultValue={text} style={{ width: '100%' }}>
          <Option value="Cái">Cái</Option>
          <Option value="Chiếc">Chiếc</Option>
          <Option value="Bộ">Bộ</Option>
          <Option value="Kg">Kg</Option>
        </Select>
      ),
    },
    {
      title: 'Đơn giá dự tính',
      dataIndex: 'estimatedPrice',
      key: 'estimatedPrice',
      width: 150,
      render: (text) => <Input defaultValue={text} placeholder="Nhập giá" />,
    },
  ];

  const handleSubmit = () => {
    form.validateFields().then((values) => {
      onSubmit(values);
      form.resetFields();
      onClose();
    });
  };

  const handleAddEquipment = () => {
    console.log('Add equipment');
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
          sender: 'Nguyễn Văn A',
          department: 'Phòng sản xuất',
          reason: 'Phục vụ sản xuất',
          supplier: 'Phục vụ sản xuất',
          assetType: 'Máy móc',
        }}
      >
        <div className="create-purchase-form__row">
          <Form.Item
            label="Người gửi"
            name="sender"
            className="create-purchase-form__item"
          >
            <Input placeholder="Nhập tên người gửi" disabled />
          </Form.Item>
          <Form.Item
            label="Phòng ban"
            name="department"
            className="create-purchase-form__item"
          >
            <Input placeholder="Nhập phòng ban" disabled />
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
            <DatePicker
              style={{ width: '100%' }}
              placeholder="dd/mm/yyyy"
              format="DD/MM/YYYY"
            />
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
            <Select placeholder="Chọn loại tài sản">
              <Option value="Máy móc">Máy móc</Option>
              <Option value="Thiết bị">Thiết bị</Option>
              <Option value="Công cụ">Công cụ</Option>
              <Option value="Vật tư">Vật tư</Option>
            </Select>
          </Form.Item>
        </div>

        <div className="create-purchase-form__section">
          <h3 className="create-purchase-form__section-title">Danh mục vật tư</h3>
          <Table
            columns={equipmentColumns}
            dataSource={equipmentData}
            pagination={false}
            className="create-purchase-equipment-table"
            summary={() => (
              <Table.Summary>
                <Table.Summary.Row>
                  <Table.Summary.Cell index={0} colSpan={5}>
                    <span className="create-purchase-equipment-total">Thành tiền</span>
                  </Table.Summary.Cell>
                  <Table.Summary.Cell index={1}>
                    <span className="create-purchase-equipment-total-value">1,000,000,000đ</span>
                  </Table.Summary.Cell>
                </Table.Summary.Row>
              </Table.Summary>
            )}
          />
          <Button
            type="dashed"
            icon={<PlusOutlined />}
            onClick={handleAddEquipment}
            className="create-purchase-btn-add-equipment"
          >
            Thêm vật tư
          </Button>
        </div>

        <div className="create-purchase-form__section">
          <Form.Item
            label="Mục đích sử dụng"
            name="purpose"
            className="create-purchase-form__item-full"
          >
            <TextArea
              rows={3}
              placeholder="*Mô tả chi tiết mục đích sử dụng vật tư"
            />
          </Form.Item>
        </div>

        <div className="create-purchase-form__section">
          <h3 className="create-purchase-form__section-title">Tài liệu đính kèm</h3>
          <div className="create-purchase-attachments">
            <div className="create-purchase-attachment-item">
              <span>#1</span>
              <span>Thông tin máy</span>
              <div className="create-purchase-attachment-actions">
                <Button type="text" icon={<EditOutlined />} size="small" />
                <Button type="text" icon={<DeleteOutlined />} size="small" danger />
              </div>
            </div>
            <div className="create-purchase-attachment-item">
              <span>#2</span>
              <span>Thông tin nhà cung cấp</span>
              <div className="create-purchase-attachment-actions">
                <Button type="text" icon={<EditOutlined />} size="small" />
                <Button type="text" icon={<DeleteOutlined />} size="small" danger />
              </div>
            </div>
          </div>
          <Button
            icon={<CloudUploadOutlined />}
            className="create-purchase-btn-upload"
          >
            Thêm file
          </Button>
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
          <Button
            icon={<span>✏</span>}
            onClick={onClose}
            className="create-purchase-btn-draft"
          >
            Nhập
          </Button>
        </div>
      </Form>
    </Modal>
  );
}

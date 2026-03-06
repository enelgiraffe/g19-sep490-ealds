import { Modal, Form, Input, DatePicker, Button } from 'antd';
import { CloudUploadOutlined, EditOutlined, DeleteOutlined } from '@ant-design/icons';
import dayjs from 'dayjs';
import './LiquidationRequestModal.css';

const { TextArea } = Input;

interface AssetInfo {
  code: string;
  name: string;
  type: string;
  specification: string;
  purchaseDate: string;
  warrantyExpiry: string;
  currentValue: string;
  remainingValue: string;
  location: string;
  status: string;
  admissionDate: string;
  department: string;
}

interface LiquidationRequestModalProps {
  open: boolean;
  onClose: () => void;
  onSubmit: (values: unknown) => void;
  assetInfo: AssetInfo | null;
}

export function LiquidationRequestModal({ open, onClose, onSubmit, assetInfo }: LiquidationRequestModalProps) {
  const [form] = Form.useForm();

  if (!assetInfo) return null;

  const handleSubmit = () => {
    form.validateFields().then((values) => {
      onSubmit(values);
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
      className="liquidation-modal"
      closeIcon={<span className="liquidation-modal__close">×</span>}
    >
      <div className="liquidation-modal__header">
        <h2 className="liquidation-modal__title">Đơn đề nghị thanh lý</h2>
      </div>

      <div className="liquidation-modal__content">
        <Form.Item label="Số biên bản" className="liquidation-form__item">
          <Input defaultValue="-" disabled className="liquidation-input--disabled" />
        </Form.Item>

        <div className="liquidation-info-section">
          <h3 className="liquidation-section-title">Thông tin tài sản</h3>
          
          <div className="liquidation-info-grid">
            <div className="liquidation-info-row">
              <div className="liquidation-info-item">
                <label>Mã tài sản</label>
                <div className="liquidation-info-value">{assetInfo.code}</div>
              </div>
              <div className="liquidation-info-item">
                <label>Giá trị tài sản</label>
                <div className="liquidation-info-value">{assetInfo.currentValue}</div>
              </div>
            </div>

            <div className="liquidation-info-row">
              <div className="liquidation-info-item">
                <label>Tên tài sản</label>
                <div className="liquidation-info-value">{assetInfo.name}</div>
              </div>
              <div className="liquidation-info-item">
                <label>Giá trị còn lại</label>
                <div className="liquidation-info-value">{assetInfo.remainingValue}</div>
              </div>
            </div>

            <div className="liquidation-info-row">
              <div className="liquidation-info-item">
                <label>Loại tài sản</label>
                <div className="liquidation-info-value">{assetInfo.type}</div>
              </div>
              <div className="liquidation-info-item">
                <label>Vị trí tài sản</label>
                <div className="liquidation-info-value">{assetInfo.location}</div>
              </div>
            </div>

            <div className="liquidation-info-row">
              <div className="liquidation-info-item">
                <label>Quy cách tài sản</label>
                <div className="liquidation-info-value">{assetInfo.specification}</div>
              </div>
              <div className="liquidation-info-item">
                <label>Tình trạng</label>
                <div className="liquidation-info-value">{assetInfo.status === 'Đang sử dụng' ? 'Đang hỏng' : assetInfo.status}</div>
              </div>
            </div>

            <div className="liquidation-info-row">
              <div className="liquidation-info-item">
                <label>Ngày mua</label>
                <div className="liquidation-info-value">{assetInfo.purchaseDate}</div>
              </div>
              <div className="liquidation-info-item">
                <label>Ngày đưa vào SD</label>
                <div className="liquidation-info-value">{assetInfo.admissionDate}</div>
              </div>
            </div>

            <div className="liquidation-info-row">
              <div className="liquidation-info-item">
                <label>Hạn bảo hành</label>
                <div className="liquidation-info-value">{assetInfo.warrantyExpiry}</div>
              </div>
              <div className="liquidation-info-item">
                <label>Phòng ban SD</label>
                <div className="liquidation-info-value">{assetInfo.department}</div>
              </div>
            </div>
          </div>
        </div>

        <div className="liquidation-form-section">
          <h3 className="liquidation-section-title">Thông tin đề nghị thanh lý</h3>
          
          <Form
            form={form}
            layout="vertical"
            initialValues={{
              liquidationDate: dayjs(),
            }}
          >
            <div className="liquidation-form-row">
              <Form.Item
                label="Ngày đề nghị thanh lý"
                name="liquidationDate"
                rules={[{ required: true, message: 'Vui lòng chọn ngày' }]}
                className="liquidation-form-col"
              >
                <DatePicker
                  style={{ width: '100%' }}
                  format="DD/MM/YYYY"
                  placeholder="dd/mm/yyyy"
                />
              </Form.Item>

              <Form.Item
                label="Lý do thanh lý"
                name="reason"
                className="liquidation-form-col"
              >
                <TextArea
                  rows={1}
                  placeholder="-"
                />
              </Form.Item>
            </div>

            <div className="liquidation-form-row">
              <Form.Item
                label="Phương án xử lý"
                name="disposalMethod"
                className="liquidation-form-col"
              >
                <Input placeholder="-" />
              </Form.Item>

              <Form.Item
                label="Ghi chú"
                name="notes"
                className="liquidation-form-col"
              >
                <Input placeholder="-" />
              </Form.Item>
            </div>

            <div className="liquidation-attachments-section">
              <h4 className="liquidation-attachments-title">Tài liệu đính kèm</h4>
              <div className="liquidation-attachments-list">
                <div className="liquidation-attachment-item">
                  <span className="liquidation-attachment-number">#1</span>
                  <span className="liquidation-attachment-name">Thông tin máy</span>
                  <div className="liquidation-attachment-actions">
                    <Button type="text" icon={<EditOutlined />} size="small" />
                    <Button type="text" icon={<DeleteOutlined />} size="small" danger />
                  </div>
                </div>
                <div className="liquidation-attachment-item">
                  <span className="liquidation-attachment-number">#2</span>
                  <span className="liquidation-attachment-name">Thông tin nhà cung cấp</span>
                  <div className="liquidation-attachment-actions">
                    <Button type="text" icon={<EditOutlined />} size="small" />
                    <Button type="text" icon={<DeleteOutlined />} size="small" danger />
                  </div>
                </div>
              </div>
              <Button
                icon={<CloudUploadOutlined />}
                className="liquidation-btn-upload"
              >
                Thêm file đính kèm
              </Button>
            </div>
          </Form>
        </div>
      </div>

      <div className="liquidation-modal__footer">
        <Button
          type="primary"
          onClick={handleSubmit}
          className="liquidation-btn-submit"
        >
          Gửi yêu cầu
        </Button>
        <Button
          onClick={onClose}
          className="liquidation-btn-draft"
        >
          Nhập
        </Button>
      </div>
    </Modal>
  );
}

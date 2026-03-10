import { Modal, Form, Input, DatePicker, Button } from 'antd';
import { CloudUploadOutlined, EditOutlined, DeleteOutlined } from '@ant-design/icons';
import dayjs from 'dayjs';
import './MarkDamagedAssetModal.css';

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

interface MarkDamagedAssetModalProps {
  open: boolean;
  onClose: () => void;
  onSubmit: (values: any) => void;
  assetInfo: AssetInfo | null;
}

export function MarkDamagedAssetModal({ open, onClose, onSubmit, assetInfo }: MarkDamagedAssetModalProps) {
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
      centered
      className="mark-damaged-modal"
      closeIcon={<span className="mark-damaged-modal__close">×</span>}
    >
      <div className="mark-damaged-modal__header">
        <h2 className="mark-damaged-modal__title">Đánh dấu hỏng tài sản</h2>
      </div>

      <div className="mark-damaged-modal__content">
        <Form.Item label="Số biên bản" className="mark-damaged-form__item">
          <Input defaultValue="-" disabled className="mark-damaged-input--disabled" />
        </Form.Item>

        <div className="mark-damaged-info-section">
          <h3 className="mark-damaged-section-title">Thông tin tài sản</h3>
          
          <div className="mark-damaged-info-grid">
            <div className="mark-damaged-info-row">
              <div className="mark-damaged-info-item">
                <label>Mã tài sản</label>
                <div className="mark-damaged-info-value">{assetInfo.code}</div>
              </div>
              <div className="mark-damaged-info-item">
                <label>Giá trị tài sản</label>
                <div className="mark-damaged-info-value">{assetInfo.currentValue}</div>
              </div>
            </div>

            <div className="mark-damaged-info-row">
              <div className="mark-damaged-info-item">
                <label>Tên tài sản</label>
                <div className="mark-damaged-info-value">{assetInfo.name}</div>
              </div>
              <div className="mark-damaged-info-item">
                <label>Giá trị còn lại</label>
                <div className="mark-damaged-info-value">{assetInfo.remainingValue}</div>
              </div>
            </div>

            <div className="mark-damaged-info-row">
              <div className="mark-damaged-info-item">
                <label>Loại tài sản</label>
                <div className="mark-damaged-info-value">{assetInfo.type}</div>
              </div>
              <div className="mark-damaged-info-item">
                <label>Vị trí tài sản</label>
                <div className="mark-damaged-info-value">{assetInfo.location}</div>
              </div>
            </div>

            <div className="mark-damaged-info-row">
              <div className="mark-damaged-info-item">
                <label>Quy cách tài sản</label>
                <div className="mark-damaged-info-value">{assetInfo.specification}</div>
              </div>
              <div className="mark-damaged-info-item">
                <label>Tình trạng</label>
                <div className="mark-damaged-info-value">{assetInfo.status}</div>
              </div>
            </div>

            <div className="mark-damaged-info-row">
              <div className="mark-damaged-info-item">
                <label>Ngày mua</label>
                <div className="mark-damaged-info-value">{assetInfo.purchaseDate}</div>
              </div>
              <div className="mark-damaged-info-item">
                <label>Ngày đưa vào SD</label>
                <div className="mark-damaged-info-value">{assetInfo.admissionDate}</div>
              </div>
            </div>

            <div className="mark-damaged-info-row">
              <div className="mark-damaged-info-item">
                <label>Hạn bảo hành</label>
                <div className="mark-damaged-info-value">{assetInfo.warrantyExpiry}</div>
              </div>
              <div className="mark-damaged-info-item">
                <label>Phòng ban SD</label>
                <div className="mark-damaged-info-value">{assetInfo.department}</div>
              </div>
            </div>
          </div>
        </div>

        <div className="mark-damaged-form-section">
          <h3 className="mark-damaged-section-title">Thông tin ghi nhận tài sản hỏng</h3>
          
          <Form
            form={form}
            layout="vertical"
            initialValues={{
              damageDate: dayjs(),
            }}
          >
            <Form.Item
              label="Ngày hỏng"
              name="damageDate"
              rules={[{ required: true, message: 'Vui lòng chọn ngày hỏng' }]}
            >
              <DatePicker
                style={{ width: '100%' }}
                format="DD/MM/YYYY"
                placeholder="dd/mm/yyyy"
              />
            </Form.Item>

            <Form.Item
              label="Tình trạng"
              name="condition"
            >
              <TextArea
                rows={4}
                placeholder="-"
              />
            </Form.Item>

            <div className="mark-damaged-attachments-section">
              <h4 className="mark-damaged-attachments-title">Tài liệu đính kèm</h4>
              <div className="mark-damaged-attachments-list">
                <div className="mark-damaged-attachment-item">
                  <span className="mark-damaged-attachment-number">#1</span>
                  <span className="mark-damaged-attachment-name">Thông tin máy</span>
                  <div className="mark-damaged-attachment-actions">
                    <Button type="text" icon={<EditOutlined />} size="small" />
                    <Button type="text" icon={<DeleteOutlined />} size="small" danger />
                  </div>
                </div>
                <div className="mark-damaged-attachment-item">
                  <span className="mark-damaged-attachment-number">#2</span>
                  <span className="mark-damaged-attachment-name">Thông tin nhà cung cấp</span>
                  <div className="mark-damaged-attachment-actions">
                    <Button type="text" icon={<EditOutlined />} size="small" />
                    <Button type="text" icon={<DeleteOutlined />} size="small" danger />
                  </div>
                </div>
              </div>
              <Button
                icon={<CloudUploadOutlined />}
                className="mark-damaged-btn-upload"
              >
                Thêm file đính kèm
              </Button>
            </div>
          </Form>
        </div>
      </div>

      <div className="mark-damaged-modal__footer">
        <Button
          type="primary"
          onClick={handleSubmit}
          className="mark-damaged-btn-submit"
        >
          Cập phát
        </Button>
        <Button
          onClick={onClose}
          className="mark-damaged-btn-draft"
        >
          Nháp
        </Button>
      </div>
    </Modal>
  );
}

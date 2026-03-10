import { useState, useEffect } from 'react';
import { Modal, Form, Input, Button } from 'antd';
import { CloudUploadOutlined, EditOutlined, DeleteOutlined } from '@ant-design/icons';
import './MaintenanceProposalModal.css';

const { TextArea } = Input;

export interface AssetInfo {
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

interface MaintenanceProposalModalProps {
  open: boolean;
  onClose: () => void;
  onSubmit: (values: {
    assetId: number;
    recordNumber?: string;
    maintenanceContent: string;
  }) => void;
  assetInfo: AssetInfo | null;
  assetId: number | null;
}

interface AttachmentItem {
  id: string;
  name: string;
}

export function MaintenanceProposalModal({
  open,
  onClose,
  onSubmit,
  assetInfo,
  assetId,
}: MaintenanceProposalModalProps) {
  const [form] = Form.useForm();

  useEffect(() => {
    if (open) form.resetFields();
  }, [open, form]);

  const [attachments, setAttachments] = useState<AttachmentItem[]>([
    { id: '1', name: 'Thông tin máy' },
    { id: '2', name: 'Thông tin nhà cung cấp' },
  ]);

  if (!assetInfo || assetId == null) return null;

  const handleSubmit = () => {
    form.validateFields().then((values) => {
      onSubmit({
        assetId,
        recordNumber: values.recordNumber,
        maintenanceContent: values.maintenanceContent ?? '',
      });
      form.resetFields();
      // Parent đóng modal sau khi gọi API thành công
    });
  };

  const handleAddAttachment = () => {
    setAttachments((prev) => [
      ...prev,
      { id: String(Date.now()), name: `File đính kèm #${prev.length + 1}` },
    ]);
  };

  const handleRemoveAttachment = (id: string) => {
    setAttachments((prev) => prev.filter((a) => a.id !== id));
  };

  return (
    <Modal
      open={open}
      onCancel={onClose}
      footer={null}
      width={900}
      className="maintenance-proposal-modal"
      closeIcon={<span className="maintenance-proposal-modal__close">×</span>}
    >
      <div className="maintenance-proposal-modal__header">
        <h2 className="maintenance-proposal-modal__title">
          Gửi đề xuất bảo dưỡng máy móc
        </h2>
      </div>

      <div className="maintenance-proposal-modal__content">
        <Form
          form={form}
          layout="vertical"
          initialValues={{ recordNumber: 'BA001', maintenanceContent: 'Hỏng nhẹ' }}
        >
          <Form.Item label="Số biên bản" name="recordNumber" className="maintenance-proposal-form__item">
            <Input placeholder="BA001" className="maintenance-proposal-input" />
          </Form.Item>

          <div className="maintenance-proposal-info-section">
            <h3 className="maintenance-proposal-section-title">Thông tin tài sản</h3>
            <div className="maintenance-proposal-info-grid">
              <div className="maintenance-proposal-info-row">
                <div className="maintenance-proposal-info-item">
                  <label>Mã tài sản</label>
                  <div className="maintenance-proposal-info-value">{assetInfo.code}</div>
                </div>
                <div className="maintenance-proposal-info-item">
                  <label>Giá trị tài sản</label>
                  <div className="maintenance-proposal-info-value">{assetInfo.currentValue}</div>
                </div>
              </div>
              <div className="maintenance-proposal-info-row">
                <div className="maintenance-proposal-info-item">
                  <label>Tên tài sản</label>
                  <div className="maintenance-proposal-info-value">{assetInfo.name}</div>
                </div>
                <div className="maintenance-proposal-info-item">
                  <label>Giá trị còn lại</label>
                  <div className="maintenance-proposal-info-value">{assetInfo.remainingValue}</div>
                </div>
              </div>
              <div className="maintenance-proposal-info-row">
                <div className="maintenance-proposal-info-item">
                  <label>Loại tài sản</label>
                  <div className="maintenance-proposal-info-value">{assetInfo.type}</div>
                </div>
                <div className="maintenance-proposal-info-item">
                  <label>Vị trí tài sản</label>
                  <div className="maintenance-proposal-info-value">{assetInfo.location}</div>
                </div>
              </div>
              <div className="maintenance-proposal-info-row">
                <div className="maintenance-proposal-info-item">
                  <label>Quy cách tài sản</label>
                  <div className="maintenance-proposal-info-value">{assetInfo.specification}</div>
                </div>
                <div className="maintenance-proposal-info-item">
                  <label>Tình trạng</label>
                  <div className="maintenance-proposal-info-value">{assetInfo.status}</div>
                </div>
              </div>
              <div className="maintenance-proposal-info-row">
                <div className="maintenance-proposal-info-item">
                  <label>Ngày mua</label>
                  <div className="maintenance-proposal-info-value">{assetInfo.purchaseDate}</div>
                </div>
                <div className="maintenance-proposal-info-item">
                  <label>Ngày đưa vào SD</label>
                  <div className="maintenance-proposal-info-value">{assetInfo.admissionDate}</div>
                </div>
              </div>
              <div className="maintenance-proposal-info-row">
                <div className="maintenance-proposal-info-item">
                  <label>Hạn bảo hành</label>
                  <div className="maintenance-proposal-info-value">{assetInfo.warrantyExpiry}</div>
                </div>
                <div className="maintenance-proposal-info-item">
                  <label>Phòng ban SD</label>
                  <div className="maintenance-proposal-info-value">{assetInfo.department}</div>
                </div>
              </div>
            </div>
          </div>

          <div className="maintenance-proposal-form-section">
            <h3 className="maintenance-proposal-section-title">Thông tin bảo dưỡng</h3>
            <Form.Item
              label="Nội dung bảo dưỡng"
              name="maintenanceContent"
              rules={[{ required: true, message: 'Vui lòng nhập nội dung bảo dưỡng' }]}
            >
              <TextArea rows={4} placeholder="Mô tả nội dung cần bảo dưỡng..." />
            </Form.Item>

            <div className="maintenance-proposal-attachments-section">
              <h4 className="maintenance-proposal-attachments-title">Tài liệu đính kèm</h4>
              <div className="maintenance-proposal-attachments-list">
                {attachments.map((att) => (
                  <div key={att.id} className="maintenance-proposal-attachment-item">
                    <span className="maintenance-proposal-attachment-number">
                      #{attachments.indexOf(att) + 1}
                    </span>
                    <span className="maintenance-proposal-attachment-name">{att.name}</span>
                    <div className="maintenance-proposal-attachment-actions">
                      <Button type="text" icon={<EditOutlined />} size="small" />
                      <Button
                        type="text"
                        icon={<DeleteOutlined />}
                        size="small"
                        danger
                        onClick={() => handleRemoveAttachment(att.id)}
                      />
                    </div>
                  </div>
                ))}
              </div>
              <Button
                icon={<CloudUploadOutlined />}
                className="maintenance-proposal-btn-upload"
                onClick={handleAddAttachment}
              >
                + Thêm file đính kèm
              </Button>
            </div>
          </div>
        </Form>
      </div>

      <div className="maintenance-proposal-modal__footer">
        <Button
          type="primary"
          onClick={handleSubmit}
          className="maintenance-proposal-btn-submit"
        >
          Gửi yêu cầu
        </Button>
        <Button onClick={onClose} className="maintenance-proposal-btn-cancel">
          Hủy
        </Button>
      </div>
    </Modal>
  );
}

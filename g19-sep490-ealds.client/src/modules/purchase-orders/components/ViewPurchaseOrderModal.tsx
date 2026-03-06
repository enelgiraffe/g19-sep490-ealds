import { Modal, Button, Tag } from 'antd';
import { CloseOutlined, DownloadOutlined } from '@ant-design/icons';
import './ViewPurchaseOrderModal.css';

interface PurchaseOrderDetail {
  sender: string;
  department: string;
  reason: string;
  needDate: string;
  supplier: string;
  assetType: string;
  equipment: {
    stt: number;
    name: string;
    quantity: number;
    machineCode: string;
    unit: string;
    estimatedPrice: string;
  }[];
  totalPrice: string;
  purpose: string;
  attachments: {
    id: number;
    name: string;
  }[];
  accountantNotes: string;
  directorNotes: string;
  status: 'approved' | 'pending' | 'rejected' | 'waiting';
}

interface ViewPurchaseOrderModalProps {
  open: boolean;
  onClose: () => void;
  data: PurchaseOrderDetail | null;
}

export function ViewPurchaseOrderModal({ open, onClose, data }: ViewPurchaseOrderModalProps) {
  if (!data) return null;

  const getStatusTag = () => {
    switch (data.status) {
      case 'approved':
        return <Tag color="success" className="view-purchase-status-tag">Duyệt</Tag>;
      case 'rejected':
        return <Tag color="warning" className="view-purchase-status-tag">Chờ ngân sách</Tag>;
      default:
        return null;
    }
  };

  return (
    <Modal
      open={open}
      onCancel={onClose}
      footer={null}
      width={700}
      className="view-purchase-modal"
      closeIcon={null}
    >
      <div className="view-purchase-modal__header">
        <div className="view-purchase-modal__header-left">
          <h2 className="view-purchase-modal__title">Chi tiết phản hồi</h2>
          {getStatusTag()}
        </div>
      </div>

      <div className="view-purchase-modal__content">
        <div className="view-purchase-form">
          <div className="view-purchase-form__row">
            <div className="view-purchase-form__field">
              <label>Người gửi</label>
              <div className="view-purchase-form__value">{data.sender}</div>
            </div>
            <div className="view-purchase-form__field">
              <label>Phòng ban</label>
              <div className="view-purchase-form__value">{data.department}</div>
            </div>
          </div>

          <div className="view-purchase-form__row">
            <div className="view-purchase-form__field">
              <label>Lý do đề nghị</label>
              <div className="view-purchase-form__value">{data.reason}</div>
            </div>
            <div className="view-purchase-form__field">
              <label>Thời gian cần vật tư</label>
              <div className="view-purchase-form__value">{data.needDate}</div>
            </div>
          </div>

          <div className="view-purchase-form__row">
            <div className="view-purchase-form__field">
              <label>Nhà cung cấp đề xuất</label>
              <div className="view-purchase-form__value">{data.supplier}</div>
            </div>
            <div className="view-purchase-form__field">
              <label>Loại tài sản</label>
              <div className="view-purchase-form__value">{data.assetType}</div>
            </div>
          </div>

          <div className="view-purchase-form__section">
            <h3 className="view-purchase-form__section-title">Danh mục vật tư</h3>
            <table className="view-purchase-equipment-table">
              <thead>
                <tr>
                  <th>STT</th>
                  <th>Tên vật tư</th>
                  <th>Số lượng</th>
                  <th>Mã máy</th>
                  <th>Đơn vị tính</th>
                  <th>Đơn giá dự tính</th>
                </tr>
              </thead>
              <tbody>
                {data.equipment.map((item) => (
                  <tr key={item.stt}>
                    <td>{item.stt}</td>
                    <td>{item.name}</td>
                    <td>{item.quantity}</td>
                    <td>{item.machineCode}</td>
                    <td>{item.unit}</td>
                    <td className="view-purchase-equipment-price">{item.estimatedPrice}</td>
                  </tr>
                ))}
                <tr className="view-purchase-equipment-total">
                  <td colSpan={5}>Thành tiền</td>
                  <td className="view-purchase-equipment-price">{data.totalPrice}</td>
                </tr>
              </tbody>
            </table>
          </div>

          <div className="view-purchase-form__section">
            <label>Mục đích sử dụng</label>
            <div className="view-purchase-form__value">{data.purpose}</div>
          </div>

          <div className="view-purchase-form__section">
            <h3 className="view-purchase-form__section-title">Tài liệu đính kèm</h3>
            <div className="view-purchase-attachments">
              {data.attachments.map((file) => (
                <div key={file.id} className="view-purchase-attachment-item">
                  <span className="view-purchase-attachment-number">#{file.id}</span>
                  <span className="view-purchase-attachment-name">{file.name}</span>
                  <Button
                    type="text"
                    icon={<DownloadOutlined />}
                    size="small"
                    className="view-purchase-attachment-download"
                  />
                </div>
              ))}
            </div>
          </div>

          <div className="view-purchase-form__section">
            <div className="view-purchase-feedback-box">
              <label>Nội dung phản hồi của kế toán</label>
              <div className="view-purchase-feedback-content">
                {data.accountantNotes || '-'}
              </div>
            </div>
          </div>

          <div className="view-purchase-form__section">
            <div className="view-purchase-feedback-box">
              <label>Nội dung phản hồi của giám đốc</label>
              <div className="view-purchase-feedback-content">
                {data.directorNotes}
              </div>
            </div>
          </div>
        </div>
      </div>

      <div className="view-purchase-modal__footer">
        <Button
          icon={<CloseOutlined />}
          onClick={onClose}
          className="view-purchase-btn-close"
        >
          Đóng
        </Button>
      </div>
    </Modal>
  );
}

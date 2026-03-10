import { Modal, Button, Tag } from 'antd';
import { CloseOutlined } from '@ant-design/icons';
import type { PurchaseOrderDetail } from '../services/purchaseOrderService';
import './ViewPurchaseOrderModal.css';

const STATUS_MAP: Record<number, { label: string; color: string }> = {
  0: { label: 'Nhập', color: 'default' },
  1: { label: 'Duyệt', color: 'success' },
  2: { label: 'Từ chối', color: 'error' },
  3: { label: 'Chờ ngân sách', color: 'warning' },
};

function formatDate(iso: string): string {
  try {
    return new Date(iso).toLocaleString('vi-VN');
  } catch {
    return iso;
  }
}

interface ViewPurchaseOrderModalProps {
  open: boolean;
  onClose: () => void;
  data: PurchaseOrderDetail | null;
}

export function ViewPurchaseOrderModal({ open, onClose, data }: ViewPurchaseOrderModalProps) {
  if (!data) return null;

  const statusConfig = STATUS_MAP[data.status] ?? STATUS_MAP[0];
  let equipment: { stt: number; name: string; quantity: number; machineCode?: string; unit?: string; estimatedPrice?: string }[] = [];
  let totalPrice = '—';
  try {
    if (data.proposedData) {
      const parsed = JSON.parse(data.proposedData) as {
        equipment?: { name?: string; quantity?: number; machineCode?: string; unit?: string; estimatedPrice?: string }[];
        totalPrice?: string;
      };
      if (Array.isArray(parsed.equipment)) {
        equipment = parsed.equipment.map((e, i) => ({
          stt: i + 1,
          name: e.name ?? '—',
          quantity: e.quantity ?? 1,
          machineCode: e.machineCode,
          unit: e.unit,
          estimatedPrice: e.estimatedPrice,
        }));
      }
      if (parsed.totalPrice) totalPrice = parsed.totalPrice;
    }
  } catch {
    // keep empty
  }

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
          <h2 className="view-purchase-modal__title">Chi tiết yêu cầu mua sắm</h2>
          <Tag color={statusConfig.color} className="view-purchase-status-tag">
            {statusConfig.label}
          </Tag>
        </div>
      </div>

      <div className="view-purchase-modal__content">
        <div className="view-purchase-form">
          <div className="view-purchase-form__row">
            <div className="view-purchase-form__field">
              <label>Mã yêu cầu</label>
              <div className="view-purchase-form__value">YC-{data.assetRequestId}</div>
            </div>
            <div className="view-purchase-form__field">
              <label>Ngày tạo</label>
              <div className="view-purchase-form__value">{formatDate(data.createDate)}</div>
            </div>
          </div>

          <div className="view-purchase-form__row">
            <div className="view-purchase-form__field">
              <label>Người tạo</label>
              <div className="view-purchase-form__value">{data.creatorName ?? data.createdBy}</div>
            </div>
          </div>

          <div className="view-purchase-form__section">
            <label>Tiêu đề</label>
            <div className="view-purchase-form__value">{data.title}</div>
          </div>

          {data.description && (
            <div className="view-purchase-form__section">
              <label>Mô tả</label>
              <div className="view-purchase-form__value">{data.description}</div>
            </div>
          )}

          {equipment.length > 0 && (
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
                  {equipment.map((item) => (
                    <tr key={item.stt}>
                      <td>{item.stt}</td>
                      <td>{item.name}</td>
                      <td>{item.quantity}</td>
                      <td>{item.machineCode ?? '—'}</td>
                      <td>{item.unit ?? '—'}</td>
                      <td className="view-purchase-equipment-price">{item.estimatedPrice ?? '—'}</td>
                    </tr>
                  ))}
                  <tr className="view-purchase-equipment-total">
                    <td colSpan={5}>Thành tiền</td>
                    <td className="view-purchase-equipment-price">{totalPrice}</td>
                  </tr>
                </tbody>
              </table>
            </div>
          )}

          {data.proposedData && equipment.length === 0 && (
            <div className="view-purchase-form__section">
              <label>Dữ liệu đề xuất</label>
              <pre className="view-purchase-form__value" style={{ whiteSpace: 'pre-wrap', wordBreak: 'break-all' }}>
                {data.proposedData}
              </pre>
            </div>
          )}
        </div>
      </div>

      <div className="view-purchase-modal__footer">
        <Button icon={<CloseOutlined />} onClick={onClose} className="view-purchase-btn-close">
          Đóng
        </Button>
      </div>
    </Modal>
  );
}

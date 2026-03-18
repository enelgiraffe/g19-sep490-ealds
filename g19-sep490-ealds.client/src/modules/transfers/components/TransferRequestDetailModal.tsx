import { Modal, Descriptions, Tag } from 'antd';
import type { TransferRequestListItem } from '../../assets/services/transferRequestService';

const STATUS_MAP: Record<
  number,
  {
    label: string;
    color: string;
  }
> = {
  0: { label: 'Nháp', color: 'default' },
  1: { label: 'Đã nộp', color: 'processing' },
  2: { label: 'Chờ phê duyệt', color: 'warning' },
  3: { label: 'Từ chối', color: 'error' },
  4: { label: 'Phê duyệt', color: 'success' },
};

interface TransferRequestDetailModalProps {
  open: boolean;
  onClose: () => void;
  request: TransferRequestListItem | null;
}

function formatDate(iso?: string | null): string {
  if (!iso) return '—';
  try {
    const d = new Date(iso);
    return d.toLocaleDateString('vi-VN');
  } catch {
    return iso ?? '—';
  }
}

export function TransferRequestDetailModal({
  open,
  onClose,
  request,
}: TransferRequestDetailModalProps) {
  const statusConfig =
    request && STATUS_MAP[request.status] ? STATUS_MAP[request.status] : undefined;

  return (
    <Modal
      open={open}
      onCancel={onClose}
      footer={null}
      width={700}
      title="Chi tiết yêu cầu điều chuyển"
      destroyOnClose
    >
      {!request ? (
        <p>Không tìm thấy dữ liệu yêu cầu điều chuyển.</p>
      ) : (
        <>
          <Descriptions
            bordered
            column={2}
            size="middle"
          >
            <Descriptions.Item label="Số biên bản">
              {request.code || '—'}
            </Descriptions.Item>
            <Descriptions.Item label="Ngày điều chuyển">
              {formatDate(request.transferDate)}
            </Descriptions.Item>
            <Descriptions.Item label="Trạng thái">
              {statusConfig ? (
                <Tag color={statusConfig.color}>{statusConfig.label}</Tag>
              ) : (
                request.statusName || '—'
              )}
            </Descriptions.Item>
            <Descriptions.Item label="Số lượng">
              {request.quantity}
            </Descriptions.Item>
          </Descriptions>

          <Descriptions
            bordered
            column={1}
            size="middle"
            style={{ marginTop: 16 }}
          >
            <Descriptions.Item label="Tài sản">
              {request.assetCode} - {request.assetName}
            </Descriptions.Item>
            <Descriptions.Item label="Điều chuyển từ">
              {request.fromDepartment}
            </Descriptions.Item>
            <Descriptions.Item label="Điều chuyển đến">
              {request.toDepartment}
            </Descriptions.Item>
            <Descriptions.Item label="Lý do điều chuyển">
              {request.reason || '—'}
            </Descriptions.Item>
          </Descriptions>
        </>
      )}
    </Modal>
  );
}


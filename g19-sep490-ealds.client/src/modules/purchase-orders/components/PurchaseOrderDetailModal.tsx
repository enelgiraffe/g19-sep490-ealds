import { Modal, Descriptions, Table, Button, Space, Tag, message } from 'antd';
import type { ColumnsType } from 'antd/es/table';
import type { PurchaseOrderDetail, PurchaseOrderLineItem } from '../services/procurementPoService';
import { PO_STATUS } from '../services/procurementPoService';

function statusLabel(status: number): { text: string; color: string } {
  if (status === PO_STATUS.cancelled) return { text: 'Đã hủy', color: 'error' };
  if (status === PO_STATUS.partiallyReceived) return { text: 'Nhận một phần', color: 'warning' };
  if (status === PO_STATUS.completed) return { text: 'Đã nhận đủ', color: 'success' };
  return { text: 'Đã tạo', color: 'processing' };
}

function formatMoney(n: number, currency: string): string {
  try {
    return `${n.toLocaleString('vi-VN')} ${currency}`;
  } catch {
    return `${n} ${currency}`;
  }
}

export interface PurchaseOrderDetailModalProps {
  open: boolean;
  data: PurchaseOrderDetail | null;
  onClose: () => void;
  onEdit: () => void;
  onCancelOrder: () => Promise<void>;
}

export function PurchaseOrderDetailModal({
  open,
  data,
  onClose,
  onEdit,
  onCancelOrder,
}: PurchaseOrderDetailModalProps) {
  const hasReceipt = data?.lines.some((l) => Number(l.receivedQuantity ?? 0) > 0) ?? false;
  const canEdit = data?.status === PO_STATUS.created && !hasReceipt;
  const canCancel =
    data != null &&
    data.status !== PO_STATUS.cancelled &&
    data.status !== PO_STATUS.completed &&
    !hasReceipt;

  const lineColumns: ColumnsType<PurchaseOrderLineItem> = [
    { title: '#', width: 48, render: (_, __, i) => i + 1 },
    { title: 'Mô tả', dataIndex: 'description', render: (v) => v || '—' },
    {
      title: 'Tài sản',
      render: (_, r) =>
        r.assetCode || r.assetName ? `${r.assetCode ?? ''} ${r.assetName ?? ''}`.trim() : '—',
    },
    {
      title: 'SL đặt',
      dataIndex: 'quantity',
      align: 'right',
      render: (v) => Number(v).toLocaleString('vi-VN'),
    },
    {
      title: 'Đã nhận',
      dataIndex: 'receivedQuantity',
      align: 'right',
      render: (v) => Number(v ?? 0).toLocaleString('vi-VN'),
    },
    {
      title: 'Còn lại',
      dataIndex: 'openQuantity',
      align: 'right',
      render: (v) => Number(v ?? 0).toLocaleString('vi-VN'),
    },
    { title: 'ĐVT', dataIndex: 'unit', render: (v) => v || '—' },
    {
      title: 'Đơn giá',
      dataIndex: 'unitPrice',
      align: 'right',
      render: (v) => Number(v).toLocaleString('vi-VN'),
    },
    {
      title: 'Ngày giao dự kiến',
      dataIndex: 'expectedDeliveryDate',
      render: (v: string | null) => (v ? new Date(v).toLocaleDateString('vi-VN') : '—'),
    },
    {
      title: 'Thành tiền',
      dataIndex: 'lineTotal',
      align: 'right',
      render: (v) => Number(v).toLocaleString('vi-VN'),
    },
  ];

  const handleCancel = async () => {
    Modal.confirm({
      title: 'Hủy đơn mua?',
      content: 'Trạng thái sẽ chuyển sang Đã hủy.',
      okText: 'Hủy đơn',
      okType: 'danger',
      cancelText: 'Đóng',
      onOk: async () => {
        try {
          await onCancelOrder();
          message.success('Đã hủy đơn mua.');
        } catch {
          message.error('Không hủy được đơn.');
        }
      },
    });
  };

  return (
    <Modal
      title={data ? `Đơn mua ${data.contractNo}` : 'Chi tiết đơn mua'}
      open={open}
      onCancel={onClose}
      width={900}
      footer={
        <Space>
          {canEdit && <Button onClick={onEdit}>Chỉnh sửa</Button>}
          {canCancel && (
            <Button danger onClick={handleCancel}>
              Hủy đơn
            </Button>
          )}
          <Button type="primary" onClick={onClose}>
            Đóng
          </Button>
        </Space>
      }
    >
      {data && (
        <>
          <Descriptions bordered size="small" column={2} style={{ marginBottom: 16 }}>
            <Descriptions.Item label="Mã đơn">{data.procurementId}</Descriptions.Item>
            <Descriptions.Item label="Số chứng từ">{data.contractNo}</Descriptions.Item>
            <Descriptions.Item label="Tiêu đề" span={2}>
              {data.title}
            </Descriptions.Item>
            <Descriptions.Item label="Nhà cung cấp">
              {data.supplierName ?? `ID ${data.supplierId}`}
            </Descriptions.Item>
            <Descriptions.Item label="Tiền tệ">{data.currency}</Descriptions.Item>
            <Descriptions.Item label="Tổng tiền">
              {formatMoney(Number(data.totalAmount), data.currency)}
            </Descriptions.Item>
            <Descriptions.Item label="Trạng thái">
              <Tag color={statusLabel(data.status).color}>{statusLabel(data.status).text}</Tag>
            </Descriptions.Item>
            <Descriptions.Item label="Ngày tạo">
              {new Date(data.createDate).toLocaleString('vi-VN')}
            </Descriptions.Item>
            {data.assetRequestId != null && (
              <Descriptions.Item label="Yêu cầu liên kết">#{data.assetRequestId}</Descriptions.Item>
            )}
          </Descriptions>
          <Table
            size="small"
            rowKey={(r) => String(r.lineId)}
            columns={lineColumns}
            dataSource={data.lines}
            pagination={false}
            scroll={{ x: 800 }}
          />
        </>
      )}
    </Modal>
  );
}

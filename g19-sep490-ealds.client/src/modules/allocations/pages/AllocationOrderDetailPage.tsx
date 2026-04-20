import { useCallback, useEffect, useState } from 'react';
import { useLocation, useNavigate, useParams } from 'react-router-dom';
import { Button, Descriptions, Spin, Table, Typography, message } from 'antd';
import type { ColumnsType } from 'antd/es/table';
import { ArrowLeftOutlined } from '@ant-design/icons';
import {
  allocationRequestService,
  type AllocationOrderDetail,
  type AllocationOrderLineDetail,
} from '../services/allocationRequestService';
import { handoverRequestService } from '../services/handoverRequestService';
import { useAppStore } from '../../../stores/appStore';
import './AccountantAllocationsPage.css';
import '../../requests/pages/RequestsPage.css';

const { Title, Text } = Typography;

/** Chỉ hiển thị phần ngày theo chuỗi ISO (tránh lệch múi giờ khi parse Date). */
function formatDateOnly(iso?: string | null): string {
  if (!iso) return '—';
  const m = /^(\d{4})-(\d{2})-(\d{2})/.exec(iso.trim());
  if (m) {
    const [, y, mo, da] = m;
    return `${da}/${mo}/${y}`;
  }
  return '—';
}

export function AllocationOrderDetailPage() {
  const { orderId } = useParams<{ orderId: string }>();
  const navigate = useNavigate();
  const { pathname } = useLocation();
  const currentRole = useAppStore((s) => s.currentRole);
  const isHandover = pathname.includes('handover-order');
  const [loading, setLoading] = useState(true);
  const [detail, setDetail] = useState<AllocationOrderDetail | null>(null);
  const [confirming, setConfirming] = useState(false);

  const idNum = orderId ? parseInt(orderId, 10) : NaN;

  const load = useCallback(async () => {
    if (!Number.isFinite(idNum)) {
      setLoading(false);
      return;
    }
    setLoading(true);
    try {
      const d = isHandover
        ? await handoverRequestService.getOrder(idNum)
        : await allocationRequestService.getOrder(idNum);
      setDetail(d);
    } catch {
      message.error(isHandover ? 'Không tải được đơn hoàn trả.' : 'Không tải được đơn cấp phát.');
      setDetail(null);
    } finally {
      setLoading(false);
    }
  }, [idNum, isHandover]);

  useEffect(() => {
    void load();
  }, [load]);

  const confirm = useCallback(async () => {
    if (!Number.isFinite(idNum)) return;
    setConfirming(true);
    try {
      if (isHandover) await handoverRequestService.confirmOrder(idNum);
      else await allocationRequestService.confirmOrder(idNum);
      message.success(
        isHandover ? 'Đã xác nhận hoàn trả tài sản về kho.' : 'Đã xác nhận và gán tài sản về phòng ban.',
      );
      await load();
    } catch {
      message.error('Xác nhận thất bại.');
    } finally {
      setConfirming(false);
    }
  }, [idNum, isHandover, load]);

  const lineColumns: ColumnsType<AllocationOrderLineDetail> = [
    { title: 'Loại', dataIndex: 'assetTypeName', width: 160, ellipsis: true },
    { title: 'Mã TS', dataIndex: 'assetCode', width: 110 },
    { title: 'Tên tài sản', dataIndex: 'assetName', ellipsis: true },
    { title: 'SL', dataIndex: 'quantity', width: 64 },
    { title: 'Lý do', dataIndex: 'reason', ellipsis: true, render: (t) => t || '—' },
  ];

  if (loading) {
    return (
      <div className="requests-page" style={{ justifyContent: 'center', alignItems: 'center', minHeight: 240 }}>
        <Spin size="large" />
      </div>
    );
  }

  if (!detail) {
    return (
      <div className="requests-page">
        <div className="requests-card" style={{ padding: 24 }}>
          <Text type="danger">Không tìm thấy đơn.</Text>
          <div style={{ marginTop: 16 }}>
            <Button icon={<ArrowLeftOutlined />} onClick={() => navigate(-1)}>
              Quay lại
            </Button>
          </div>
        </div>
      </div>
    );
  }

  const canConfirm = detail.orderStatus === 'awaiting_confirm' && detail.requestStatus === 2;

  const orderKind = detail.orderKind ?? (isHandover ? 'return' : 'allocation');
  const isReturnFlow = orderKind === 'return';
  /** Hoàn trả: chỉ trưởng phòng thấy nút xác nhận; cấp phát: giữ theo trạng thái đơn (mọi vai trò có quyền trang). */
  const showConfirmButton =
    canConfirm && (!isReturnFlow || currentRole === 'department_head');

  return (
    <div className="requests-page">
      <div className="requests-header" style={{ alignItems: 'flex-start', gap: 12 }}>
        <div style={{ display: 'flex', alignItems: 'center', gap: 8, flexWrap: 'wrap' }}>
          <Button type="text" icon={<ArrowLeftOutlined />} onClick={() => navigate(-1)}>
            Quay lại
          </Button>
          <div>
            <h1 className="requests-title" style={{ fontSize: 22 }}>
              {isReturnFlow ? 'Đơn hoàn trả' : 'Đơn cấp phát'}
            </h1>
            <Text type="secondary">
              {detail.departmentName}
            </Text>
          </div>
        </div>
        {showConfirmButton && (
          <Button type="primary" loading={confirming} onClick={() => void confirm()}>
            {isReturnFlow ? 'Xác nhận hoàn trả' : 'Xác nhận cấp phát'}
          </Button>
        )}
      </div>

      <div className="requests-card">
        <Descriptions column={1} size="small" bordered style={{ marginBottom: 16 }}>
          <Descriptions.Item label="Tiêu đề">{detail.title}</Descriptions.Item>
          <Descriptions.Item label={isReturnFlow ? 'Phòng ban trả' : 'Phòng ban nhận'}>
            {detail.departmentName}
          </Descriptions.Item>
          <Descriptions.Item label="Người yêu cầu">{detail.requestedByName}</Descriptions.Item>
          <Descriptions.Item label="Gửi yêu cầu lúc">{formatDateOnly(detail.requestSubmittedAt)}</Descriptions.Item>
          <Descriptions.Item label="Trạng thái đơn">
            <span
              className={
                detail.orderStatus === 'confirmed'
                  ? 'asset-status-pill asset-status-pill--active'
                  : 'asset-status-pill asset-status-pill--processing'
              }
            >
              {detail.orderStatus === 'confirmed'
                ? 'Đã xác nhận'
                : 'Chờ xác nhận'}
            </span>
          </Descriptions.Item>
          <Descriptions.Item label="Đơn tạo lúc">{formatDateOnly(detail.createdAt)}</Descriptions.Item>
          {detail.confirmedAt && (
            <Descriptions.Item label={isReturnFlow ? 'Xác nhận hoàn trả lúc' : 'Xác nhận nhận tài sản lúc'}>
              {formatDateOnly(detail.confirmedAt)}
            </Descriptions.Item>
          )}
          <Descriptions.Item label="Ý kiến kế toán">
            {detail.accountantComment?.trim() ? detail.accountantComment.trim() : '—'}
          </Descriptions.Item>
          {detail.confirmedByName && (
            <Descriptions.Item label={isReturnFlow ? 'Người xác nhận hoàn trả' : 'Người xác nhận nhận'}>
              {detail.confirmedByName}
            </Descriptions.Item>
          )}
        </Descriptions>

        <Title level={5}>Chi tiết tài sản</Title>
        <div className="asset-table-wrapper requests-table-wrapper">
          <Table<AllocationOrderLineDetail>
            rowKey={(r, i) => `${r.assetId}-${i}`}
            columns={lineColumns}
            dataSource={detail.lines}
            pagination={false}
            size="small"
            scroll={{ x: true }}
            className="requests-table"
          />
        </div>
      </div>
    </div>
  );
}

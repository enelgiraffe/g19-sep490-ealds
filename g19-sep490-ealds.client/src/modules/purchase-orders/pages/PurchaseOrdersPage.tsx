import { useState, useEffect } from 'react';
import { Table, Button, Input, Select, Tag, Dropdown, message } from 'antd';
import type { MenuProps, TableColumnsType } from 'antd';
import { SearchOutlined, FilterOutlined, SettingOutlined, EyeOutlined, EditOutlined, DeleteOutlined } from '@ant-design/icons';
import { CreatePurchaseOrderModal } from '../components/CreatePurchaseOrderModal';
import { ViewPurchaseOrderModal } from '../components/ViewPurchaseOrderModal';
import { purchaseOrderService, type PurchaseOrderListItem, type PurchaseOrderDetail } from '../services/purchaseOrderService';
import { profileService, type UserProfile } from '../../profile/services/profileService';
import './PurchaseOrdersPage.css';

const { Option } = Select;

/** Backend status: 0=Chờ/Nhập, 1=Duyệt, 2=Từ chối, 3=Chờ ngân sách */
const STATUS_MAP: Record<number, { label: string; color: string }> = {
  0: { label: 'Nhập', color: 'default' },
  1: { label: 'Duyệt', color: 'success' },
  2: { label: 'Từ chối', color: 'error' },
  3: { label: 'Chờ ngân sách', color: 'warning' },
};

interface TableRow extends PurchaseOrderListItem {
  key: string;
  stt: number;
  code: string;
  requestDate: string;
  equipment: string;
  quantity: number;
  estimatedPrice: string;
}

function formatDate(iso: string): string {
  try {
    const d = new Date(iso);
    return d.toLocaleDateString('vi-VN');
  } catch {
    return iso;
  }
}

function toTableRow(item: PurchaseOrderListItem, index: number): TableRow {
  let quantity = 1;
  let estimatedPrice = '—';
  try {
    if (item.proposedData) {
      const parsed = JSON.parse(item.proposedData) as { equipment?: { quantity?: number; estimatedPrice?: string }[]; totalPrice?: string };
      if (Array.isArray(parsed.equipment) && parsed.equipment.length > 0) {
        quantity = parsed.equipment.reduce((s, e) => s + (e.quantity ?? 1), 0);
        estimatedPrice = parsed.totalPrice ?? parsed.equipment[0]?.estimatedPrice ?? '—';
      }
    }
  } catch {
    // keep defaults
  }
  return {
    ...item,
    key: String(item.assetRequestId),
    stt: index + 1,
    code: `YC-${item.assetRequestId}`,
    requestDate: formatDate(item.createDate),
    equipment: item.title,
    quantity,
    estimatedPrice,
  };
}

export function PurchaseOrdersPage() {
  const [data, setData] = useState<TableRow[]>([]);
  const [loading, setLoading] = useState(true);
  const [selectedRowKeys, setSelectedRowKeys] = useState<React.Key[]>([]);
  const [isCreateModalOpen, setIsCreateModalOpen] = useState(false);
  const [isViewModalOpen, setIsViewModalOpen] = useState(false);
  const [selectedDetail, setSelectedDetail] = useState<PurchaseOrderDetail | null>(null);
  const [profile, setProfile] = useState<UserProfile | null>(null);
  const [statusFilter, setStatusFilter] = useState<number | 'all'>('all');
  const [searchText, setSearchText] = useState('');

  const loadList = async () => {
    setLoading(true);
    try {
      const list = await purchaseOrderService.getList();
      setData(list.map((item, i) => toTableRow(item, i)));
    } catch (e) {
      message.error('Không tải được danh sách đơn mua.');
      setData([]);
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    loadList();
  }, []);

  const handleOpenCreateModal = async () => {
    try {
      const p = await profileService.getProfile();
      setProfile(p);
      setIsCreateModalOpen(true);
    } catch {
      message.error('Không lấy được thông tin người dùng.');
    }
  };
  const handleCloseCreateModal = () => {
    setIsCreateModalOpen(false);
    setProfile(null);
  };

  const handleViewDetail = async (record: TableRow) => {
    try {
      const detail = await purchaseOrderService.getById(record.assetRequestId);
      setSelectedDetail(detail);
      setIsViewModalOpen(true);
    } catch {
      message.error('Không tải được chi tiết đơn.');
    }
  };

  const handleCloseViewModal = () => {
    setIsViewModalOpen(false);
    setSelectedDetail(null);
  };

  const handleSubmitPurchaseOrder = async (payload: {
    title: string;
    description?: string;
    proposedData?: string;
  }) => {
    if (!profile) {
      message.error('Vui lòng đăng nhập lại.');
      return;
    }
    try {
      await purchaseOrderService.create({
        userId: profile.id,
        title: payload.title,
        description: payload.description ?? null,
        proposedData: payload.proposedData ?? null,
        createdBy: profile.id,
      });
      message.success('Tạo yêu cầu mua sắm thành công.');
      handleCloseCreateModal();
      loadList();
    } catch (e: unknown) {
      const err = e as { response?: { data?: string } };
      message.error(err?.response?.data ?? 'Tạo yêu cầu thất bại.');
    }
  };

  const getActionMenu = (record: TableRow): MenuProps['items'] => [
    {
      key: 'view',
      label: 'Xem',
      icon: <EyeOutlined />,
      onClick: () => handleViewDetail(record),
    },
    ...(record.status === 0 || record.status === 3
      ? [{
          key: 'edit',
          label: 'Sửa',
          icon: <EditOutlined />,
          onClick: () => message.info('Chức năng sửa đang phát triển'),
        }]
      : []),
    {
      key: 'delete',
      label: 'Xóa',
      icon: <DeleteOutlined />,
      danger: true,
      onClick: () => message.info('Chức năng xóa đang phát triển'),
    },
  ];

  const filteredData = data.filter((row) => {
    const matchStatus = statusFilter === 'all' || row.status === statusFilter;
    const matchSearch =
      !searchText ||
      row.title.toLowerCase().includes(searchText.toLowerCase()) ||
      row.code.toLowerCase().includes(searchText.toLowerCase());
    return matchStatus && matchSearch;
  });

  const columns: TableColumnsType<TableRow> = [
    { title: 'STT', dataIndex: 'stt', key: 'stt', width: 60, align: 'center' },
    { title: 'MÃ YÊU CẦU', dataIndex: 'code', key: 'code', width: 120 },
    { title: 'NGÀY ĐỀ XUẤT', dataIndex: 'requestDate', key: 'requestDate', width: 140 },
    { title: 'MỤC ĐÍCH MUA', dataIndex: 'equipment', key: 'equipment', width: 200 },
    { title: 'SỐ LƯỢNG', dataIndex: 'quantity', key: 'quantity', width: 100, align: 'center' },
    { title: 'TỔNG GIÁ TRỊ DỰ KIẾN', dataIndex: 'estimatedPrice', key: 'estimatedPrice', width: 180, align: 'right' },
    {
      title: 'TRẠNG THÁI',
      dataIndex: 'status',
      key: 'status',
      width: 140,
      render: (status: number) => {
        const config = STATUS_MAP[status] ?? STATUS_MAP[0];
        return (
          <Tag color={config.color} className="purchase-orders-status-tag">
            {config.label}
          </Tag>
        );
      },
    },
    {
      title: '',
      key: 'actions',
      width: 120,
      align: 'center',
      render: (_, record) => (
        <div className="purchase-orders-actions">
          <Dropdown menu={{ items: getActionMenu(record) }} trigger={['click']} placement="bottomRight">
            <Button type="text" icon={<EyeOutlined />} size="small" />
          </Dropdown>
          {(record.status === 0 || record.status === 3) && (
            <Button type="text" icon={<EditOutlined />} size="small" />
          )}
          <Button type="text" icon={<DeleteOutlined />} size="small" danger />
        </div>
      ),
    },
  ];

  const rowSelection = {
    selectedRowKeys,
    onChange: (newKeys: React.Key[]) => setSelectedRowKeys(newKeys),
  };

  return (
    <div className="purchase-orders-page">
      <div className="purchase-orders-header">
        <h1 className="purchase-orders-title">Đơn mua</h1>
        <Button type="primary" className="purchase-orders-btn-add" onClick={handleOpenCreateModal}>
          + Tạo yêu cầu mua sắm
        </Button>
      </div>

      <div className="purchase-orders-filters">
        <Input
          placeholder="Tìm kiếm"
          prefix={<SearchOutlined />}
          className="purchase-orders-search"
          value={searchText}
          onChange={(e) => setSearchText(e.target.value)}
        />
        <Select
          placeholder="Trạng thái"
          className="purchase-orders-select"
          suffixIcon={<FilterOutlined />}
          value={statusFilter}
          onChange={(v) => setStatusFilter(v)}
        >
          <Option value="all">Tất cả</Option>
          {Object.entries(STATUS_MAP).map(([k, v]) => (
            <Option key={k} value={Number(k)}>{v.label}</Option>
          ))}
        </Select>
        <Button icon={<FilterOutlined />} className="purchase-orders-filter-advanced">
          Gộp bộ lọc
        </Button>
        <Button icon={<SettingOutlined />} className="purchase-orders-settings" />
      </div>

      <div className="purchase-orders-table-container">
        <Table
          rowSelection={rowSelection}
          columns={columns}
          dataSource={filteredData}
          loading={loading}
          pagination={{
            total: filteredData.length,
            pageSize: 25,
            showSizeChanger: true,
            showTotal: (total, range) => `${range[0]}-${range[1]} / ${total}`,
            pageSizeOptions: ['10', '25', '50', '100'],
            className: 'purchase-orders-pagination',
          }}
          className="purchase-orders-table"
        />
      </div>

      <CreatePurchaseOrderModal
        open={isCreateModalOpen}
        onClose={handleCloseCreateModal}
        onSubmit={handleSubmitPurchaseOrder}
        creatorName={profile?.name ?? profile?.email ?? null}
      />

      <ViewPurchaseOrderModal
        open={isViewModalOpen}
        onClose={handleCloseViewModal}
        data={selectedDetail}
      />
    </div>
  );
}

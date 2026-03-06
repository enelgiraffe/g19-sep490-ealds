import { useState } from 'react';
import { Table, Button, Input, Select, Tag, Dropdown } from 'antd';
import type { MenuProps, TableColumnsType } from 'antd';
import { SearchOutlined, FilterOutlined, SettingOutlined, EyeOutlined, EditOutlined, DeleteOutlined } from '@ant-design/icons';
import { CreatePurchaseOrderModal } from '../components/CreatePurchaseOrderModal';
import { ViewPurchaseOrderModal } from '../components/ViewPurchaseOrderModal';
import './PurchaseOrdersPage.css';

const { Option } = Select;

interface PurchaseOrder {
  key: string;
  stt: number;
  code: string;
  requestDate: string;
  equipment: string;
  quantity: number;
  estimatedPrice: string;
  status: 'approved' | 'pending' | 'rejected' | 'waiting';
}

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

const STATUS_CONFIG = {
  approved: { label: 'Duyệt', color: 'success' },
  pending: { label: 'Từ chối', color: 'error' },
  rejected: { label: 'Chờ ngân sách', color: 'warning' },
  waiting: { label: 'Nhập', color: 'default' },
};

const mockData: PurchaseOrder[] = [
  {
    key: '1',
    stt: 1,
    code: 'YC-1',
    requestDate: '25/01/2026',
    equipment: 'Cơ khí',
    quantity: 1,
    estimatedPrice: '910,000,000 đ',
    status: 'waiting',
  },
  {
    key: '2',
    stt: 2,
    code: 'YC-2',
    requestDate: '15/12/2025',
    equipment: 'Cơ khí',
    quantity: 1,
    estimatedPrice: '500,000,000 đ',
    status: 'pending',
  },
  {
    key: '3',
    stt: 3,
    code: 'YC-3',
    requestDate: '12/10/2025',
    equipment: 'Máy móc',
    quantity: 1,
    estimatedPrice: '34,000,500,000,000 đ',
    status: 'approved',
  },
  {
    key: '4',
    stt: 4,
    code: 'YC-4',
    requestDate: '24/09/2025',
    equipment: 'Cơ khí',
    quantity: 1,
    estimatedPrice: '500,000,000 đ',
    status: 'rejected',
  },
];

// Mock detail data
const mockDetailData: Record<string, PurchaseOrderDetail> = {
  '3': {
    sender: 'Nguyễn Văn A',
    department: 'Phòng sản xuất',
    reason: 'Phục vụ sản xuất',
    needDate: '20/02/2024',
    supplier: 'Phục vụ sản xuất',
    assetType: 'Máy móc',
    equipment: [
      {
        stt: 1,
        name: 'Máy cắt sắt',
        quantity: 1,
        machineCode: 'EGHB',
        unit: 'Cái',
        estimatedPrice: '1,000,000,000đ',
      },
    ],
    totalPrice: '1,000,000,000đ',
    purpose: 'Phục vụ sản xuất đơn hàng ABC',
    attachments: [
      { id: 1, name: 'Thông tin máy' },
      { id: 2, name: 'Thông tin nhà cung cấp' },
    ],
    accountantNotes: '-',
    directorNotes: 'Chờ phản bổ ngân sách',
    status: 'approved',
  },
};

export function PurchaseOrdersPage() {
  const [data] = useState<PurchaseOrder[]>(mockData);
  const [selectedRowKeys, setSelectedRowKeys] = useState<React.Key[]>([]);
  const [isCreateModalOpen, setIsCreateModalOpen] = useState(false);
  const [isViewModalOpen, setIsViewModalOpen] = useState(false);
  const [selectedDetail, setSelectedDetail] = useState<PurchaseOrderDetail | null>(null);

  const handleOpenCreateModal = () => {
    setIsCreateModalOpen(true);
  };

  const handleCloseCreateModal = () => {
    setIsCreateModalOpen(false);
  };

  const handleViewDetail = (record: PurchaseOrder) => {
    const detail = mockDetailData[record.key];
    if (detail) {
      setSelectedDetail(detail);
      setIsViewModalOpen(true);
    }
  };

  const handleCloseViewModal = () => {
    setIsViewModalOpen(false);
    setSelectedDetail(null);
  };

  const handleSubmitPurchaseOrder = (values: any) => {
    console.log('Submit purchase order:', values);
    // TODO: Call API to create purchase order
  };

  const getActionMenu = (record: PurchaseOrder): MenuProps['items'] => {
    const items: MenuProps['items'] = [
      {
        key: 'view',
        label: 'Xem',
        icon: <EyeOutlined />,
        onClick: () => handleViewDetail(record),
      },
    ];

    if (record.status === 'waiting' || record.status === 'rejected') {
      items.push({
        key: 'edit',
        label: 'Sửa',
        icon: <EditOutlined />,
        onClick: () => console.log('Edit', record),
      });
    }

    items.push({
      key: 'delete',
      label: 'Xóa',
      icon: <DeleteOutlined />,
      danger: true,
      onClick: () => console.log('Delete', record),
    });

    return items;
  };

  const columns: TableColumnsType<PurchaseOrder> = [
    {
      title: 'STT',
      dataIndex: 'stt',
      key: 'stt',
      width: 60,
      align: 'center',
    },
    {
      title: 'MÃ YÊU CẦU',
      dataIndex: 'code',
      key: 'code',
      width: 120,
    },
    {
      title: 'NGÀY ĐỀ XUẤT',
      dataIndex: 'requestDate',
      key: 'requestDate',
      width: 140,
    },
    {
      title: 'MỤC DỊCH MUA',
      dataIndex: 'equipment',
      key: 'equipment',
      width: 150,
    },
    {
      title: 'SỐ LƯỢNG',
      dataIndex: 'quantity',
      key: 'quantity',
      width: 100,
      align: 'center',
    },
    {
      title: 'TỔNG GIÁ TRỊ DỰ KIẾN',
      dataIndex: 'estimatedPrice',
      key: 'estimatedPrice',
      width: 200,
      align: 'right',
    },
    {
      title: 'TRẠNG THÁI',
      dataIndex: 'status',
      key: 'status',
      width: 150,
      render: (status: keyof typeof STATUS_CONFIG) => {
        const config = STATUS_CONFIG[status];
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
          {(record.status === 'waiting' || record.status === 'rejected') && (
            <Button type="text" icon={<EditOutlined />} size="small" />
          )}
          <Button type="text" icon={<DeleteOutlined />} size="small" danger />
        </div>
      ),
    },
  ];

  const rowSelection = {
    selectedRowKeys,
    onChange: (newSelectedRowKeys: React.Key[]) => {
      setSelectedRowKeys(newSelectedRowKeys);
    },
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
        />
        <Select
          placeholder="Trạng thái xuất bản"
          className="purchase-orders-select"
          suffixIcon={<FilterOutlined />}
        >
          <Option value="all">Tất cả</Option>
          <Option value="approved">Duyệt</Option>
          <Option value="pending">Từ chối</Option>
          <Option value="rejected">Chờ ngân sách</Option>
          <Option value="waiting">Nhập</Option>
        </Select>
        <Select
          placeholder="Trạng thái hàng"
          className="purchase-orders-select"
          suffixIcon={<FilterOutlined />}
        >
          <Option value="all">Tất cả</Option>
        </Select>
        <Select
          placeholder="Giá"
          className="purchase-orders-select"
          suffixIcon={<FilterOutlined />}
        >
          <Option value="all">Tất cả</Option>
        </Select>
        <Button
          icon={<FilterOutlined />}
          className="purchase-orders-filter-advanced"
        >
          Gộp bộ lọc
        </Button>
        <Button
          icon={<SettingOutlined />}
          className="purchase-orders-settings"
        />
      </div>

      <div className="purchase-orders-table-container">
        <Table
          rowSelection={rowSelection}
          columns={columns}
          dataSource={data}
          pagination={{
            total: 289,
            pageSize: 25,
            current: 1,
            showSizeChanger: true,
            showTotal: (total, range) => `${range[0]}-${range[1]} of ${total}`,
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
      />

      <ViewPurchaseOrderModal
        open={isViewModalOpen}
        onClose={handleCloseViewModal}
        data={selectedDetail}
      />
    </div>
  );
}

import { useEffect, useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { Table, Button, Input, Select, Tag, Dropdown, Modal, message } from 'antd';
import type { MenuProps, TableColumnsType } from 'antd';
import {
  SearchOutlined,
  FilterOutlined,
  SettingOutlined,
  PlusOutlined,
  DownloadOutlined,
  MoreOutlined,
  SwapOutlined,
  DollarOutlined,
  EditOutlined,
  QrcodeOutlined,
  DeleteOutlined,
} from '@ant-design/icons';
import {
  assetService,
  formatVnd,
  getStatusLabel,
  type AssetResponse,
  type GetAssetsParams,
} from '../../assets/services/assetService';
import './AccountantAssetListPage.css';

const { Option } = Select;

interface AccountantAssetRow {
  key: string;
  id: number;
  code: string;
  name: string;
  type: string;
  location: string;
  quantity: number;
  price: string;
  status: 'in-use' | 'pending-use';
  statusLabel: string;
  depreciation: string;
}

function mapAssetToRow(a: AssetResponse): AccountantAssetRow {
  const isInUse = a.status === 1; // InUse
  const depValue = Math.max(0, a.originalPrice - a.currentValue);
  return {
    key: String(a.assetId),
    id: a.assetId,
    code: a.code,
    name: a.name,
    type: a.assetTypeName ?? '—',
    location: a.warehouseName ?? '—',
    quantity: a.quantity,
    price: formatVnd(a.currentValue),
    status: isInUse ? 'in-use' : 'pending-use',
    statusLabel: getStatusLabel(a.statusName),
    depreciation: formatVnd(depValue),
  };
}

export function AccountantAssetListPage() {
  const [data, setData] = useState<AccountantAssetRow[]>([]);
  const [loading, setLoading] = useState(true);
  const [selectedRowKeys, setSelectedRowKeys] = useState<React.Key[]>([]);
  const [searchInput, setSearchInput] = useState('');
  const [keyword, setKeyword] = useState('');
  const [statusFilter, setStatusFilter] = useState<number | undefined>(undefined);
  const [assetTypeFilter, setAssetTypeFilter] = useState<number | undefined>(undefined);
  const [refreshKey, setRefreshKey] = useState(0);
  const navigate = useNavigate();

  useEffect(() => {
    const t = setTimeout(() => setKeyword(searchInput.trim()), 400);
    return () => clearTimeout(t);
  }, [searchInput]);

  useEffect(() => {
    let cancelled = false;
    setLoading(true);
    const params: GetAssetsParams = {
      keyword: keyword || undefined,
      status: statusFilter,
      assetTypeId: assetTypeFilter,
    };
    assetService
      .getAll(params)
      .then((list) => {
        if (!cancelled) setData(list.map(mapAssetToRow));
      })
      .catch(() => {
        if (!cancelled) {
          message.error('Không tải được danh sách tài sản.');
          setData([]);
        }
      })
      .finally(() => {
        if (!cancelled) setLoading(false);
      });
    return () => {
      cancelled = true;
    };
  }, [keyword, statusFilter, assetTypeFilter, refreshKey]);

  const handleDeleteAsset = (asset: AccountantAssetRow) => {
    Modal.confirm({
      title: 'Xóa tài sản',
      content: `Bạn có chắc muốn xóa tài sản "${asset.name}" (${asset.code})?`,
      okText: 'Xóa',
      okType: 'danger',
      cancelText: 'Hủy',
      async onOk() {
        try {
          await assetService.softDelete(asset.id, { status: 4, reason: null });
          message.success('Đã gửi yêu cầu xóa (Disposed) lên hệ thống.');
          setRefreshKey((k) => k + 1);
        } catch {
          message.error('Xóa tài sản thất bại. Vui lòng thử lại.');
        }
      },
    });
  };

  const handleMenuClick: MenuProps['onClick'] = ({ domEvent }) => {
    domEvent.stopPropagation();
  };

  const buildMenu = (record: AccountantAssetRow): MenuProps => ({
    onClick: handleMenuClick,
    items: [
      {
        key: 'move',
        icon: <SwapOutlined />,
        label: 'Điều chuyển',
        disabled: true,
      },
      {
        key: 'liquidate',
        icon: <DollarOutlined />,
        label: 'Đề nghị thanh lý',
        disabled: true,
      },
      {
        key: 'edit',
        icon: <EditOutlined />,
        label: 'Sửa',
        onClick: (info) => {
          info.domEvent.stopPropagation();
          navigate(`/assets/${record.id}/edit`);
        },
      },
      {
        key: 'print-qr',
        icon: <QrcodeOutlined />,
        label: 'In mã QR',
        disabled: true,
      },
      {
        type: 'divider',
      },
      {
        key: 'delete',
        icon: <DeleteOutlined style={{ color: '#FE3720' }} />,
        label: <span style={{ color: '#FE3720' }}>Xóa</span>,
        onClick: (info) => {
          info.domEvent.stopPropagation();
          handleDeleteAsset(record);
        },
      },
    ],
  });

  const columns: TableColumnsType<AccountantAssetRow> = [
    {
      title: 'MÃ TÀI SẢN',
      dataIndex: 'code',
      key: 'code',
      width: 120,
      render: (text, record) => (
        <button
          type="button"
          className="accountant-asset-code accountant-asset-code--link"
          onClick={() => navigate(`/assets/${record.id}`)}
        >
          {text}
        </button>
      ),
    },
    {
      title: 'TÊN TÀI SẢN',
      dataIndex: 'name',
      key: 'name',
      width: 200,
    },
    {
      title: 'LOẠI TÀI SẢN',
      dataIndex: 'type',
      key: 'type',
      width: 150,
    },
    {
      title: 'VỊ TRÍ TÀI SẢN',
      dataIndex: 'location',
      key: 'location',
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
      title: 'GIÁ',
      dataIndex: 'price',
      key: 'price',
      width: 180,
      align: 'right',
    },
    {
      title: 'TRẠNG THÁI',
      dataIndex: 'status',
      key: 'status',
      width: 150,
      render: (status: string, record) => (
        <Tag 
          color={status === 'in-use' ? 'success' : 'default'}
          className="accountant-asset-status-tag"
        >
          {record.statusLabel}
        </Tag>
      ),
    },
    {
      title: 'GIÁ TRỊ KHẤU HAO',
      dataIndex: 'depreciation',
      key: 'depreciation',
      width: 180,
      align: 'right',
    },
    {
      title: '',
      key: 'actions',
      width: 60,
      align: 'center',
      render: (_, record) => (
        <Dropdown
          menu={buildMenu(record)}
          trigger={['click']}
          placement="bottomRight"
        >
          <Button
            type="text"
            icon={<MoreOutlined />}
            className="accountant-asset-actions-btn"
            onClick={(e) => e.stopPropagation()}
          />
        </Dropdown>
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
    <div className="accountant-asset-page">
      <div className="accountant-asset-header">
        <div className="accountant-asset-header-left">
          <Input
            placeholder="Tìm kiếm"
            prefix={<SearchOutlined />}
            className="accountant-asset-search"
            value={searchInput}
            onChange={(e) => setSearchInput(e.target.value)}
          />
          <Select
            placeholder="Loại tài sản"
            className="accountant-asset-filter"
            suffixIcon={<FilterOutlined />}
            value={assetTypeFilter ?? ''}
            onChange={(v) => setAssetTypeFilter(v === '' || v == null ? undefined : Number(v))}
            allowClear
          >
            <Option value="">Tất cả</Option>
            <Option value={1}>Máy móc</Option>
            <Option value={2}>Thiết bị</Option>
          </Select>
          <Select
            placeholder="Trạng thái"
            className="accountant-asset-filter"
            suffixIcon={<FilterOutlined />}
            value={statusFilter ?? ''}
            onChange={(v) => setStatusFilter(v === '' || v == null ? undefined : Number(v))}
            allowClear
          >
            <Option value="">Tất cả</Option>
            <Option value={0}>Sẵn có</Option>
            <Option value={1}>Đang sử dụng</Option>
            <Option value={2}>Đang bảo trì</Option>
          </Select>
          <Button
            icon={<FilterOutlined />}
            className="accountant-asset-filter-reset"
            onClick={() => {
              setSearchInput('');
              setKeyword('');
              setStatusFilter(undefined);
              setAssetTypeFilter(undefined);
            }}
          >
            Gỡ bộ lọc
          </Button>
          <Button
            icon={<SettingOutlined />}
            className="accountant-asset-settings"
          />
        </div>
        <div className="accountant-asset-header-right">
          <Button
            type="primary"
            icon={<PlusOutlined />}
            className="accountant-asset-btn-add"
            onClick={() => navigate('/assets/new')}
          >
            Thêm tài sản
          </Button>
          <Button
            icon={<DownloadOutlined />}
            className="accountant-asset-btn-template"
          >
            Template ghi tăng
          </Button>
        </div>
      </div>

      <div className="accountant-asset-table-container">
        <Table
          rowSelection={rowSelection}
          columns={columns}
          dataSource={data}
          loading={loading}
          pagination={{
            total: data.length,
            pageSize: 25,
            showSizeChanger: true,
            showTotal: (total, range) => `${range[0]}-${range[1]} / ${total}`,
            pageSizeOptions: ['10', '25', '50', '100'],
            className: 'accountant-asset-pagination',
          }}
          className="accountant-asset-table"
        />
      </div>
    </div>
  );
}

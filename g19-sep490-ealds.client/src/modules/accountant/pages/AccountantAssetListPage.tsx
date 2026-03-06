import { useState } from 'react';
import { Table, Button, Input, Select, Tag } from 'antd';
import type { TableColumnsType } from 'antd';
import { SearchOutlined, FilterOutlined, SettingOutlined, PlusOutlined, DownloadOutlined, MoreOutlined } from '@ant-design/icons';
import './AccountantAssetListPage.css';

const { Option } = Select;

interface AccountantAsset {
  key: string;
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

const mockData: AccountantAsset[] = [
  {
    key: '1',
    code: 'MCS',
    name: 'Máy cắt sắt',
    type: 'Cơ khí',
    location: 'Kho A',
    quantity: 1,
    price: '910,000,000 đ',
    status: 'in-use',
    statusLabel: 'Đang sử dụng',
    depreciation: '810,000,000 đ',
  },
  {
    key: '2',
    code: 'MUV',
    name: 'Máy uốn vòm',
    type: 'Cơ khí',
    location: 'Kho A',
    quantity: 1,
    price: '500,000,000 đ',
    status: 'in-use',
    statusLabel: 'Đang sử dụng',
    depreciation: '400,000,000 đ',
  },
  {
    key: '3',
    code: 'FSF90',
    name: 'Ôtô Ferrari SF90',
    type: 'Máy móc',
    location: 'Kho A',
    quantity: 1,
    price: '34,000,500,000,000 đ',
    status: 'in-use',
    statusLabel: 'Đang sử dụng',
    depreciation: '34,000,000,000,000 đ',
  },
  {
    key: '4',
    code: 'MEG',
    name: 'Máy ép góc',
    type: 'Cơ khí',
    location: 'Kho A',
    quantity: 1,
    price: '500,000,000 đ',
    status: 'pending-use',
    statusLabel: 'Chưa sử dụng',
    depreciation: '450,000,000 đ',
  },
];

export function AccountantAssetListPage() {
  const [data] = useState<AccountantAsset[]>(mockData);
  const [selectedRowKeys, setSelectedRowKeys] = useState<React.Key[]>([]);

  const columns: TableColumnsType<AccountantAsset> = [
    {
      title: 'MÃ TÀI SẢN',
      dataIndex: 'code',
      key: 'code',
      width: 120,
      render: (text) => <span className="accountant-asset-code">{text}</span>,
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
      render: () => (
        <Button
          type="text"
          icon={<MoreOutlined />}
          className="accountant-asset-actions-btn"
        />
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
          />
          <Select
            placeholder="Loại tài sản"
            className="accountant-asset-filter"
            suffixIcon={<FilterOutlined />}
          >
            <Option value="all">Tất cả</Option>
            <Option value="mechanical">Cơ khí</Option>
            <Option value="machinery">Máy móc</Option>
          </Select>
          <Select
            placeholder="Trạng thái"
            className="accountant-asset-filter"
            suffixIcon={<FilterOutlined />}
          >
            <Option value="all">Tất cả</Option>
            <Option value="in-use">Đang sử dụng</Option>
            <Option value="pending-use">Chưa sử dụng</Option>
          </Select>
          <Select
            placeholder="Giá"
            className="accountant-asset-filter"
            suffixIcon={<FilterOutlined />}
          >
            <Option value="all">Tất cả</Option>
          </Select>
          <Button
            icon={<FilterOutlined />}
            className="accountant-asset-filter-reset"
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
          pagination={{
            total: 289,
            pageSize: 25,
            current: 1,
            showSizeChanger: true,
            showTotal: (total, range) => `${range[0]}-${range[1]} of ${total}`,
            pageSizeOptions: ['10', '25', '50', '100'],
            className: 'accountant-asset-pagination',
          }}
          className="accountant-asset-table"
        />
      </div>
    </div>
  );
}

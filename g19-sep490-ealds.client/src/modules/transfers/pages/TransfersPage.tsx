import { useEffect, useState } from 'react';
import { Table, Button, Input, Select, Tag, message } from 'antd';
import type { TableColumnsType } from 'antd';
import { SearchOutlined, FilterOutlined, SettingOutlined } from '@ant-design/icons';
import { transferRequestService, type TransferRequestListItem } from '../../assets/services/transferRequestService';
import './TransfersPage.css';

const { Option } = Select;

const STATUS_MAP: Record<number, { label: string; color: string }> = {
  0: { label: 'Nháp', color: 'default' },
  1: { label: 'Đã nộp', color: 'processing' },
  2: { label: 'Hợp lệ', color: 'success' },
  3: { label: 'Chờ phê duyệt', color: 'warning' },
  4: { label: 'Phê duyệt', color: 'success' },
};

interface TableRow extends TransferRequestListItem {
  key: string;
  stt: number;
  transferDateText: string;
}

function formatDate(iso: string): string {
  try {
    const d = new Date(iso);
    return d.toLocaleDateString('vi-VN');
  } catch {
    return iso;
  }
}

export function TransfersPage() {
  const [data, setData] = useState<TableRow[]>([]);
  const [loading, setLoading] = useState(true);
  const [statusFilter, setStatusFilter] = useState<number | 'all'>('all');
  const [searchText, setSearchText] = useState('');

  const loadList = async () => {
    setLoading(true);
    try {
      const list = await transferRequestService.getList();
      const rows: TableRow[] = list.map((item, index) => ({
        ...item,
        key: String(item.recordId),
        stt: index + 1,
        transferDateText: formatDate(item.transferDate),
      }));
      setData(rows);
    } catch {
      message.error('Không tải được danh sách yêu cầu điều chuyển.');
      setData([]);
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    loadList();
  }, []);

  const filteredData = data.filter((row) => {
    const matchStatus = statusFilter === 'all' || row.status === statusFilter;
    const kw = searchText.trim().toLowerCase();
    const matchSearch =
      !kw ||
      row.code.toLowerCase().includes(kw) ||
      row.assetCode.toLowerCase().includes(kw) ||
      row.assetName.toLowerCase().includes(kw);
    return matchStatus && matchSearch;
  });

  const columns: TableColumnsType<TableRow> = [
    { title: 'SỐ BIÊN BẢN', dataIndex: 'code', key: 'code', width: 140 },
    { title: 'NGÀY ĐIỀU CHUYỂN', dataIndex: 'transferDateText', key: 'transferDateText', width: 140 },
    { title: 'ĐIỀU CHUYỂN TỪ', dataIndex: 'fromDepartment', key: 'fromDepartment', width: 160 },
    { title: 'ĐIỀU CHUYỂN ĐẾN', dataIndex: 'toDepartment', key: 'toDepartment', width: 160 },
    { title: 'SỐ LƯỢNG', dataIndex: 'quantity', key: 'quantity', width: 100, align: 'center' },
    {
      title: 'TRẠNG THÁI',
      dataIndex: 'status',
      key: 'status',
      width: 140,
      render: (status: number) => {
        const config = STATUS_MAP[status] ?? STATUS_MAP[0];
        return (
          <Tag color={config.color} className="transfers-status-tag">
            {config.label}
          </Tag>
        );
      },
    },
    { title: 'LÝ DO ĐIỀU CHUYỂN', dataIndex: 'reason', key: 'reason', ellipsis: true },
  ];

  return (
    <div className="transfers-page">
      <div className="transfers-header">
        <h1 className="transfers-title">Điều chuyển</h1>
        <Button
          type="primary"
          className="transfers-btn-add"
          onClick={() => message.info('Vui lòng tạo yêu cầu điều chuyển từ màn hình Tài sản.')}
        >
          + Tạo yêu cầu điều chuyển
        </Button>
      </div>

      <div className="transfers-filters">
        <Input
          placeholder="Tìm kiếm"
          prefix={<SearchOutlined />}
          className="transfers-search"
          value={searchText}
          onChange={(e) => setSearchText(e.target.value)}
        />
        <Select
          placeholder="Trạng thái"
          className="transfers-select"
          suffixIcon={<FilterOutlined />}
          value={statusFilter}
          onChange={(v) => setStatusFilter(v)}
        >
          <Option value="all">Tất cả</Option>
          {Object.entries(STATUS_MAP).map(([k, v]) => (
            <Option key={k} value={Number(k)}>
              {v.label}
            </Option>
          ))}
        </Select>
        <Button icon={<FilterOutlined />} className="transfers-filter-advanced">
          Gộp bộ lọc
        </Button>
        <Button icon={<SettingOutlined />} className="transfers-settings" />
      </div>

      <div className="transfers-table-container">
        <Table
          columns={columns}
          dataSource={filteredData}
          loading={loading}
          pagination={{
            total: filteredData.length,
            pageSize: 25,
            showSizeChanger: true,
            showTotal: (total, range) => `${range[0]}-${range[1]} / ${total}`,
            pageSizeOptions: ['10', '25', '50', '100'],
            className: 'transfers-pagination',
          }}
          className="transfers-table"
          rowKey="key"
        />
      </div>
    </div>
  );
}


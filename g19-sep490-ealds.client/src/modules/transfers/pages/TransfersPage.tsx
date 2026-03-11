import { useEffect, useRef, useState } from 'react';
import { Button, Input, Select, message, Tabs } from 'antd';
import { SearchOutlined, FilterOutlined, SettingOutlined, EyeOutlined } from '@ant-design/icons';
import { transferRequestService, type TransferRequestListItem } from '../../assets/services/transferRequestService';
import { TransferAssetModal } from '../../assets/components/TransferAssetModal';
import { TransferRequestDetailModal } from '../components/TransferRequestDetailModal';
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
  const [activeTab, setActiveTab] = useState<'location' | 'department'>('location');
  const [isTransferModalOpen, setIsTransferModalOpen] = useState(false);
  const [isDetailModalOpen, setIsDetailModalOpen] = useState(false);
  const [selectedRequest, setSelectedRequest] = useState<TableRow | null>(null);
  const tableHostRef = useRef<HTMLDivElement | null>(null);
  const [page, setPage] = useState(1);
  const [pageSize, setPageSize] = useState(25);

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

  useEffect(() => {
    // Reset to first page when filters change
    setPage(1);
  }, [activeTab, statusFilter, searchText]);

  const total = filteredData.length;
  const totalPages = Math.max(1, Math.ceil(total / pageSize));
  const safePage = Math.min(page, totalPages);
  const startIndex = total === 0 ? 0 : (safePage - 1) * pageSize + 1;
  const endIndex = Math.min(safePage * pageSize, total);
  const pagedData = filteredData.slice((safePage - 1) * pageSize, safePage * pageSize);

  return (
    <div className="transfers-page">
      <div className="transfers-header">
        <h1 className="transfers-title">Điều chuyển</h1>
        <Button
          type="primary"
          danger
          className="transfers-btn-add"
          onClick={() => setIsTransferModalOpen(true)}
        >
          + Tạo yêu cầu điều chuyển
        </Button>
      </div>

      <div className="transfers-card">
        <Tabs
          activeKey={activeTab}
          onChange={(key) => setActiveTab(key as 'location' | 'department')}
          className="transfers-tabs"
          items={[
            { key: 'location', label: 'Vị trí tài sản' },
            { key: 'department', label: 'Phòng ban sử dụng' },
          ]}
        />

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
            Gỡ bộ lọc
          </Button>
          <Button icon={<SettingOutlined />} className="transfers-settings" />
        </div>

        <div className="asset-table-wrapper transfers-table-wrapper" ref={tableHostRef}>
          {loading ? (
            <div className="transfers-table-loading">Đang tải danh sách yêu cầu điều chuyển...</div>
          ) : (
            <table className="asset-table transfers-table">
              <thead>
                <tr>
                  <th className="asset-table__cell asset-table__cell--checkbox">
                    <input type="checkbox" />
                  </th>
                  <th>STT</th>
                  <th>SỐ BIÊN BẢN</th>
                  <th>NGÀY ĐIỀU CHUYỂN</th>
                  <th>ĐIỀU CHUYỂN TỪ</th>
                  <th>ĐIỀU CHUYỂN ĐẾN</th>
                  <th>SỐ LƯỢNG</th>
                  <th>TRẠNG THÁI</th>
                  <th>LÝ DO ĐIỀU CHUYỂN</th>
                  <th className="asset-table__cell asset-table__cell--actions" />
                </tr>
              </thead>
              <tbody>
                {pagedData.length === 0 ? (
                  <tr>
                    <td colSpan={10} className="transfers-table-empty">
                      Không có dữ liệu.
                    </td>
                  </tr>
                ) : (
                  pagedData.map((row) => {
                    const config = STATUS_MAP[row.status] ?? STATUS_MAP[0];
                    return (
                      <tr key={row.key} className="asset-row">
                        <td className="asset-table__cell asset-table__cell--checkbox">
                          <input type="checkbox" />
                        </td>
                        <td className="asset-align-right">{row.stt}</td>
                        <td>
                          <button
                            type="button"
                            className="asset-code asset-code--link"
                            onClick={() => {
                              setSelectedRequest(row);
                              setIsDetailModalOpen(true);
                            }}
                          >
                            {row.code}
                          </button>
                        </td>
                        <td>{row.transferDateText}</td>
                        <td>{row.fromDepartment}</td>
                        <td>{row.toDepartment}</td>
                        <td className="asset-align-right">{row.quantity}</td>
                        <td>
                          <span
                            className={
                              config.color === 'success'
                                ? 'asset-status-pill asset-status-pill--active'
                                : config.color === 'default'
                                ? 'asset-status-pill asset-status-pill--inactive'
                                : 'asset-status-pill'
                            }
                          >
                            {config.label}
                          </span>
                        </td>
                        <td>{row.reason}</td>
                        <td className="asset-table__cell asset-table__cell--actions">
                          <Button
                            type="text"
                            icon={<EyeOutlined />}
                            onClick={() => {
                              setSelectedRequest(row);
                              setIsDetailModalOpen(true);
                            }}
                          />
                        </td>
                      </tr>
                    );
                  })
                )}
              </tbody>
            </table>
          )}
        </div>

        <div className="transfers-card__footer">
          <div className="transfers-footer__left">
            Số lượng trên trang:
            <select
              className="transfers-footer__select"
              value={pageSize}
              onChange={(e) => {
                const next = Number(e.target.value);
                setPageSize(next);
                setPage(1);
              }}
            >
              <option value={10}>10</option>
              <option value={25}>25</option>
              <option value={50}>50</option>
              <option value={100}>100</option>
            </select>
          </div>
          <div className="transfers-footer__center">
            {total === 0 ? '0-0 trên 0' : `${startIndex}-${endIndex} trên ${total}`}
          </div>
          <div className="transfers-footer__right">
            <button
              className="transfers-footer__pager"
              disabled={safePage <= 1}
              onClick={() => setPage((p) => Math.max(1, p - 1))}
              type="button"
            >
              ⟨
            </button>
            <button
              className="transfers-footer__pager transfers-footer__pager--active"
              type="button"
            >
              {safePage}
            </button>
            <button
              className="transfers-footer__pager"
              disabled={safePage >= totalPages}
              onClick={() => setPage((p) => Math.min(totalPages, p + 1))}
              type="button"
            >
              ⟩
            </button>
          </div>
        </div>
      </div>

      <TransferAssetModal
        open={isTransferModalOpen}
        onClose={() => setIsTransferModalOpen(false)}
        onSubmit={() => {
          message.info('Vui lòng gửi yêu cầu điều chuyển từ màn Tài sản sau khi chọn tài sản cụ thể.');
        }}
        assetInfo={null}
      />

      <TransferRequestDetailModal
        open={isDetailModalOpen}
        onClose={() => {
          setIsDetailModalOpen(false);
          setSelectedRequest(null);
        }}
        request={selectedRequest}
      />
    </div>
  );
}


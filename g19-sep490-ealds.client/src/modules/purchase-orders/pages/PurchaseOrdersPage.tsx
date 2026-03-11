import { useState, useEffect } from 'react';
import { Button, Input, Select, Tag, Dropdown, message } from 'antd';
import type { MenuProps } from 'antd';
import { SearchOutlined, FilterOutlined, SettingOutlined, EyeOutlined, EditOutlined, DeleteOutlined } from '@ant-design/icons';
import { CreatePurchaseOrderModal } from '../components/CreatePurchaseOrderModal';
import { ViewPurchaseOrderModal } from '../components/ViewPurchaseOrderModal';
import { purchaseOrderService, type PurchaseOrderListItem, type PurchaseOrderDetail } from '../services/purchaseOrderService';
import { profileService, type UserProfile } from '../../profile/services/profileService';
import './PurchaseOrdersPage.css';

const { Option } = Select;

/** Backend status: 0=Chờ/Nháp, 1=Duyệt, 2=Từ chối, 3=Chờ ngân sách */
const STATUS_MAP: Record<number, { label: string; color: string }> = {
  0: { label: 'Nháp', color: 'default' },
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
  const [isCreateModalOpen, setIsCreateModalOpen] = useState(false);
  const [isViewModalOpen, setIsViewModalOpen] = useState(false);
  const [selectedDetail, setSelectedDetail] = useState<PurchaseOrderDetail | null>(null);
  const [profile, setProfile] = useState<UserProfile | null>(null);
  const [statusFilter, setStatusFilter] = useState<number | 'all'>('all');
  const [searchText, setSearchText] = useState('');
  const [page, setPage] = useState(1);
  const [pageSize, setPageSize] = useState(25);

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

  useEffect(() => {
    setPage(1);
  }, [statusFilter, searchText]);

  const total = filteredData.length;
  const totalPages = Math.max(1, Math.ceil(total / pageSize));
  const safePage = Math.min(page, totalPages);
  const startIndex = total === 0 ? 0 : (safePage - 1) * pageSize + 1;
  const endIndex = Math.min(safePage * pageSize, total);
  const pagedData = filteredData.slice((safePage - 1) * pageSize, safePage * pageSize);

  return (
    <div className="purchase-orders-page">
      <div className="purchase-orders-header">
        <h1 className="purchase-orders-title">Đơn mua</h1>
        <Button type="primary" className="purchase-orders-btn-add" onClick={handleOpenCreateModal}>
          + Tạo yêu cầu mua sắm
        </Button>
      </div>

      <div className="purchase-orders-card">
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
            Gỡ bộ lọc
          </Button>
          <Button icon={<SettingOutlined />} className="purchase-orders-settings" />
        </div>

        <div className="asset-table-wrapper">
          {loading ? (
            <div className="purchase-orders-table-loading">Đang tải danh sách đơn mua...</div>
          ) : (
            <table className="asset-table purchase-orders-table">
              <thead>
                <tr>
                  <th className="asset-table__cell asset-table__cell--checkbox">
                    <input type="checkbox" />
                  </th>
                  <th>STT</th>
                  <th>MÃ YÊU CẦU</th>
                  <th>NGÀY ĐỀ XUẤT</th>
                  <th>MỤC ĐÍCH MUA</th>
                  <th>SỐ LƯỢNG</th>
                  <th>TỔNG GIÁ TRỊ DỰ KIẾN</th>
                  <th>TRẠNG THÁI</th>
                  <th className="asset-table__cell asset-table__cell--actions" />
                </tr>
              </thead>
              <tbody>
                {pagedData.map((row) => {
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
                          onClick={() => handleViewDetail(row)}
                        >
                          {row.code}
                        </button>
                      </td>
                      <td>{row.requestDate}</td>
                      <td>{row.equipment}</td>
                      <td className="asset-align-right">{row.quantity}</td>
                      <td className="asset-align-right">{row.estimatedPrice}</td>
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
                      <td className="asset-table__cell asset-table__cell--actions">
                        <div className="purchase-orders-actions">
                          <Dropdown
                            menu={{ items: getActionMenu(row) }}
                            trigger={['click']}
                            placement="bottomRight"
                          >
                            <Button type="text" icon={<EyeOutlined />} size="small" />
                          </Dropdown>
                          {(row.status === 0 || row.status === 3) && (
                            <Button type="text" icon={<EditOutlined />} size="small" />
                          )}
                          <Button type="text" icon={<DeleteOutlined />} size="small" danger />
                        </div>
                      </td>
                    </tr>
                  );
                })}
              </tbody>
            </table>
          )}
        </div>

        <div className="purchase-orders-card__footer">
          <div className="purchase-orders-footer__left">
            Số lượng trên trang:
            <select
              className="purchase-orders-footer__select"
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
          <div className="purchase-orders-footer__center">
            {total === 0 ? '0-0 trên 0' : `${startIndex}-${endIndex} trên ${total}`}
          </div>
          <div className="purchase-orders-footer__right">
            <button
              className="purchase-orders-footer__pager"
              disabled={safePage <= 1}
              onClick={() => setPage((p) => Math.max(1, p - 1))}
              type="button"
            >
              ⟨
            </button>
            <button
              className="purchase-orders-footer__pager purchase-orders-footer__pager--active"
              type="button"
            >
              {safePage}
            </button>
            <button
              className="purchase-orders-footer__pager"
              disabled={safePage >= totalPages}
              onClick={() => setPage((p) => Math.min(totalPages, p + 1))}
              type="button"
            >
              ⟩
            </button>
          </div>
        </div>
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

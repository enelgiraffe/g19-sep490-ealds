import { useEffect, useMemo, useState } from 'react';
import { Button, Input, Select, Tabs, Dropdown, message } from 'antd';
import {
  DeleteOutlined,
  EditOutlined,
  EyeOutlined,
  FilterOutlined,
  SearchOutlined,
  SettingOutlined,
} from '@ant-design/icons';
import { CreatePurchaseOrderModal } from '../../purchase-orders/components/CreatePurchaseOrderModal';
import { ViewPurchaseOrderModal } from '../../purchase-orders/components/ViewPurchaseOrderModal';
import {
  purchaseOrderService,
  type PurchaseOrderDetail,
  type PurchaseOrderListItem,
} from '../../purchase-orders/services/purchaseOrderService';
import { profileService, type UserProfile } from '../../profile/services/profileService';
import {
  transferRequestService,
  type TransferRequestListItem,
} from '../../assets/services/transferRequestService';
import { TransferRequestDetailModal } from '../../transfers/components/TransferRequestDetailModal';
import type { MenuProps } from 'antd';
import './RequestsPage.css';

const { Option } = Select;

type ActiveTabKey = 'purchase' | 'transfer';

const PURCHASE_STATUS_MAP: Record<number, { label: string; color: string }> = {
  0: { label: 'Nháp', color: 'default' },
  1: { label: 'Duyệt', color: 'success' },
  2: { label: 'Từ chối', color: 'error' },
  3: { label: 'Chờ ngân sách', color: 'warning' },
};

const TRANSFER_STATUS_MAP: Record<number, { label: string; color: string }> = {
  0: { label: 'Nháp', color: 'default' },
  1: { label: 'Đã nộp', color: 'processing' },
  2: { label: 'Hợp lệ', color: 'success' },
  3: { label: 'Chờ phê duyệt', color: 'warning' },
  4: { label: 'Phê duyệt', color: 'success' },
};

interface PurchaseTableRow extends PurchaseOrderListItem {
  key: string;
  stt: number;
  code: string;
  requestDate: string;
  equipment: string;
  quantity: number;
  estimatedPrice: string;
}

interface TransferTableRow extends TransferRequestListItem {
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

function toPurchaseTableRow(item: PurchaseOrderListItem, index: number): PurchaseTableRow {
  let quantity = 1;
  let estimatedPrice = '—';
  try {
    if (item.proposedData) {
      const parsed = JSON.parse(item.proposedData) as {
        equipment?: { quantity?: number; estimatedPrice?: string }[];
        totalPrice?: string;
      };
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

function toTransferTableRow(item: TransferRequestListItem, index: number): TransferTableRow {
  return {
    ...item,
    key: String(item.recordId),
    stt: index + 1,
    transferDateText: formatDate(item.transferDate),
  };
}

export function RequestsPage() {
  const [activeTab, setActiveTab] = useState<ActiveTabKey>('purchase');

  const [purchaseRows, setPurchaseRows] = useState<PurchaseTableRow[]>([]);
  const [purchaseLoading, setPurchaseLoading] = useState(false);
  const [isCreatePurchaseOpen, setIsCreatePurchaseOpen] = useState(false);
  const [isViewPurchaseOpen, setIsViewPurchaseOpen] = useState(false);
  const [selectedPurchaseDetail, setSelectedPurchaseDetail] = useState<PurchaseOrderDetail | null>(
    null,
  );
  const [userProfile, setUserProfile] = useState<UserProfile | null>(null);

  const [transferRows, setTransferRows] = useState<TransferTableRow[]>([]);
  const [transferLoading, setTransferLoading] = useState(false);
  const [isTransferDetailOpen, setIsTransferDetailOpen] = useState(false);
  const [selectedTransfer, setSelectedTransfer] = useState<TransferTableRow | null>(null);

  const [statusFilter, setStatusFilter] = useState<number | 'all'>('all');
  const [searchText, setSearchText] = useState('');
  const [page, setPage] = useState(1);
  const [pageSize, setPageSize] = useState(25);

  useEffect(() => {
    const loadPurchase = async () => {
      setPurchaseLoading(true);
      try {
        const list = await purchaseOrderService.getList();
        setPurchaseRows(list.map((item, i) => toPurchaseTableRow(item, i)));
      } catch {
        message.error('Không tải được danh sách đơn mua.');
        setPurchaseRows([]);
      } finally {
        setPurchaseLoading(false);
      }
    };

    const loadTransfers = async () => {
      setTransferLoading(true);
      try {
        const list = await transferRequestService.getList();
        setTransferRows(list.map((item, i) => toTransferTableRow(item, i)));
      } catch {
        message.error('Không tải được danh sách yêu cầu điều chuyển.');
        setTransferRows([]);
      } finally {
        setTransferLoading(false);
      }
    };

    loadPurchase();
    loadTransfers();
  }, []);

  useEffect(() => {
    setPage(1);
  }, [activeTab, statusFilter, searchText]);

  const filteredPurchaseRows = useMemo(() => {
    const keyword = searchText.trim().toLowerCase();
    return purchaseRows.filter((row) => {
      const matchStatus = statusFilter === 'all' || row.status === statusFilter;
      const matchKeyword =
        !keyword ||
        row.title.toLowerCase().includes(keyword) ||
        row.code.toLowerCase().includes(keyword);
      return matchStatus && matchKeyword;
    });
  }, [purchaseRows, searchText, statusFilter]);

  const filteredTransferRows = useMemo(() => {
    const keyword = searchText.trim().toLowerCase();
    return transferRows.filter((row) => {
      const matchStatus = statusFilter === 'all' || row.status === statusFilter;
      const matchKeyword =
        !keyword ||
        row.code.toLowerCase().includes(keyword) ||
        row.assetCode.toLowerCase().includes(keyword) ||
        row.assetName.toLowerCase().includes(keyword);
      return matchStatus && matchKeyword;
    });
  }, [transferRows, searchText, statusFilter]);

  const isPurchaseTab = activeTab === 'purchase';
  const currentRows = isPurchaseTab ? filteredPurchaseRows : filteredTransferRows;
  const loading = isPurchaseTab ? purchaseLoading : transferLoading;

  const total = currentRows.length;
  const totalPages = Math.max(1, Math.ceil(total / pageSize));
  const safePage = Math.min(page, totalPages);
  const startIndex = total === 0 ? 0 : (safePage - 1) * pageSize + 1;
  const endIndex = Math.min(safePage * pageSize, total);
  const pagedRows = currentRows.slice((safePage - 1) * pageSize, safePage * pageSize);

  const handleOpenCreatePurchase = async () => {
    try {
      const profile = await profileService.getProfile();
      setUserProfile(profile);
      setIsCreatePurchaseOpen(true);
    } catch {
      message.error('Không lấy được thông tin người dùng.');
    }
  };

  const handleCloseCreatePurchase = () => {
    setIsCreatePurchaseOpen(false);
    setUserProfile(null);
  };

  const handleSubmitPurchaseOrder = async (payload: {
    title: string;
    description?: string;
    proposedData?: string;
  }) => {
    if (!userProfile) {
      message.error('Vui lòng đăng nhập lại.');
      return;
    }
    try {
      await purchaseOrderService.create({
        userId: userProfile.id,
        title: payload.title,
        description: payload.description ?? null,
        proposedData: payload.proposedData ?? null,
        createdBy: userProfile.id,
      });
      message.success('Tạo yêu cầu mua sắm thành công.');
      handleCloseCreatePurchase();
      const list = await purchaseOrderService.getList();
      setPurchaseRows(list.map((item, i) => toPurchaseTableRow(item, i)));
    } catch (e: unknown) {
      const err = e as { response?: { data?: string } };
      message.error(err?.response?.data ?? 'Tạo yêu cầu thất bại.');
    }
  };

  const handleViewPurchaseDetail = async (row: PurchaseTableRow) => {
    try {
      const detail = await purchaseOrderService.getById(row.assetRequestId);
      setSelectedPurchaseDetail(detail);
      setIsViewPurchaseOpen(true);
    } catch {
      message.error('Không tải được chi tiết đơn.');
    }
  };

  const purchaseActionMenu = (row: PurchaseTableRow): MenuProps['items'] => [
    {
      key: 'view',
      label: 'Xem',
      icon: <EyeOutlined />,
      onClick: () => handleViewPurchaseDetail(row),
    },
    ...(row.status === 0 || row.status === 3
      ? [
          {
            key: 'edit',
            label: 'Sửa',
            icon: <EditOutlined />,
            onClick: () => message.info('Chức năng sửa đang phát triển'),
          },
        ]
      : []),
    {
      key: 'delete',
      label: 'Xóa',
      icon: <DeleteOutlined />,
      danger: true,
      onClick: () => message.info('Chức năng xóa đang phát triển'),
    },
  ];

  const handleClickMainButton = () => {
    if (isPurchaseTab) {
      handleOpenCreatePurchase();
    } else {
      message.info('Vui lòng gửi yêu cầu điều chuyển từ màn Tài sản sau khi chọn tài sản cụ thể.');
    }
  };

  const renderStatusFilterOptions = () => {
    const map = isPurchaseTab ? PURCHASE_STATUS_MAP : TRANSFER_STATUS_MAP;
    return Object.entries(map).map(([k, v]) => (
      <Option key={k} value={Number(k)}>
        {v.label}
      </Option>
    ));
  };

  return (
    <div className="requests-page">
      <div className="requests-header">
        <h1 className="requests-title">Yêu cầu</h1>
        <Button type="primary" className="requests-btn-add" onClick={handleClickMainButton}>
          {isPurchaseTab ? '+ Tạo yêu cầu mua sắm' : '+ Tạo yêu cầu điều chuyển'}
        </Button>
      </div>

      <div className="requests-card">
        <Tabs
          activeKey={activeTab}
          onChange={(key) => setActiveTab(key as ActiveTabKey)}
          className="requests-tabs"
          items={[
            { key: 'purchase', label: 'Đơn mua' },
            { key: 'transfer', label: 'Điều chuyển' },
          ]}
        />

        <div className="requests-filters">
          <Input
            placeholder="Tìm kiếm"
            prefix={<SearchOutlined />}
            className="requests-search"
            value={searchText}
            onChange={(e) => setSearchText(e.target.value)}
          />
          <Select
            placeholder="Trạng thái"
            className="requests-select"
            suffixIcon={<FilterOutlined />}
            value={statusFilter}
            onChange={(v) => setStatusFilter(v)}
          >
            <Option value="all">Tất cả</Option>
            {renderStatusFilterOptions()}
          </Select>
          <Button
            icon={<FilterOutlined />}
            className="requests-filter-advanced"
            onClick={() => {
              setSearchText('');
              setStatusFilter('all');
            }}
          >
            Gỡ bộ lọc
          </Button>
          <Button icon={<SettingOutlined />} className="requests-settings" />
        </div>

        <div className="asset-table-wrapper requests-table-wrapper">
          {loading ? (
            <div className="requests-table-loading">
              {isPurchaseTab
                ? 'Đang tải danh sách đơn mua...'
                : 'Đang tải danh sách yêu cầu điều chuyển...'}
            </div>
          ) : isPurchaseTab ? (
            <table className="asset-table requests-table">
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
                {pagedRows.length === 0 ? (
                  <tr>
                    <td colSpan={9} className="requests-table-empty">
                      Không có dữ liệu.
                    </td>
                  </tr>
                ) : (
                  (pagedRows as PurchaseTableRow[]).map((row) => {
                    const config = PURCHASE_STATUS_MAP[row.status] ?? PURCHASE_STATUS_MAP[0];
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
                            onClick={() => handleViewPurchaseDetail(row)}
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
                          <div className="requests-actions">
                            <Dropdown
                              menu={{ items: purchaseActionMenu(row) }}
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
                  })
                )}
              </tbody>
            </table>
          ) : (
            <table className="asset-table requests-table">
              <thead>
                <tr>
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
                {pagedRows.length === 0 ? (
                  <tr>
                    <td colSpan={9} className="requests-table-empty">
                      Không có dữ liệu.
                    </td>
                  </tr>
                ) : (
                  (pagedRows as TransferTableRow[]).map((row) => {
                    const config = TRANSFER_STATUS_MAP[row.status] ?? TRANSFER_STATUS_MAP[0];
                    return (
                      <tr key={row.key} className="asset-row">
                        <td className="asset-align-right">{row.stt}</td>
                        <td>
                          <button
                            type="button"
                            className="asset-code asset-code--link"
                            onClick={() => {
                              setSelectedTransfer(row);
                              setIsTransferDetailOpen(true);
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
                              setSelectedTransfer(row);
                              setIsTransferDetailOpen(true);
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

        <div className="requests-card__footer">
          <div className="requests-footer__left">
            Số lượng trên trang:
            <select
              className="requests-footer__select"
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
          <div className="requests-footer__center">
            {total === 0 ? '0-0 trên 0' : `${startIndex}-${endIndex} trên ${total}`}
          </div>
          <div className="requests-footer__right">
            <button
              className="requests-footer__pager"
              disabled={safePage <= 1}
              onClick={() => setPage((p) => Math.max(1, p - 1))}
              type="button"
            >
              ⟨
            </button>
            <button
              className="requests-footer__pager requests-footer__pager--active"
              type="button"
            >
              {safePage}
            </button>
            <button
              className="requests-footer__pager"
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
        open={isCreatePurchaseOpen}
        onClose={handleCloseCreatePurchase}
        onSubmit={handleSubmitPurchaseOrder}
        creatorName={userProfile?.name ?? userProfile?.email ?? null}
      />

      <ViewPurchaseOrderModal
        open={isViewPurchaseOpen}
        onClose={() => {
          setIsViewPurchaseOpen(false);
          setSelectedPurchaseDetail(null);
        }}
        data={selectedPurchaseDetail}
      />

      <TransferRequestDetailModal
        open={isTransferDetailOpen}
        onClose={() => {
          setIsTransferDetailOpen(false);
          setSelectedTransfer(null);
        }}
        request={selectedTransfer}
      />
    </div>
  );
}


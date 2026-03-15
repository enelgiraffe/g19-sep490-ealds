import { useEffect, useMemo, useRef, useState } from 'react';
import { Button, DatePicker, Modal, Select, Tabs, message } from 'antd';
import { EyeOutlined } from '@ant-design/icons';
import { CreatePurchaseOrderModal } from '../../purchase-orders/components/CreatePurchaseOrderModal';
import { ViewPurchaseOrderModal } from '../../purchase-orders/components/ViewPurchaseOrderModal';
import {
  purchaseOrderService,
  type PurchaseOrderDetail,
} from '../../purchase-orders/services/purchaseOrderService';
import { profileService, type UserProfile } from '../../profile/services/profileService';
import {
  transferRequestService,
  type TransferRequestListItem,
} from '../../assets/services/transferRequestService';
import { TransferRequestDetailModal } from '../../transfers/components/TransferRequestDetailModal';
import {
  accountantRequestService,
  type AccountantRequestListItem,
} from '../services/accountantRequestService';
import {
  directorRequestService,
  REQUEST_TYPE_IDS,
  type DirectorRequestListItem,
} from '../services/directorRequestService';
import './RequestsPage.css';

const { Option } = Select;

type ActiveTabKey = 'purchase' | 'transfer' | 'maintenance' | 'repair' | 'liquidation';

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

/** Dùng chung cho tab Bảo dưỡng / Sửa chữa / Thanh lý (API director) */
const DIRECTOR_STATUS_MAP: Record<number, { label: string; color: string }> = {
  0: { label: 'Nháp', color: 'default' },
  1: { label: 'Đã nộp', color: 'processing' },
  2: { label: 'Từ chối', color: 'error' },
  3: { label: 'Chờ phê duyệt', color: 'warning' },
  4: { label: 'Phê duyệt', color: 'success' },
};

interface PurchaseTableRow {
  assetRequestId: number;
  title: string;
  status: number;
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

function toPurchaseTableRow(item: AccountantRequestListItem, index: number): PurchaseTableRow {
  return {
    key: String(item.assetRequestId),
    stt: index + 1,
    assetRequestId: item.assetRequestId,
    title: item.title,
    status: item.status,
    code: `YC-${item.assetRequestId}`,
    requestDate: formatDate(item.createDate),
    equipment: item.title,
    quantity: 1,
    estimatedPrice: '—',
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

  const [directorRows, setDirectorRows] = useState<DirectorRequestListItem[]>([]);
  const [directorTotal, setDirectorTotal] = useState(0);
  const [directorLoading, setDirectorLoading] = useState(false);
  const [selectedDirectorItem, setSelectedDirectorItem] = useState<DirectorRequestListItem | null>(
    null,
  );
  const [isDirectorDetailOpen, setIsDirectorDetailOpen] = useState(false);

  const [statusFilter, setStatusFilter] = useState<number | 'all'>('all');
  const [departmentFilter, setDepartmentFilter] = useState<string | 'all'>('all');
  const [sentDateFilter, setSentDateFilter] = useState<string | null>(null);
  const [searchText, setSearchText] = useState('');
  const [page, setPage] = useState(1);
  const [pageSize, setPageSize] = useState(25);

  useEffect(() => {
    const loadPurchase = async () => {
      setPurchaseLoading(true);
      try {
        const list = await accountantRequestService.getPurchaseRequests();
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

    const loadProfile = async () => {
      try {
        const profile = await profileService.getProfile();
        setUserProfile(profile);
      } catch {
        // ignore profile error here; will be handled on demand
      }
    };

    loadPurchase();
    loadTransfers();
    loadProfile();
  }, []);

  const isDirectorTab =
    activeTab === 'maintenance' || activeTab === 'repair' || activeTab === 'liquidation';
  const directorRequestTypeId =
    activeTab === 'maintenance'
      ? REQUEST_TYPE_IDS.maintenance
      : activeTab === 'repair'
        ? REQUEST_TYPE_IDS.repair
        : REQUEST_TYPE_IDS.liquidation;

  const prevDirectorTypeIdRef = useRef<number | null>(null);
  const effectiveDirectorPage =
    prevDirectorTypeIdRef.current !== directorRequestTypeId ? 1 : page;

  useEffect(() => {
    if (!isDirectorTab) return;
    if (prevDirectorTypeIdRef.current !== directorRequestTypeId) {
      setPage(1);
    }
    prevDirectorTypeIdRef.current = directorRequestTypeId;
    let cancelled = false;
    setDirectorLoading(true);
    directorRequestService
      .getView({
        requestTypeId: directorRequestTypeId,
        status: statusFilter === 'all' ? undefined : statusFilter,
        page: effectiveDirectorPage,
        pageSize,
      })
      .then((res) => {
        if (!cancelled) {
          const items = res.items as DirectorRequestListItem[];
          const byType = items.filter((item) => item.requestTypeId === directorRequestTypeId);
          setDirectorRows(byType);
          setDirectorTotal(res.total);
        }
      })
      .catch(() => {
        if (!cancelled) {
          message.error('Không tải được danh sách yêu cầu.');
          setDirectorRows([]);
          setDirectorTotal(0);
        }
      })
      .finally(() => {
        if (!cancelled) setDirectorLoading(false);
      });
    return () => {
      cancelled = true;
    };
  }, [isDirectorTab, directorRequestTypeId, statusFilter, effectiveDirectorPage, pageSize]);

  useEffect(() => {
    setPage(1);
  }, [activeTab, statusFilter, searchText, departmentFilter, sentDateFilter]);

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
      const matchDepartment =
        departmentFilter === 'all' ||
        row.fromDepartment.toLowerCase() === String(departmentFilter).toLowerCase();
      let matchDate = true;
      if (sentDateFilter) {
        try {
          const rowDate = new Date(row.transferDate).toISOString().slice(0, 10);
          matchDate = rowDate === sentDateFilter;
        } catch {
          matchDate = true;
        }
      }
      const matchKeyword =
        !keyword ||
        row.code.toLowerCase().includes(keyword) ||
        row.assetCode.toLowerCase().includes(keyword) ||
        row.assetName.toLowerCase().includes(keyword);
      return matchStatus && matchDepartment && matchDate && matchKeyword;
    });
  }, [transferRows, searchText, statusFilter, departmentFilter, sentDateFilter]);

  const departmentOptions = useMemo(
    () =>
      Array.from(new Set(transferRows.map((row) => row.fromDepartment)))
        .filter((name) => !!name)
        .sort(),
    [transferRows],
  );

  const isPurchaseTab = activeTab === 'purchase';
  const isTransferTab = activeTab === 'transfer';
  const hasDataTable = isPurchaseTab || isTransferTab || isDirectorTab;

  const currentRows = isPurchaseTab
    ? filteredPurchaseRows
    : isTransferTab
      ? filteredTransferRows
      : [];
  const loading = isPurchaseTab
    ? purchaseLoading
    : isTransferTab
      ? transferLoading
      : isDirectorTab
        ? directorLoading
        : false;

  const total = isDirectorTab ? directorTotal : currentRows.length;
  const totalPages = Math.max(1, Math.ceil(total / pageSize));
  const safePage = Math.min(page, totalPages);
  const startIndex = total === 0 ? 0 : (safePage - 1) * pageSize + 1;
  const endIndex = Math.min(safePage * pageSize, total);
  const pagedRows = isDirectorTab
    ? directorRows
    : currentRows.slice((safePage - 1) * pageSize, safePage * pageSize);

  const handleOpenCreatePurchase = async () => {
    if (!userProfile) {
      try {
        const profile = await profileService.getProfile();
        setUserProfile(profile);
      } catch {
        message.error('Không lấy được thông tin người dùng.');
        return;
      }
    }
    setIsCreatePurchaseOpen(true);
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
      const list = await accountantRequestService.getPurchaseRequests();
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
      </div>

      <div className="requests-card">
        <Tabs
          activeKey={activeTab}
          onChange={(key) => setActiveTab(key as ActiveTabKey)}
          className="requests-tabs"
          items={[
            { key: 'purchase', label: 'Đơn mua' },
            { key: 'transfer', label: 'Điều chuyển' },
            { key: 'maintenance', label: 'Bảo dưỡng' },
            { key: 'repair', label: 'Sửa chữa' },
            { key: 'liquidation', label: 'Thanh lý' },
          ]}
        />

        <div className="requests-filters">
          {isTransferTab && (
            <>
              <Select
                placeholder="Phòng ban đề xuất"
                className="requests-select"
                value={departmentFilter}
                onChange={(v) => setDepartmentFilter((v ?? 'all') as string | 'all')}
              >
                <Option value="all">Tất cả phòng ban</Option>
                {departmentOptions.map((name) => (
                  <Option key={name} value={name}>
                    {name}
                  </Option>
                ))}
              </Select>
              <Select
                placeholder="Trạng thái"
                className="requests-select"
                value={statusFilter}
                onChange={(v) => setStatusFilter(v)}
              >
                <Option value="all">Tất cả</Option>
                {renderStatusFilterOptions()}
              </Select>
              <DatePicker
                placeholder="Ngày gửi"
                className="requests-date-picker"
                onChange={(_, dateString) => {
                  setSentDateFilter(dateString || null);
                }}
              />
            </>
          )}
          {isDirectorTab && (
            <Select
              placeholder="Trạng thái"
              className="requests-select"
              value={statusFilter}
              onChange={(v) => setStatusFilter(v)}
            >
              <Option value="all">Tất cả</Option>
              {Object.entries(DIRECTOR_STATUS_MAP).map(([k, v]) => (
                <Option key={k} value={Number(k)}>
                  {v.label}
                </Option>
              ))}
            </Select>
          )}
        </div>

        <div className="asset-table-wrapper requests-table-wrapper">
          {!hasDataTable ? (
            <div className="requests-table-loading">
              Chức năng đang được phát triển cho tab này.
            </div>
          ) : loading ? (
            <div className="requests-table-loading">
              {isPurchaseTab
                ? 'Đang tải danh sách đơn mua...'
                : isTransferTab
                  ? 'Đang tải danh sách yêu cầu điều chuyển...'
                  : 'Đang tải danh sách yêu cầu...'}
            </div>
          ) : isDirectorTab ? (
            <table className="asset-table requests-table">
              <thead>
                <tr>
                  <th>MÃ YÊU CẦU</th>
                  <th>PHÒNG BAN ĐỀ XUẤT</th>
                  <th>NGÀY GỬI</th>
                  <th>MÃ TÀI SẢN</th>
                  <th>TÊN TÀI SẢN</th>
                  <th>TRẠNG THÁI</th>
                  <th className="asset-table__cell asset-table__cell--actions" />
                </tr>
              </thead>
              <tbody>
                {pagedRows.length === 0 ? (
                  <tr>
                    <td colSpan={7} className="requests-table-empty">
                      Không có dữ liệu.
                    </td>
                  </tr>
                ) : (
                  (pagedRows as DirectorRequestListItem[]).map((row) => {
                    const config = DIRECTOR_STATUS_MAP[row.status] ?? DIRECTOR_STATUS_MAP[0];
                    return (
                      <tr key={row.assetRequestId} className="asset-row">
                        <td>
                          <button
                            type="button"
                            className="asset-code asset-code--link"
                            onClick={() => {
                              setSelectedDirectorItem(row);
                              setIsDirectorDetailOpen(true);
                            }}
                          >
                            YC-{row.assetRequestId}
                          </button>
                        </td>
                        <td>{row.currentDepartmentName ?? row.creatorEmail ?? '—'}</td>
                        <td>{formatDate(row.createDate)}</td>
                        <td>{row.assetCode ?? '—'}</td>
                        <td>{row.assetName ?? row.title ?? '—'}</td>
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
                          <Button
                            type="text"
                            icon={<EyeOutlined />}
                            onClick={() => {
                              setSelectedDirectorItem(row);
                              setIsDirectorDetailOpen(true);
                            }}
                          />
                        </td>
                      </tr>
                    );
                  })
                )}
              </tbody>
            </table>
          ) : isPurchaseTab ? (
            <table className="asset-table requests-table">
              <thead>
                <tr>
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
                    <td colSpan={7} className="requests-table-empty">
                      Không có dữ liệu.
                    </td>
                  </tr>
                ) : (
                  (pagedRows as PurchaseTableRow[]).map((row) => {
                    const config = PURCHASE_STATUS_MAP[row.status] ?? PURCHASE_STATUS_MAP[0];
                    return (
                      <tr key={row.key} className="asset-row">
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
                            <Button
                              type="text"
                              icon={<EyeOutlined />}
                              size="small"
                              onClick={() => handleViewPurchaseDetail(row)}
                            >
                              Xem
                            </Button>
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
                  <th>MÃ YÊU CẦU</th>
                  <th>PHÒNG BAN ĐỀ XUẤT</th>
                  <th>NGÀY GỬI</th>
                  <th>ĐƠN VỊ CHUYỂN</th>
                  <th>ĐƠN VỊ NHẬN</th>
                  <th>TRẠNG THÁI</th>
                  <th className="asset-table__cell asset-table__cell--actions" />
                </tr>
              </thead>
              <tbody>
                {pagedRows.length === 0 ? (
                  <tr>
                    <td colSpan={7} className="requests-table-empty">
                      Không có dữ liệu.
                    </td>
                  </tr>
                ) : (
                  (pagedRows as TransferTableRow[]).map((row) => {
                    const config = TRANSFER_STATUS_MAP[row.status] ?? TRANSFER_STATUS_MAP[0];
                    return (
                      <tr key={row.key} className="asset-row">
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
                        <td>{row.fromDepartment}</td>
                        <td>{row.transferDateText}</td>
                        <td>{row.fromDepartment}</td>
                        <td>{row.toDepartment}</td>
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
        currentUserId={userProfile?.id ?? null}
      />

      <TransferRequestDetailModal
        open={isTransferDetailOpen}
        onClose={() => {
          setIsTransferDetailOpen(false);
          setSelectedTransfer(null);
        }}
        request={selectedTransfer}
      />

      <Modal
        title={`Chi tiết yêu cầu YC-${selectedDirectorItem?.assetRequestId ?? ''}`}
        open={isDirectorDetailOpen}
        onCancel={() => {
          setIsDirectorDetailOpen(false);
          setSelectedDirectorItem(null);
        }}
        footer={null}
        width={520}
      >
        {selectedDirectorItem && (
          <div className="requests-director-detail">
            <p><strong>Tiêu đề:</strong> {selectedDirectorItem.title}</p>
            {selectedDirectorItem.description && (
              <p><strong>Mô tả:</strong> {selectedDirectorItem.description}</p>
            )}
            <p><strong>Ngày gửi:</strong> {formatDate(selectedDirectorItem.createDate)}</p>
            <p><strong>Trạng thái:</strong>{' '}
              {(DIRECTOR_STATUS_MAP[selectedDirectorItem.status] ?? DIRECTOR_STATUS_MAP[0]).label}
            </p>
            {(selectedDirectorItem.assetCode || selectedDirectorItem.assetName) && (
              <p><strong>Tài sản:</strong>{' '}
                {[selectedDirectorItem.assetCode, selectedDirectorItem.assetName].filter(Boolean).join(' - ')}
              </p>
            )}
            {(selectedDirectorItem.currentDepartmentName || selectedDirectorItem.creatorEmail) && (
              <p><strong>Phòng ban / Người tạo:</strong>{' '}
                {selectedDirectorItem.currentDepartmentName ?? selectedDirectorItem.creatorEmail}
              </p>
            )}
          </div>
        )}
      </Modal>
    </div>
  );
}


import { useCallback, useEffect, useState, type ComponentProps } from 'react';
import { Button, Input, Select, message } from 'antd';
import { PlusOutlined, SearchOutlined, FilterOutlined, SettingOutlined, EyeOutlined, EditOutlined } from '@ant-design/icons';
import {
  procurementPoService,
  PO_STATUS,
  type PurchaseOrderDetail,
  type PurchaseOrderListItem,
} from '../services/procurementPoService';
import { PurchaseOrderFormModalNew } from '../components/PurchaseOrderFormModalNew';
import { PurchaseOrderDetailModal } from '../components/PurchaseOrderDetailModal';
import { supplierService, type SupplierItem } from '../../admin/services/supplierService';
import './PurchaseOrdersPage.css';

function statusTag(status: number) {
  if (status === PO_STATUS.draft) return { label: 'Nháp', color: 'gray' as const };
  if (status === PO_STATUS.cancelled) return { label: 'Đã hủy', color: 'red' as const };
  if (status === PO_STATUS.partiallyReceived) return { label: 'Nhận một phần', color: 'orange' as const };
  if (status === PO_STATUS.completed) return { label: 'Đã nhận đủ', color: 'green' as const };
  return { label: 'Đã tạo', color: 'blue' as const };
}

export function PurchaseOrdersPage() {
  const [loading, setLoading] = useState(false);
  const [items, setItems] = useState<PurchaseOrderListItem[]>([]);
  const [total, setTotal] = useState(0);
  const [page, setPage] = useState(1);
  const [pageSize, setPageSize] = useState(20);
  const [filterId, setFilterId] = useState<string>('');
  const [filterSupplierId, setFilterSupplierId] = useState<number | 'all'>('all');
  const [filterStatus, setFilterStatus] = useState<number | 'all'>('all');
  const [suppliers, setSuppliers] = useState<SupplierItem[]>([]);

  const [detailOpen, setDetailOpen] = useState(false);
  const [selected, setSelected] = useState<PurchaseOrderDetail | null>(null);

  const [formOpen, setFormOpen] = useState(false);
  const [formMode, setFormMode] = useState<'create' | 'edit'>('create');

  const loadList = useCallback(async () => {
    setLoading(true);
    try {
      const parsedId = filterId.trim() ? parseInt(filterId.trim(), 10) : undefined;
      const res = await procurementPoService.getList({
        procurementId: parsedId && !isNaN(parsedId) ? parsedId : undefined,
        supplierId: filterSupplierId === 'all' ? undefined : filterSupplierId,
        status: filterStatus === 'all' ? undefined : filterStatus,
        page,
        pageSize,
      });
      setItems(res.items);
      setTotal(res.total);
    } catch {
      message.error('Không tải được danh sách đơn mua (cần quyền kế toán).');
      setItems([]);
    } finally {
      setLoading(false);
    }
  }, [filterId, filterSupplierId, filterStatus, page, pageSize]);

  useEffect(() => {
    loadList();
  }, [loadList]);

  useEffect(() => {
    let c = false;
    (async () => {
      try {
        const s = await supplierService.getAll();
        if (!c) setSuppliers(s);
      } catch {
        if (!c) setSuppliers([]);
      }
    })();
    return () => {
      c = true;
    };
  }, []);

  const openDetail = async (id: number) => {
    try {
      const d = await procurementPoService.getById(id);
      setSelected(d);
      setDetailOpen(true);
    } catch {
      message.error('Không tải được chi tiết.');
    }
  };

  const handleCreateSubmit = async (
    payload: Parameters<ComponentProps<typeof PurchaseOrderFormModalNew>['onSubmit']>[0],
  ) => {
    await procurementPoService.create({
      supplierId: payload.supplierId,
      currency: payload.currency,
      assetRequestId: payload.assetRequestId,
      lines: payload.lines,
      isDraft: payload.isDraft,
    });
    setFormOpen(false);
    await loadList();
  };

  const handleEditSubmit = async (
    payload: Parameters<ComponentProps<typeof PurchaseOrderFormModalNew>['onSubmit']>[0],
  ) => {
    if (!selected) return;
    await procurementPoService.update(selected.procurementId, {
      supplierId: payload.supplierId,
      currency: payload.currency,
      assetRequestId: payload.assetRequestId,
      lines: payload.lines,
      isDraft: payload.isDraft,
    });
    setFormOpen(false);
    setDetailOpen(false);
    setSelected(null);
    await loadList();
  };

  return (
    <div className="purchase-orders-page">
      <div className="purchase-orders-header">
        <h1 className="purchase-orders-title">Đơn mua</h1>
        <Button type="primary" icon={<PlusOutlined />} onClick={() => {
          setFormMode('create');
          setFormOpen(true);
        }}>
          Tạo đơn mua
        </Button>
      </div>

      <div className="purchase-orders-card">
        <div className="purchase-orders-filters">
          <Input
            placeholder="Tìm kiếm mã đơn"
            prefix={<SearchOutlined />}
            value={filterId}
            onChange={(e) => {
              setFilterId(e.target.value);
              setPage(1);
            }}
            onPressEnter={() => loadList()}
            className="purchase-orders-search"
            allowClear
          />
          <Select
            placeholder="Nhà cung cấp"
            allowClear
            className="purchase-orders-select"
            value={filterSupplierId === 'all' ? undefined : filterSupplierId}
            onChange={(v) => {
              setFilterSupplierId(v ?? 'all');
              setPage(1);
            }}
            options={[
              ...suppliers.map((s) => ({
                value: s.supplierId,
                label: `${s.code} — ${s.name}`,
              })),
            ]}
          />
          <Select
            placeholder="Trạng thái"
            className="purchase-orders-select"
            value={filterStatus === 'all' ? undefined : filterStatus}
            onChange={(v) => {
              setFilterStatus(v ?? 'all');
              setPage(1);
            }}
            options={[
              { value: PO_STATUS.draft, label: 'Nháp' },
              { value: PO_STATUS.created, label: 'Đã tạo' },
              { value: PO_STATUS.partiallyReceived, label: 'Nhận một phần' },
              { value: PO_STATUS.completed, label: 'Đã nhận đủ' },
              { value: PO_STATUS.cancelled, label: 'Đã hủy' },
            ]}
          />
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
                  <th>MÃ ĐƠN</th>
                  <th>SỐ CHỨNG TỪ</th>
                  <th>TIÊU ĐỀ</th>
                  <th>NHÀ CUNG CẤP</th>
                  <th className="asset-align-right">TỔNG TIỀN</th>
                  <th>TRẠNG THÁI</th>
                  <th>NGÀY TẠO</th>
                  <th className="asset-table__cell asset-table__cell--actions" />
                </tr>
              </thead>
              <tbody>
                {items.map((item) => {
                  const tag = statusTag(item.status);
                  return (
                    <tr key={item.procurementId} className="asset-row">
                      <td>
                        <button
                          type="button"
                          className="asset-code asset-code--link"
                          onClick={() => openDetail(item.procurementId)}
                        >
                          {item.procurementId}
                        </button>
                      </td>
                      <td>{item.contractNo}</td>
                      <td>{item.title}</td>
                      <td>{item.supplierName ?? `ID ${item.supplierId}`}</td>
                      <td className="asset-align-right">
                        {Number(item.totalAmount).toLocaleString('vi-VN')} {item.currency}
                      </td>
                      <td>
                        <span
                          className={
                            tag.color === 'gray'
                              ? 'asset-status-pill asset-status-pill--inactive'
                              : tag.color === 'green'
                                ? 'asset-status-pill asset-status-pill--active'
                                : tag.color === 'red'
                                  ? 'asset-status-pill asset-status-pill--danger'
                                  : tag.color === 'orange'
                                    ? 'asset-status-pill asset-status-pill--warning'
                                    : 'asset-status-pill asset-status-pill--processing'
                          }
                        >
                          {tag.label}
                        </span>
                      </td>
                      <td>{new Date(item.createDate).toLocaleDateString('vi-VN')}</td>
                      <td className="asset-table__cell asset-table__cell--actions">
                        <div className="purchase-orders-actions">
                          <Button
                            type="text"
                            icon={<EyeOutlined />}
                            size="small"
                            onClick={() => openDetail(item.procurementId)}
                            title="Xem chi tiết"
                          />
                          {(item.status === PO_STATUS.created || item.status === PO_STATUS.draft) && (
                            <Button
                              type="text"
                              icon={<EditOutlined />}
                              size="small"
                              onClick={async () => {
                                try {
                                  const detail = await procurementPoService.getById(item.procurementId);
                                  setSelected(detail);
                                  setFormMode('edit');
                                  setFormOpen(true);
                                } catch {
                                  message.error('Không tải được chi tiết đơn mua.');
                                }
                              }}
                              title="Chỉnh sửa"
                            />
                          )}
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
                setPageSize(Number(e.target.value));
                setPage(1);
              }}
            >
              <option value={10}>10</option>
              <option value={20}>20</option>
              <option value={50}>50</option>
              <option value={100}>100</option>
            </select>
          </div>
          <div className="purchase-orders-footer__center">
            {total === 0 ? '0-0 trên 0' : `${(page - 1) * pageSize + 1}-${Math.min(page * pageSize, total)} trên ${total}`}
          </div>
          <div className="purchase-orders-footer__right">
            <button
              className="purchase-orders-footer__pager"
              disabled={page <= 1}
              onClick={() => setPage((p) => Math.max(1, p - 1))}
              type="button"
            >
              ⟨
            </button>
            <button
              className="purchase-orders-footer__pager purchase-orders-footer__pager--active"
              type="button"
            >
              {page}
            </button>
            <button
              className="purchase-orders-footer__pager"
              disabled={page >= Math.ceil(total / pageSize)}
              onClick={() => setPage((p) => p + 1)}
              type="button"
            >
              ⟩
            </button>
          </div>
        </div>
      </div>

      <PurchaseOrderDetailModal
        open={detailOpen}
        data={selected}
        onClose={() => {
          setDetailOpen(false);
          setSelected(null);
        }}
        onEdit={() => {
          if (!selected || selected.status !== PO_STATUS.created) return;
          setFormMode('edit');
          setFormOpen(true);
        }}
        onCancelOrder={async () => {
          if (!selected) return;
          await procurementPoService.cancel(selected.procurementId);
          await loadList();
          const d = await procurementPoService.getById(selected.procurementId);
          setSelected(d);
        }}
      />

      <PurchaseOrderFormModalNew
        open={formOpen}
        mode={formMode}
        initial={formMode === 'edit' ? selected : null}
        onClose={() => setFormOpen(false)}
        onSubmit={async (payload) => {
          if (formMode === 'edit') await handleEditSubmit(payload);
          else await handleCreateSubmit(payload);
        }}
      />
    </div>
  );
}

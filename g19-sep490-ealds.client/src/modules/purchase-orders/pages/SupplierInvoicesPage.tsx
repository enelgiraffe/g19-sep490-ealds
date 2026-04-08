import { useCallback, useEffect, useState } from 'react';
import { Button, DatePicker, Input, Modal, Select, message } from 'antd';
import { PlusOutlined, SearchOutlined, EyeOutlined } from '@ant-design/icons';
import dayjs, { type Dayjs } from 'dayjs';
import {
  SUPPLIER_INVOICE_STATUS,
  supplierInvoiceService,
  type SupplierInvoiceDetail,
  type SupplierInvoiceListItem,
} from '../services/supplierInvoiceService';
import { supplierService, type SupplierItem } from '../../admin/services/supplierService';
import { SupplierInvoiceFormModal } from '../components/SupplierInvoiceFormModal';
import { SupplierInvoiceDetailModal } from '../components/SupplierInvoiceDetailModal';
import './SupplierInvoicesPage.css';

function formatDate(iso: string): string {
  try {
    return dayjs(iso).format('DD/MM/YYYY');
  } catch {
    return iso;
  }
}

export function SupplierInvoicesPage() {
  const [loading, setLoading] = useState(false);
  const [items, setItems] = useState<SupplierInvoiceListItem[]>([]);
  const [total, setTotal] = useState(0);
  const [page, setPage] = useState(1);
  const [pageSize, setPageSize] = useState(25);
  const [filterInvoiceNo, setFilterInvoiceNo] = useState('');
  const [filterSupplierId, setFilterSupplierId] = useState<number | 'all'>('all');
  const [filterDateFrom, setFilterDateFrom] = useState<Dayjs | null>(null);
  const [filterDateTo, setFilterDateTo] = useState<Dayjs | null>(null);
  const [suppliers, setSuppliers] = useState<SupplierItem[]>([]);

  const [detailOpen, setDetailOpen] = useState(false);
  const [detail, setDetail] = useState<SupplierInvoiceDetail | null>(null);
  const [detailLoading, setDetailLoading] = useState(false);

  const [createOpen, setCreateOpen] = useState(false);

  const loadList = useCallback(async () => {
    setLoading(true);
    try {
      const res = await supplierInvoiceService.getList({
        invoiceNumber: filterInvoiceNo.trim() || undefined,
        supplierId: filterSupplierId === 'all' ? undefined : filterSupplierId,
        dateFrom: filterDateFrom?.startOf('day').toISOString(),
        dateTo: filterDateTo?.endOf('day').toISOString(),
        page,
        pageSize,
      });
      setItems(res.items);
      setTotal(res.total);
    } catch {
      message.error('Không tải được danh sách hóa đơn NCC.');
      setItems([]);
    } finally {
      setLoading(false);
    }
  }, [filterInvoiceNo, filterSupplierId, filterDateFrom, filterDateTo, page, pageSize]);

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
    setDetailLoading(true);
    setDetailOpen(true);
    setDetail(null);
    try {
      const d = await supplierInvoiceService.getById(id);
      setDetail(d);
    } catch {
      message.error('Không tải được chi tiết hóa đơn.');
      setDetailOpen(false);
    } finally {
      setDetailLoading(false);
    }
  };

  const handleCreateSubmit = async (values: {
    procurementId: number;
    goodsReceiptId: number | null;
    supplierId: number;
    invoiceNumber: string;
    invoiceDate: string;
    note: string | null;
    lines: Array<{
      procurementLineId: number;
      goodsReceiptLineId?: number;
      quantity: number;
      unitPrice: number;
    }>;
  }) => {
    try {
      await supplierInvoiceService.create(values);
      message.success('Đã tạo hóa đơn nhà cung cấp.');
      setCreateOpen(false);
      await loadList();
    } catch (e: unknown) {
      const msg =
        typeof e === 'object' && e !== null && 'response' in e
          ? String((e as { response?: { data?: unknown } }).response?.data)
          : 'Không tạo được hóa đơn.';
      message.error(msg.length > 200 ? 'Không tạo được hóa đơn.' : msg);
      throw e;
    }
  };

  const confirmCancel = (id: number) => {
    Modal.confirm({
      title: 'Hủy hóa đơn?',
      content: 'Trạng thái sẽ chuyển sang Đã hủy.',
      okText: 'Hủy hóa đơn',
      okType: 'danger',
      cancelText: 'Đóng',
      onOk: async () => {
        try {
          await supplierInvoiceService.cancel(id);
          message.success('Đã cập nhật trạng thái.');
          setDetailOpen(false);
          await loadList();
        } catch {
          message.error('Không hủy được hóa đơn.');
        }
      },
    });
  };


  const totalPages = Math.max(1, Math.ceil(total / pageSize));
  const safePage = Math.min(page, totalPages);
  const startIndex = total === 0 ? 0 : (safePage - 1) * pageSize + 1;
  const endIndex = Math.min(safePage * pageSize, total);

  return (
    <div className="supplier-invoices-page">
      <div className="supplier-invoices-header">
        <h1 className="supplier-invoices-title">Hóa đơn nhà cung cấp</h1>
        <Button
          type="primary"
          icon={<PlusOutlined />}
          onClick={() => setCreateOpen(true)}
          className="supplier-invoices-btn-add"
        >
          Tạo hóa đơn
        </Button>
      </div>

      <div className="supplier-invoices-card">
        <div className="supplier-invoices-filters">
          <Input
            allowClear
            placeholder="Tìm kiếm số hóa đơn"
            value={filterInvoiceNo}
            onChange={(e) => {
              setFilterInvoiceNo(e.target.value);
              setPage(1);
            }}
            className="supplier-invoices-search"
            prefix={<SearchOutlined />}
          />
          <Select
            placeholder="Nhà cung cấp"
            allowClear
            className="supplier-invoices-select"
            value={filterSupplierId === 'all' ? undefined : filterSupplierId}
            onChange={(v) => {
              setFilterSupplierId(v ?? 'all');
              setPage(1);
            }}
            options={suppliers.map((s) => ({ value: s.supplierId, label: s.name }))}
          />
          <DatePicker
            placeholder="Từ ngày"
            value={filterDateFrom}
            onChange={(d) => {
              setFilterDateFrom(d);
              setPage(1);
            }}
            className="supplier-invoices-date-picker"
          />
          <DatePicker
            placeholder="Đến ngày"
            value={filterDateTo}
            onChange={(d) => {
              setFilterDateTo(d);
              setPage(1);
            }}
            className="supplier-invoices-date-picker"
          />
        </div>

        <div className="asset-table-wrapper supplier-invoices-table-wrapper">
          {loading ? (
            <div className="supplier-invoices-table-loading">Đang tải danh sách hóa đơn...</div>
          ) : (
            <table className="asset-table supplier-invoices-table">
              <thead>
                <tr>
                  <th>MÃ HÓA ĐƠN</th>
                  <th>SỐ HÓA ĐƠN</th>
                  <th>NHÀ CUNG CẤP</th>
                  <th>NGÀY HÓA ĐƠN</th>
                  <th>TỔNG TIỀN</th>
                  <th>ĐƠN MUA</th>
                  <th>BIÊN NHẬN</th>
                  <th>TRẠNG THÁI</th>
                  <th className="asset-table__cell asset-table__cell--actions" />
                </tr>
              </thead>
              <tbody>
                {items.length === 0 ? (
                  <tr>
                    <td colSpan={9} className="supplier-invoices-table-empty">
                      Không có dữ liệu.
                    </td>
                  </tr>
                ) : (
                  items.map((item) => {
                    const statusConfig =
                      item.status === SUPPLIER_INVOICE_STATUS.cancelled
                        ? { label: 'Đã hủy', color: 'inactive' }
                        : { label: 'Hiệu lực', color: 'active' };
                    return (
                      <tr key={item.supplierInvoiceId} className="asset-row">
                        <td>
                          <button
                            type="button"
                            className="asset-code asset-code--link"
                            onClick={() => openDetail(item.supplierInvoiceId)}
                          >
                            #{item.supplierInvoiceId}
                          </button>
                        </td>
                        <td>{item.invoiceNumber}</td>
                        <td>{item.supplierName || '—'}</td>
                        <td>{formatDate(item.invoiceDate)}</td>
                        <td className="asset-align-right">
                          {Number(item.totalAmount).toLocaleString('vi-VN')}
                        </td>
                        <td>#{item.procurementId}</td>
                        <td>{item.goodsReceiptId ? `#${item.goodsReceiptId}` : '—'}</td>
                        <td>
                          <span
                            className={
                              statusConfig.color === 'active'
                                ? 'asset-status-pill asset-status-pill--active'
                                : 'asset-status-pill asset-status-pill--inactive'
                            }
                          >
                            {statusConfig.label}
                          </span>
                        </td>
                        <td className="asset-table__cell asset-table__cell--actions">
                          <Button
                            type="text"
                            icon={<EyeOutlined />}
                            onClick={() => openDetail(item.supplierInvoiceId)}
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

        <div className="supplier-invoices-card__footer">
          <div className="supplier-invoices-footer__left">
            Số lượng trên trang:
            <select
              className="supplier-invoices-footer__select"
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
          <div className="supplier-invoices-footer__center">
            {total === 0 ? '0-0 trên 0' : `${startIndex}-${endIndex} trên ${total}`}
          </div>
          <div className="supplier-invoices-footer__right">
            <button
              className="supplier-invoices-footer__pager"
              disabled={safePage <= 1}
              onClick={() => setPage((p) => Math.max(1, p - 1))}
              type="button"
            >
              ⟨
            </button>
            <button
              className="supplier-invoices-footer__pager supplier-invoices-footer__pager--active"
              type="button"
            >
              {safePage}
            </button>
            <button
              className="supplier-invoices-footer__pager"
              disabled={safePage >= totalPages}
              onClick={() => setPage((p) => Math.min(totalPages, p + 1))}
              type="button"
            >
              ⟩
            </button>
          </div>
        </div>
      </div>

      <SupplierInvoiceFormModal
        open={createOpen}
        onClose={() => setCreateOpen(false)}
        onSubmit={handleCreateSubmit}
      />

      <SupplierInvoiceDetailModal
        open={detailOpen}
        loading={detailLoading}
        detail={detail}
        onClose={() => setDetailOpen(false)}
        onCancel={confirmCancel}
      />
    </div>
  );
}

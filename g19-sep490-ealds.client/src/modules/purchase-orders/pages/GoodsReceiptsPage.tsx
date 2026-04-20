import { useCallback, useEffect, useState } from 'react';
import { Button, Input, Select, DatePicker, message, InputNumber } from 'antd';
import { SearchOutlined, FilterOutlined, SettingOutlined, EyeOutlined, PlusOutlined } from '@ant-design/icons';
import dayjs, { type Dayjs } from 'dayjs';
import {
  goodsReceiptService,
  type GoodsReceiptDetail,
  type GoodsReceiptListItem,
} from '../services/goodsReceiptService';
import { supplierService, type SupplierItem } from '../../admin/services/supplierService';
import { GoodsReceiptFormModal } from '../components/GoodsReceiptFormModal';
import { GoodsReceiptDetailModal } from '../components/GoodsReceiptDetailModal';
import { PrintQRLabelsModal } from '../components/PrintQRLabelsModal';
import './GoodsReceiptsPage.css';

export function GoodsReceiptsPage() {
  const [loading, setLoading] = useState(false);
  const [items, setItems] = useState<GoodsReceiptListItem[]>([]);
  const [total, setTotal] = useState(0);
  const [page, setPage] = useState(1);
  const [pageSize, setPageSize] = useState(20);
  const [filterGrId, setFilterGrId] = useState<number | null>(null);
  const [filterPoId, setFilterPoId] = useState<number | null>(null);
  const [filterSupplierId, setFilterSupplierId] = useState<number | 'all'>('all');
  const [filterDateFrom, setFilterDateFrom] = useState<Dayjs | null>(null);
  const [filterDateTo, setFilterDateTo] = useState<Dayjs | null>(null);
  const [suppliers, setSuppliers] = useState<SupplierItem[]>([]);

  const [detailOpen, setDetailOpen] = useState(false);
  const [detail, setDetail] = useState<GoodsReceiptDetail | null>(null);

  const [createOpen, setCreateOpen] = useState(false);
  
  const [printQROpen, setPrintQROpen] = useState(false);
  const [printQRId, setPrintQRId] = useState<number | null>(null);

  const loadList = useCallback(async () => {
    setLoading(true);
    try {
      const res = await goodsReceiptService.getList({
        goodsReceiptId: filterGrId ?? undefined,
        procurementId: filterPoId ?? undefined,
        supplierId: filterSupplierId === 'all' ? undefined : filterSupplierId,
        dateFrom: filterDateFrom?.startOf('day').toISOString(),
        dateTo: filterDateTo?.endOf('day').toISOString(),
        page,
        pageSize,
      });
      setItems(res.items);
      setTotal(res.total);
    } catch {
      message.error('Không tải được danh sách biên nhận (cần quyền kế toán).');
      setItems([]);
    } finally {
      setLoading(false);
    }
  }, [filterGrId, filterPoId, filterSupplierId, filterDateFrom, filterDateTo, page, pageSize]);

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
      const d = await goodsReceiptService.getById(id);
      setDetail(d);
      setDetailOpen(true);
    } catch {
      message.error('Không tải được chi tiết biên nhận.');
    }
  };

  const handleCreateSubmit = async (payload: {
    procurementId: number;
    warehouseId: number;
    postingDate: string;
    note: string | null;
    attachmentFileUrls?: string[];
    lines: {
      procurementLineId: number;
      quantityReceived: number;
      assetId: number;
      instanceSerialNumbers?: (string | null)[] | null;
      instanceCodes?: (string | null)[] | null;
    }[];
  }) => {
    const result = await goodsReceiptService.create(payload);
    // Mở modal in QR
    if (result && result.goodsReceiptId) {
      setPrintQRId(result.goodsReceiptId);
      setPrintQROpen(true);
    }
    await loadList();
  };

  const handlePrintLabels = (goodsReceiptId: number) => {
    setPrintQRId(goodsReceiptId);
    setPrintQROpen(true);
    setDetailOpen(false);
  };


  return (
    <div className="goods-receipts-page">
      <div className="goods-receipts-header">
        <h1 className="goods-receipts-title">Biên nhận hàng</h1>
        <Button
          type="primary"
          icon={<PlusOutlined />}
          className="goods-receipts-btn-add"
          onClick={() => setCreateOpen(true)}
        >
          Tạo biên nhận
        </Button>
      </div>

      <div className="goods-receipts-card">
        <div className="goods-receipts-filters">
          <InputNumber
            min={1}
            placeholder="Mã biên nhận"
            value={filterGrId ?? undefined}
            onChange={(v) => {
              setFilterGrId(v ?? null);
              setPage(1);
            }}
            style={{ width: 140 }}
          />
          <InputNumber
            min={1}
            placeholder="Mã đơn mua"
            value={filterPoId ?? undefined}
            onChange={(v) => {
              setFilterPoId(v ?? null);
              setPage(1);
            }}
            style={{ width: 140 }}
          />
          <Select
            placeholder="Nhà cung cấp"
            allowClear
            style={{ width: 220 }}
            value={filterSupplierId === 'all' ? undefined : filterSupplierId}
            onChange={(v) => {
              setFilterSupplierId(v ?? 'all');
              setPage(1);
            }}
            options={suppliers.map((s) => ({
              value: s.supplierId,
              label: `${s.code} — ${s.name}`,
            }))}
          />
          <DatePicker
            placeholder="Từ ngày"
            value={filterDateFrom}
            onChange={(d) => {
              setFilterDateFrom(d);
              setPage(1);
            }}
            style={{ width: 140 }}
          />
          <DatePicker
            placeholder="Đến ngày"
            value={filterDateTo}
            onChange={(d) => {
              setFilterDateTo(d);
              setPage(1);
            }}
            style={{ width: 140 }}
          />
          <Button icon={<SearchOutlined />} onClick={() => loadList()}>
            Làm mới
          </Button>
        </div>

        <div className="asset-table-wrapper" style={{ marginTop: 16 }}>
          <table className="asset-table">
            <thead>
              <tr>
                <th style={{ width: '60px' }}>STT</th>
                <th style={{ width: '120px' }}>Mã BN</th>
                <th style={{ width: '100px' }}>Đơn mua</th>
                <th style={{ width: '150px' }}>Số chứng từ</th>
                <th>Nhà cung cấp</th>
                <th style={{ width: '100px', textAlign: 'right' }}>SL nhận</th>
                <th style={{ width: '120px' }}>Ngày tạo</th>
                <th style={{ width: '80px', textAlign: 'center' }}>Thao tác</th>
              </tr>
            </thead>
            <tbody>
              {loading ? (
                <tr>
                  <td colSpan={8} style={{ textAlign: 'center', padding: '40px' }}>
                    Đang tải...
                  </td>
                </tr>
              ) : items.length === 0 ? (
                <tr>
                  <td colSpan={8} style={{ textAlign: 'center', padding: '40px', color: '#6b7280' }}>
                    Không có biên nhận nào
                  </td>
                </tr>
              ) : (
                items.map((item, idx) => (
                  <tr key={item.goodsReceiptId}>
                    <td>{(page - 1) * pageSize + idx + 1}</td>
                    <td>
                      <button
                        type="button"
                        className="asset-code-link"
                        onClick={() => openDetail(item.goodsReceiptId)}
                      >
                        #{item.goodsReceiptId}
                      </button>
                    </td>
                    <td>#{item.procurementId}</td>
                    <td>{item.contractNo || '—'}</td>
                    <td>{item.supplierName || '—'}</td>
                    <td style={{ textAlign: 'right' }}>
                      {item.totalReceivedQuantity.toLocaleString('vi-VN')}
                    </td>
                    <td>{new Date(item.createdDate).toLocaleDateString('vi-VN')}</td>
                    <td style={{ textAlign: 'center' }}>
                      <EyeOutlined
                        onClick={() => openDetail(item.goodsReceiptId)}
                        style={{ cursor: 'pointer', fontSize: '16px', color: '#1890ff' }}
                      />
                    </td>
                  </tr>
                ))
              )}
            </tbody>
          </table>
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
              <option value={20}>20</option>
              <option value={50}>50</option>
              <option value={100}>100</option>
            </select>
          </div>
          <div className="purchase-orders-footer__center">
            {total === 0
              ? '0-0 trên 0'
              : `${(page - 1) * pageSize + 1}-${Math.min(page * pageSize, total)} trên ${total}`}
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
              onClick={() => setPage((p) => Math.min(Math.ceil(total / pageSize), p + 1))}
              type="button"
            >
              ⟩
            </button>
          </div>
        </div>
      </div>

      <GoodsReceiptFormModal
        open={createOpen}
        onClose={() => setCreateOpen(false)}
        onSubmit={handleCreateSubmit}
      />

      <GoodsReceiptDetailModal
        open={detailOpen}
        onClose={() => {
          setDetailOpen(false);
          setDetail(null);
        }}
        detail={detail}
        onPrintLabels={handlePrintLabels}
      />

      <PrintQRLabelsModal
        open={printQROpen}
        onClose={() => {
          setPrintQROpen(false);
          setPrintQRId(null);
        }}
        goodsReceiptId={printQRId}
      />
    </div>
  );
}

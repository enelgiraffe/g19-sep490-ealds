import { useCallback, useEffect, useMemo, useState } from 'react';
import {
  Button,
  DatePicker,
  Input,
  InputNumber,
  Modal,
  Radio,
  Select,
  Table,
  Tag,
  message,
} from 'antd';
import type { ColumnsType } from 'antd/es/table';
import { PlusOutlined, SearchOutlined } from '@ant-design/icons';
import dayjs, { type Dayjs } from 'dayjs';
import {
  SUPPLIER_INVOICE_STATUS,
  supplierInvoiceService,
  type SupplierInvoiceDetail,
  type SupplierInvoiceDetailLine,
  type SupplierInvoiceListItem,
} from '../services/supplierInvoiceService';
import {
  goodsReceiptService,
  type GoodsReceiptDetail,
  type GoodsReceiptDetailLine,
  type GoodsReceiptListItem,
} from '../services/goodsReceiptService';
import {
  procurementPoService,
  type PurchaseOrderDetail,
  type PurchaseOrderLineItem,
  type PurchaseOrderListItem,
} from '../services/procurementPoService';
import { supplierService, type SupplierItem } from '../../admin/services/supplierService';
import './GoodsReceiptsPage.css';

type RefKind = 'po' | 'gr';

interface PoLineEdit {
  procurementLineId: number;
  quantity: number;
  unitPrice: number;
}

interface GrLineEdit {
  procurementLineId: number;
  goodsReceiptLineId: number;
  quantity: number;
  unitPrice: number;
}

function statusLabel(status: number): string {
  if (status === SUPPLIER_INVOICE_STATUS.cancelled) return 'Đã hủy';
  return 'Hiệu lực';
}

function statusTag(status: number) {
  const cancelled = status === SUPPLIER_INVOICE_STATUS.cancelled;
  return <Tag color={cancelled ? 'default' : 'green'}>{statusLabel(status)}</Tag>;
}

export function SupplierInvoicesPage() {
  const [loading, setLoading] = useState(false);
  const [items, setItems] = useState<SupplierInvoiceListItem[]>([]);
  const [total, setTotal] = useState(0);
  const [page, setPage] = useState(1);
  const [pageSize, setPageSize] = useState(20);
  const [filterInvoiceNo, setFilterInvoiceNo] = useState('');
  const [filterSupplierId, setFilterSupplierId] = useState<number | 'all'>('all');
  const [filterDateFrom, setFilterDateFrom] = useState<Dayjs | null>(null);
  const [filterDateTo, setFilterDateTo] = useState<Dayjs | null>(null);
  const [suppliers, setSuppliers] = useState<SupplierItem[]>([]);

  const [detailOpen, setDetailOpen] = useState(false);
  const [detail, setDetail] = useState<SupplierInvoiceDetail | null>(null);
  const [detailLoading, setDetailLoading] = useState(false);

  const [createOpen, setCreateOpen] = useState(false);
  const [refKind, setRefKind] = useState<RefKind>('po');
  const [poOptions, setPoOptions] = useState<PurchaseOrderListItem[]>([]);
  const [grOptions, setGrOptions] = useState<GoodsReceiptListItem[]>([]);
  const [refLoading, setRefLoading] = useState(false);
  const [selectedPoId, setSelectedPoId] = useState<number | null>(null);
  const [selectedGrId, setSelectedGrId] = useState<number | null>(null);
  const [poDetail, setPoDetail] = useState<PurchaseOrderDetail | null>(null);
  const [grDetail, setGrDetail] = useState<GoodsReceiptDetail | null>(null);
  const [poLineEdits, setPoLineEdits] = useState<PoLineEdit[]>([]);
  const [grLineEdits, setGrLineEdits] = useState<GrLineEdit[]>([]);
  const [invoiceNumber, setInvoiceNumber] = useState('');
  const [invoiceDate, setInvoiceDate] = useState<Dayjs | null>(dayjs());
  const [note, setNote] = useState('');
  const [submitting, setSubmitting] = useState(false);

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

  const openCreate = async () => {
    setCreateOpen(true);
    setRefKind('po');
    setSelectedPoId(null);
    setSelectedGrId(null);
    setPoDetail(null);
    setGrDetail(null);
    setPoLineEdits([]);
    setGrLineEdits([]);
    setInvoiceNumber('');
    setInvoiceDate(dayjs());
    setNote('');
    setRefLoading(true);
    try {
      const [pos, grs] = await Promise.all([
        procurementPoService.getList({ pageSize: 200, page: 1 }),
        goodsReceiptService.getList({ pageSize: 200, page: 1 }),
      ]);
      setPoOptions(pos.items.filter((p) => p.status !== 2));
      setGrOptions(grs.items);
    } catch {
      message.error('Không tải dữ liệu chứng từ tham chiếu.');
      setPoOptions([]);
      setGrOptions([]);
    } finally {
      setRefLoading(false);
    }
  };

  const onSelectPoForCreate = async (procurementId: number) => {
    setSelectedPoId(procurementId);
    setSelectedGrId(null);
    setGrDetail(null);
    setGrLineEdits([]);
    setRefLoading(true);
    try {
      const d = await procurementPoService.getById(procurementId);
      setPoDetail(d);
      setPoLineEdits(
        d.lines.map((l) => ({
          procurementLineId: l.lineId,
          quantity: Number(l.quantity),
          unitPrice: Number(l.unitPrice),
        })),
      );
    } catch {
      message.error('Không tải chi tiết đơn mua.');
      setPoDetail(null);
      setPoLineEdits([]);
    } finally {
      setRefLoading(false);
    }
  };

  const onSelectGrForCreate = async (goodsReceiptId: number) => {
    setSelectedGrId(goodsReceiptId);
    setSelectedPoId(null);
    setPoDetail(null);
    setPoLineEdits([]);
    setRefLoading(true);
    try {
      const gr = await goodsReceiptService.getById(goodsReceiptId);
      setGrDetail(gr);
      const po = await procurementPoService.getById(gr.procurementId);
      setPoDetail(po);
      setSelectedPoId(gr.procurementId);
      const priceByLine = new Map(po.lines.map((l) => [l.lineId, Number(l.unitPrice)]));
      setGrLineEdits(
        gr.lines.map((ln) => ({
          procurementLineId: ln.procurementLineId,
          goodsReceiptLineId: ln.goodsReceiptLineId,
          quantity: Number(ln.quantityReceivedOnThisReceipt),
          unitPrice: priceByLine.get(ln.procurementLineId) ?? 0,
        })),
      );
    } catch {
      message.error('Không tải biên nhận / đơn mua.');
      setGrDetail(null);
      setGrLineEdits([]);
      setPoDetail(null);
    } finally {
      setRefLoading(false);
    }
  };

  const updatePoLine = (procurementLineId: number, patch: Partial<PoLineEdit>) => {
    setPoLineEdits((prev) => prev.map((r) => (r.procurementLineId === procurementLineId ? { ...r, ...patch } : r)));
  };

  const updateGrLine = (goodsReceiptLineId: number, patch: Partial<GrLineEdit>) => {
    setGrLineEdits((prev) => prev.map((r) => (r.goodsReceiptLineId === goodsReceiptLineId ? { ...r, ...patch } : r)));
  };

  const poLineById = useMemo(() => {
    const m = new Map<number, PurchaseOrderLineItem>();
    poDetail?.lines.forEach((l) => m.set(l.lineId, l));
    return m;
  }, [poDetail]);

  const handleCreateSubmit = async () => {
    if (!invoiceNumber.trim()) {
      message.warning('Nhập số hóa đơn.');
      return;
    }
    if (!invoiceDate) {
      message.warning('Chọn ngày hóa đơn.');
      return;
    }

    if (refKind === 'po') {
      if (!selectedPoId || !poDetail) {
        message.warning('Chọn đơn mua.');
        return;
      }
      const lines = poLineEdits
        .filter((r) => r.quantity > 0)
        .map((r) => ({
          procurementLineId: r.procurementLineId,
          quantity: r.quantity,
          unitPrice: r.unitPrice,
        }));
      if (lines.length === 0) {
        message.warning('Nhập số lượng > 0 cho ít nhất một dòng.');
        return;
      }
      setSubmitting(true);
      try {
        await supplierInvoiceService.create({
          procurementId: selectedPoId,
          goodsReceiptId: null,
          invoiceNumber: invoiceNumber.trim(),
          invoiceDate: invoiceDate.format('YYYY-MM-DD'),
          note: note.trim() || null,
          lines,
        });
        message.success('Đã tạo hóa đơn nhà cung cấp.');
        setCreateOpen(false);
        await loadList();
      } catch (e: unknown) {
        const msg =
          typeof e === 'object' && e !== null && 'response' in e
            ? String((e as { response?: { data?: unknown } }).response?.data)
            : 'Không tạo được hóa đơn.';
        message.error(msg.length > 200 ? 'Không tạo được hóa đơn.' : msg);
      } finally {
        setSubmitting(false);
      }
      return;
    }

    if (!selectedGrId || !grDetail || !selectedPoId) {
      message.warning('Chọn biên nhận hàng.');
      return;
    }
    const lines = grLineEdits
      .filter((r) => r.quantity > 0)
      .map((r) => ({
        procurementLineId: r.procurementLineId,
        goodsReceiptLineId: r.goodsReceiptLineId,
        quantity: r.quantity,
        unitPrice: r.unitPrice,
      }));
    if (lines.length === 0) {
      message.warning('Nhập số lượng > 0 cho ít nhất một dòng.');
      return;
    }
    setSubmitting(true);
    try {
      await supplierInvoiceService.create({
        procurementId: selectedPoId,
        goodsReceiptId: selectedGrId,
        invoiceNumber: invoiceNumber.trim(),
        invoiceDate: invoiceDate.format('YYYY-MM-DD'),
        note: note.trim() || null,
        lines,
      });
      message.success('Đã tạo hóa đơn nhà cung cấp.');
      setCreateOpen(false);
      await loadList();
    } catch (e: unknown) {
      const msg =
        typeof e === 'object' && e !== null && 'response' in e
          ? String((e as { response?: { data?: unknown } }).response?.data)
          : 'Không tạo được hóa đơn.';
      message.error(msg.length > 200 ? 'Không tạo được hóa đơn.' : msg);
    } finally {
      setSubmitting(false);
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

  const listColumns: ColumnsType<SupplierInvoiceListItem> = [
    {
      title: 'Mã HĐ',
      dataIndex: 'supplierInvoiceId',
      width: 88,
      render: (id: number) => (
        <button type="button" className="gr-code-link" onClick={() => openDetail(id)}>
          {id}
        </button>
      ),
    },
    { title: 'Số HĐ', dataIndex: 'invoiceNumber', width: 130, ellipsis: true },
    {
      title: 'NCC',
      dataIndex: 'supplierName',
      ellipsis: true,
      render: (v) => v || '—',
    },
    {
      title: 'Tổng tiền',
      dataIndex: 'totalAmount',
      align: 'right',
      width: 130,
      render: (v) => Number(v).toLocaleString('vi-VN'),
    },
    {
      title: 'Ngày HĐ',
      dataIndex: 'invoiceDate',
      width: 120,
      render: (d: string) => (d ? dayjs(d).format('DD/MM/YYYY') : '—'),
    },
    { title: 'Trạng thái', dataIndex: 'status', width: 110, render: (s: number) => statusTag(s) },
    { title: 'Đơn mua', dataIndex: 'procurementId', width: 88 },
    {
      title: 'Biên nhận',
      dataIndex: 'goodsReceiptId',
      width: 88,
      render: (v: number | null) => v ?? '—',
    },
  ];

  const poCreateColumns: ColumnsType<PoLineEdit> = [
    { title: '#', width: 40, render: (_, __, i) => i + 1 },
    {
      title: 'Dòng / tài sản',
      render: (_, r) => {
        const pl = poLineById.get(r.procurementLineId);
        const label =
          pl?.description ||
          [pl?.assetCode, pl?.assetName].filter(Boolean).join(' ') ||
          `Dòng ${(pl?.lineIndex ?? 0) + 1}`;
        return label;
      },
    },
    {
      title: 'SL tối đa (đặt)',
      align: 'right',
      width: 110,
      render: (_, r) => {
        const pl = poLineById.get(r.procurementLineId);
        return pl ? Number(pl.quantity).toLocaleString('vi-VN') : '—';
      },
    },
    {
      title: 'SL trên HĐ',
      width: 120,
      render: (_, r) => {
        const pl = poLineById.get(r.procurementLineId);
        const max = pl ? Number(pl.quantity) : undefined;
        return (
          <InputNumber
            min={0}
            max={max}
            value={r.quantity}
            onChange={(v) => updatePoLine(r.procurementLineId, { quantity: v ?? 0 })}
            style={{ width: '100%' }}
          />
        );
      },
    },
    {
      title: 'Đơn giá',
      width: 130,
      render: (_, r) => (
        <InputNumber
          min={0}
          value={r.unitPrice}
          onChange={(v) => updatePoLine(r.procurementLineId, { unitPrice: v ?? 0 })}
          style={{ width: '100%' }}
        />
      ),
    },
    {
      title: 'Thành tiền',
      align: 'right',
      width: 120,
      render: (_, r) => (r.quantity * r.unitPrice).toLocaleString('vi-VN', { maximumFractionDigits: 0 }),
    },
  ];

  const grLineById = useMemo(() => {
    const m = new Map<number, GoodsReceiptDetailLine>();
    grDetail?.lines.forEach((l) => m.set(l.goodsReceiptLineId, l));
    return m;
  }, [grDetail]);

  const grCreateColumns: ColumnsType<GrLineEdit> = [
    { title: '#', width: 40, render: (_, __, i) => i + 1 },
    {
      title: 'Tài sản',
      render: (_, r) => {
        const ln = grLineById.get(r.goodsReceiptLineId);
        return [ln?.assetCode, ln?.assetName].filter(Boolean).join(' ') || '—';
      },
    },
    {
      title: 'SL BN (tối đa)',
      align: 'right',
      width: 120,
      render: (_, r) => {
        const ln = grLineById.get(r.goodsReceiptLineId);
        return ln ? Number(ln.quantityReceivedOnThisReceipt).toLocaleString('vi-VN') : '—';
      },
    },
    {
      title: 'SL trên HĐ',
      width: 120,
      render: (_, r) => {
        const ln = grLineById.get(r.goodsReceiptLineId);
        const max = ln ? Number(ln.quantityReceivedOnThisReceipt) : undefined;
        return (
          <InputNumber
            min={0}
            max={max}
            value={r.quantity}
            onChange={(v) => updateGrLine(r.goodsReceiptLineId, { quantity: v ?? 0 })}
            style={{ width: '100%' }}
          />
        );
      },
    },
    {
      title: 'Đơn giá',
      width: 130,
      render: (_, r) => (
        <InputNumber
          min={0}
          value={r.unitPrice}
          onChange={(v) => updateGrLine(r.goodsReceiptLineId, { unitPrice: v ?? 0 })}
          style={{ width: '100%' }}
        />
      ),
    },
    {
      title: 'Thành tiền',
      align: 'right',
      width: 120,
      render: (_, r) => (r.quantity * r.unitPrice).toLocaleString('vi-VN', { maximumFractionDigits: 0 }),
    },
  ];

  const detailLineColumns: ColumnsType<SupplierInvoiceDetailLine> = [
    { title: '#', width: 40, render: (_, __, i) => i + 1 },
    {
      title: 'Tài sản',
      render: (_, r) => [r.assetCode, r.assetName].filter(Boolean).join(' ') || '—',
    },
    {
      title: 'SL',
      dataIndex: 'quantity',
      align: 'right',
      render: (v) => Number(v).toLocaleString('vi-VN'),
    },
    {
      title: 'Đơn giá',
      dataIndex: 'unitPrice',
      align: 'right',
      render: (v) => Number(v).toLocaleString('vi-VN'),
    },
    {
      title: 'Thành tiền',
      dataIndex: 'lineTotal',
      align: 'right',
      render: (v) => Number(v).toLocaleString('vi-VN'),
    },
  ];

  return (
    <div className="goods-receipts-page">
      <div className="goods-receipts-header">
        <h1 className="goods-receipts-title">Hóa đơn nhà cung cấp</h1>
        <Button type="primary" icon={<PlusOutlined />} onClick={() => openCreate()}>
          Tạo hóa đơn
        </Button>
      </div>

      <div className="goods-receipts-card">
        <div className="goods-receipts-filters">
          <Input
            allowClear
            placeholder="Số hóa đơn"
            value={filterInvoiceNo}
            onChange={(e) => {
              setFilterInvoiceNo(e.target.value);
              setPage(1);
            }}
            style={{ width: 160 }}
            prefix={<SearchOutlined />}
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
            options={suppliers.map((s) => ({ value: s.supplierId, label: s.name }))}
          />
          <DatePicker
            placeholder="Từ ngày"
            value={filterDateFrom}
            onChange={(d) => {
              setFilterDateFrom(d);
              setPage(1);
            }}
          />
          <DatePicker
            placeholder="Đến ngày"
            value={filterDateTo}
            onChange={(d) => {
              setFilterDateTo(d);
              setPage(1);
            }}
          />
          <Button type="default" icon={<SearchOutlined />} onClick={() => loadList()}>
            Lọc
          </Button>
        </div>

        <Table<SupplierInvoiceListItem>
          rowKey="supplierInvoiceId"
          loading={loading}
          columns={listColumns}
          dataSource={items}
          scroll={{ x: 960 }}
          pagination={{
            current: page,
            pageSize,
            total,
            showSizeChanger: true,
            onChange: (p, ps) => {
              setPage(p);
              setPageSize(ps);
            },
          }}
        />
      </div>

      <Modal
        title={`Hóa đơn NCC #${detail?.supplierInvoiceId ?? ''}`}
        open={detailOpen}
        onCancel={() => setDetailOpen(false)}
        footer={null}
        width={880}
        destroyOnClose
      >
        {detailLoading && <p>Đang tải…</p>}
        {detail && !detailLoading && (
          <>
            <div style={{ marginBottom: 12, display: 'flex', flexWrap: 'wrap', gap: 12 }}>
              <div>
                <strong>Số HĐ:</strong> {detail.invoiceNumber}
              </div>
              <div>
                <strong>NCC:</strong> {detail.supplierName || '—'}
              </div>
              <div>
                <strong>Ngày HĐ:</strong> {dayjs(detail.invoiceDate).format('DD/MM/YYYY')}
              </div>
              <div>
                <strong>Tổng:</strong> {Number(detail.totalAmount).toLocaleString('vi-VN')} {detail.currency}
              </div>
              <div>
                <strong>Trạng thái:</strong> {statusTag(detail.status)}
              </div>
            </div>
            <div style={{ marginBottom: 8 }}>
              <strong>Đơn mua:</strong> {detail.procurementId}
              {' · '}
              <strong>Biên nhận:</strong> {detail.goodsReceiptId ?? '—'}
            </div>
            {detail.note && (
              <p style={{ marginBottom: 8 }}>
                <strong>Ghi chú:</strong> {detail.note}
              </p>
            )}
            <Table<SupplierInvoiceDetailLine>
              size="small"
              rowKey="supplierInvoiceLineId"
              columns={detailLineColumns}
              dataSource={detail.lines}
              pagination={false}
            />
            {detail.status === SUPPLIER_INVOICE_STATUS.active && (
              <div style={{ marginTop: 16 }}>
                <Button danger onClick={() => confirmCancel(detail.supplierInvoiceId)}>
                  Hủy hóa đơn
                </Button>
              </div>
            )}
          </>
        )}
      </Modal>

      <Modal
        title="Tạo hóa đơn nhà cung cấp"
        open={createOpen}
        onCancel={() => !submitting && setCreateOpen(false)}
        footer={[
          <Button key="close" disabled={submitting} onClick={() => setCreateOpen(false)}>
            Đóng
          </Button>,
          <Button key="save" type="primary" loading={submitting} onClick={() => void handleCreateSubmit()}>
            Lưu
          </Button>,
        ]}
        width={960}
        destroyOnClose
      >
        <div style={{ marginBottom: 12 }}>
          <Radio.Group
            value={refKind}
            onChange={(e) => {
              const k = e.target.value as RefKind;
              setRefKind(k);
              setSelectedPoId(null);
              setSelectedGrId(null);
              setPoDetail(null);
              setGrDetail(null);
              setPoLineEdits([]);
              setGrLineEdits([]);
            }}
          >
            <Radio value="po">Theo đơn mua</Radio>
            <Radio value="gr">Theo biên nhận hàng</Radio>
          </Radio.Group>
        </div>

        {refKind === 'po' && (
          <div style={{ marginBottom: 12 }}>
            <div style={{ marginBottom: 8 }}>Chọn đơn mua</div>
            <Select
              showSearch
              optionFilterProp="label"
              placeholder="Đơn mua"
              style={{ width: '100%' }}
              loading={refLoading}
              value={selectedPoId ?? undefined}
              onChange={(v) => onSelectPoForCreate(v)}
              options={poOptions.map((p) => ({
                value: p.procurementId,
                label: `${p.procurementId} — ${p.contractNo} — ${p.supplierName || 'NCC'}`,
              }))}
            />
          </div>
        )}

        {refKind === 'gr' && (
          <div style={{ marginBottom: 12 }}>
            <div style={{ marginBottom: 8 }}>Chọn biên nhận</div>
            <Select
              showSearch
              optionFilterProp="label"
              placeholder="Biên nhận hàng"
              style={{ width: '100%' }}
              loading={refLoading}
              value={selectedGrId ?? undefined}
              onChange={(v) => onSelectGrForCreate(v)}
              options={grOptions.map((g) => ({
                value: g.goodsReceiptId,
                label: `${g.goodsReceiptId} — ĐM ${g.procurementId} — ${g.supplierName || 'NCC'}`,
              }))}
            />
          </div>
        )}

        <div style={{ display: 'flex', gap: 12, flexWrap: 'wrap', marginBottom: 12 }}>
          <div>
            <div style={{ marginBottom: 4 }}>Số hóa đơn</div>
            <Input value={invoiceNumber} onChange={(e) => setInvoiceNumber(e.target.value)} placeholder="Bắt buộc" />
          </div>
          <div>
            <div style={{ marginBottom: 4 }}>Ngày hóa đơn</div>
            <DatePicker value={invoiceDate} onChange={(d) => setInvoiceDate(d)} style={{ width: 160 }} />
          </div>
        </div>
        <div style={{ marginBottom: 12 }}>
          <div style={{ marginBottom: 4 }}>Ghi chú</div>
          <Input.TextArea rows={2} value={note} onChange={(e) => setNote(e.target.value)} />
        </div>

        {refKind === 'po' && poDetail && (
          <Table<PoLineEdit>
            size="small"
            rowKey="procurementLineId"
            loading={refLoading}
            columns={poCreateColumns}
            dataSource={poLineEdits}
            pagination={false}
            scroll={{ x: 720 }}
          />
        )}

        {refKind === 'gr' && grDetail && grLineEdits.length > 0 && (
          <Table<GrLineEdit>
            size="small"
            rowKey="goodsReceiptLineId"
            loading={refLoading}
            columns={grCreateColumns}
            dataSource={grLineEdits}
            pagination={false}
            scroll={{ x: 720 }}
          />
        )}
      </Modal>
    </div>
  );
}

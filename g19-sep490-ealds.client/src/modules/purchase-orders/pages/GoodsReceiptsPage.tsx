import { useCallback, useEffect, useState } from 'react';
import {
  Button,
  DatePicker,
  Input,
  InputNumber,
  Modal,
  Select,
  Table,
  message,
} from 'antd';
import type { ColumnsType } from 'antd/es/table';
import { PlusOutlined, SearchOutlined } from '@ant-design/icons';
import dayjs, { type Dayjs } from 'dayjs';
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
import {
  assetService,
  type AssetCatalogResponse,
  type WarehouseItem,
} from '../../assets/services/assetService';
import { supplierService, type SupplierItem } from '../../admin/services/supplierService';
import './GoodsReceiptsPage.css';

function parseSerialTokens(text: string): string[] {
  return text
    .split(/[\n,;]+/)
    .map((s) => s.trim())
    .filter(Boolean);
}

interface LineEditState {
  lineId: number;
  quantityReceived: number;
  assetId: number | null;
  serialsText: string;
}

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
  const [poOptions, setPoOptions] = useState<PurchaseOrderListItem[]>([]);
  const [poLoading, setPoLoading] = useState(false);
  const [selectedPoId, setSelectedPoId] = useState<number | null>(null);
  const [poDetail, setPoDetail] = useState<PurchaseOrderDetail | null>(null);
  const [warehouseId, setWarehouseId] = useState<number | null>(null);
  const [warehouses, setWarehouses] = useState<WarehouseItem[]>([]);
  const [assets, setAssets] = useState<AssetCatalogResponse[]>([]);
  const [lineStates, setLineStates] = useState<LineEditState[]>([]);
  const [note, setNote] = useState('');
  const [submitting, setSubmitting] = useState(false);

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

  const openCreate = async () => {
    setCreateOpen(true);
    setSelectedPoId(null);
    setPoDetail(null);
    setLineStates([]);
    setNote('');
    setWarehouseId(null);
    setPoLoading(true);
    try {
      const [wh, ast, pos] = await Promise.all([
        assetService.getWarehouses(),
        assetService.getAll(),
        procurementPoService.getList({ receivingEligible: true, pageSize: 200, page: 1 }),
      ]);
      setWarehouses(wh);
      setAssets(ast);
      setPoOptions(pos.items);
      if (wh.length > 0) setWarehouseId(wh[0].warehouseId);
    } catch {
      message.error('Không tải dữ liệu tạo biên nhận.');
      setWarehouses([]);
      setAssets([]);
      setPoOptions([]);
    } finally {
      setPoLoading(false);
    }
  };

  const onSelectPo = async (procurementId: number) => {
    setSelectedPoId(procurementId);
    setPoLoading(true);
    try {
      const d = await procurementPoService.getById(procurementId);
      setPoDetail(d);
      setLineStates(
        d.lines.map((l) => ({
          lineId: l.lineId,
          quantityReceived: 0,
          assetId: l.assetId,
          serialsText: '',
        })),
      );
    } catch {
      message.error('Không tải chi tiết đơn mua.');
      setPoDetail(null);
      setLineStates([]);
    } finally {
      setPoLoading(false);
    }
  };

  const updateLine = (lineId: number, patch: Partial<LineEditState>) => {
    setLineStates((prev) => prev.map((r) => (r.lineId === lineId ? { ...r, ...patch } : r)));
  };

  const handleCreateSubmit = async () => {
    if (!selectedPoId || !poDetail) {
      message.warning('Chọn đơn mua.');
      return;
    }
    if (!warehouseId || warehouseId <= 0) {
      message.warning('Chọn kho nhập.');
      return;
    }
    const poLineById = new Map(poDetail.lines.map((l) => [l.lineId, l]));
    const linesPayload: {
      procurementLineId: number;
      quantityReceived: number;
      assetId?: number | null;
      instanceSerialNumbers?: (string | null)[] | null;
    }[] = [];

    for (const row of lineStates) {
      const q = row.quantityReceived;
      if (q <= 0) continue;
      const pl = poLineById.get(row.lineId);
      if (!pl) continue;
      if (!Number.isInteger(q)) {
        message.error(`Dòng ${pl.lineIndex + 1}: số lượng nhận phải là số nguyên.`);
        return;
      }
      const open = Number(pl.openQuantity ?? pl.quantity - (pl.receivedQuantity ?? 0));
      if (q > open) {
        message.error(`Dòng ${pl.lineIndex + 1}: vượt số lượng còn nhận (${open}).`);
        return;
      }
      const assetId = row.assetId ?? pl.assetId;
      if (!assetId || assetId <= 0) {
        message.error(`Dòng ${pl.lineIndex + 1}: cần chọn tài sản danh mục.`);
        return;
      }
      const tokens = parseSerialTokens(row.serialsText);
      let serials: (string | null)[] | null = null;
      if (tokens.length > 0) {
        if (tokens.length !== q) {
          message.error(
            `Dòng ${pl.lineIndex + 1}: cần ${q} số seri (hoặc để trống), hiện có ${tokens.length}.`,
          );
          return;
        }
        serials = tokens;
      }
      linesPayload.push({
        procurementLineId: row.lineId,
        quantityReceived: q,
        assetId,
        instanceSerialNumbers: serials,
      });
    }

    if (linesPayload.length === 0) {
      message.warning('Nhập số lượng nhận cho ít nhất một dòng.');
      return;
    }

    setSubmitting(true);
    try {
      await goodsReceiptService.create({
        procurementId: selectedPoId,
        warehouseId,
        note: note.trim() || null,
        lines: linesPayload,
      });
      message.success('Đã tạo biên nhận và sinh thể hiện tài sản.');
      setCreateOpen(false);
      await loadList();
    } catch (e: unknown) {
      const msg =
        typeof e === 'object' && e !== null && 'response' in e
          ? String((e as { response?: { data?: unknown } }).response?.data)
          : 'Không tạo được biên nhận.';
      message.error(msg.length > 200 ? 'Không tạo được biên nhận.' : msg);
    } finally {
      setSubmitting(false);
    }
  };

  const listColumns: ColumnsType<GoodsReceiptListItem> = [
    {
      title: 'Mã BN',
      dataIndex: 'goodsReceiptId',
      width: 90,
      render: (id: number) => (
        <button type="button" className="gr-code-link" onClick={() => openDetail(id)}>
          {id}
        </button>
      ),
    },
    { title: 'Đơn mua', dataIndex: 'procurementId', width: 100 },
    { title: 'Số chứng từ', dataIndex: 'contractNo', width: 120, render: (v) => v || '—' },
    {
      title: 'NCC',
      dataIndex: 'supplierName',
      ellipsis: true,
      render: (v) => v || '—',
    },
    {
      title: 'SL nhận',
      dataIndex: 'totalReceivedQuantity',
      align: 'right',
      width: 110,
      render: (v) => Number(v).toLocaleString('vi-VN'),
    },
    {
      title: 'Ngày tạo',
      dataIndex: 'createdDate',
      width: 170,
      render: (d: string) => new Date(d).toLocaleString('vi-VN'),
    },
  ];

  const createColumns: ColumnsType<LineEditState & { pl: PurchaseOrderLineItem }> = [
    { title: '#', width: 40, render: (_, __, i) => i + 1 },
    {
      title: 'Mô tả / TS',
      render: (_, r) => {
        const pl = r.pl;
        const label =
          pl.description ||
          [pl.assetCode, pl.assetName].filter(Boolean).join(' ') ||
          `Dòng ${pl.lineIndex + 1}`;
        return label;
      },
    },
    {
      title: 'Đặt',
      align: 'right',
      width: 72,
      render: (_, r) => Number(r.pl.quantity).toLocaleString('vi-VN'),
    },
    {
      title: 'Đã nhận',
      align: 'right',
      width: 80,
      render: (_, r) => Number(r.pl.receivedQuantity ?? 0).toLocaleString('vi-VN'),
    },
    {
      title: 'Còn lại',
      align: 'right',
      width: 80,
      render: (_, r) => Number(r.pl.openQuantity ?? 0).toLocaleString('vi-VN'),
    },
    {
      title: 'Nhận nữa',
      width: 110,
      render: (_, r) => (
        <InputNumber
          min={0}
          max={Number(r.pl.openQuantity ?? 0)}
          precision={0}
          value={r.quantityReceived}
          onChange={(v) => updateLine(r.lineId, { quantityReceived: v ?? 0 })}
          style={{ width: '100%' }}
        />
      ),
    },
    {
      title: 'Tài sản (danh mục)',
      width: 220,
      render: (_, r) => (
        <Select
          allowClear
          showSearch
          optionFilterProp="label"
          placeholder="Theo dòng PO"
          style={{ width: '100%' }}
          value={r.assetId ?? undefined}
          options={assets.map((a) => ({
            value: a.assetId,
            label: `${a.code} — ${a.name}`,
          }))}
          onChange={(v) => updateLine(r.lineId, { assetId: v ?? null })}
        />
      ),
    },
    {
      title: 'Số seri (tuỳ chọn)',
      render: (_, r) => (
        <Input.TextArea
          rows={2}
          placeholder="Mỗi dòng hoặc dấu phẩy; đủ số với SL nhận"
          value={r.serialsText}
          onChange={(e) => updateLine(r.lineId, { serialsText: e.target.value })}
        />
      ),
    },
  ];

  const mergedRows: (LineEditState & { pl: PurchaseOrderLineItem })[] = lineStates
    .map((s) => {
      const pl = poDetail?.lines.find((l) => l.lineId === s.lineId);
      return pl ? { ...s, pl } : null;
    })
    .filter(Boolean) as (LineEditState & { pl: PurchaseOrderLineItem })[];

  const detailLineColumns: ColumnsType<GoodsReceiptDetailLine> = [
    { title: '#', width: 40, render: (_, __, i) => i + 1 },
    {
      title: 'Tài sản',
      render: (_, r) => [r.assetCode, r.assetName].filter(Boolean).join(' ') || '—',
    },
    {
      title: 'Đặt',
      dataIndex: 'orderedQuantity',
      align: 'right',
      render: (v) => Number(v).toLocaleString('vi-VN'),
    },
    {
      title: 'Nhận (BN này)',
      dataIndex: 'quantityReceivedOnThisReceipt',
      align: 'right',
      render: (v) => Number(v).toLocaleString('vi-VN'),
    },
    {
      title: 'Đã nhận (lũy kế)',
      dataIndex: 'cumulativeReceivedQuantity',
      align: 'right',
      render: (v) => Number(v).toLocaleString('vi-VN'),
    },
    {
      title: 'Còn lại',
      dataIndex: 'openQuantity',
      align: 'right',
      render: (v) => Number(v).toLocaleString('vi-VN'),
    },
  ];

  return (
    <div className="goods-receipts-page">
      <div className="goods-receipts-header">
        <h1 className="goods-receipts-title">Biên nhận hàng</h1>
        <Button type="primary" icon={<PlusOutlined />} onClick={() => openCreate()}>
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
          <Table<GoodsReceiptListItem>
            loading={loading}
            rowKey={(r) => String(r.goodsReceiptId)}
            dataSource={items}
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
            scroll={{ x: 900 }}
            columns={listColumns}
          />
        </div>
      </div>

      <Modal
        title={detail ? `Biên nhận #${detail.goodsReceiptId}` : 'Chi tiết'}
        open={detailOpen}
        onCancel={() => {
          setDetailOpen(false);
          setDetail(null);
        }}
        footer={
          <Button type="primary" onClick={() => setDetailOpen(false)}>
            Đóng
          </Button>
        }
        width={960}
      >
        {detail && (
          <>
            <p>
              <strong>Đơn mua:</strong> {detail.procurementId} — {detail.contractNo ?? '—'} —{' '}
              {detail.supplierName ?? '—'}
            </p>
            <p style={{ marginBottom: 12 }}>
              <strong>Ngày tạo:</strong> {dayjs(detail.createdDate).format('DD/MM/YYYY HH:mm')}
            </p>
            <Table
              size="small"
              rowKey={(r) => String(r.goodsReceiptLineId)}
              columns={detailLineColumns}
              dataSource={detail.lines}
              pagination={false}
              expandable={{
                expandedRowRender: (r) => (
                  <Table
                    size="small"
                    rowKey={(i) => String(i.assetInstanceId)}
                    pagination={false}
                    dataSource={r.instances}
                    columns={[
                      { title: 'Mã thể hiện', dataIndex: 'instanceCode' },
                      { title: 'Serial', dataIndex: 'serialNumber', render: (v) => v || '—' },
                    ]}
                  />
                ),
                rowExpandable: (r) => r.instances.length > 0,
              }}
            />
          </>
        )}
      </Modal>

      <Modal
        title="Tạo biên nhận hàng"
        open={createOpen}
        onCancel={() => setCreateOpen(false)}
        width={1100}
        okText="Ghi nhận"
        confirmLoading={submitting}
        onOk={() => handleCreateSubmit()}
      >
        <div style={{ marginBottom: 12 }}>
          <div style={{ marginBottom: 8 }}>
            <strong>Đơn mua</strong>
          </div>
          <Select
            loading={poLoading}
            showSearch
            optionFilterProp="label"
            placeholder="Chọn đơn mua còn nhận hàng"
            style={{ width: '100%' }}
            value={selectedPoId ?? undefined}
            options={poOptions.map((p) => ({
              value: p.procurementId,
              label: `${p.contractNo} (#${p.procurementId}) — ${p.title}`,
            }))}
            onChange={(v) => onSelectPo(v)}
          />
        </div>
        <div style={{ marginBottom: 12 }}>
          <strong>Kho nhập</strong>
          <Select
            style={{ width: '100%', marginTop: 4 }}
            placeholder="Kho"
            value={warehouseId ?? undefined}
            options={warehouses.map((w) => ({
              value: w.warehouseId,
              label: w.name,
            }))}
            onChange={(v) => setWarehouseId(v ?? null)}
          />
        </div>
        <div style={{ marginBottom: 12 }}>
          <strong>Ghi chú</strong>
          <Input value={note} onChange={(e) => setNote(e.target.value)} style={{ marginTop: 4 }} />
        </div>
        {poDetail && (
          <Table
            loading={poLoading}
            size="small"
            rowKey={(r) => String(r.lineId)}
            columns={createColumns}
            dataSource={mergedRows}
            pagination={false}
            scroll={{ x: 1000 }}
          />
        )}
      </Modal>
    </div>
  );
}

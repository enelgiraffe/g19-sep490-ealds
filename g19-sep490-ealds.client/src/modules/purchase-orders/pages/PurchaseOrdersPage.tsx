import { useCallback, useEffect, useState, type ComponentProps } from 'react';
import { Button, InputNumber, Select, Table, message } from 'antd';
import { PlusOutlined, SearchOutlined } from '@ant-design/icons';
import {
  procurementPoService,
  PO_STATUS,
  type PurchaseOrderDetail,
  type PurchaseOrderListItem,
} from '../services/procurementPoService';
import { PurchaseOrderFormModal } from '../components/PurchaseOrderFormModal';
import { PurchaseOrderDetailModal } from '../components/PurchaseOrderDetailModal';
import { supplierService, type SupplierItem } from '../../admin/services/supplierService';
import './PurchaseOrdersPage.css';

function statusTag(status: number) {
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
  const [filterId, setFilterId] = useState<number | null>(null);
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
      const res = await procurementPoService.getList({
        procurementId: filterId ?? undefined,
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
    payload: Parameters<ComponentProps<typeof PurchaseOrderFormModal>['onSubmit']>[0],
  ) => {
    await procurementPoService.create({
      supplierId: payload.supplierId,
      currency: payload.currency,
      assetRequestId: payload.assetRequestId,
      lines: payload.lines,
    });
    message.success('Đã tạo đơn mua.');
    setFormOpen(false);
    await loadList();
  };

  const handleEditSubmit = async (
    payload: Parameters<ComponentProps<typeof PurchaseOrderFormModal>['onSubmit']>[0],
  ) => {
    if (!selected) return;
    await procurementPoService.update(selected.procurementId, {
      supplierId: payload.supplierId,
      currency: payload.currency,
      assetRequestId: payload.assetRequestId,
      lines: payload.lines,
    });
    message.success('Đã cập nhật đơn mua.');
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
        <div className="purchase-orders-filters" style={{ flexWrap: 'wrap', gap: 8 }}>
          <InputNumber
            min={1}
            placeholder="Mã đơn (ID)"
            value={filterId ?? undefined}
            onChange={(v) => {
              setFilterId(v ?? null);
              setPage(1);
            }}
            style={{ width: 160 }}
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
            options={[
              ...suppliers.map((s) => ({
                value: s.supplierId,
                label: `${s.code} — ${s.name}`,
              })),
            ]}
          />
          <Select
            placeholder="Trạng thái"
            style={{ width: 160 }}
            value={filterStatus === 'all' ? undefined : filterStatus}
            onChange={(v) => {
              setFilterStatus(v ?? 'all');
              setPage(1);
            }}
            options={[
              { value: PO_STATUS.created, label: 'Đã tạo' },
              { value: PO_STATUS.partiallyReceived, label: 'Nhận một phần' },
              { value: PO_STATUS.completed, label: 'Đã nhận đủ' },
              { value: PO_STATUS.cancelled, label: 'Đã hủy' },
            ]}
          />
          <Button icon={<SearchOutlined />} onClick={() => loadList()}>
            Làm mới
          </Button>
        </div>

        <div className="asset-table-wrapper" style={{ marginTop: 16 }}>
          <Table<PurchaseOrderListItem>
            loading={loading}
            rowKey={(r) => String(r.procurementId)}
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
            scroll={{ x: 960 }}
            columns={[
              {
                title: 'Mã đơn',
                dataIndex: 'procurementId',
                width: 100,
                render: (id: number) => (
                  <button
                    type="button"
                    className="asset-code asset-code--link"
                    onClick={() => openDetail(id)}
                  >
                    {id}
                  </button>
                ),
              },
              { title: 'Số chứng từ', dataIndex: 'contractNo', width: 120 },
              { title: 'Tiêu đề', dataIndex: 'title', ellipsis: true },
              {
                title: 'NCC',
                dataIndex: 'supplierName',
                width: 200,
                ellipsis: true,
                render: (v, r) => v ?? `ID ${r.supplierId}`,
              },
              {
                title: 'Tổng tiền',
                dataIndex: 'totalAmount',
                align: 'right',
                width: 140,
                render: (v, r) => `${Number(v).toLocaleString('vi-VN')} ${r.currency}`,
              },
              {
                title: 'TT',
                dataIndex: 'status',
                width: 100,
                render: (s: number) => {
                  const t = statusTag(s);
                  const color =
                    t.color === 'red'
                      ? '#cf1322'
                      : t.color === 'green'
                        ? '#389e0d'
                        : t.color === 'orange'
                          ? '#d46b08'
                          : '#1677ff';
                  return <span style={{ color }}>{t.label}</span>;
                },
              },
              {
                title: 'Ngày tạo',
                dataIndex: 'createDate',
                width: 160,
                render: (d: string) => new Date(d).toLocaleString('vi-VN'),
              },
            ]}
          />
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

      <PurchaseOrderFormModal
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

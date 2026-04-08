import { useEffect, useState } from 'react';
import { Modal, Form, Select, Input, InputNumber, Button, DatePicker, Space, Table, message } from 'antd';
import type { ColumnsType } from 'antd/es/table';
import dayjs, { type Dayjs } from 'dayjs';
import { supplierService, type SupplierItem } from '../../admin/services/supplierService';
import { assetService, type AssetCatalogResponse } from '../../assets/services/assetService';
import type { PurchaseOrderDetail, PurchaseOrderLineWrite } from '../services/procurementPoService';

const { TextArea } = Input;

const CURRENCIES = ['VND', 'USD', 'EUR'] as const;

interface LineRow {
  key: string;
  description: string;
  assetId: number | null;
  quantity: number;
  unit: string;
  unitPrice: number;
  expectedDelivery: Dayjs | null;
}

function toRows(lines: PurchaseOrderDetail['lines']): LineRow[] {
  return lines.map((l, i) => ({
    key: `l-${l.lineId}-${i}`,
    description: l.description ?? '',
    assetId: l.assetId,
    quantity: Number(l.quantity),
    unit: l.unit ?? 'Cái',
    unitPrice: Number(l.unitPrice),
    expectedDelivery: l.expectedDeliveryDate ? dayjs(l.expectedDeliveryDate) : null,
  }));
}

function emptyRow(): LineRow {
  return {
    key: `new-${Date.now()}`,
    description: '',
    assetId: null,
    quantity: 1,
    unit: 'Cái',
    unitPrice: 0,
    expectedDelivery: null,
  };
}

export interface PurchaseOrderFormModalProps {
  open: boolean;
  mode: 'create' | 'edit';
  initial: PurchaseOrderDetail | null;
  onClose: () => void;
  onSubmit: (payload: {
    supplierId: number;
    currency: string;
    assetRequestId: number | null;
    lines: PurchaseOrderLineWrite[];
  }) => Promise<void>;
}

export function PurchaseOrderFormModal({
  open,
  mode,
  initial,
  onClose,
  onSubmit,
}: PurchaseOrderFormModalProps) {
  const [form] = Form.useForm<{ supplierId: number; currency: string; assetRequestId?: number | null }>();
  const [suppliers, setSuppliers] = useState<SupplierItem[]>([]);
  const [assets, setAssets] = useState<AssetCatalogResponse[]>([]);
  const [lines, setLines] = useState<LineRow[]>([emptyRow()]);
  const [loading, setLoading] = useState(false);
  const [metaLoading, setMetaLoading] = useState(false);

  useEffect(() => {
    if (!open) return;
    let cancelled = false;
    (async () => {
      setMetaLoading(true);
      try {
        const [sup, ast] = await Promise.all([
          supplierService.getAll(),
          assetService.getAll(),
        ]);
        if (!cancelled) {
          setSuppliers(sup);
          setAssets(ast);
        }
      } catch {
        if (!cancelled) {
          setSuppliers([]);
          setAssets([]);
        }
      } finally {
        if (!cancelled) setMetaLoading(false);
      }
    })();
    return () => {
      cancelled = true;
    };
  }, [open]);

  useEffect(() => {
    if (!open) return;
    if (mode === 'edit' && initial) {
      form.setFieldsValue({
        supplierId: initial.supplierId,
        currency: initial.currency || 'VND',
        assetRequestId: initial.assetRequestId ?? undefined,
      });
      setLines(initial.lines.length > 0 ? toRows(initial.lines) : [emptyRow()]);
    } else {
      form.setFieldsValue({
        supplierId: undefined,
        currency: 'VND',
        assetRequestId: undefined,
      });
      setLines([emptyRow()]);
    }
  }, [open, mode, initial, form]);

  const assetOptions = assets.map((a) => ({
    value: a.assetId,
    label: `${a.code} — ${a.name}`,
  }));

  const updateLine = (key: string, patch: Partial<LineRow>) => {
    setLines((prev) => prev.map((r) => (r.key === key ? { ...r, ...patch } : r)));
  };

  const columns: ColumnsType<LineRow> = [
    {
      title: 'Mô tả',
      dataIndex: 'description',
      width: 200,
      render: (_, row) => (
        <TextArea
          rows={2}
          value={row.description}
          onChange={(e) => updateLine(row.key, { description: e.target.value })}
          placeholder="Mô tả hàng hóa / dịch vụ"
        />
      ),
    },
    {
      title: 'Tài sản',
      width: 220,
      render: (_, row) => (
        <Select
          allowClear
          showSearch
          optionFilterProp="label"
          placeholder="Chọn tài sản"
          style={{ width: '100%' }}
          value={row.assetId ?? undefined}
          onChange={(v) => updateLine(row.key, { assetId: v ?? null })}
          options={assetOptions}
        />
      ),
    },
    {
      title: 'SL',
      width: 90,
      render: (_, row) => (
        <InputNumber
          min={0.0001}
          step={1}
          style={{ width: '100%' }}
          value={row.quantity}
          onChange={(v) => updateLine(row.key, { quantity: Number(v) || 1 })}
        />
      ),
    },
    {
      title: 'ĐVT',
      width: 90,
      render: (_, row) => (
        <Input
          value={row.unit}
          onChange={(e) => updateLine(row.key, { unit: e.target.value })}
        />
      ),
    },
    {
      title: 'Đơn giá',
      width: 120,
      render: (_, row) => (
        <InputNumber
          min={0}
          style={{ width: '100%' }}
          value={row.unitPrice}
          onChange={(v) => updateLine(row.key, { unitPrice: Number(v) || 0 })}
        />
      ),
    },
    {
      title: 'Ngày giao dự kiến',
      width: 140,
      render: (_, row) => (
        <DatePicker
          style={{ width: '100%' }}
          value={row.expectedDelivery}
          onChange={(d) => updateLine(row.key, { expectedDelivery: d })}
          format="DD/MM/YYYY"
        />
      ),
    },
    {
      title: '',
      width: 48,
      render: (_, row) => (
        <Button
          type="link"
          danger
          disabled={lines.length <= 1}
          onClick={() => setLines((prev) => prev.filter((r) => r.key !== row.key))}
        >
          Xóa
        </Button>
      ),
    },
  ];

  const handleOk = async () => {
    try {
      const v = await form.validateFields();
      const payloadLines: PurchaseOrderLineWrite[] = lines.map((r) => ({
        description: r.description.trim() || null,
        assetId: r.assetId,
        quantity: r.quantity,
        unit: r.unit.trim() || null,
        unitPrice: r.unitPrice,
        expectedDeliveryDate: r.expectedDelivery
          ? r.expectedDelivery.format('YYYY-MM-DD')
          : null,
      }));
      const kept = payloadLines.filter((l) => l.quantity > 0);
      if (kept.length === 0 && mode !== 'edit') {
        message.warning('Cần ít nhất một dòng hàng với số lượng lớn hơn 0.');
        return;
      }
      setLoading(true);
      await onSubmit({
        supplierId: v.supplierId,
        currency: v.currency,
        assetRequestId: v.assetRequestId ?? null,
        lines: kept.length > 0 ? kept : [],
      });
      onClose();
    } catch {
      // validation
    } finally {
      setLoading(false);
    }
  };

  return (
    <Modal
      title={mode === 'edit' ? 'Cập nhật đơn mua' : 'Tạo đơn mua'}
      open={open}
      onCancel={onClose}
      width={960}
      okText={mode === 'edit' ? 'Lưu' : 'Tạo đơn'}
      confirmLoading={loading}
      onOk={handleOk}
      destroyOnClose
    >
      <Form form={form} layout="vertical" disabled={metaLoading}>
        <Space wrap style={{ width: '100%' }}>
          <Form.Item
            name="supplierId"
            label="Nhà cung cấp"
            rules={[{ required: true, message: 'Chọn NCC' }]}
            style={{ minWidth: 260 }}
          >
            <Select
              showSearch
              optionFilterProp="label"
              placeholder="Chọn nhà cung cấp"
              options={suppliers.map((s) => ({
                value: s.supplierId,
                label: `${s.code} — ${s.name}`,
              }))}
            />
          </Form.Item>
          <Form.Item
            name="currency"
            label="Tiền tệ"
            rules={[{ required: true }]}
            style={{ width: 140 }}
          >
            <Select options={CURRENCIES.map((c) => ({ value: c, label: c }))} />
          </Form.Item>
          <Form.Item name="assetRequestId" label="Mã yêu cầu (tuỳ chọn)" style={{ width: 160 }}>
            <InputNumber min={1} style={{ width: '100%' }} placeholder="AssetRequestId" />
          </Form.Item>
        </Space>
      </Form>
      <div style={{ marginBottom: 8 }}>
        <Button type="dashed" onClick={() => setLines((prev) => [...prev, emptyRow()])}>
          + Thêm dòng
        </Button>
      </div>
      <Table
        size="small"
        pagination={false}
        columns={columns}
        dataSource={lines}
        scroll={{ x: 900 }}
      />
    </Modal>
  );
}

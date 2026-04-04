import { useCallback, useEffect, useMemo, useState } from 'react';
import { Link } from 'react-router-dom';
import { Button, Input, InputNumber, Modal, Select, Space, Table, message } from 'antd';
import type { ColumnsType } from 'antd/es/table';
import { DeleteOutlined, PlusOutlined, ReloadOutlined, FormOutlined } from '@ant-design/icons';
import {
  allocationRequestApiErrorMessage,
  allocationRequestService,
  type AllocationRequestListItem,
  type CatalogAssetOption,
} from '../services/allocationRequestService';
import { handoverRequestService } from '../services/handoverRequestService';
import axios from 'axios';
import { assetTypeService, type AssetTypeListItem } from '../../admin/services/assetTypeService';
import { profileService } from '../../profile/services/profileService';
import './AccountantAllocationsPage.css';
import '../../requests/pages/RequestsPage.css';

function formatDateVi(iso: string): string {
  try {
    return new Date(iso).toLocaleDateString('vi-VN');
  } catch {
    return iso?.slice(0, 10) ?? '';
  }
}

function deptStatusPillClass(color: string): string {
  if (color === 'green') return 'asset-status-pill asset-status-pill--active';
  if (color === 'red') return 'asset-status-pill asset-status-pill--danger';
  if (color === 'gold') return 'asset-status-pill asset-status-pill--warning';
  if (color === 'blue') return 'asset-status-pill asset-status-pill--processing';
  if (color === 'default') return 'asset-status-pill asset-status-pill--inactive';
  return 'asset-status-pill';
}

export type DepartmentRequestsMode = 'allocation' | 'handover';

function statusLabel(mode: DepartmentRequestsMode, status: number): { text: string; color: string } {
  switch (status) {
    case 0:
      return { text: 'Chờ kế toán', color: 'gold' };
    case 2:
      return { text: 'Đã duyệt — chờ xác nhận', color: 'blue' };
    case 3:
      return { text: 'Từ chối', color: 'red' };
    case 4:
      return {
        text: mode === 'handover' ? 'Hoàn tất thu hồi' : 'Hoàn tất cấp phát',
        color: 'green',
      };
    default:
      return { text: `Trạng thái ${status}`, color: 'default' };
  }
}

type FormRow = {
  key: string;
  assetTypeId?: number;
  assetId?: number;
  quantity?: number;
  reason?: string;
  maxQty?: number;
};

let rowSeq = 0;
function newKey() {
  rowSeq += 1;
  return `r-${rowSeq}`;
}

const DEFAULT_TITLES: Record<DepartmentRequestsMode, string> = {
  allocation: 'Yêu cầu cấp phát tài sản',
  handover: 'Yêu cầu thu hồi tài sản về kho',
};

export function DepartmentRequestsByModePanel({ mode }: { mode: DepartmentRequestsMode }) {
  const [profileDept, setProfileDept] = useState<string | null>(null);
  const [types, setTypes] = useState<AssetTypeListItem[]>([]);
  const [rows, setRows] = useState<FormRow[]>([{ key: newKey() }]);
  const [title, setTitle] = useState(DEFAULT_TITLES[mode]);
  const [submitting, setSubmitting] = useState(false);
  const [formModalOpen, setFormModalOpen] = useState(false);

  const [assetOptionsByRow, setAssetOptionsByRow] = useState<Record<string, CatalogAssetOption[]>>({});
  const [listLoading, setListLoading] = useState(false);
  const [requests, setRequests] = useState<AllocationRequestListItem[]>([]);
  const [requestPage, setRequestPage] = useState(1);
  const [requestPageSize, setRequestPageSize] = useState(10);

  const loadTypes = useCallback(async () => {
    try {
      const t = await assetTypeService.getAll();
      setTypes(t);
    } catch {
      message.error('Không tải được danh sách loại tài sản.');
    }
  }, []);

  const loadRequests = useCallback(async () => {
    setListLoading(true);
    try {
      const list =
        mode === 'handover' ? await handoverRequestService.list() : await allocationRequestService.list();
      setRequests(list);
      setRequestPage(1);
    } catch {
      message.error('Không tải được danh sách yêu cầu.');
    } finally {
      setListLoading(false);
    }
  }, [mode]);

  useEffect(() => {
    void loadTypes();
    void loadRequests();
    void profileService
      .getProfile()
      .then((p) => setProfileDept(p.departmentName ?? null))
      .catch(() => setProfileDept(null));
  }, [loadRequests, loadTypes]);

  useEffect(() => {
    setTitle(DEFAULT_TITLES[mode]);
  }, [mode]);

  const updateRow = useCallback((key: string, patch: Partial<FormRow>) => {
    setRows((prev) => prev.map((r) => (r.key === key ? { ...r, ...patch } : r)));
  }, []);

  const loadAssetsForRow = useCallback(async (rowKey: string, typeId: number, search?: string) => {
    try {
      const list = await allocationRequestService.catalogByType(typeId, search);
      setAssetOptionsByRow((m) => ({ ...m, [rowKey]: list }));
    } catch {
      message.error('Không tải được danh mục tài sản.');
    }
  }, []);

  const onTypeChange = useCallback(
    (rowKey: string, typeId: number | undefined) => {
      updateRow(rowKey, {
        assetTypeId: typeId,
        assetId: undefined,
        quantity: undefined,
        maxQty: undefined,
      });
      if (typeId) void loadAssetsForRow(rowKey, typeId);
      else
        setAssetOptionsByRow((m) => {
          const next = { ...m };
          delete next[rowKey];
          return next;
        });
    },
    [loadAssetsForRow, updateRow],
  );

  const onAssetChange = useCallback(
    async (rowKey: string, assetId: number | undefined) => {
      updateRow(rowKey, { assetId, quantity: undefined, maxQty: undefined });
      if (!assetId) return;
      try {
        const n =
          mode === 'handover'
            ? await handoverRequestService.departmentAssigned(assetId)
            : await allocationRequestService.warehouseAvailable(assetId);
        updateRow(rowKey, { maxQty: n, quantity: n > 0 ? 1 : undefined });
      } catch {
        message.error(mode === 'handover' ? 'Không kiểm tra được SL tại phòng ban.' : 'Không kiểm tra được tồn kho.');
      }
    },
    [mode, updateRow],
  );

  const addRow = useCallback(() => {
    setRows((r) => [...r, { key: newKey() }]);
  }, []);

  const removeRow = useCallback((key: string) => {
    setRows((r) => (r.length <= 1 ? r : r.filter((x) => x.key !== key)));
    setAssetOptionsByRow((m) => {
      const next = { ...m };
      delete next[key];
      return next;
    });
  }, []);

  const resetRequestForm = useCallback(() => {
    setRows([{ key: newKey() }]);
    setTitle(DEFAULT_TITLES[mode]);
    setAssetOptionsByRow({});
  }, [mode]);

  const openRequestModal = useCallback(() => {
    resetRequestForm();
    setFormModalOpen(true);
  }, [resetRequestForm]);

  const closeRequestModal = useCallback(() => {
    setFormModalOpen(false);
    resetRequestForm();
  }, [resetRequestForm]);

  const submit = useCallback(async () => {
    const lines = rows
      .filter(
        (r) =>
          r.assetTypeId &&
          r.assetId &&
          r.quantity &&
          r.quantity > 0 &&
          (r.maxQty == null || r.quantity <= r.maxQty),
      )
      .map((r) => ({
        assetTypeId: r.assetTypeId!,
        assetId: r.assetId!,
        quantity: r.quantity!,
        reason: r.reason?.trim() || null,
      }));

    if (!title.trim()) {
      message.warning('Nhập tiêu đề yêu cầu.');
      return;
    }
    if (lines.length === 0) {
      message.warning(
        mode === 'handover'
          ? 'Thêm ít nhất một dòng hợp lệ (loại, tài sản, số lượng tại phòng ban).'
          : 'Thêm ít nhất một dòng hợp lệ (loại, tài sản, số lượng trong tồn kho).',
      );
      return;
    }

    for (const r of rows) {
      if (r.assetId && r.quantity && r.maxQty != null && r.quantity > r.maxQty) {
        message.warning('Số lượng vượt quá giới hạn cho phép.');
        return;
      }
    }

    setSubmitting(true);
    try {
      if (mode === 'handover') {
        await handoverRequestService.create({ title: title.trim(), lines });
        message.success('Đã gửi yêu cầu thu hồi. Kế toán sẽ nhận thông báo.');
      } else {
        await allocationRequestService.create({ title: title.trim(), lines });
        message.success('Đã gửi yêu cầu. Kế toán sẽ nhận thông báo.');
      }
      closeRequestModal();
      await loadRequests();
    } catch (e) {
      if (e instanceof Error && !axios.isAxiosError(e)) {
        message.error(e.message || 'Gửi yêu cầu thất bại.');
      } else {
        const fromAxios =
          axios.isAxiosError(e) && e.response?.data ? allocationRequestApiErrorMessage(e.response.data) : null;
        message.error(fromAxios ?? (e instanceof Error ? e.message : 'Gửi yêu cầu thất bại.'));
      }
    } finally {
      setSubmitting(false);
    }
  }, [closeRequestModal, loadRequests, mode, rows, title]);

  const orderPath = mode === 'handover' ? 'handover-order' : 'order';
  const orderLinkLabel = mode === 'handover' ? 'Đơn thu hồi' : 'Đơn cấp phát';
  const receiptCol = mode === 'handover' ? 'Xác nhận trả' : 'Nhận TS';

  const lineColumns: ColumnsType<FormRow> = useMemo(
    () => [
      {
        title: 'Loại TS',
        key: 'assetTypeId',
        width: 168,
        render: (_, row) => (
          <Select
            showSearch
            allowClear
            placeholder="Chọn loại"
            className="alloc-request-lines-table__select"
            style={{ width: '100%', minWidth: 0 }}
            popupMatchSelectWidth={false}
            options={types.map((t) => ({
              value: t.assetTypeId,
              label: `${t.name} (${t.categoryName})`,
            }))}
            optionFilterProp="label"
            value={row.assetTypeId}
            onChange={(v) => onTypeChange(row.key, v ?? undefined)}
          />
        ),
      },
      {
        title: 'Tài sản',
        key: 'assetId',
        width: 200,
        ellipsis: true,
        render: (_, row) => (
          <Select
            showSearch
            allowClear
            disabled={!row.assetTypeId}
            placeholder={row.assetTypeId ? 'Chọn tài sản' : '—'}
            className="alloc-request-lines-table__select"
            style={{ width: '100%', minWidth: 0 }}
            popupMatchSelectWidth={280}
            options={(assetOptionsByRow[row.key] ?? []).map((a) => ({
              value: a.assetId,
              label: `${a.name} (${a.code})`,
            }))}
            optionFilterProp="label"
            value={row.assetId}
            onSearch={(q) => {
              if (row.assetTypeId) void loadAssetsForRow(row.key, row.assetTypeId, q);
            }}
            onChange={(v) => void onAssetChange(row.key, v ?? undefined)}
            filterOption={false}
            notFoundContent={row.assetTypeId ? undefined : null}
          />
        ),
      },
      {
        title: 'SL',
        key: 'quantity',
        width: 88,
        align: 'center',
        render: (_, row) => (
          <div>
            <InputNumber
              min={1}
              max={row.maxQty ?? undefined}
              size="small"
              style={{ width: '100%' }}
              disabled={!row.assetId}
              controls
              value={row.quantity}
              onChange={(v) => updateRow(row.key, { quantity: typeof v === 'number' ? v : undefined })}
            />
            {row.maxQty != null && (
              <div className="alloc-muted" style={{ marginTop: 2, fontSize: 11, lineHeight: 1.2, whiteSpace: 'nowrap' }}>
                max {row.maxQty}
              </div>
            )}
          </div>
        ),
      },
      {
        title: 'Lý do',
        key: 'reason',
        render: (_, row) => (
          <Input
            size="small"
            value={row.reason}
            onChange={(e) => updateRow(row.key, { reason: e.target.value })}
            placeholder="Ghi chú"
          />
        ),
      },
      {
        title: '',
        key: 'actions',
        width: 40,
        align: 'center',
        render: (_, row) => (
          <Button
            type="text"
            danger
            size="small"
            icon={<DeleteOutlined />}
            disabled={rows.length <= 1}
            onClick={() => removeRow(row.key)}
            aria-label="Xóa dòng"
          />
        ),
      },
    ],
    [
      assetOptionsByRow,
      loadAssetsForRow,
      onAssetChange,
      onTypeChange,
      removeRow,
      rows.length,
      types,
      updateRow,
    ],
  );

  const requestTotal = requests.length;
  const requestTotalPages = Math.max(1, Math.ceil(requestTotal / requestPageSize));
  const safeRequestPage = Math.min(requestPage, requestTotalPages);
  const requestStart = requestTotal === 0 ? 0 : (safeRequestPage - 1) * requestPageSize + 1;
  const requestEnd = Math.min(safeRequestPage * requestPageSize, requestTotal);
  const pagedRequests = useMemo(
    () => requests.slice((safeRequestPage - 1) * requestPageSize, safeRequestPage * requestPageSize),
    [requests, safeRequestPage, requestPageSize],
  );

  const pageSubtitle =
    mode === 'handover'
      ? 'Gửi danh sách tài sản trả về kho'
      : 'Gửi danh sách tài sản cần cấp về phòng ban';
  const modalTitle = mode === 'handover' ? 'Gửi yêu cầu thu hồi' : 'Gửi yêu cầu cấp phát';
  const listHeading = mode === 'handover' ? 'Yêu cầu thu hồi của phòng ban' : 'Yêu cầu của phòng ban';

  return (
    <>
      <div
        className="requests-filters"
        style={{ justifyContent: 'space-between', width: '100%', marginBottom: 12, flexWrap: 'wrap' }}
      >
        <span style={{ color: '#6b7280', fontSize: 13 }}>
          {pageSubtitle}
          {profileDept ? ` · ${profileDept}` : ''}
        </span>
        <Space wrap>
          <Button type="primary" icon={<FormOutlined />} onClick={openRequestModal}>
            Tạo yêu cầu
          </Button>
          <Button icon={<ReloadOutlined />} onClick={() => void loadRequests()}>
            Làm mới danh sách
          </Button>
        </Space>
      </div>

      <Modal
        title={modalTitle}
        open={formModalOpen}
        onCancel={closeRequestModal}
        width={720}
        footer={[
          <Button key="cancel" onClick={closeRequestModal}>
            Hủy
          </Button>,
          <Button key="submit" type="primary" loading={submitting} onClick={() => void submit()}>
            Gửi yêu cầu
          </Button>,
        ]}
        destroyOnHidden
        styles={{ body: { maxHeight: 'min(70vh, 640px)', overflowY: 'auto', paddingTop: 8 } }}
      >
        <Space orientation="vertical" style={{ width: '100%' }} size="middle">
          <div>
            <div style={{ marginBottom: 6, fontSize: 13 }}>Tiêu đề</div>
            <Input value={title} onChange={(e) => setTitle(e.target.value)} maxLength={255} placeholder={DEFAULT_TITLES[mode]} />
          </div>

          <Table<FormRow>
            className="alloc-request-lines-table"
            size="small"
            rowKey="key"
            columns={lineColumns}
            dataSource={rows}
            pagination={false}
            tableLayout="fixed"
            scroll={{ x: 640 }}
          />

          <Button icon={<PlusOutlined />} onClick={addRow}>
            Thêm dòng
          </Button>
        </Space>
      </Modal>

      <p style={{ fontWeight: 600, margin: '0 0 8px', fontSize: 15, color: '#111827' }}>{listHeading}</p>

      <div className="asset-table-wrapper requests-table-wrapper">
        {listLoading ? (
          <div className="requests-table-loading">Đang tải danh sách...</div>
        ) : (
          <table className="asset-table requests-table">
            <thead>
              <tr>
                <th>MÃ YÊU CẦU</th>
                <th>NGÀY GỬI</th>
                <th>TIÊU ĐỀ</th>
                <th>NGƯỜI YÊU CẦU</th>
                <th>TRẠNG THÁI</th>
                <th>{receiptCol.toUpperCase()}</th>
                <th className="asset-table__cell asset-table__cell--actions" />
              </tr>
            </thead>
            <tbody>
              {pagedRequests.length === 0 ? (
                <tr>
                  <td colSpan={7} className="requests-table-empty">
                    Không có dữ liệu.
                  </td>
                </tr>
              ) : (
                pagedRequests.map((row) => {
                  const st = statusLabel(mode, row.status);
                  return (
                    <tr key={row.assetRequestId} className="asset-row">
                      <td>YC-{row.assetRequestId}</td>
                      <td>{formatDateVi(row.createDate)}</td>
                      <td>{row.title}</td>
                      <td>{row.requestedByName}</td>
                      <td>
                        <span className={deptStatusPillClass(st.color)}>{st.text}</span>
                      </td>
                      <td>
                        {row.receiptConfirmedAt ? formatDateVi(row.receiptConfirmedAt) : '—'}
                      </td>
                      <td className="asset-table__cell asset-table__cell--actions">
                        <div className="requests-actions">
                          {row.assetAllocationOrderId != null && row.status >= 2 ? (
                            <Link
                              className="asset-code asset-code--link"
                              to={`/allocations/${orderPath}/${row.assetAllocationOrderId}`}
                            >
                              {orderLinkLabel}
                            </Link>
                          ) : (
                            <span style={{ color: '#9ca3af', fontSize: 13 }}>—</span>
                          )}
                        </div>
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
            value={requestPageSize}
            onChange={(e) => {
              setRequestPageSize(Number(e.target.value));
              setRequestPage(1);
            }}
          >
            <option value={8}>8</option>
            <option value={10}>10</option>
            <option value={25}>25</option>
          </select>
        </div>
        <div className="requests-footer__center">
          {requestTotal === 0 ? '0-0 trên 0' : `${requestStart}-${requestEnd} trên ${requestTotal}`}
        </div>
        <div className="requests-footer__right">
          <button
            className="requests-footer__pager"
            disabled={safeRequestPage <= 1}
            onClick={() => setRequestPage((p) => Math.max(1, p - 1))}
            type="button"
          >
            ⟨
          </button>
          <button className="requests-footer__pager requests-footer__pager--active" type="button">
            {safeRequestPage}
          </button>
          <button
            className="requests-footer__pager"
            disabled={safeRequestPage >= requestTotalPages}
            onClick={() => setRequestPage((p) => Math.min(requestTotalPages, p + 1))}
            type="button"
          >
            ⟩
          </button>
        </div>
      </div>
    </>
  );
}

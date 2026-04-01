import { useCallback, useEffect, useMemo, useRef, useState } from 'react';
import { Button, Input, Modal, Pagination, Segmented, Select, Spin, message } from 'antd';
import { DeleteOutlined, EyeOutlined, PlusOutlined, ReloadOutlined, RollbackOutlined, SearchOutlined } from '@ant-design/icons';
import { transferRequestService } from '../../assets/services/transferRequestService';
import { assetCategoryService, type AssetCategoryItem } from '../../admin/services/assetCategoryService';
import {
  budgetAllocationService,
  type AssetInstanceOption,
  type BudgetAllocationListItem,
} from '../services/budgetAllocationService';
import './AccountantAllocationsPage.css';

const { TextArea } = Input;

const DEPT_COLORS = [
  '#2563eb',
  '#0891b2',
  '#059669',
  '#dc2626',
  '#7c3aed',
  '#ca8a04',
  '#ea580c',
  '#4f46e5',
];

type TxStatus = 'allocated' | 'recalled';

interface DeptRow {
  id: string;
  name: string;
  budget: number;
}

interface BudgetTx {
  id: string;
  name: string;
  cat: string;
  deptId: string;
  date: string;
  status: TxStatus;
  submittedBy: string;
}

const FALLBACK_DEPTS: DeptRow[] = [
  { id: 'all', name: 'Tất cả phòng ban', budget: 5_000_000_000 },
  { id: 'it', name: 'Phòng Công nghệ', budget: 900_000_000 },
  { id: 'hr', name: 'Nhân sự & Đào tạo', budget: 600_000_000 },
  { id: 'mkt', name: 'Marketing', budget: 800_000_000 },
  { id: 'fin', name: 'Tài chính - Kế toán', budget: 500_000_000 },
  { id: 'ops', name: 'Vận hành', budget: 700_000_000 },
  { id: 'rd', name: 'R&D', budget: 1_000_000_000 },
  { id: 'admin', name: 'Hành chính', budget: 500_000_000 },
];

const SEED_TEMPLATE: Omit<BudgetTx, 'id' | 'deptId'>[] = [
  { name: 'Laptop Dell XPS 15 — LD001', cat: 'Thiết bị IT', date: '2025-01-10', status: 'allocated', submittedBy: 'ketoan@demo.com' },
  { name: 'Máy in HP — IN002', cat: 'Thiết bị IT', date: '2025-02-01', status: 'recalled', submittedBy: 'ketoan@demo.com' },
  { name: 'Bàn làm việc — BF003', cat: 'Văn phòng phẩm', date: '2025-02-10', status: 'allocated', submittedBy: 'ketoan@demo.com' },
];

function buildSeedTxs(deptIds: string[]): BudgetTx[] {
  if (!deptIds.length) return [];
  return SEED_TEMPLATE.map((row, i) => ({
    ...row,
    id: `TX${String(i + 1).padStart(3, '0')}`,
    deptId: deptIds[i % deptIds.length],
  }));
}

function mapApiToTx(row: BudgetAllocationListItem): BudgetTx {
  const st = row.status === 'recalled' ? 'recalled' : 'allocated';
  return {
    id: String(row.id),
    name: row.name,
    cat: row.category,
    deptId: String(row.departmentId),
    date: row.date,
    status: st,
    submittedBy: row.submittedBy,
  };
}

function getDeptColor(depts: DeptRow[], deptId: string): string {
  const idx = depts.findIndex((d) => d.id === deptId);
  if (idx <= 0) return '#9ca3af';
  return DEPT_COLORS[(idx - 1 + DEPT_COLORS.length) % DEPT_COLORS.length];
}

const PAGE_SIZE = 8;

const TABLE_TITLE: Record<'all' | TxStatus, string> = {
  all: 'Tất cả giao dịch',
  allocated: 'Cấp phát tài sản',
  recalled: 'Thu hồi từ phòng ban',
};

const STATUS_LABEL: Record<TxStatus, string> = {
  allocated: 'Đã cấp phát',
  recalled: 'Đã thu hồi',
};

const STATUS_ICON: Record<TxStatus, string> = {
  allocated: '✓',
  recalled: '↩',
};

const SEGMENT_OPTIONS = [
  { label: 'Tất cả', value: 'all' },
  { label: 'Cấp phát', value: 'allocated' },
  { label: 'Thu hồi', value: 'recalled' },
] as const satisfies ReadonlyArray<{ label: string; value: 'all' | TxStatus }>;

export function AccountantAllocationsPage() {
  const [depts, setDepts] = useState<DeptRow[]>(FALLBACK_DEPTS);
  const [txs, setTxs] = useState<BudgetTx[]>([]);
  const seededFromApi = useRef(false);

  const [categories, setCategories] = useState<AssetCategoryItem[]>([]);

  const [filterType, setFilterType] = useState<'all' | TxStatus>('all');
  const [curDept, setCurDept] = useState('all');
  const [search, setSearch] = useState('');
  const [curPage, setCurPage] = useState(1);
  const [selectedIds, setSelectedIds] = useState<Set<string>>(() => new Set());

  const [modalOpen, setModalOpen] = useState(false);
  const [modalMode, setModalMode] = useState<'alloc' | 'recall'>('alloc');
  const [formDept, setFormDept] = useState<string | undefined>(undefined);
  const [formCategoryId, setFormCategoryId] = useState<number | undefined>(undefined);
  const [formInstanceId, setFormInstanceId] = useState<number | undefined>(undefined);
  const [instanceOptions, setInstanceOptions] = useState<AssetInstanceOption[]>([]);
  const [instanceLoading, setInstanceLoading] = useState(false);
  const [instanceSearchInput, setInstanceSearchInput] = useState('');
  const [debouncedInstanceQ, setDebouncedInstanceQ] = useState('');
  const instanceSearchTimer = useRef<ReturnType<typeof setTimeout> | null>(null);

  const [formDate, setFormDate] = useState('');
  const [formNote, setFormNote] = useState('');

  const loadBudgetRows = useCallback(async () => {
    try {
      const list = await budgetAllocationService.list();
      setTxs(list.map(mapApiToTx));
      return true;
    } catch {
      return false;
    }
  }, []);

  useEffect(() => {
    void assetCategoryService.getAll().then(setCategories).catch(() => setCategories([]));
  }, []);

  useEffect(() => {
    let cancelled = false;
    (async () => {
      const [locs, apiOk] = await Promise.all([
        transferRequestService.getAssetLocations().catch(() => [] as { locationId: number; displayName: string }[]),
        loadBudgetRows(),
      ]);
      if (cancelled) return;

      if (locs.length) {
        const mapped: DeptRow[] = [
          { id: 'all', name: 'Tất cả phòng ban', budget: 5_000_000_000 },
          ...locs.map((d) => ({
            id: String(d.locationId),
            name: d.displayName,
            budget: 500_000_000,
          })),
        ];
        setDepts(mapped);
        if (!seededFromApi.current) {
          setCurDept('all');
          setCurPage(1);
        }
      }

      if (!apiOk && !seededFromApi.current) {
        const deptSource = locs.length
          ? locs.map((d) => String(d.locationId))
          : FALLBACK_DEPTS.filter((d) => d.id !== 'all').map((d) => d.id);
        setTxs(buildSeedTxs(deptSource));
      }
      seededFromApi.current = true;
    })();
    return () => {
      cancelled = true;
    };
  }, [loadBudgetRows]);

  useEffect(() => {
    if (instanceSearchTimer.current) clearTimeout(instanceSearchTimer.current);
    instanceSearchTimer.current = setTimeout(() => setDebouncedInstanceQ(instanceSearchInput), 300);
    return () => {
      if (instanceSearchTimer.current) clearTimeout(instanceSearchTimer.current);
    };
  }, [instanceSearchInput]);

  const loadInstanceOptions = useCallback(async () => {
    if (!formDept || formCategoryId == null || !modalOpen) {
      setInstanceOptions([]);
      return;
    }
    const departmentId = Number(formDept);
    if (!Number.isFinite(departmentId)) return;
    setInstanceLoading(true);
    try {
      const mode = modalMode === 'alloc' ? 'assign' : 'recall';
      const list = await budgetAllocationService.getAssetInstanceOptions({
        categoryId: formCategoryId,
        departmentId,
        mode,
        search: debouncedInstanceQ || undefined,
      });
      setInstanceOptions(list);
    } catch {
      setInstanceOptions([]);
      message.error('Không tải được danh sách tài sản.');
    } finally {
      setInstanceLoading(false);
    }
  }, [formDept, formCategoryId, modalMode, modalOpen, debouncedInstanceQ]);

  useEffect(() => {
    void loadInstanceOptions();
  }, [loadInstanceOptions]);

  const getDeptName = useCallback(
    (deptId: string) => depts.find((d) => d.id === deptId)?.name ?? deptId,
    [depts]
  );

  const filtered = useMemo(() => {
    const q = search.trim().toLowerCase();
    return txs.filter((t) => {
      const deptOk = curDept === 'all' || t.deptId === curDept;
      const typeOk = filterType === 'all' || t.status === filterType;
      const searchOk =
        !q ||
        t.name.toLowerCase().includes(q) ||
        getDeptName(t.deptId).toLowerCase().includes(q) ||
        t.id.toLowerCase().includes(q) ||
        t.cat.toLowerCase().includes(q);
      return deptOk && typeOk && searchOk;
    });
  }, [txs, curDept, filterType, search, getDeptName]);

  const totalPages = Math.max(1, Math.ceil(filtered.length / PAGE_SIZE));
  const pageSafe = Math.min(curPage, totalPages);
  const slice = filtered.slice((pageSafe - 1) * PAGE_SIZE, pageSafe * PAGE_SIZE);
  const pageIds = useMemo(() => slice.map((t) => t.id), [slice]);
  const allPageSelected = pageIds.length > 0 && pageIds.every((id) => selectedIds.has(id));

  useEffect(() => {
    if (curPage > totalPages) setCurPage(totalPages);
  }, [curPage, totalPages]);

  const toggleSelectAllOnPage = (checked: boolean) => {
    setSelectedIds((prev) => {
      const next = new Set(prev);
      if (checked) pageIds.forEach((id) => next.add(id));
      else pageIds.forEach((id) => next.delete(id));
      return next;
    });
  };

  const toggleRowSelected = (id: string, checked: boolean) => {
    setSelectedIds((prev) => {
      const next = new Set(prev);
      if (checked) next.add(id);
      else next.delete(id);
      return next;
    });
  };

  const openModal = (mode: 'alloc' | 'recall') => {
    setModalMode(mode);
    setFormDate(new Date().toISOString().slice(0, 10));
    setFormDept(undefined);
    setFormCategoryId(undefined);
    setFormInstanceId(undefined);
    setInstanceOptions([]);
    setInstanceSearchInput('');
    setDebouncedInstanceQ('');
    setFormNote('');
    setModalOpen(true);
  };

  const closeModal = () => {
    setModalOpen(false);
    setFormDept(undefined);
    setFormCategoryId(undefined);
    setFormInstanceId(undefined);
    setInstanceOptions([]);
    setInstanceSearchInput('');
    setFormNote('');
  };

  const submitForm = async () => {
    if (!formDept || formCategoryId == null || formInstanceId == null) {
      message.error('Vui lòng chọn phòng ban, nhóm tài sản và một tài sản.');
      return;
    }
    const departmentId = Number(formDept);
    try {
      const created = await budgetAllocationService.create({
        departmentId,
        assetCategoryId: formCategoryId,
        assetInstanceId: formInstanceId,
        transactionDate: formDate || null,
        note: formNote.trim() || null,
        isRecall: modalMode === 'recall',
      });
      setTxs((prev) => [mapApiToTx(created), ...prev]);
      closeModal();
      message.success(
        modalMode === 'alloc'
          ? 'Đã cấp phát tài sản cho phòng ban.'
          : 'Đã thu hồi tài sản khỏi phòng ban.'
      );
    } catch {
      message.error('Không thực hiện được. Kiểm tra đăng nhập kế toán (ACCOUNTANT) và dữ liệu.');
    }
  };

  const deleteTx = async (id: string) => {
    const numericId = Number(id);
    if (!Number.isFinite(numericId)) return;
    try {
      await budgetAllocationService.remove(numericId);
      setTxs((prev) => prev.filter((t) => t.id !== id));
      setSelectedIds((prev) => {
        const next = new Set(prev);
        next.delete(id);
        return next;
      });
      message.success('Đã xóa bản ghi lịch sử.');
    } catch {
      message.error('Không xóa được bản ghi.');
    }
  };

  const selectedDept = depts.find((d) => d.id === curDept);
  const allocCountForDept =
    selectedDept && curDept !== 'all'
      ? txs.filter((t) => t.deptId === curDept && t.status === 'allocated').length
      : 0;
  const recallCountForDept =
    selectedDept && curDept !== 'all'
      ? txs.filter((t) => t.deptId === curDept && t.status === 'recalled').length
      : 0;

  const modalTitle =
    modalMode === 'alloc' ? 'Cấp phát tài sản cho phòng ban' : 'Thu hồi tài sản từ phòng ban';
  const modalSubtitle =
    modalMode === 'alloc'
      ? 'Chỉ hiển thị tài sản chưa gán phòng ban nào; chọn phòng ban nhận tài sản.'
      : 'Chỉ hiển thị tài sản đang được gán cho phòng ban đã chọn.';

  return (
    <div className="alloc-page">
      <div className="alloc-page__header">
        <div className="alloc-page__header-main">
          <div>
            <h1 className="alloc-page__title">Cấp phát &amp; Thu hồi</h1>
          </div>
        </div>
      </div>

      <div className="alloc-page__card">
        <div className="alloc-page__toolbar">
          <Segmented<'all' | TxStatus>
            className="alloc-page__segmented"
            options={[...SEGMENT_OPTIONS]}
            value={filterType}
            onChange={(v) => {
              setFilterType(v);
              setCurPage(1);
            }}
          />
          <div className="alloc-page__toolbar-grow" />
          <Input
            className="alloc-page__search"
            placeholder="Tìm kiếm..."
            value={search}
            onChange={(e) => {
              setSearch(e.target.value);
              setCurPage(1);
            }}
            allowClear
            prefix={<SearchOutlined style={{ color: '#9ca3af' }} />}
          />
          <Button danger icon={<RollbackOutlined />} onClick={() => openModal('recall')}>
            Thu hồi
          </Button>
          <Button type="primary" icon={<PlusOutlined />} onClick={() => openModal('alloc')}>
            Cấp phát mới
          </Button>
        </div>

        <div className="alloc-content-grid">
          <aside className="alloc-dept-sidebar">
            <div className="alloc-sidebar-title">Phòng ban</div>
            {depts.map((d) => {
              const count =
                d.id === 'all' ? txs.length : txs.filter((t) => t.deptId === d.id).length;
              const col = d.id === 'all' ? '#9ca3af' : getDeptColor(depts, d.id);
              return (
                <button
                  key={d.id}
                  type="button"
                  className={`alloc-dept-item${curDept === d.id ? ' alloc-dept-item--active' : ''}`}
                  onClick={() => {
                    setCurDept(d.id);
                    setCurPage(1);
                  }}
                >
                  <span className="alloc-dept-dot" style={{ background: col }} />
                  <span className="alloc-dept-name">{d.name}</span>
                  <span className="alloc-dept-count">{count}</span>
                </button>
              );
            })}
            {curDept !== 'all' && selectedDept && (
              <div className="alloc-dept-budget">
                <div className="alloc-budget-row">
                  <span className="alloc-budget-label">Lượt cấp phát</span>
                  <span className="alloc-budget-val">{allocCountForDept}</span>
                </div>
                <div className="alloc-budget-row">
                  <span className="alloc-budget-label">Lượt thu hồi</span>
                  <span className="alloc-budget-val">{recallCountForDept}</span>
                </div>
              </div>
            )}
          </aside>

          <section className="alloc-table-shell">
            <div className="alloc-table-header">
              <div className="alloc-table-header-left">
                <h2>{TABLE_TITLE[filterType]}</h2>
                <span className="alloc-record-count">{filtered.length} bản ghi</span>
              </div>
              <Button
                type="text"
                icon={<ReloadOutlined />}
                title="Làm mới"
                onClick={async () => {
                  setSelectedIds(new Set());
                  const ok = await loadBudgetRows();
                  message[ok ? 'success' : 'error'](
                    ok ? 'Đã làm mới danh sách.' : 'Không tải lại được từ máy chủ.'
                  );
                }}
              />
            </div>
            <div className="alloc-table-wrap">
              <table className="alloc-table">
                <thead>
                  <tr>
                    <th>
                      <input
                        type="checkbox"
                        checked={allPageSelected}
                        onChange={(e) => toggleSelectAllOnPage(e.target.checked)}
                        aria-label="Chọn tất cả trên trang"
                      />
                    </th>
                    <th>Tài sản</th>
                    <th>Nhóm tài sản</th>
                    <th>Phòng ban</th>
                    <th>Ngày</th>
                    <th>Trạng thái</th>
                    <th>Kế toán thực hiện</th>
                    <th aria-label="Thao tác" />
                  </tr>
                </thead>
                <tbody>
                  {slice.length === 0 ? (
                    <tr>
                      <td colSpan={8} className="alloc-empty">
                        Không có dữ liệu
                      </td>
                    </tr>
                  ) : (
                    slice.map((t) => {
                      const col = getDeptColor(depts, t.deptId);
                      const dateDisplay = t.date.split('-').reverse().join('/');
                      return (
                        <tr key={t.id}>
                          <td>
                            <input
                              type="checkbox"
                              checked={selectedIds.has(t.id)}
                              onChange={(e) => toggleRowSelected(t.id, e.target.checked)}
                              aria-label={`Chọn ${t.id}`}
                            />
                          </td>
                          <td>
                            <div className="alloc-asset-cell">
                              <div>
                                <div className="alloc-asset-name">{t.name}</div>
                                <div className="alloc-asset-id">#{t.id}</div>
                              </div>
                            </div>
                          </td>
                          <td style={{ fontSize: 13, color: '#4b5563' }}>{t.cat}</td>
                          <td>
                            <span
                              className="alloc-dept-tag"
                              style={{
                                background: `${col}14`,
                                color: col,
                                border: `1px solid ${col}40`,
                              }}
                            >
                              <span
                                style={{
                                  width: 5,
                                  height: 5,
                                  borderRadius: '50%',
                                  background: col,
                                  display: 'inline-block',
                                }}
                              />
                              {getDeptName(t.deptId)}
                            </span>
                          </td>
                          <td style={{ color: '#6b7280', fontSize: 12 }}>{dateDisplay}</td>
                          <td>
                            <span className={`alloc-status-badge alloc-status-badge--${t.status}`}>
                              {STATUS_ICON[t.status]} {STATUS_LABEL[t.status]}
                            </span>
                          </td>
                          <td style={{ fontSize: 12, color: '#6b7280' }}>{t.submittedBy}</td>
                          <td>
                            <div className="alloc-action-cell">
                              <Button
                                type="text"
                                size="small"
                                icon={<EyeOutlined />}
                                title="Xem"
                                onClick={() => message.info(t.name)}
                              />
                              <Button
                                type="text"
                                size="small"
                                danger
                                icon={<DeleteOutlined />}
                                title="Xóa lịch sử"
                                onClick={() => deleteTx(t.id)}
                              />
                            </div>
                          </td>
                        </tr>
                      );
                    })
                  )}
                </tbody>
              </table>
            </div>
            <div className="alloc-pagination-row">
              <div className="alloc-pagination-info">
                {filtered.length === 0
                  ? '0 / 0'
                  : `${(pageSafe - 1) * PAGE_SIZE + 1}–${Math.min(pageSafe * PAGE_SIZE, filtered.length)} / ${filtered.length}`}
              </div>
              <Pagination
                size="small"
                current={pageSafe}
                total={filtered.length}
                pageSize={PAGE_SIZE}
                onChange={(p) => setCurPage(p)}
                showSizeChanger={false}
                hideOnSinglePage={false}
              />
            </div>
          </section>
        </div>
      </div>

      <Modal
        title={
          <div>
            <div style={{ fontWeight: 600 }}>{modalTitle}</div>
            <div style={{ fontSize: 13, fontWeight: 400, color: '#6b7280', marginTop: 4 }}>
              {modalSubtitle}
            </div>
          </div>
        }
        open={modalOpen}
        onCancel={closeModal}
        width={560}
        destroyOnHidden
        footer={[
          <Button key="cancel" onClick={closeModal}>
            Hủy bỏ
          </Button>,
          <Button
            key="submit"
            type="primary"
            danger={modalMode === 'recall'}
            onClick={() => void submitForm()}
            icon={modalMode === 'alloc' ? <PlusOutlined /> : <RollbackOutlined />}
          >
            {modalMode === 'alloc' ? 'Cấp phát' : 'Thu hồi'}
          </Button>,
        ]}
      >
        <div className="alloc-modal-form">
          <div className="alloc-modal-row">
            <div>
              <div style={{ marginBottom: 6, fontSize: 12, fontWeight: 500 }}>Phòng ban *</div>
              <Select
                placeholder="Chọn phòng ban"
                style={{ width: '100%' }}
                value={formDept}
                onChange={(v) => {
                  setFormDept(v);
                  setFormInstanceId(undefined);
                }}
                options={depts.filter((d) => d.id !== 'all').map((d) => ({ value: d.id, label: d.name }))}
              />
            </div>
            <div>
              <div style={{ marginBottom: 6, fontSize: 12, fontWeight: 500 }}>Nhóm tài sản *</div>
              <Select
                placeholder="Chọn nhóm"
                style={{ width: '100%' }}
                value={formCategoryId}
                onChange={(v) => {
                  setFormCategoryId(v);
                  setFormInstanceId(undefined);
                }}
                options={categories.map((c) => ({ value: c.categoryId, label: c.name }))}
                showSearch
                optionFilterProp="label"
              />
            </div>
          </div>
          <div className="alloc-modal-row alloc-modal-row--single">
            <div>
              <div style={{ marginBottom: 6, fontSize: 12, fontWeight: 500 }}>Tài sản (thể hiện) *</div>
              <Select
                placeholder={
                  !formCategoryId ? 'Chọn nhóm tài sản trước' : !formDept ? 'Chọn phòng ban trước' : 'Tìm và chọn tài sản'
                }
                style={{ width: '100%' }}
                disabled={!formCategoryId || !formDept}
                showSearch
                filterOption={false}
                onSearch={setInstanceSearchInput}
                value={formInstanceId}
                onChange={(v) => setFormInstanceId(v)}
                notFoundContent={instanceLoading ? <Spin size="small" /> : null}
                options={instanceOptions.map((o) => ({
                  value: o.assetInstanceId,
                  label: o.label,
                }))}
              />
            </div>
          </div>
          <div className="alloc-modal-row alloc-modal-row--single">
            <div>
              <div style={{ marginBottom: 6, fontSize: 12, fontWeight: 500 }}>Ngày hiệu lực</div>
              <Input type="date" value={formDate} onChange={(e) => setFormDate(e.target.value)} />
            </div>
          </div>
          <div className="alloc-modal-row alloc-modal-row--single">
            <div>
              <div style={{ marginBottom: 6, fontSize: 12, fontWeight: 500 }}>Ghi chú</div>
              <TextArea
                value={formNote}
                onChange={(e) => setFormNote(e.target.value)}
                placeholder="Tùy chọn..."
                rows={3}
              />
            </div>
          </div>
        </div>
      </Modal>
    </div>
  );
}

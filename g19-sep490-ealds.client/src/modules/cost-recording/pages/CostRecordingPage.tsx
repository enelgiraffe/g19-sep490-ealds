import { useMemo, useRef, useState } from 'react';
import { Button, DatePicker, Input, Modal, Select, message } from 'antd';
import { DeleteOutlined, EditOutlined, EyeOutlined, UploadOutlined } from '@ant-design/icons';
import dayjs, { type Dayjs } from 'dayjs';
import './CostRecordingPage.css';

const { Option } = Select;

type CostType = 'Cơ khí' | 'Máy móc' | 'Điện' | 'Khác';

interface AttachmentItem {
  id: string;
  name: string;
  file?: File;
}

interface CostRecord {
  id: string;
  code: string;
  occurredDate: string; // ISO date (YYYY-MM-DD)
  costType: CostType;
  assetName: string;
  departmentName: string;
  amountVnd: number;
  description?: string;
  attachments: AttachmentItem[];
}

function formatDateVi(iso: string): string {
  try {
    return new Date(iso).toLocaleDateString('vi-VN');
  } catch {
    return iso;
  }
}

function formatVnd(amount: number): string {
  try {
    return amount.toLocaleString('vi-VN') + ' đ';
  } catch {
    return String(amount);
  }
}

function normalizeAmountInput(raw: string): number | null {
  const cleaned = raw.replace(/[^\d]/g, '');
  if (!cleaned) return null;
  const n = Number(cleaned);
  return Number.isFinite(n) ? n : null;
}

const SEED_ROWS: CostRecord[] = [
  {
    id: '1',
    code: 'MCS',
    occurredDate: '2026-01-28',
    costType: 'Cơ khí',
    assetName: '1',
    departmentName: 'Phòng IT',
    amountVnd: 910_000_000,
    description: '',
    attachments: [{ id: 'a1', name: 'Hóa đơn' }],
  },
  {
    id: '2',
    code: 'MUV',
    occurredDate: '2026-01-28',
    costType: 'Cơ khí',
    assetName: '1',
    departmentName: 'Phòng Sản Xuất',
    amountVnd: 500_000_000,
    description: '',
    attachments: [{ id: 'a2', name: 'Hóa đơn' }],
  },
  {
    id: '3',
    code: 'FSF90',
    occurredDate: '2026-01-28',
    costType: 'Máy móc',
    assetName: '1',
    departmentName: 'Phòng Thiết kế',
    amountVnd: 34_000_500_000_000,
    description: '',
    attachments: [{ id: 'a3', name: 'Hóa đơn' }],
  },
];

export function CostRecordingPage() {
  const [rows, setRows] = useState<CostRecord[]>(SEED_ROWS);

  const [searchText, setSearchText] = useState('');
  const [dateFilter, setDateFilter] = useState<string | null>(null); // ISO (YYYY-MM-DD)
  const [costTypeFilter, setCostTypeFilter] = useState<CostType | 'all'>('all');
  const [amountFilter, setAmountFilter] = useState<'all' | 'lt1b' | '1b-10b' | 'gt10b'>('all');

  const [page, setPage] = useState(1);
  const [pageSize, setPageSize] = useState(25);

  const [isModalOpen, setIsModalOpen] = useState(false);
  const [editingRow, setEditingRow] = useState<CostRecord | null>(null);
  const [viewingRow, setViewingRow] = useState<CostRecord | null>(null);

  const [formCode, setFormCode] = useState('');
  const [formDate, setFormDate] = useState<Dayjs | null>(null);
  const [formAmountText, setFormAmountText] = useState('');
  const [formCostType, setFormCostType] = useState<CostType | undefined>(undefined);
  const [formAssetName, setFormAssetName] = useState<string | undefined>(undefined);
  const [formDescription, setFormDescription] = useState('');
  const [formAttachments, setFormAttachments] = useState<AttachmentItem[]>([]);
  const fileInputRef = useRef<HTMLInputElement | null>(null);

  const isViewMode = !!viewingRow;

  const costTypeOptions: CostType[] = ['Cơ khí', 'Máy móc', 'Điện', 'Khác'];
  const assetOptions = useMemo(
    () => Array.from(new Set(rows.map((r) => r.assetName))).filter(Boolean).sort(),
    [rows],
  );

  const filteredRows = useMemo(() => {
    const keyword = searchText.trim().toLowerCase();
    return rows.filter((r) => {
      const matchKeyword =
        !keyword ||
        r.code.toLowerCase().includes(keyword) ||
        r.departmentName.toLowerCase().includes(keyword) ||
        r.costType.toLowerCase().includes(keyword) ||
        r.assetName.toLowerCase().includes(keyword);

      const matchDate = !dateFilter || r.occurredDate === dateFilter;
      const matchType = costTypeFilter === 'all' || r.costType === costTypeFilter;

      let matchAmount = true;
      if (amountFilter === 'lt1b') matchAmount = r.amountVnd < 1_000_000_000;
      if (amountFilter === '1b-10b')
        matchAmount = r.amountVnd >= 1_000_000_000 && r.amountVnd <= 10_000_000_000;
      if (amountFilter === 'gt10b') matchAmount = r.amountVnd > 10_000_000_000;

      return matchKeyword && matchDate && matchType && matchAmount;
    });
  }, [rows, searchText, dateFilter, costTypeFilter, amountFilter]);

  const total = filteredRows.length;
  const totalPages = Math.max(1, Math.ceil(total / pageSize));
  const safePage = Math.min(page, totalPages);
  const startIndex = total === 0 ? 0 : (safePage - 1) * pageSize + 1;
  const endIndex = Math.min(safePage * pageSize, total);
  const pagedRows = filteredRows.slice((safePage - 1) * pageSize, safePage * pageSize);

  const resetFilters = () => {
    setSearchText('');
    setDateFilter(null);
    setCostTypeFilter('all');
    setAmountFilter('all');
    setPage(1);
  };

  const openCreateModal = () => {
    setEditingRow(null);
    setViewingRow(null);
    setFormCode('');
    setFormDate(dayjs());
    setFormAmountText('');
    setFormCostType(undefined);
    setFormAssetName(undefined);
    setFormDescription('');
    setFormAttachments([{ id: crypto.randomUUID(), name: 'Hóa đơn' }]);
    setIsModalOpen(true);
  };

  const openEditModal = (row: CostRecord) => {
    setEditingRow(row);
    setViewingRow(null);
    setFormCode(row.code);
    setFormDate(dayjs(row.occurredDate));
    setFormAmountText(String(row.amountVnd));
    setFormCostType(row.costType);
    setFormAssetName(row.assetName);
    setFormDescription(row.description ?? '');
    setFormAttachments(row.attachments.map((a) => ({ ...a })));
    setIsModalOpen(true);
  };

  const openViewModal = (row: CostRecord) => {
    setEditingRow(null);
    setViewingRow(row);
    setFormCode(row.code);
    setFormDate(dayjs(row.occurredDate));
    setFormAmountText(String(row.amountVnd));
    setFormCostType(row.costType);
    setFormAssetName(row.assetName);
    setFormDescription(row.description ?? '');
    setFormAttachments(row.attachments.map((a) => ({ ...a })));
    setIsModalOpen(true);
  };

  const closeModal = () => {
    setIsModalOpen(false);
    setEditingRow(null);
    setViewingRow(null);
  };

  const validateForm = (): boolean => {
    const amount = normalizeAmountInput(formAmountText);
    if (!formDate) {
      message.error('Vui lòng chọn ngày phát sinh.');
      return false;
    }
    if (!formCostType) {
      message.error('Vui lòng chọn loại chi phí.');
      return false;
    }
    if (!formAssetName) {
      message.error('Vui lòng chọn tên tài sản.');
      return false;
    }
    if (amount == null || amount <= 0) {
      message.error('Vui lòng nhập số tiền hợp lệ.');
      return false;
    }
    return true;
  };

  const handleSubmit = () => {
    if (!validateForm()) return;
    const amount = normalizeAmountInput(formAmountText) ?? 0;

    const payload: CostRecord = {
      id: editingRow?.id ?? crypto.randomUUID(),
      code: formCode?.trim() || '—',
      occurredDate: formDate ? formDate.format('YYYY-MM-DD') : dayjs().format('YYYY-MM-DD'),
      costType: formCostType!,
      assetName: formAssetName!,
      departmentName: editingRow?.departmentName ?? 'Phòng IT',
      amountVnd: amount,
      description: formDescription?.trim() || '',
      attachments: formAttachments.map((a) => ({ ...a })),
    };

    setRows((prev) => {
      if (editingRow) {
        return prev.map((r) => (r.id === editingRow.id ? { ...r, ...payload } : r));
      }
      return [payload, ...prev];
    });

    message.success(editingRow ? 'Cập nhật ghi nhận chi phí thành công.' : 'Thêm ghi nhận chi phí thành công.');
    closeModal();
  };

  const handleDeleteRow = (row: CostRecord) => {
    Modal.confirm({
      title: 'Xóa ghi nhận chi phí?',
      content: `Bạn có chắc muốn xóa mã chi phí "${row.code}"?`,
      okText: 'Xóa',
      okButtonProps: { danger: true },
      cancelText: 'Hủy',
      onOk: () => {
        setRows((prev) => prev.filter((r) => r.id !== row.id));
        message.success('Đã xóa ghi nhận chi phí.');
      },
    });
  };

  const handlePickFile = (attachmentId: string) => {
    const input = fileInputRef.current;
    if (!input) return;
    input.dataset.attachmentId = attachmentId;
    input.click();
  };

  const handleFileSelected = (file: File | null, attachmentId: string | null) => {
    if (!file || !attachmentId) return;
    setFormAttachments((prev) =>
      prev.map((a) => (a.id === attachmentId ? { ...a, file, name: file.name } : a)),
    );
  };

  return (
    <div className="cost-recording-page">
      <div className="cost-recording-header">
        <h1 className="cost-recording-title">Ghi nhận chi phí</h1>
        <Button type="primary" className="cost-recording-btn-add" onClick={openCreateModal}>
          + Thêm ghi nhận chi phí
        </Button>
      </div>

      <div className="cost-recording-card">
        <div className="cost-recording-filters">
          <Input
            placeholder="Tìm kiếm"
            className="cost-recording-search"
            value={searchText}
            onChange={(e) => {
              setSearchText(e.target.value);
              setPage(1);
            }}
            allowClear
          />

          <DatePicker
            placeholder="Ngày phát sinh"
            className="cost-recording-date"
            value={dateFilter ? dayjs(dateFilter, 'YYYY-MM-DD') : null}
            onChange={(d) => {
              setDateFilter(d ? d.format('YYYY-MM-DD') : null);
              setPage(1);
            }}
            format="DD/MM/YYYY"
          />

          <Select
            placeholder="Loại chi phí"
            className="cost-recording-select"
            value={costTypeFilter}
            onChange={(v) => {
              setCostTypeFilter(v);
              setPage(1);
            }}
          >
            <Option value="all">Tất cả</Option>
            {costTypeOptions.map((t) => (
              <Option key={t} value={t}>
                {t}
              </Option>
            ))}
          </Select>

          <Select
            placeholder="Số tiền"
            className="cost-recording-select"
            value={amountFilter}
            onChange={(v) => {
              setAmountFilter(v);
              setPage(1);
            }}
          >
            <Option value="all">Tất cả</Option>
            <Option value="lt1b">&lt; 1 tỷ</Option>
            <Option value="1b-10b">1 - 10 tỷ</Option>
            <Option value="gt10b">&gt; 10 tỷ</Option>
          </Select>

          <Button className="cost-recording-reset" onClick={resetFilters}>
            Gỡ bộ lọc
          </Button>
        </div>

        <div className="asset-table-wrapper cost-recording-table-wrapper">
          <table className="asset-table cost-recording-table">
            <thead>
              <tr>
                <th>STT</th>
                <th>MÃ CHI PHÍ</th>
                <th>NGÀY PHÁT SINH</th>
                <th>LOẠI CHI PHÍ</th>
                <th>TÀI SẢN</th>
                <th>PHÒNG BAN</th>
                <th className="asset-align-right">SỐ TIỀN</th>
                <th className="asset-table__cell asset-table__cell--actions" />
              </tr>
            </thead>
            <tbody>
              {pagedRows.length === 0 ? (
                <tr>
                  <td colSpan={8} className="cost-recording-table-empty">
                    Không có dữ liệu.
                  </td>
                </tr>
              ) : (
                pagedRows.map((r, i) => (
                  <tr key={r.id} className="asset-row">
                    <td>{(safePage - 1) * pageSize + i + 1}</td>
                    <td>
                      <button type="button" className="asset-code asset-code--link" onClick={() => openViewModal(r)}>
                        {r.code}
                      </button>
                    </td>
                    <td>{formatDateVi(r.occurredDate)}</td>
                    <td>{r.costType}</td>
                    <td>{r.assetName}</td>
                    <td>{r.departmentName}</td>
                    <td className="asset-align-right">{formatVnd(r.amountVnd)}</td>
                    <td className="asset-table__cell asset-table__cell--actions">
                      <div className="cost-recording-actions">
                        <Button type="text" icon={<EyeOutlined />} onClick={() => openViewModal(r)} />
                        <Button type="text" icon={<EditOutlined />} onClick={() => openEditModal(r)} />
                        <Button type="text" danger icon={<DeleteOutlined />} onClick={() => handleDeleteRow(r)} />
                      </div>
                    </td>
                  </tr>
                ))
              )}
            </tbody>
          </table>
        </div>

        <div className="cost-recording-card__footer">
          <div className="cost-recording-footer__left">
            Items per page:
            <select
              className="cost-recording-footer__select"
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
          <div className="cost-recording-footer__center">
            {total === 0 ? '0-0 trên 0' : `${startIndex}-${endIndex} trên ${total}`}
          </div>
          <div className="cost-recording-footer__right">
            <button
              className="cost-recording-footer__pager"
              disabled={safePage <= 1}
              onClick={() => setPage((p) => Math.max(1, p - 1))}
              type="button"
            >
              ⟨
            </button>
            <button className="cost-recording-footer__pager cost-recording-footer__pager--active" type="button">
              {safePage}
            </button>
            <button
              className="cost-recording-footer__pager"
              disabled={safePage >= totalPages}
              onClick={() => setPage((p) => Math.min(totalPages, p + 1))}
              type="button"
            >
              ⟩
            </button>
          </div>
        </div>
      </div>

      <input
        ref={fileInputRef}
        type="file"
        className="cost-recording-hidden-file"
        onChange={(e) => {
          const file = e.target.files?.[0] ?? null;
          const attachmentId = e.currentTarget.dataset.attachmentId ?? null;
          handleFileSelected(file, attachmentId);
          e.currentTarget.value = '';
        }}
      />

      <Modal
        title={isViewMode ? 'Chi tiết chi phí' : editingRow ? 'Sửa chi phí' : 'Thêm chi phí'}
        open={isModalOpen}
        onCancel={closeModal}
        footer={null}
        width={900}
        className="cost-recording-modal"
        closeIcon={<span className="cost-recording-modal__close">×</span>}
      >
        <div className="cost-recording-modal__form">
          <div className="cost-recording-form-row">
            <div className="cost-recording-form-col cost-recording-form-col--single">
              <div className="cost-recording-label">Mã chi phí</div>
              <Input
                value={formCode}
                onChange={(e) => setFormCode(e.target.value)}
                disabled={isViewMode}
                placeholder="—"
              />
            </div>
          </div>

          <div className="cost-recording-section-title">Thông tin chi phí</div>

          <div className="cost-recording-form-row">
            <div className="cost-recording-form-col">
              <div className="cost-recording-label">
                Ngày phát sinh<span className="cost-recording-required">*</span>
              </div>
              <DatePicker
                className="cost-recording-input"
                value={formDate}
                onChange={(v) => setFormDate(v)}
                format="DD/MM/YYYY"
                disabled={isViewMode}
              />
            </div>
            <div className="cost-recording-form-col">
              <div className="cost-recording-label">Số tiền</div>
              <Input
                value={formAmountText}
                onChange={(e) => setFormAmountText(e.target.value)}
                disabled={isViewMode}
                placeholder="—"
              />
            </div>
          </div>

          <div className="cost-recording-form-row">
            <div className="cost-recording-form-col">
              <div className="cost-recording-label">
                Loại chi phí<span className="cost-recording-required">*</span>
              </div>
              <Select
                value={formCostType}
                onChange={(v) => setFormCostType(v)}
                disabled={isViewMode}
                placeholder="—"
              >
                {costTypeOptions.map((t) => (
                  <Option key={t} value={t}>
                    {t}
                  </Option>
                ))}
              </Select>
            </div>
            <div className="cost-recording-form-col">
              <div className="cost-recording-label">
                Tên tài sản<span className="cost-recording-required">*</span>
              </div>
              <Select
                value={formAssetName}
                onChange={(v) => setFormAssetName(v)}
                disabled={isViewMode}
                placeholder="—"
                showSearch
                optionFilterProp="children"
              >
                {assetOptions.map((name) => (
                  <Option key={name} value={name}>
                    {name}
                  </Option>
                ))}
              </Select>
            </div>
          </div>

          <div className="cost-recording-form-row">
            <div className="cost-recording-form-col cost-recording-form-col--full">
              <div className="cost-recording-label">Diễn giải chi phí</div>
              <Input.TextArea
                value={formDescription}
                onChange={(e) => setFormDescription(e.target.value)}
                disabled={isViewMode}
                placeholder="—"
                rows={4}
              />
            </div>
          </div>

          <div className="cost-recording-section-title">Tài liệu đính kèm</div>

          <div className="cost-recording-attachments">
            {formAttachments.length === 0 ? (
              <div className="cost-recording-attachments-empty">Chưa có tài liệu.</div>
            ) : (
              formAttachments.map((a, idx) => (
                <div key={a.id} className="cost-recording-attachment-row">
                  <div className="cost-recording-attachment-left">
                    <div className="cost-recording-attachment-index">#{idx + 1}</div>
                    <div className="cost-recording-attachment-name">{a.name || 'Tài liệu'}</div>
                  </div>
                  <div className="cost-recording-attachment-actions">
                    <Button
                      type="text"
                      icon={<EditOutlined />}
                      disabled={isViewMode}
                      onClick={() => handlePickFile(a.id)}
                    />
                    <Button
                      type="text"
                      danger
                      icon={<DeleteOutlined />}
                      disabled={isViewMode}
                      onClick={() =>
                        setFormAttachments((prev) => prev.filter((x) => x.id !== a.id))
                      }
                    />
                  </div>
                </div>
              ))
            )}

            {!isViewMode && (
              <div className="cost-recording-attachments-footer">
                <Button
                  icon={<UploadOutlined />}
                  className="cost-recording-upload-btn"
                  onClick={() => {
                    const id = crypto.randomUUID();
                    setFormAttachments((prev) => [...prev, { id, name: 'Hóa đơn' }]);
                    setTimeout(() => handlePickFile(id), 0);
                  }}
                >
                  Thêm file đính kèm
                </Button>
              </div>
            )}
          </div>

          <div className="cost-recording-modal__footer">
            {!isViewMode && (
              <Button className="cost-recording-modal__btn cost-recording-modal__btn--primary" type="primary" onClick={handleSubmit}>
                {editingRow ? 'Lưu' : '+ Thêm'}
              </Button>
            )}
            <Button
              className="cost-recording-modal__btn cost-recording-modal__btn--secondary"
              onClick={closeModal}
            >
              {isViewMode ? 'Đóng' : 'Hủy'}
            </Button>
          </div>
        </div>
      </Modal>
    </div>
  );
}


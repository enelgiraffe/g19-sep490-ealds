import { useCallback, useEffect, useMemo, useState } from 'react';
import { Button, Input, Modal, Select, message } from 'antd';
import { FilterOutlined, SearchOutlined } from '@ant-design/icons';
import { isAxiosError } from 'axios';
import './CategoriesPage.css';
import {
  departmentsAdminService,
  type DepartmentAdminItem,
} from '../services/departmentsAdminService';

const { Option } = Select;

type DeptStatusUi = 'active' | 'inactive';

interface DepartmentRow {
  key: number;
  departmentId: number;
  index: number;
  code: string;
  name: string;
  status: DeptStatusUi;
}

const STATUS_LABELS: Record<DeptStatusUi, { label: string; className: string }> = {
  active: { label: 'Đang hoạt động', className: 'categories-status-pill categories-status-pill--active' },
  inactive: { label: 'Không hoạt động', className: 'categories-status-pill categories-status-pill--inactive' },
};

const CODE_MAX = 50;
const NAME_MAX = 255;

function mapToRow(item: DepartmentAdminItem, index: number): DepartmentRow {
  return {
    key: item.departmentId,
    departmentId: item.departmentId,
    index: index + 1,
    code: item.code,
    name: item.name,
    status: item.status === 1 ? 'active' : 'inactive',
  };
}

interface FormErrors {
  code?: string;
  name?: string;
  status?: string;
}

export function DepartmentsPage() {
  const [searchText, setSearchText] = useState('');
  const [statusFilter, setStatusFilter] = useState<'all' | DeptStatusUi>('all');
  const [rows, setRows] = useState<DepartmentRow[]>([]);
  const [loading, setLoading] = useState(false);
  const [modalOpen, setModalOpen] = useState(false);
  const [modalMode, setModalMode] = useState<'create' | 'edit'>('create');
  const [editingId, setEditingId] = useState<number | null>(null);
  const [saving, setSaving] = useState(false);
  const [draft, setDraft] = useState({ code: '', name: '', status: 'active' as DeptStatusUi });
  const [formErrors, setFormErrors] = useState<FormErrors>({});
  const [deleteOpen, setDeleteOpen] = useState(false);
  const [deleteTarget, setDeleteTarget] = useState<DepartmentRow | null>(null);
  const [deleting, setDeleting] = useState(false);

  const load = useCallback(async () => {
    try {
      setLoading(true);
      const data = await departmentsAdminService.getAll(searchText.trim() || undefined);
      setRows(data.map((item, i) => mapToRow(item, i)));
    } catch (error) {
      if (isAxiosError(error) && error.response?.status === 403) {
        message.error('Chỉ quản trị viên mới được quản lý phòng ban.');
        return;
      }
      // eslint-disable-next-line no-console
      console.error('Failed to load departments', error);
      message.error('Không tải được danh sách phòng ban.');
    } finally {
      setLoading(false);
    }
  }, [searchText]);

  useEffect(() => {
    void load();
  }, [load]);

  const filteredRows = useMemo(() => {
    return rows.filter((row) => statusFilter === 'all' || row.status === statusFilter);
  }, [rows, statusFilter]);

  const openCreate = () => {
    setModalMode('create');
    setEditingId(null);
    setDraft({ code: '', name: '', status: 'active' });
    setFormErrors({});
    setModalOpen(true);
  };

  const openEdit = (row: DepartmentRow) => {
    setModalMode('edit');
    setEditingId(row.departmentId);
    setDraft({ code: row.code, name: row.name, status: row.status });
    setFormErrors({});
    setModalOpen(true);
  };

  const validate = (): boolean => {
    const next: FormErrors = {};
    const code = draft.code.trim();
    const name = draft.name.trim();
    if (!code) next.code = 'Vui lòng nhập mã phòng ban.';
    else if (code.length > CODE_MAX) next.code = `Mã tối đa ${CODE_MAX} ký tự.`;
    if (!name) next.name = 'Vui lòng nhập tên phòng ban.';
    else if (name.length > NAME_MAX) next.name = `Tên tối đa ${NAME_MAX} ký tự.`;
    setFormErrors(next);
    return Object.keys(next).length === 0;
  };

  const handleSave = async () => {
    if (!validate()) return;
    const code = draft.code.trim();
    const name = draft.name.trim();
    const status = draft.status === 'active' ? 1 : 0;
    setSaving(true);
    try {
      if (modalMode === 'create') {
        await departmentsAdminService.create({ code, name, status });
        message.success('Tạo phòng ban thành công.');
      } else if (editingId != null) {
        await departmentsAdminService.update(editingId, { code, name, status });
        message.success('Cập nhật phòng ban thành công.');
      }
      setModalOpen(false);
      await load();
    } catch (error) {
      if (isAxiosError(error)) {
        if (error.response?.status === 403) {
          message.error('Chỉ quản trị viên mới thực hiện được thao tác này.');
          return;
        }
        const data = error.response?.data;
        if (typeof data === 'string') {
          message.error(data);
          return;
        }
        if (data && typeof data === 'object' && 'message' in data && typeof (data as { message: unknown }).message === 'string') {
          message.error((data as { message: string }).message);
          return;
        }
      }
      // eslint-disable-next-line no-console
      console.error('Failed to save department', error);
      message.error('Không thể lưu phòng ban.');
    } finally {
      setSaving(false);
    }
  };

  const handleConfirmDelete = async () => {
    if (!deleteTarget) return;
    setDeleting(true);
    try {
      const res = await departmentsAdminService.delete(deleteTarget.departmentId);
      if (res && typeof res === 'object' && 'message' in res && typeof res.message === 'string') {
        message.info(res.message);
      } else {
        message.success('Đã xóa phòng ban.');
      }
      setDeleteOpen(false);
      setDeleteTarget(null);
      await load();
    } catch (error) {
      if (isAxiosError(error) && error.response?.status === 403) {
        message.error('Chỉ quản trị viên mới thực hiện được thao tác này.');
        return;
      }
      // eslint-disable-next-line no-console
      console.error('Failed to delete department', error);
      message.error('Không thể xóa phòng ban.');
    } finally {
      setDeleting(false);
    }
  };

  return (
    <div className="categories-page">
      <div className="categories-header">
        <h1 className="categories-title">Quản lý phòng ban</h1>
        <Button type="primary" danger className="categories-btn-add" onClick={openCreate}>
          + Tạo mới
        </Button>
      </div>

      <div className="categories-card">
        <div className="categories-filters">
          <Input
            placeholder="Tìm kiếm theo mã hoặc tên"
            prefix={<SearchOutlined />}
            className="categories-search"
            value={searchText}
            onChange={(e) => setSearchText(e.target.value)}
            onPressEnter={() => void load()}
          />
          <Select
            placeholder="Trạng thái"
            className="categories-select"
            suffixIcon={<FilterOutlined />}
            value={statusFilter}
            onChange={(v) => setStatusFilter(v as 'all' | DeptStatusUi)}
          >
            <Option value="all">Tất cả</Option>
            <Option value="active">Đang hoạt động</Option>
            <Option value="inactive">Không hoạt động</Option>
          </Select>
        </div>

        <div className="asset-table-wrapper categories-table-wrapper">
          <table className="asset-table categories-table">
            <thead>
              <tr>
                <th>STT</th>
                <th>MÃ PHÒNG BAN</th>
                <th>TÊN PHÒNG BAN</th>
                <th>TRẠNG THÁI</th>
                <th className="asset-table__cell asset-table__cell--actions" />
              </tr>
            </thead>
            <tbody>
              {loading ? (
                <tr>
                  <td colSpan={5} className="categories-table-empty">
                    Đang tải dữ liệu...
                  </td>
                </tr>
              ) : filteredRows.length === 0 ? (
                <tr>
                  <td colSpan={5} className="categories-table-empty">
                    Không có dữ liệu.
                  </td>
                </tr>
              ) : (
                filteredRows.map((row) => (
                  <tr key={row.key} className="asset-row">
                    <td className="asset-align-right">{row.index}</td>
                    <td>{row.code}</td>
                    <td>{row.name}</td>
                    <td>
                      <span className={STATUS_LABELS[row.status].className}>
                        {STATUS_LABELS[row.status].label}
                      </span>
                    </td>
                    <td className="asset-table__cell asset-table__cell--actions">
                      <button type="button" className="categories-action-btn" onClick={() => openEdit(row)}>
                        ✎
                      </button>
                      <button
                        type="button"
                        className="categories-action-btn categories-action-btn--danger"
                        onClick={() => {
                          setDeleteTarget(row);
                          setDeleteOpen(true);
                        }}
                      >
                        🗑
                      </button>
                    </td>
                  </tr>
                ))
              )}
            </tbody>
          </table>
        </div>
      </div>

      <Modal
        title={modalMode === 'create' ? 'Tạo phòng ban' : 'Chỉnh sửa phòng ban'}
        open={modalOpen}
        onOk={() => void handleSave()}
        onCancel={() => setModalOpen(false)}
        confirmLoading={saving}
        okText={modalMode === 'create' ? 'Tạo' : 'Lưu'}
        cancelText="Hủy"
      >
        <div style={{ display: 'flex', flexDirection: 'column', gap: 12, marginTop: 8 }}>
          <div>
            <label htmlFor="dept-code">
              Mã phòng ban <span style={{ color: '#fe3720' }}>*</span>
            </label>
            <Input
              id="dept-code"
              value={draft.code}
              maxLength={CODE_MAX}
              onChange={(e) => {
                setDraft((d) => ({ ...d, code: e.target.value }));
                setFormErrors((er) => ({ ...er, code: undefined }));
              }}
              placeholder="VD: PB-IT"
            />
            {formErrors.code && <div style={{ color: '#fe3720', fontSize: 12 }}>{formErrors.code}</div>}
          </div>
          <div>
            <label htmlFor="dept-name">
              Tên phòng ban <span style={{ color: '#fe3720' }}>*</span>
            </label>
            <Input
              id="dept-name"
              value={draft.name}
              maxLength={NAME_MAX}
              onChange={(e) => {
                setDraft((d) => ({ ...d, name: e.target.value }));
                setFormErrors((er) => ({ ...er, name: undefined }));
              }}
              placeholder="Tên phòng ban"
            />
            {formErrors.name && <div style={{ color: '#fe3720', fontSize: 12 }}>{formErrors.name}</div>}
          </div>
          <div>
            <label htmlFor="dept-status">Trạng thái</label>
            <Select
              id="dept-status"
              style={{ width: '100%' }}
              value={draft.status}
              onChange={(v) => setDraft((d) => ({ ...d, status: v as DeptStatusUi }))}
            >
              <Option value="active">Đang hoạt động</Option>
              <Option value="inactive">Không hoạt động</Option>
            </Select>
            {formErrors.status && <div style={{ color: '#fe3720', fontSize: 12 }}>{formErrors.status}</div>}
          </div>
        </div>
      </Modal>

      <Modal
        title="Xóa phòng ban"
        open={deleteOpen}
        onOk={() => void handleConfirmDelete()}
        onCancel={() => {
          setDeleteOpen(false);
          setDeleteTarget(null);
        }}
        confirmLoading={deleting}
        okText="Xóa"
        okButtonProps={{ danger: true }}
        cancelText="Hủy"
      >
        <p>
          Bạn có chắc muốn xóa phòng ban <strong>{deleteTarget?.name}</strong>? Nếu phòng ban đang được sử dụng, hệ
          thống sẽ chỉ chuyển sang trạng thái không hoạt động.
        </p>
      </Modal>
    </div>
  );
}

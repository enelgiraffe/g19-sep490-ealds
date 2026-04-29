import { Input, Select } from 'antd';
import { FilterOutlined, SearchOutlined } from '@ant-design/icons';

const { Option } = Select;

export type SupplierStatus = 'active' | 'inactive';

export interface SupplierRow {
  key: number;
  supplierId: number;
  index: number;
  code: string;
  name: string;
  taxCode: string | null;
  phone: string | null;
  address: string | null;
  email: string | null;
  status: SupplierStatus;
}

export interface SupplierDraft {
  code: string;
  name: string;
  taxCode: string;
  address: string;
  phone: string;
  email: string;
  status: SupplierStatus;
}

export interface SupplierFormErrors {
  code?: string;
  name?: string;
  taxCode?: string;
  address?: string;
  phone?: string;
  email?: string;
  status?: string;
}

interface SuppliersSectionProps {
  searchText: string;
  onSearchTextChange: (value: string) => void;
  supplierStatusFilter: 'all' | SupplierStatus;
  onSupplierStatusFilterChange: (value: 'all' | SupplierStatus) => void;
  isLoadingSuppliers: boolean;
  rows: SupplierRow[];
  onOpenEditSupplier: (row: SupplierRow) => void;
  onDeleteSupplier: (row: SupplierRow) => void;
  isSupplierDeleteConfirmOpen: boolean;
  supplierDeleteTarget: SupplierRow | null;
  onCloseSupplierDeleteConfirm: () => void;
  onConfirmDeleteSupplier: () => void;
  isDeletingSupplier: boolean;
  isSupplierModalOpen: boolean;
  supplierModalMode: 'create' | 'edit';
  supplierDraft: SupplierDraft;
  supplierFormErrors: SupplierFormErrors;
  onSupplierDraftFieldChange: (field: keyof SupplierDraft, value: string) => void;
  onClearSupplierFieldError: (field: keyof SupplierFormErrors) => void;
  onCloseSupplierModal: () => void;
  onSubmitSupplier: () => void;
  isSavingSupplier: boolean;
  supplierCodeMaxLength: number;
  supplierNameMaxLength: number;
  supplierAddressMaxLength: number;
}

export function SuppliersSection({
  searchText,
  onSearchTextChange,
  supplierStatusFilter,
  onSupplierStatusFilterChange,
  isLoadingSuppliers,
  rows,
  onOpenEditSupplier,
  onDeleteSupplier,
  isSupplierDeleteConfirmOpen,
  supplierDeleteTarget,
  onCloseSupplierDeleteConfirm,
  onConfirmDeleteSupplier,
  isDeletingSupplier,
  isSupplierModalOpen,
  supplierModalMode,
  supplierDraft,
  supplierFormErrors,
  onSupplierDraftFieldChange,
  onClearSupplierFieldError,
  onCloseSupplierModal,
  onSubmitSupplier,
  isSavingSupplier,
  supplierCodeMaxLength,
  supplierNameMaxLength,
  supplierAddressMaxLength,
}: SuppliersSectionProps) {
  return (
    <>
      <div className="categories-filters">
        <Input
          placeholder="Tìm kiếm"
          prefix={<SearchOutlined />}
          className="categories-search"
          value={searchText}
          onChange={(e) => onSearchTextChange(e.target.value)}
        />
        <Select
          placeholder="Trạng thái"
          className="categories-select"
          suffixIcon={<FilterOutlined />}
          value={supplierStatusFilter}
          onChange={(v) => onSupplierStatusFilterChange(v as 'all' | SupplierStatus)}
        >
          <Option value="all">Tất cả</Option>
          <Option value="active">Đang hoạt động</Option>
          <Option value="inactive">Không hoạt động</Option>
        </Select>
      </div>

      <div className="asset-table-wrapper categories-table-wrapper">
        <table className="asset-table categories-table categories-table--suppliers">
          <thead>
            <tr>
              <th>STT</th>
              <th>MÃ NHÀ CUNG CẤP</th>
              <th>TÊN NHÀ CUNG CẤP</th>
              <th>MST</th>
              <th>SỐ ĐIỆN THOẠI</th>
              <th className="asset-table__cell asset-table__cell--actions" />
            </tr>
          </thead>
          <tbody>
            {isLoadingSuppliers ? (
              <tr>
                <td colSpan={6} className="categories-table-empty">
                  Đang tải dữ liệu...
                </td>
              </tr>
            ) : rows.length === 0 ? (
              <tr>
                <td colSpan={6} className="categories-table-empty">
                  Không có dữ liệu.
                </td>
              </tr>
            ) : (
              rows.map((row) => (
                <tr key={row.key} className="asset-row">
                  <td className="asset-align-right">{row.index}</td>
                  <td>{row.code}</td>
                  <td>{row.name}</td>
                  <td>{row.taxCode ?? '—'}</td>
                  <td>{row.phone ?? '—'}</td>
                  <td className="asset-table__cell asset-table__cell--actions">
                    <button
                      type="button"
                      className="categories-action-btn"
                      onClick={() => onOpenEditSupplier(row)}
                    >
                      ✎
                    </button>
                    <button
                      type="button"
                      className="categories-action-btn categories-action-btn--danger"
                      onClick={() => onDeleteSupplier(row)}
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

      {isSupplierDeleteConfirmOpen && supplierDeleteTarget && (
        <div className="supplier-confirm-overlay" role="dialog" aria-modal="true">
          <div className="supplier-confirm-modal">
            <button
              type="button"
              className="supplier-confirm-modal__close-btn"
              onClick={onCloseSupplierDeleteConfirm}
              aria-label="Đóng"
            >
              <span className="supplier-confirm-modal__close">×</span>
            </button>

            <div className="supplier-confirm-modal__header">
              <h2 className="supplier-confirm-modal__title">Xóa nhà cung cấp</h2>
            </div>

            <div className="supplier-confirm-modal__body">
              Bạn có chắc chắn muốn xóa &quot;{supplierDeleteTarget.name}&quot; không?
            </div>

            <div className="supplier-confirm-modal__footer">
              <button
                type="button"
                className="supplier-confirm-btn supplier-confirm-btn--danger"
                onClick={onConfirmDeleteSupplier}
                disabled={isDeletingSupplier}
              >
                {isDeletingSupplier ? 'Đang xóa...' : 'Xóa'}
              </button>
              <button
                type="button"
                className="supplier-confirm-btn supplier-confirm-btn--cancel"
                onClick={onCloseSupplierDeleteConfirm}
              >
                Hủy
              </button>
            </div>
          </div>
        </div>
      )}

      {isSupplierModalOpen && (
        <div className="supplier-modal-overlay" role="dialog" aria-modal="true">
          <div className="supplier-modal">
            <button
              type="button"
              className="supplier-modal__close-btn"
              onClick={onCloseSupplierModal}
              aria-label="Đóng"
            >
              <span className="supplier-modal__close">×</span>
            </button>

            <div className="supplier-modal__header">
              <h2 className="supplier-modal__title">
                {supplierModalMode === 'create' ? 'Tạo nhà cung cấp' : 'Chỉnh sửa nhà cung cấp'}
              </h2>
            </div>

            <div className="supplier-modal__body">
              <div className="supplier-form-section">
                <h3 className="supplier-section-title">Thông tin nhà cung cấp</h3>

                <div className="supplier-form-grid">
                  <div className="supplier-form-item">
                    <label htmlFor="supplier-code">
                      Mã nhà cung cấp<span className="supplier-required">*</span>
                    </label>
                    <input
                      id="supplier-code"
                      className="supplier-input"
                      value={supplierDraft.code}
                      onChange={(e) => {
                        onSupplierDraftFieldChange('code', e.target.value);
                        onClearSupplierFieldError('code');
                      }}
                      placeholder="Mã NCC"
                      maxLength={supplierCodeMaxLength}
                      autoFocus
                    />
                    {supplierFormErrors.code && (
                      <div className="supplier-error-text">{supplierFormErrors.code}</div>
                    )}
                  </div>

                  <div className="supplier-form-item">
                    <label htmlFor="supplier-name">
                      Tên nhà cung cấp<span className="supplier-required">*</span>
                    </label>
                    <input
                      id="supplier-name"
                      className="supplier-input"
                      value={supplierDraft.name}
                      onChange={(e) => {
                        onSupplierDraftFieldChange('name', e.target.value);
                        onClearSupplierFieldError('name');
                      }}
                      placeholder="Tên nhà cung cấp"
                      maxLength={supplierNameMaxLength}
                    />
                    {supplierFormErrors.name && (
                      <div className="supplier-error-text">{supplierFormErrors.name}</div>
                    )}
                  </div>

                  <div className="supplier-form-item">
                    <label htmlFor="supplier-taxCode">MST</label>
                    <input
                      id="supplier-taxCode"
                      className="supplier-input"
                      value={supplierDraft.taxCode}
                      onChange={(e) => {
                        onSupplierDraftFieldChange('taxCode', e.target.value);
                        onClearSupplierFieldError('taxCode');
                      }}
                      placeholder="MST"
                    />
                    {supplierFormErrors.taxCode && (
                      <div className="supplier-error-text">{supplierFormErrors.taxCode}</div>
                    )}
                  </div>

                  <div className="supplier-form-item">
                    <label htmlFor="supplier-phone">Số điện thoại</label>
                    <input
                      id="supplier-phone"
                      className="supplier-input"
                      value={supplierDraft.phone}
                      onChange={(e) => {
                        onSupplierDraftFieldChange('phone', e.target.value);
                        onClearSupplierFieldError('phone');
                      }}
                      placeholder="Số điện thoại"
                      type="tel"
                    />
                    {supplierFormErrors.phone && (
                      <div className="supplier-error-text">{supplierFormErrors.phone}</div>
                    )}
                  </div>

                  <div className="supplier-form-item">
                    <label htmlFor="supplier-address">Địa chỉ</label>
                    <input
                      id="supplier-address"
                      className="supplier-input"
                      value={supplierDraft.address}
                      onChange={(e) => {
                        onSupplierDraftFieldChange('address', e.target.value);
                        onClearSupplierFieldError('address');
                      }}
                      placeholder="Địa chỉ"
                      maxLength={supplierAddressMaxLength}
                    />
                    {supplierFormErrors.address && (
                      <div className="supplier-error-text">{supplierFormErrors.address}</div>
                    )}
                  </div>

                  <div className="supplier-form-item">
                    <label htmlFor="supplier-email">Email</label>
                    <input
                      id="supplier-email"
                      className="supplier-input"
                      value={supplierDraft.email}
                      onChange={(e) => {
                        onSupplierDraftFieldChange('email', e.target.value);
                        onClearSupplierFieldError('email');
                      }}
                      placeholder="Email"
                      type="email"
                    />
                    {supplierFormErrors.email && (
                      <div className="supplier-error-text">{supplierFormErrors.email}</div>
                    )}
                  </div>

                  <div className="supplier-form-item">
                    <label htmlFor="supplier-status">
                      Trạng thái<span className="supplier-required">*</span>
                    </label>
                    <select
                      id="supplier-status"
                      className="supplier-select"
                      value={supplierDraft.status}
                      onChange={(e) => {
                        onSupplierDraftFieldChange('status', e.target.value as SupplierStatus);
                        onClearSupplierFieldError('status');
                      }}
                    >
                      <option value="active">Đang hoạt động</option>
                      <option value="inactive">Không hoạt động</option>
                    </select>
                    {supplierFormErrors.status && (
                      <div className="supplier-error-text">{supplierFormErrors.status}</div>
                    )}
                  </div>
                </div>
              </div>
            </div>

            <div className="supplier-modal__footer">
              <button
                type="button"
                className="supplier-btn-submit"
                onClick={onSubmitSupplier}
                disabled={isSavingSupplier}
              >
                {supplierModalMode === 'create'
                  ? isSavingSupplier
                    ? 'Đang tạo...'
                    : 'Tạo'
                  : isSavingSupplier
                    ? 'Đang lưu...'
                    : 'Lưu'}
              </button>
              <button
                type="button"
                className="supplier-btn-cancel"
                onClick={onCloseSupplierModal}
              >
                ✕ Hủy
              </button>
            </div>
          </div>
        </div>
      )}
    </>
  );
}


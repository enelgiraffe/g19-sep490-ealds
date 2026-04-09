import { useEffect, useState } from 'react';
import { message } from 'antd';
import { supplierService, type SupplierItem, type CreateSupplierPayload } from '../../admin/services/supplierService';
import './SupplierSelectionModal.css';

interface SupplierSelectionModalProps {
  open: boolean;
  onClose: () => void;
  onSelect: (supplier: SupplierItem) => void;
}

export function SupplierSelectionModal({
  open,
  onClose,
  onSelect,
}: SupplierSelectionModalProps) {
  const [suppliers, setSuppliers] = useState<SupplierItem[]>([]);
  const [loading, setLoading] = useState(false);
  const [searchKeyword, setSearchKeyword] = useState('');
  const [showCreateForm, setShowCreateForm] = useState(false);
  const [selectedSupplier, setSelectedSupplier] = useState<number | null>(null);

  const [newSupplier, setNewSupplier] = useState<CreateSupplierPayload>({
    code: '',
    name: '',
    taxCode: '',
    address: '',
    phone: '',
    email: '',
    status: 1,
  });
  const [errors, setErrors] = useState<Record<string, string>>({});

  useEffect(() => {
    if (open) {
      loadSuppliers();
      setShowCreateForm(false);
      setSelectedSupplier(null);
      setSearchKeyword('');
      setNewSupplier({
        code: '',
        name: '',
        taxCode: '',
        address: '',
        phone: '',
        email: '',
        status: 1,
      });
      setErrors({});
    }
  }, [open]);

  const loadSuppliers = async (keyword?: string) => {
    setLoading(true);
    try {
      const data = await supplierService.getAll(keyword);
      setSuppliers(data);
    } catch {
      message.error('Không tải được danh sách nhà cung cấp');
      setSuppliers([]);
    } finally {
      setLoading(false);
    }
  };

  const handleSearch = () => {
    loadSuppliers(searchKeyword.trim() || undefined);
  };

  const validateNewSupplier = (): boolean => {
    const newErrors: Record<string, string> = {};
    if (!newSupplier.code.trim()) {
      newErrors.code = 'Vui lòng nhập mã nhà cung cấp';
    }
    if (!newSupplier.name.trim()) {
      newErrors.name = 'Vui lòng nhập tên nhà cung cấp';
    }
    setErrors(newErrors);
    return Object.keys(newErrors).length === 0;
  };

  const handleCreateSupplier = async () => {
    if (!validateNewSupplier()) return;

    setLoading(true);
    try {
      const created = await supplierService.create(newSupplier);
      message.success('Tạo nhà cung cấp thành công');
      onSelect(created);
      onClose();
    } catch (e: any) {
      message.error(e?.response?.data ?? 'Tạo nhà cung cấp thất bại');
    } finally {
      setLoading(false);
    }
  };

  const handleSelectExisting = () => {
    const supplier = suppliers.find((s) => s.supplierId === selectedSupplier);
    if (supplier) {
      onSelect(supplier);
      onClose();
    } else {
      message.warning('Vui lòng chọn nhà cung cấp');
    }
  };

  if (!open) return null;

  return (
    <div className="supplier-selection-modal-overlay" role="dialog" aria-modal="true">
      <div className="supplier-selection-modal">
        <button
          type="button"
          className="supplier-selection-modal__close-btn"
          onClick={onClose}
          aria-label="Đóng"
        >
          <span className="supplier-selection-modal__close">×</span>
        </button>

        <div className="supplier-selection-modal__header">
          <h2 className="supplier-selection-modal__title">
            {showCreateForm ? 'Tạo nhà cung cấp mới' : 'Chọn nhà cung cấp'}
          </h2>
        </div>

        <div className="supplier-selection-modal__body">
          <div className="supplier-selection-modal__content">
            {!showCreateForm ? (
              <>
                <div className="supplier-selection-search">
                  <input
                    type="text"
                    className="supplier-selection-input"
                    placeholder="Tìm kiếm nhà cung cấp..."
                    value={searchKeyword}
                    onChange={(e) => setSearchKeyword(e.target.value)}
                    onKeyDown={(e) => e.key === 'Enter' && handleSearch()}
                  />
                  <button
                    type="button"
                    className="supplier-selection-btn-search"
                    onClick={handleSearch}
                  >
                    Tìm kiếm
                  </button>
                </div>

                <div className="supplier-selection-list-section">
                  <h3 className="supplier-selection-section-title">Danh sách nhà cung cấp</h3>
                  {loading ? (
                    <div className="supplier-selection-loading">Đang tải...</div>
                  ) : suppliers.length === 0 ? (
                    <div className="supplier-selection-empty">Không có nhà cung cấp nào</div>
                  ) : (
                    <div className="supplier-selection-list">
                      {suppliers.map((supplier) => (
                        <div
                          key={supplier.supplierId}
                          className={`supplier-selection-item ${
                            selectedSupplier === supplier.supplierId
                              ? 'supplier-selection-item--selected'
                              : ''
                          }`}
                          onClick={() => setSelectedSupplier(supplier.supplierId)}
                        >
                          <div className="supplier-selection-item-radio">
                            <input
                              type="radio"
                              checked={selectedSupplier === supplier.supplierId}
                              onChange={() => setSelectedSupplier(supplier.supplierId)}
                            />
                          </div>
                          <div className="supplier-selection-item-info">
                            <div className="supplier-selection-item-name">{supplier.name}</div>
                            <div className="supplier-selection-item-details">
                              Mã: {supplier.code}
                              {supplier.phone && ` | SĐT: ${supplier.phone}`}
                              {supplier.email && ` | Email: ${supplier.email}`}
                            </div>
                          </div>
                        </div>
                      ))}
                    </div>
                  )}
                </div>

                <div className="supplier-selection-divider">
                  <span>hoặc</span>
                </div>

                <button
                  type="button"
                  className="supplier-selection-btn-create"
                  onClick={() => setShowCreateForm(true)}
                >
                  + Tạo nhà cung cấp mới
                </button>
              </>
            ) : (
              <div className="supplier-selection-form-section">
                <h3 className="supplier-selection-section-title">Thông tin nhà cung cấp</h3>

                <div className="supplier-selection-form-row">
                  <div className="supplier-selection-form-item">
                    <label htmlFor="supplier-code">
                      Mã nhà cung cấp<span style={{ color: '#ef4444' }}>*</span>
                    </label>
                    <input
                      id="supplier-code"
                      type="text"
                      className="supplier-selection-input"
                      value={newSupplier.code}
                      onChange={(e) => {
                        setNewSupplier({ ...newSupplier, code: e.target.value });
                        setErrors({ ...errors, code: '' });
                      }}
                    />
                    {errors.code && <div className="supplier-selection-error-text">{errors.code}</div>}
                  </div>
                  <div className="supplier-selection-form-item">
                    <label htmlFor="supplier-name">
                      Tên nhà cung cấp<span style={{ color: '#ef4444' }}>*</span>
                    </label>
                    <input
                      id="supplier-name"
                      type="text"
                      className="supplier-selection-input"
                      value={newSupplier.name}
                      onChange={(e) => {
                        setNewSupplier({ ...newSupplier, name: e.target.value });
                        setErrors({ ...errors, name: '' });
                      }}
                    />
                    {errors.name && <div className="supplier-selection-error-text">{errors.name}</div>}
                  </div>
                </div>

                <div className="supplier-selection-form-row">
                  <div className="supplier-selection-form-item">
                    <label htmlFor="supplier-taxcode">Mã số thuế</label>
                    <input
                      id="supplier-taxcode"
                      type="text"
                      className="supplier-selection-input"
                      value={newSupplier.taxCode}
                      onChange={(e) => setNewSupplier({ ...newSupplier, taxCode: e.target.value })}
                    />
                  </div>
                  <div className="supplier-selection-form-item">
                    <label htmlFor="supplier-phone">Số điện thoại</label>
                    <input
                      id="supplier-phone"
                      type="text"
                      className="supplier-selection-input"
                      value={newSupplier.phone}
                      onChange={(e) => setNewSupplier({ ...newSupplier, phone: e.target.value })}
                    />
                  </div>
                </div>

                <div className="supplier-selection-form-row">
                  <div className="supplier-selection-form-item">
                    <label htmlFor="supplier-email">Email</label>
                    <input
                      id="supplier-email"
                      type="email"
                      className="supplier-selection-input"
                      value={newSupplier.email}
                      onChange={(e) => setNewSupplier({ ...newSupplier, email: e.target.value })}
                    />
                  </div>
                  <div className="supplier-selection-form-item">
                    <label htmlFor="supplier-address">Địa chỉ</label>
                    <input
                      id="supplier-address"
                      type="text"
                      className="supplier-selection-input"
                      value={newSupplier.address}
                      onChange={(e) => setNewSupplier({ ...newSupplier, address: e.target.value })}
                    />
                  </div>
                </div>
              </div>
            )}
          </div>
        </div>

        <div className="supplier-selection-modal__footer">
          {!showCreateForm ? (
            <>
              <button
                type="button"
                onClick={handleSelectExisting}
                className="supplier-selection-btn-submit"
                disabled={!selectedSupplier}
              >
                Chọn
              </button>
              <button type="button" onClick={onClose} className="supplier-selection-btn-cancel">
                Hủy
              </button>
            </>
          ) : (
            <>
              <button
                type="button"
                onClick={handleCreateSupplier}
                className="supplier-selection-btn-submit"
                disabled={loading}
              >
                Tạo
              </button>
              <button
                type="button"
                onClick={() => setShowCreateForm(false)}
                className="supplier-selection-btn-cancel"
              >
                Quay lại
              </button>
            </>
          )}
        </div>
      </div>
    </div>
  );
}

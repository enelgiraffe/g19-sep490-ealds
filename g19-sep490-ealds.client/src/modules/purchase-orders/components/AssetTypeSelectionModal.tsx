import { useEffect, useState } from 'react';
import { message } from 'antd';
import { assetService, type AssetTypeItem } from '../../assets/services/assetService';
import './AssetTypeSelectionModal.css';

interface CreateAssetTypePayload {
  name: string;
  description?: string;
}

interface AssetTypeSelectionModalProps {
  open: boolean;
  onClose: () => void;
  onSelect: (assetType: AssetTypeItem) => void;
}

export function AssetTypeSelectionModal({
  open,
  onClose,
  onSelect,
}: AssetTypeSelectionModalProps) {
  const [assetTypes, setAssetTypes] = useState<AssetTypeItem[]>([]);
  const [loading, setLoading] = useState(false);
  const [searchKeyword, setSearchKeyword] = useState('');
  const [showCreateForm, setShowCreateForm] = useState(false);
  const [selectedAssetType, setSelectedAssetType] = useState<number | null>(null);

  const [newAssetType, setNewAssetType] = useState<CreateAssetTypePayload>({
    name: '',
    description: '',
  });
  const [errors, setErrors] = useState<Record<string, string>>({});

  useEffect(() => {
    if (open) {
      loadAssetTypes();
      setShowCreateForm(false);
      setSelectedAssetType(null);
      setSearchKeyword('');
      setNewAssetType({
        name: '',
        description: '',
      });
      setErrors({});
    }
  }, [open]);

  const loadAssetTypes = async () => {
    setLoading(true);
    try {
      const data = await assetService.getAssetTypes();
      setAssetTypes(data);
    } catch {
      message.error('Không tải được danh sách loại tài sản');
      setAssetTypes([]);
    } finally {
      setLoading(false);
    }
  };

  const filteredAssetTypes = assetTypes.filter((type) =>
    type.name.toLowerCase().includes(searchKeyword.toLowerCase())
  );

  const validateNewAssetType = (): boolean => {
    const newErrors: Record<string, string> = {};
    if (!newAssetType.name.trim()) {
      newErrors.name = 'Vui lòng nhập tên loại tài sản';
    }
    setErrors(newErrors);
    return Object.keys(newErrors).length === 0;
  };

  const handleCreateAssetType = async () => {
    if (!validateNewAssetType()) return;

    setLoading(true);
    try {
      const created = await assetService.createAssetType({
        name: newAssetType.name.trim(),
        description: newAssetType.description?.trim() || null,
      });
      message.success('Tạo loại tài sản thành công');
      onSelect(created);
      onClose();
    } catch (e: any) {
      message.error(e?.response?.data ?? 'Tạo loại tài sản thất bại');
    } finally {
      setLoading(false);
    }
  };

  const handleSelectExisting = () => {
    const assetType = assetTypes.find((t) => t.assetTypeId === selectedAssetType);
    if (assetType) {
      onSelect(assetType);
      onClose();
    } else {
      message.warning('Vui lòng chọn loại tài sản');
    }
  };

  if (!open) return null;

  return (
    <div className="asset-type-selection-modal-overlay" role="dialog" aria-modal="true">
      <div className="asset-type-selection-modal">
        <button
          type="button"
          className="asset-type-selection-modal__close-btn"
          onClick={onClose}
          aria-label="Đóng"
        >
          <span className="asset-type-selection-modal__close">×</span>
        </button>

        <div className="asset-type-selection-modal__header">
          <h2 className="asset-type-selection-modal__title">
            {showCreateForm ? 'Tạo loại tài sản mới' : 'Chọn loại tài sản'}
          </h2>
        </div>

        <div className="asset-type-selection-modal__body">
          <div className="asset-type-selection-modal__content">
            {!showCreateForm ? (
              <>
                <div className="asset-type-selection-search">
                  <input
                    type="text"
                    className="asset-type-selection-input"
                    placeholder="Tìm kiếm loại tài sản..."
                    value={searchKeyword}
                    onChange={(e) => setSearchKeyword(e.target.value)}
                  />
                </div>

                <div className="asset-type-selection-list-section">
                  <h3 className="asset-type-selection-section-title">Danh sách loại tài sản</h3>
                  {loading ? (
                    <div className="asset-type-selection-loading">Đang tải...</div>
                  ) : filteredAssetTypes.length === 0 ? (
                    <div className="asset-type-selection-empty">Không có loại tài sản nào</div>
                  ) : (
                    <div className="asset-type-selection-list">
                      {filteredAssetTypes.map((assetType) => (
                        <div
                          key={assetType.assetTypeId}
                          className={`asset-type-selection-item ${
                            selectedAssetType === assetType.assetTypeId
                              ? 'asset-type-selection-item--selected'
                              : ''
                          }`}
                          onClick={() => setSelectedAssetType(assetType.assetTypeId)}
                        >
                          <div className="asset-type-selection-item-radio">
                            <input
                              type="radio"
                              checked={selectedAssetType === assetType.assetTypeId}
                              onChange={() => setSelectedAssetType(assetType.assetTypeId)}
                            />
                          </div>
                          <div className="asset-type-selection-item-info">
                            <div className="asset-type-selection-item-name">{assetType.name}</div>
                            {assetType.description && (
                              <div className="asset-type-selection-item-details">
                                {assetType.description}
                              </div>
                            )}
                          </div>
                        </div>
                      ))}
                    </div>
                  )}
                </div>

                <div className="asset-type-selection-divider">
                  <span>hoặc</span>
                </div>

                <button
                  type="button"
                  className="asset-type-selection-btn-create"
                  onClick={() => setShowCreateForm(true)}
                >
                  + Tạo loại tài sản mới
                </button>
              </>
            ) : (
              <div className="asset-type-selection-form-section">
                <h3 className="asset-type-selection-section-title">Thông tin loại tài sản</h3>

                <div className="asset-type-selection-form-item">
                  <label htmlFor="asset-type-name">
                    Tên loại tài sản<span style={{ color: '#ef4444' }}>*</span>
                  </label>
                  <input
                    id="asset-type-name"
                    type="text"
                    className="asset-type-selection-input"
                    value={newAssetType.name}
                    onChange={(e) => {
                      setNewAssetType({ ...newAssetType, name: e.target.value });
                      setErrors({ ...errors, name: '' });
                    }}
                    placeholder="VD: Máy tính, Bàn ghế, Thiết bị văn phòng..."
                  />
                  {errors.name && (
                    <div className="asset-type-selection-error-text">{errors.name}</div>
                  )}
                </div>

                <div className="asset-type-selection-form-item">
                  <label htmlFor="asset-type-description">Mô tả</label>
                  <textarea
                    id="asset-type-description"
                    className="asset-type-selection-textarea"
                    rows={4}
                    value={newAssetType.description}
                    onChange={(e) =>
                      setNewAssetType({ ...newAssetType, description: e.target.value })
                    }
                    placeholder="Mô tả về loại tài sản (không bắt buộc)"
                  />
                </div>
              </div>
            )}
          </div>
        </div>

        <div className="asset-type-selection-modal__footer">
          {!showCreateForm ? (
            <>
              <button
                type="button"
                onClick={handleSelectExisting}
                className="asset-type-selection-btn-submit"
                disabled={!selectedAssetType}
              >
                Chọn
              </button>
              <button
                type="button"
                onClick={onClose}
                className="asset-type-selection-btn-cancel"
              >
                Hủy
              </button>
            </>
          ) : (
            <>
              <button
                type="button"
                onClick={handleCreateAssetType}
                className="asset-type-selection-btn-submit"
                disabled={loading}
              >
                Tạo
              </button>
              <button
                type="button"
                onClick={() => setShowCreateForm(false)}
                className="asset-type-selection-btn-cancel"
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

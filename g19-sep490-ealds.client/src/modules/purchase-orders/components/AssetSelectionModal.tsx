import { useEffect, useState } from 'react';
import { message, Input } from 'antd';
import { SearchOutlined } from '@ant-design/icons';
import { assetService, type AssetCatalogResponse } from '../../assets/services/assetService';
import './AssetSelectionModal.css';

interface AssetSelectionModalProps {
  open: boolean;
  onClose: () => void;
  onSelect: (asset: AssetCatalogResponse) => void;
}

export function AssetSelectionModal({
  open,
  onClose,
  onSelect,
}: AssetSelectionModalProps) {
  const [assets, setAssets] = useState<AssetCatalogResponse[]>([]);
  const [loading, setLoading] = useState(false);
  const [searchKeyword, setSearchKeyword] = useState('');
  const [selectedAsset, setSelectedAsset] = useState<number | null>(null);

  useEffect(() => {
    if (open) {
      loadAssets();
      setSelectedAsset(null);
      setSearchKeyword('');
    }
  }, [open]);

  const loadAssets = async () => {
    setLoading(true);
    try {
      const data = await assetService.getAll();
      setAssets(data);
    } catch {
      message.error('Không tải được danh sách tài sản');
      setAssets([]);
    } finally {
      setLoading(false);
    }
  };

  const filteredAssets = assets.filter((asset) =>
    asset.name.toLowerCase().includes(searchKeyword.toLowerCase()) ||
    asset.code.toLowerCase().includes(searchKeyword.toLowerCase()) ||
    (asset.assetTypeName && asset.assetTypeName.toLowerCase().includes(searchKeyword.toLowerCase()))
  );

  const handleSelectAsset = () => {
    const asset = assets.find((a) => a.assetId === selectedAsset);
    if (asset) {
      onSelect(asset);
      onClose();
    } else {
      message.warning('Vui lòng chọn tài sản');
    }
  };

  if (!open) return null;

  return (
    <div className="asset-selection-modal-overlay" role="dialog" aria-modal="true">
      <div className="asset-selection-modal">
        <button
          type="button"
          className="asset-selection-modal__close-btn"
          onClick={onClose}
          aria-label="Đóng"
        >
          <span className="asset-selection-modal__close">×</span>
        </button>

        <div className="asset-selection-modal__header">
          <h2 className="asset-selection-modal__title">Chọn tài sản</h2>
        </div>

        <div className="asset-selection-modal__body">
          <div className="asset-selection-modal__content">
            <div className="asset-selection-search">
              <Input
                placeholder="Tìm kiếm tài sản theo mã, tên, loại..."
                prefix={<SearchOutlined />}
                value={searchKeyword}
                onChange={(e) => setSearchKeyword(e.target.value)}
                allowClear
                size="large"
              />
            </div>

            <div className="asset-selection-list-section">
              <h3 className="asset-selection-section-title">
                Danh sách tài sản ({filteredAssets.length})
              </h3>
              {loading ? (
                <div className="asset-selection-loading">Đang tải...</div>
              ) : filteredAssets.length === 0 ? (
                <div className="asset-selection-empty">Không có tài sản nào</div>
              ) : (
                <div className="asset-selection-list">
                  {filteredAssets.map((asset) => (
                    <div
                      key={asset.assetId}
                      className={`asset-selection-item ${
                        selectedAsset === asset.assetId
                          ? 'asset-selection-item--selected'
                          : ''
                      }`}
                      onClick={() => setSelectedAsset(asset.assetId)}
                    >
                      <div className="asset-selection-item-radio">
                        <input
                          type="radio"
                          checked={selectedAsset === asset.assetId}
                          onChange={() => setSelectedAsset(asset.assetId)}
                        />
                      </div>
                      <div className="asset-selection-item-info">
                        <div className="asset-selection-item-header">
                          <span className="asset-selection-item-code">{asset.code}</span>
                          <span className="asset-selection-item-name">{asset.name}</span>
                        </div>
                        <div className="asset-selection-item-details">
                          {asset.assetTypeName && (
                            <span className="asset-selection-item-type">
                              Loại: {asset.assetTypeName}
                            </span>
                          )}
                          {asset.specification && (
                            <span className="asset-selection-item-spec">
                              | Quy cách: {asset.specification}
                            </span>
                          )}
                          {asset.unit && (
                            <span className="asset-selection-item-unit">
                              | ĐVT: {asset.unit}
                            </span>
                          )}
                        </div>
                      </div>
                    </div>
                  ))}
                </div>
              )}
            </div>
          </div>
        </div>

        <div className="asset-selection-modal__footer">
          <button
            type="button"
            onClick={handleSelectAsset}
            className="asset-selection-btn-submit"
            disabled={!selectedAsset}
          >
            Chọn
          </button>
          <button type="button" onClick={onClose} className="asset-selection-btn-cancel">
            Hủy
          </button>
        </div>
      </div>
    </div>
  );
}

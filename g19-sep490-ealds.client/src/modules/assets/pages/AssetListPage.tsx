import { useEffect, useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { MarkDamagedAssetModal } from '../components/MarkDamagedAssetModal';
import { LiquidationRequestModal } from '../components/LiquidationRequestModal';
import './AssetListPage.css';

interface AssetItem {
  id: number;
  code: string;
  name: string;
  type: string;
  quantity: number;
  price: string;
  status: string;
  statusColor: 'green' | 'gray';
  depreciation: string;
}

interface AssetInfo {
  code: string;
  name: string;
  type: string;
  specification: string;
  purchaseDate: string;
  warrantyExpiry: string;
  currentValue: string;
  remainingValue: string;
  location: string;
  status: string;
  admissionDate: string;
  department: string;
}

const MOCK_ASSETS: AssetItem[] = [
  {
    id: 1,
    code: 'MCS',
    name: 'Máy cắt sắt',
    type: 'Cơ khí',
    quantity: 1,
    price: '910,000,000 đ',
    status: 'Đang sử dụng',
    statusColor: 'green',
    depreciation: '810,000,000 đ',
  },
  {
    id: 2,
    code: 'MUV',
    name: 'Máy uốn vòm',
    type: 'Cơ khí',
    quantity: 1,
    price: '500,000,000 đ',
    status: 'Đang sử dụng',
    statusColor: 'green',
    depreciation: '400,000,000 đ',
  },
  {
    id: 3,
    code: 'FSF90',
    name: 'Ôtô Ferrari SF90',
    type: 'Máy móc',
    quantity: 1,
    price: '34,000,500,000 đ',
    status: 'Đang sử dụng',
    statusColor: 'green',
    depreciation: '34,000,000,000,000 đ',
  },
  {
    id: 4,
    code: 'MEG',
    name: 'Máy ép góc',
    type: 'Cơ khí',
    quantity: 1,
    price: '500,000,000 đ',
    status: 'Chưa sử dụng',
    statusColor: 'gray',
    depreciation: '450,000,000 đ',
  },
];

// Mock asset detail data for mark damaged modal
const MOCK_ASSET_INFO: Record<number, AssetInfo> = {
  1: {
    code: 'MCS-01',
    name: 'Máy cắt sắt',
    type: 'Máy móc',
    specification: 'Công suất 24000W',
    purchaseDate: '28/01/2025',
    warrantyExpiry: '28/01/2029',
    currentValue: '100,000,000đ',
    remainingValue: '83,000,000đ',
    location: 'Kho b',
    status: 'Đang sử dụng',
    admissionDate: '29/01/2025',
    department: 'Phòng IT',
  },
  2: {
    code: 'MUV-01',
    name: 'Máy uốn vòm',
    type: 'Cơ khí',
    specification: 'Công suất 18000W',
    purchaseDate: '15/12/2024',
    warrantyExpiry: '15/12/2028',
    currentValue: '500,000,000đ',
    remainingValue: '400,000,000đ',
    location: 'Kho A',
    status: 'Đang sử dụng',
    admissionDate: '16/12/2024',
    department: 'Phòng sản xuất',
  },
  3: {
    code: 'FSF90-01',
    name: 'Ôtô Ferrari SF90',
    type: 'Máy móc',
    specification: 'V8 Twin-Turbo',
    purchaseDate: '12/10/2024',
    warrantyExpiry: '12/10/2027',
    currentValue: '34,000,500,000đ',
    remainingValue: '34,000,000,000đ',
    location: 'Bãi xe',
    status: 'Đang sử dụng',
    admissionDate: '13/10/2024',
    department: 'Ban giám đốc',
  },
  4: {
    code: 'MP789-01',
    name: 'Máy Photocopy',
    type: 'Cơ khí',
    specification: 'A3, Màu',
    purchaseDate: '24/09/2024',
    warrantyExpiry: '24/09/2027',
    currentValue: '500,000,000đ',
    remainingValue: '450,000,000đ',
    location: 'Văn phòng tầng 2',
    status: 'Đang sử dụng',
    admissionDate: '25/09/2024',
    department: 'Phòng hành chính',
  },
};

// Helper function to generate asset info dynamically if not exists
const getAssetInfo = (asset: AssetItem): AssetInfo => {
  // If we have mock data, use it
  if (MOCK_ASSET_INFO[asset.id]) {
    return MOCK_ASSET_INFO[asset.id];
  }
  
  // Otherwise generate from asset data
  const currentDate = new Date();
  const purchaseDate = new Date(currentDate);
  purchaseDate.setMonth(purchaseDate.getMonth() - 6); // 6 months ago
  
  const warrantyExpiry = new Date(purchaseDate);
  warrantyExpiry.setFullYear(warrantyExpiry.getFullYear() + 3); // 3 years warranty
  
  const admissionDate = new Date(purchaseDate);
  admissionDate.setDate(admissionDate.getDate() + 1); // 1 day after purchase
  
  return {
    code: `${asset.code}-01`,
    name: asset.name,
    type: asset.type,
    specification: 'Quy cách tiêu chuẩn',
    purchaseDate: purchaseDate.toLocaleDateString('vi-VN'),
    warrantyExpiry: warrantyExpiry.toLocaleDateString('vi-VN'),
    currentValue: asset.price,
    remainingValue: asset.depreciation,
    location: 'Kho chính',
    status: asset.status,
    admissionDate: admissionDate.toLocaleDateString('vi-VN'),
    department: 'Phòng kỹ thuật',
  };
};

export function AssetListPage() {
  const [assets] = useState<AssetItem[]>(MOCK_ASSETS);
  const [openMenuId, setOpenMenuId] = useState<number | null>(null);
  const [isMarkDamagedModalOpen, setIsMarkDamagedModalOpen] = useState(false);
  const [isLiquidationModalOpen, setIsLiquidationModalOpen] = useState(false);
  const [selectedAssetInfo, setSelectedAssetInfo] = useState<AssetInfo | null>(null);
  const navigate = useNavigate();

  useEffect(() => {
    function handleClickOutside(e: MouseEvent) {
      const target = e.target as HTMLElement;
      if (
        !target.closest('.asset-row-menu') &&
        !target.closest('.asset-row__more-btn')
      ) {
        setOpenMenuId(null);
      }
    }

    document.addEventListener('click', handleClickOutside);
    return () => {
      document.removeEventListener('click', handleClickOutside);
    };
  }, []);

  const handleToggleMenu = (id: number) => {
    setOpenMenuId((current) => (current === id ? null : id));
  };

  const handleMenuAction = (actionKey: string, asset: AssetItem) => {
    setOpenMenuId(null);
    
    if (actionKey === 'mark-broken') {
      const assetInfo = getAssetInfo(asset);
      setSelectedAssetInfo(assetInfo);
      setIsMarkDamagedModalOpen(true);
    } else if (actionKey === 'liquidate') {
      const assetInfo = getAssetInfo(asset);
      setSelectedAssetInfo(assetInfo);
      setIsLiquidationModalOpen(true);
    } else {
      // Tạm thời chỉ log, sau có thể nối API / popup chi tiết
      console.log('Action', actionKey, 'for asset', asset);
    }
  };

  const handleCloseMarkDamagedModal = () => {
    setIsMarkDamagedModalOpen(false);
    setSelectedAssetInfo(null);
  };

  const handleCloseLiquidationModal = () => {
    setIsLiquidationModalOpen(false);
    setSelectedAssetInfo(null);
  };

  const handleSubmitMarkDamaged = (values: unknown) => {
    console.log('Mark damaged:', values);
    // TODO: Call API to mark asset as damaged
  };

  const handleSubmitLiquidation = (values: unknown) => {
    console.log('Liquidation request:', values);
    // TODO: Call API to submit liquidation request
  };

  return (
    <div className="asset-page">
      <h1 className="asset-page__title">Tài sản</h1>
      <div className="asset-card">
        <div className="asset-card__header">
          <div className="asset-card__search-group">
            <input
              type="text"
              className="asset-search-input"
              placeholder="Tìm kiếm"
            />
          </div>
          <div className="asset-card__filters">
            <select className="asset-filter-select">
              <option>Loại tài sản</option>
            </select>
            <select className="asset-filter-select">
              <option>Trạng thái</option>
            </select>
            <select className="asset-filter-select">
              <option>Giá</option>
            </select>
            <button className="asset-filter-reset">Gỡ bộ lọc</button>
            <button
              className="asset-filter-settings"
              aria-label="Cài đặt hiển thị"
            >
              ⚙
            </button>
          </div>
        </div>

        <div className="asset-table-wrapper">
          <table className="asset-table">
            <thead>
              <tr>
                <th className="asset-table__cell asset-table__cell--checkbox">
                  <input type="checkbox" />
                </th>
                <th>MÃ TÀI SẢN</th>
                <th>TÊN TÀI SẢN</th>
                <th>LOẠI TÀI SẢN</th>
                <th>SỐ LƯỢNG</th>
                <th>GIÁ</th>
                <th>TRẠNG THÁI</th>
                <th>GIÁ TRỊ KHẤU HAO</th>
                <th className="asset-table__cell asset-table__cell--actions" />
              </tr>
            </thead>
            <tbody>
              {assets.map((asset) => (
                <tr key={asset.id} className="asset-row">
                  <td className="asset-table__cell asset-table__cell--checkbox">
                    <input type="checkbox" />
                  </td>
                  <td>
                    <button
                      type="button"
                      className="asset-code asset-code--link"
                      onClick={() => navigate(`/assets/${asset.id}`)}
                    >
                      {asset.code}
                    </button>
                  </td>
                  <td>{asset.name}</td>
                  <td>{asset.type}</td>
                  <td className="asset-align-right">{asset.quantity}</td>
                  <td className="asset-align-right">{asset.price}</td>
                  <td>
                    <span
                      className={
                        asset.statusColor === 'green'
                          ? 'asset-status-pill asset-status-pill--active'
                          : 'asset-status-pill asset-status-pill--inactive'
                      }
                    >
                      {asset.status}
                    </span>
                  </td>
                  <td className="asset-align-right">{asset.depreciation}</td>
                  <td className="asset-table__cell asset-table__cell--actions">
                    <div className="asset-row__more">
                      <button
                        type="button"
                        className="asset-row__more-btn"
                        onClick={(e) => {
                          e.stopPropagation();
                          handleToggleMenu(asset.id);
                        }}
                      >
                        ⋯
                      </button>
                      {openMenuId === asset.id && (
                        <div className="asset-row-menu">
                          <button
                            className="asset-row-menu__item"
                            onClick={() => handleMenuAction('move', asset)}
                          >
                            <span className="asset-row-menu__icon">↔</span>
                            <span>Di chuyển</span>
                          </button>
                          <button
                            className="asset-row-menu__item"
                            onClick={() =>
                              handleMenuAction('maintenance', asset)
                            }
                          >
                            <span className="asset-row-menu__icon">🛠</span>
                            <span>Bảo dưỡng</span>
                          </button>
                          <button
                            className="asset-row-menu__item"
                            onClick={() => handleMenuAction('mark-lost', asset)}
                          >
                            <span className="asset-row-menu__icon">−</span>
                            <span>Đánh dấu mất</span>
                          </button>
                          <button
                            className="asset-row-menu__item"
                            onClick={() => handleMenuAction('liquidate', asset)}
                          >
                            <span className="asset-row-menu__icon">$</span>
                            <span>Đề nghị thanh lý</span>
                          </button>
                          <button
                            className="asset-row-menu__item"
                            onClick={() =>
                              handleMenuAction('mark-broken', asset)
                            }
                          >
                            <span className="asset-row-menu__icon">!</span>
                            <span>Đánh dấu hỏng</span>
                          </button>
                          <button
                            className="asset-row-menu__item"
                            onClick={() => handleMenuAction('print-qr', asset)}
                          >
                            <span className="asset-row-menu__icon">▤</span>
                            <span>In mã QR</span>
                          </button>
                          <button
                            className="asset-row-menu__item asset-row-menu__item--danger"
                            onClick={() => handleMenuAction('delete', asset)}
                          >
                            <span className="asset-row-menu__icon">🗑</span>
                            <span>Xóa</span>
                          </button>
                        </div>
                      )}
                    </div>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>

        <div className="asset-card__footer">
          <div className="asset-footer__left">
            Items per page:
            <select className="asset-footer__select">
              <option>25</option>
              <option>50</option>
              <option>100</option>
            </select>
          </div>
          <div className="asset-footer__center">1-25 of 289</div>
          <div className="asset-footer__right">
            <button className="asset-footer__pager" disabled>
              ⟨
            </button>
            <button className="asset-footer__pager asset-footer__pager--active">
              1
            </button>
            <button className="asset-footer__pager">2</button>
            <button className="asset-footer__pager">⟩</button>
          </div>
        </div>
      </div>

      <MarkDamagedAssetModal
        open={isMarkDamagedModalOpen}
        onClose={handleCloseMarkDamagedModal}
        onSubmit={handleSubmitMarkDamaged}
        assetInfo={selectedAssetInfo}
      />

      <LiquidationRequestModal
        open={isLiquidationModalOpen}
        onClose={handleCloseLiquidationModal}
        onSubmit={handleSubmitLiquidation}
        assetInfo={selectedAssetInfo}
      />
    </div>
  );
}


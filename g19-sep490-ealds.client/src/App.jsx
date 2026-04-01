import { useEffect, useState } from 'react';
import './App.css';
import './modules/assets/pages/AssetListPage.css';

const MOCK_ASSETS = [
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

function App() {
  const [assets] = useState(MOCK_ASSETS);
  const [openMenuId, setOpenMenuId] = useState(null);

  useEffect(() => {
    function handleClickOutside(e) {
      const target = e.target;
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

  const handleToggleMenu = (id) => {
    setOpenMenuId((current) => (current === id ? null : id));
  };

  const handleMenuAction = (actionKey, asset) => {
    console.log('Action', actionKey, 'for asset', asset);
    setOpenMenuId(null);
  };

  return (
    <div className="asset-page">
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
                <th className="asset-table__cell asset-table__cell--stt">STT</th>
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
              {assets.map((asset, index) => (
                <tr key={asset.id} className="asset-row">
                  <td className="asset-table__cell asset-table__cell--stt">{index + 1}</td>
                  <td className="asset-code asset-code--link">{asset.code}</td>
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
    </div>
  );
}

export default App;
import { useState } from 'react';
import './RepairsPage.css';

type RepairStatus = 'draft' | 'pending' | 'approved' | 'rejected';

function getStatusLabel(status: RepairStatus): string {
  if (status === 'draft') return 'Chưa gửi';
  if (status === 'pending') return 'Chờ phê duyệt';
  if (status === 'approved') return 'Phê duyệt';
  return 'Từ chối';
}

export function RepairsPage() {
  const [activeTab, setActiveTab] = useState<'need-repair' | 'in-repair'>('need-repair');
  const [search, setSearch] = useState('');
  const [statusFilter, setStatusFilter] = useState<'all' | RepairStatus>('all');

  return (
    <div className="repairs-page">
      <h1 className="repairs-page__title">Sửa chữa</h1>

      <div className="repairs-card">
        <div className="repairs-card__tabs">
          <button
            type="button"
            className={
              activeTab === 'need-repair'
                ? 'repairs-tab repairs-tab--active'
                : 'repairs-tab'
            }
            onClick={() => setActiveTab('need-repair')}
          >
            Tài sản cần sửa chữa
          </button>
          <button
            type="button"
            className={
              activeTab === 'in-repair'
                ? 'repairs-tab repairs-tab--active'
                : 'repairs-tab'
            }
            onClick={() => setActiveTab('in-repair')}
          >
            Đang sửa chữa
          </button>
        </div>

        <div className="repairs-card__header">
          <div className="repairs-card__search-group">
            <input
              type="text"
              className="repairs-search-input"
              placeholder="Tìm kiếm"
              value={search}
              onChange={(e) => setSearch(e.target.value)}
            />
          </div>
          <div className="repairs-card__filters">
            <select
              className="repairs-filter-select"
              value={statusFilter}
              onChange={(e) =>
                setStatusFilter(e.target.value as 'all' | RepairStatus)
              }
            >
              <option value="all">Trạng thái</option>
              <option value="draft">Chưa gửi</option>
              <option value="pending">Chờ phê duyệt</option>
              <option value="approved">Phê duyệt</option>
              <option value="rejected">Từ chối</option>
            </select>
            <button type="button" className="repairs-filter-advanced">
              Gộp bộ lọc
            </button>
          </div>
        </div>

        <div className="repairs-table-wrapper">
          <table className="repairs-table">
            <thead>
              <tr>
                <th>Mã tài sản</th>
                <th>Tên tài sản</th>
                <th>Tình trạng</th>
                <th>Ngày hỏng</th>
                <th>Số lượng</th>
                <th>Vị trí tài sản</th>
                <th>Phòng ban quản lý</th>
                <th>Trạng thái</th>
                <th />
              </tr>
            </thead>
            <tbody>
              <tr>
                <td colSpan={9} className="repairs-table-empty">
                  Không có dữ liệu.
                </td>
              </tr>
              {false && (
                <tr>
                  <td colSpan={9} className="repairs-table-empty">
                    Không có dữ liệu.
                  </td>
                </tr>
              )}
            </tbody>
          </table>
        </div>

        <div className="repairs-card__footer">
          <div className="repairs-footer__left">
            Items per page:
            <select className="repairs-footer__select" defaultValue={25}>
              <option value={10}>10</option>
              <option value={25}>25</option>
              <option value={50}>50</option>
              <option value={100}>100</option>
            </select>
          </div>
          <div className="repairs-footer__center">1-25 of 289</div>
          <div className="repairs-footer__right">
            <button className="repairs-footer__pager" disabled type="button">
              ⟨
            </button>
            <button
              className="repairs-footer__pager repairs-footer__pager--active"
              type="button"
            >
              1
            </button>
            <button className="repairs-footer__pager" type="button">
              ⟩
            </button>
          </div>
        </div>
      </div>
    </div>
  );
}


import { useEffect, useMemo, useState } from 'react';
import { Button, Input, Select, Tabs, message } from 'antd';
import { EyeOutlined, FilterOutlined, SearchOutlined, SettingOutlined } from '@ant-design/icons';
import './RepairsPage.css';
import { damageReportService } from '../../assets/services/damageReportService';

type RepairStatus = 'draft' | 'pending' | 'approved' | 'rejected';

interface RepairRow {
  id: string;
  assetCode: string;
  assetName: string;
  condition: string;
  brokenDate: string;
  quantity: number;
  location: string;
  department: string;
  status: RepairStatus;
}

function formatDate(value?: string | null): string {
  if (!value) return '';
  const d = new Date(value);
  if (Number.isNaN(d.getTime())) return value;
  return d.toLocaleDateString('vi-VN');
}

function mapStatus(status: number): RepairStatus {
  // Align with the common workflow in backend:
  // 0=Nháp, 3=Chờ phê duyệt, 4=Phê duyệt, others => Từ chối/khác
  if (status === 0) return 'draft';
  if (status === 3) return 'pending';
  if (status === 4) return 'approved';
  return 'rejected';
}

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
  const [rows, setRows] = useState<RepairRow[]>([]);
  const [loading, setLoading] = useState(false);

  useEffect(() => {
    let cancelled = false;

    async function loadNeedRepair() {
      setLoading(true);
      try {
        const res = await damageReportService.list({
          requestTypeId: 4,
          page: 1,
          pageSize: 200,
        });

        const mapped: RepairRow[] = (res.items ?? []).map((it) => {
          const assetCode = it.assetCode ?? '';
          const assetName = it.assetName ?? '';

          return {
            id: String(it.id),
            assetCode: assetCode || String(it.assetId ?? ''),
            // Nếu assetName trống, lấy phần title nhưng bỏ prefix "Báo hỏng tài sản ..."
            assetName:
              assetName ||
              (it.title
                ? it.title.replace(/^Báo hỏng tài sản\s*/i, '').trim() || it.title
                : '(Không có tên)'),
            condition: it.description ?? '',
            brokenDate: formatDate(it.createDate),
            // nếu backend chưa có quantity thì tối thiểu hiển thị 1
            quantity: it.assetQuantity && it.assetQuantity > 0 ? it.assetQuantity : 1,
            location: it.currentDepartmentName ?? '',
            department: it.currentDepartmentName ?? '',
            status: mapStatus(it.status),
          };
        });

        if (!cancelled) setRows(mapped);
      } catch (e: any) {
        const msg = e?.response?.data?.title ?? e?.response?.data ?? e?.message;
        message.error(typeof msg === 'string' ? msg : 'Không tải được danh sách tài sản cần sửa chữa.');
        if (!cancelled) setRows([]);
      } finally {
        if (!cancelled) setLoading(false);
      }
    }

    if (activeTab === 'need-repair') {
      loadNeedRepair();
    } else {
      setRows([]);
    }

    return () => {
      cancelled = true;
    };
  }, [activeTab]);

  const filteredData: RepairRow[] = useMemo(() => {
    const keyword = search.trim().toLowerCase();
    return rows.filter((row) => {
      const matchStatus = statusFilter === 'all' || row.status === statusFilter;
      const matchKeyword =
        !keyword ||
        row.assetCode.toLowerCase().includes(keyword) ||
        row.assetName.toLowerCase().includes(keyword) ||
        row.condition.toLowerCase().includes(keyword);
      return matchStatus && matchKeyword;
    });
  }, [rows, search, statusFilter]);

  return (
    <div className="repairs-page">
      <h1 className="repairs-page__title">Sửa chữa</h1>

      <div className="repairs-card">
        <Tabs
          activeKey={activeTab}
          onChange={(key) => setActiveTab(key as 'need-repair' | 'in-repair')}
          className="repairs-tabs"
          items={[
            { key: 'need-repair', label: 'Tài sản cần sửa chữa' },
            { key: 'in-repair', label: 'Đang sửa chữa' },
          ]}
        />

        <div className="repairs-filters">
          <Input
            placeholder="Tìm kiếm"
            prefix={<SearchOutlined />}
            className="repairs-search"
            value={search}
            onChange={(e) => setSearch(e.target.value)}
          />
          <Select
            placeholder="Trạng thái"
            className="repairs-select"
            suffixIcon={<FilterOutlined />}
            value={statusFilter}
            onChange={(v) => setStatusFilter(v)}
            options={[
              { value: 'all', label: 'Tất cả' },
              { value: 'draft', label: 'Chưa gửi' },
              { value: 'pending', label: 'Chờ phê duyệt' },
              { value: 'approved', label: 'Phê duyệt' },
              { value: 'rejected', label: 'Từ chối' },
            ]}
          />
          <Button
            icon={<FilterOutlined />}
            className="repairs-filter-advanced"
            onClick={() => {
              setSearch('');
              setStatusFilter('all');
            }}
          >
            Gỡ bộ lọc
          </Button>
          <Button icon={<SettingOutlined />} className="repairs-settings" />
        </div>

        <div className="asset-table-wrapper repairs-table-wrapper">
          <table className="asset-table repairs-table">
            <thead>
              <tr>
                <th className="asset-table__cell asset-table__cell--checkbox">
                  <input type="checkbox" />
                </th>
                <th>MÃ TÀI SẢN</th>
                <th>TÊN TÀI SẢN</th>
                <th>TÌNH TRẠNG</th>
                <th>NGÀY HỎNG</th>
                <th>SỐ LƯỢNG</th>
                <th>VỊ TRÍ TÀI SẢN</th>
                <th>PHÒNG BAN QUẢN LÝ</th>
                <th>TRẠNG THÁI</th>
                <th className="asset-table__cell asset-table__cell--actions" />
              </tr>
            </thead>
            <tbody>
              {loading ? (
                <tr>
                  <td colSpan={10} className="repairs-table-empty">
                    Đang tải dữ liệu...
                  </td>
                </tr>
              ) : filteredData.length === 0 ? (
                <tr>
                  <td colSpan={10} className="repairs-table-empty">
                    Không có dữ liệu.
                  </td>
                </tr>
              ) : (
                filteredData.map((row) => (
                  <tr key={row.id} className="asset-row">
                    <td className="asset-table__cell asset-table__cell--checkbox">
                      <input type="checkbox" />
                    </td>
                    <td>
                      <button type="button" className="asset-code asset-code--link">
                        {row.assetCode}
                      </button>
                    </td>
                    <td>{row.assetName}</td>
                    <td>{row.condition}</td>
                    <td>{row.brokenDate}</td>
                    <td className="asset-align-right">{row.quantity}</td>
                    <td>{row.location}</td>
                    <td>{row.department}</td>
                    <td>{getStatusLabel(row.status)}</td>
                    <td className="asset-table__cell asset-table__cell--actions">
                      <Button type="text" icon={<EyeOutlined />} />
                    </td>
                  </tr>
                ))
              )}
            </tbody>
          </table>
        </div>

        <div className="repairs-card__footer">
          <div className="repairs-footer__left">
            Số lượng trên trang:
            <select className="repairs-footer__select" defaultValue={25}>
              <option value={10}>10</option>
              <option value={25}>25</option>
              <option value={50}>50</option>
              <option value={100}>100</option>
            </select>
          </div>
          <div className="repairs-footer__center">1-25 trên 289</div>
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


import { useEffect, useState } from 'react';
import { useParams, Link } from 'react-router-dom';
import {
  assetService,
  formatVnd,
  getStatusLabel,
  type AssetResponse,
} from '../services/assetService';
import { profileService, type UserProfile } from '../../profile/services/profileService';
import { mapBackendRoleToAppRole } from '../../auth/types/auth.types';
import './AssetDetailPage.css';

function formatDate(iso?: string | null): string {
  if (!iso) return '—';
  try {
    const d = new Date(iso);
    return d.toLocaleDateString('vi-VN');
  } catch {
    return iso;
  }
}

export function AssetDetailPage() {
  const params = useParams<{ id: string }>();
  const id = params.id ? Number(params.id) : NaN;
  const [asset, setAsset] = useState<AssetResponse | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [profile, setProfile] = useState<UserProfile | null>(null);

  const storedRole = (() => {
    const raw = localStorage.getItem('user');
    if (!raw) return null;
    try {
      const parsed = JSON.parse(raw) as { role?: string | null };
      return parsed.role ?? null;
    } catch {
      return null;
    }
  })();

  const isAccountant = mapBackendRoleToAppRole(profile?.role ?? storedRole) === 'accountant';

  useEffect(() => {
    if (!id || Number.isNaN(id)) {
      setError('ID tài sản không hợp lệ.');
      setLoading(false);
      return;
    }
    let cancelled = false;
    setLoading(true);
    setError(null);
    async function load() {
      try {
        const [assetRes, profileRes] = await Promise.all([
          assetService.getById(id),
          profileService.getProfile().catch(() => null),
        ]);
        if (cancelled) return;
        setAsset(assetRes);
        if (profileRes) setProfile(profileRes);
      } catch {
        if (!cancelled) setError('Không tải được thông tin tài sản.');
      } finally {
        if (!cancelled) setLoading(false);
      }
    }
    load();
    return () => {
      cancelled = true;
    };
  }, [id]);

  if (loading) {
    return (
      <div className="asset-detail-page">
        <div className="asset-detail__header">
          <Link to={isAccountant ? '/accountant-assets' : '/assets'} className="asset-detail__back">
            ← Tất cả tài sản
          </Link>
        </div>
        <div className="asset-detail__card">
          <p>Đang tải...</p>
        </div>
      </div>
    );
  }

  if (error || !asset) {
    return (
      <div className="asset-detail-page">
        <div className="asset-detail__header">
          <Link
            to={isAccountant ? '/accountant-assets' : '/assets'}
            className="asset-detail__back"
          >
            ← Quay lại danh sách tài sản
          </Link>
        </div>
        <div className="asset-detail__card">
          <p>{error ?? 'Không tìm thấy tài sản.'}</p>
        </div>
      </div>
    );
  }

  const statusLabel = getStatusLabel(asset.statusName);

  const depreciationPolicyLabel =
    asset.depreciationPolicyName ?? 'Chưa cấu hình chính sách khấu hao';

  const depreciationUsefulLifeLabel =
    asset.depreciationUsefulLifeMonths != null
      ? `${asset.depreciationUsefulLifeMonths} tháng`
      : '—';

  const depreciationSalvageValueLabel =
    asset.depreciationSalvageValue != null
      ? formatVnd(asset.depreciationSalvageValue)
      : '—';

  const depreciationAmountLabel =
    asset.depreciationAmount != null
      ? formatVnd(asset.depreciationAmount)
      : '—';

  const accumulatedDepreciationLabel =
    asset.accumulatedDepreciation != null
      ? formatVnd(asset.accumulatedDepreciation)
      : '—';

  const remainingValueLabel =
    asset.remainingValue != null ? formatVnd(asset.remainingValue) : '—';

  return (
    <div className="asset-detail-page">
      <div className="asset-detail__header">
        <Link
          to={isAccountant ? '/accountant-assets' : '/assets'}
          className="asset-detail__back"
        >
          ← Tất cả tài sản
        </Link>
        <div className="asset-detail__title-row">
          <div className="asset-detail__title-group">
            <h1 className="asset-detail__title">{asset.name}</h1>
            <span className="asset-detail__status">{statusLabel}</span>
          </div>
          {isAccountant && (
            <div className="asset-detail__actions">
              <Link
                to={`/assets/${asset.assetId}/edit`}
                className="asset-detail__btn asset-detail__btn--primary"
              >
                ✏️ Chỉnh sửa
              </Link>
            </div>
          )}
        </div>
      </div>

      <div className="asset-detail__card">
        <div className="asset-detail__section">
          <h2 className="asset-detail__section-title">Thông tin chung</h2>
          <div className="asset-detail__info-grid">
            <div className="asset-detail__info-col">
              <div className="asset-detail__info-row">
                <span className="label">Mã tài sản</span>
                <span className="value">{asset.code}</span>
              </div>
              <div className="asset-detail__info-row">
                <span className="label">Loại tài sản</span>
                <span className="value">{asset.assetTypeName ?? '—'}</span>
              </div>
              <div className="asset-detail__info-row">
                <span className="label">Tên tài sản</span>
                <span className="value">{asset.name}</span>
              </div>
              <div className="asset-detail__info-row">
                <span className="label">Số lượng</span>
                <span className="value">{asset.quantity}</span>
              </div>
              <div className="asset-detail__info-row">
                <span className="label">Đơn vị tính</span>
                <span className="value">{asset.unit}</span>
              </div>
              <div className="asset-detail__info-row">
                <span className="label">Giá trị hiện tại</span>
                <span className="value">{formatVnd(asset.currentValue)}</span>
              </div>
            </div>
            <div className="asset-detail__info-col">
              <div className="asset-detail__info-row">
                <span className="label">Phòng ban sử dụng</span>
                <span className="value">{asset.currentDepartmentName ?? '—'}</span>
              </div>
              <div className="asset-detail__info-row">
                <span className="label">Ngày mua</span>
                <span className="value">{formatDate(asset.purchaseDate)}</span>
              </div>
              <div className="asset-detail__info-row">
                <span className="label">Giá gốc</span>
                <span className="value">{formatVnd(asset.originalPrice)}</span>
              </div>
              <div className="asset-detail__info-row">
                <span className="label">Ngày đưa vào sử dụng</span>
                <span className="value">{formatDate(asset.inUseDate)}</span>
              </div>
            </div>
          </div>
        </div>

        <div className="asset-detail__section">
          <h2 className="asset-detail__section-title">Bảo hành</h2>
          <div className="asset-detail__info-row">
            <span className="label">Hạn bảo hành</span>
            <span className="value">{formatDate(asset.warrantyEndDate)}</span>
          </div>
        </div>

        <div className="asset-detail__section">
          <h2 className="asset-detail__section-title">Thông tin khấu hao</h2>
          <div className="asset-detail__info-row">
            <span className="label">Chính sách khấu hao</span>
            <span className="value">{depreciationPolicyLabel}</span>
          </div>
          <div className="asset-detail__info-row">
            <span className="label">Thời gian sử dụng hữu ích</span>
            <span className="value">{depreciationUsefulLifeLabel}</span>
          </div>
          <div className="asset-detail__info-row">
            <span className="label">Giá trị thu hồi ước tính</span>
            <span className="value">{depreciationSalvageValueLabel}</span>
          </div>
          <div className="asset-detail__info-row">
            <span className="label">Kỳ khấu hao gần nhất</span>
            <span className="value">
              {asset.depreciationPeriod
                ? formatDate(asset.depreciationPeriod)
                : '—'}
            </span>
          </div>
          <div className="asset-detail__info-row">
            <span className="label">Mức khấu hao kỳ gần nhất</span>
            <span className="value">{depreciationAmountLabel}</span>
          </div>
          <div className="asset-detail__info-row">
            <span className="label">Khấu hao lũy kế</span>
            <span className="value">{accumulatedDepreciationLabel}</span>
          </div>
          <div className="asset-detail__info-row">
            <span className="label">Giá trị còn lại</span>
            <span className="value">{remainingValueLabel}</span>
          </div>
          <div className="asset-detail__info-row">
            <span className="label">Giá trị tính khấu hao (giá gốc)</span>
            <span className="value">{formatVnd(asset.originalPrice)}</span>
          </div>
          <div className="asset-detail__info-row">
            <span className="label">Giá trị hiện tại</span>
            <span className="value">{formatVnd(asset.currentValue)}</span>
          </div>
        </div>

        <div className="asset-detail__section">
          <h2 className="asset-detail__section-title">Quá trình sử dụng</h2>
          <div className="asset-detail__table-wrapper">
            <table className="asset-detail__table">
              <thead>
                <tr>
                  <th>NGÀY THỰC HIỆN</th>
                  <th>SỐ BIÊN BẢN</th>
                  <th>NGHIỆP VỤ</th>
                  <th>TÌNH TRẠNG</th>
                  <th>VỊ TRÍ TÀI SẢN</th>
                  <th>GIÁ TRỊ</th>
                </tr>
              </thead>
              <tbody>
                <tr>
                  <td colSpan={6} className="asset-detail__empty">
                    Chưa có dữ liệu (cần API quá trình sử dụng).
                  </td>
                </tr>
              </tbody>
            </table>
          </div>
        </div>

        <div className="asset-detail__section">
          <h2 className="asset-detail__section-title">
            Lịch sửa chữa, bảo dưỡng / bảo trì
          </h2>
          <div className="asset-detail__table-wrapper">
            <table className="asset-detail__table">
              <thead>
                <tr>
                  <th>NGÀY BẮT ĐẦU</th>
                  <th>MẪU LỊCH / NỘI DUNG</th>
                  <th>LOẠI LỊCH</th>
                  <th>CHU KỲ</th>
                  <th>NGÀY HẠN TIẾP THEO</th>
                  <th>NGÀY KẾT THÚC</th>
                  <th>TRẠNG THÁI</th>
                </tr>
              </thead>
              <tbody>
                {asset.maintenanceSchedules &&
                asset.maintenanceSchedules.length > 0 ? (
                  asset.maintenanceSchedules.map((s) => (
                    <tr key={s.scheduleId}>
                      <td>{formatDate(s.startDate)}</td>
                      <td>{s.templateName ?? '—'}</td>
                      <td>{s.scheduleType}</td>
                      <td>
                        {s.intervalMonths
                          ? `${s.intervalMonths} tháng`
                          : s.intervalHours
                          ? `${s.intervalHours} giờ`
                          : '—'}
                      </td>
                      <td>
                        {s.nextDueDate ? formatDate(s.nextDueDate) : '—'}
                      </td>
                      <td>{s.endDate ? formatDate(s.endDate) : '—'}</td>
                      <td>
                        {s.isActive == null
                          ? '—'
                          : s.isActive
                          ? 'Đang hiệu lực'
                          : 'Ngừng'}
                      </td>
                    </tr>
                  ))
                ) : (
                  <tr>
                    <td colSpan={7} className="asset-detail__empty">
                      Chưa có lịch bảo trì/bảo dưỡng cho tài sản này.
                    </td>
                  </tr>
                )}
              </tbody>
            </table>
          </div>
        </div>

        <div className="asset-detail__section">
          <h2 className="asset-detail__section-title">Tài liệu</h2>
          <div className="asset-detail__files">
            <p className="asset-detail__empty">Chưa có tài liệu đính kèm.</p>
          </div>
        </div>
      </div>
    </div>
  );
}

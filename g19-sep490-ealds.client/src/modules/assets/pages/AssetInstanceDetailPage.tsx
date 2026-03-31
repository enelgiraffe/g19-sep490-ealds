import { useEffect, useState } from 'react';
import { Link, useParams } from 'react-router-dom';
import {
  assetInstanceService,
  formatVnd,
  getStatusLabel,
  type AssetInstanceResponse,
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

function formatCurrentLocationLabel(row: AssetInstanceResponse): string {
  const dept = row.currentDepartmentName?.trim();
  const note = row.currentLocationNote?.trim();
  if (dept && note) return `${dept} · ${note}`;
  if (dept) return dept;
  if (note) return note;
  return '—';
}

export function AssetInstanceDetailPage() {
  const params = useParams<{ instanceId: string }>();
  const instanceId = params.instanceId ? Number(params.instanceId) : NaN;
  const [instance, setInstance] = useState<AssetInstanceResponse | null>(null);
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
    if (!instanceId || Number.isNaN(instanceId)) {
      setError('ID cá thể không hợp lệ.');
      setLoading(false);
      return;
    }
    let cancelled = false;
    setLoading(true);
    setError(null);
    async function load() {
      try {
        const [instRes, profileRes] = await Promise.all([
          assetInstanceService.getById(instanceId),
          profileService.getProfile().catch(() => null),
        ]);
        if (cancelled) return;
        setInstance(instRes);
        if (profileRes) setProfile(profileRes);
      } catch {
        if (!cancelled) setError('Không tải được thông tin cá thể.');
      } finally {
        if (!cancelled) setLoading(false);
      }
    }
    load();
    return () => {
      cancelled = true;
    };
  }, [instanceId]);

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

  if (error || !instance) {
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
          <p>{error ?? 'Không tìm thấy cá thể.'}</p>
        </div>
      </div>
    );
  }

  const backToAssetPath = `/assets/${instance.assetId}`;
  const statusLabel = getStatusLabel(instance.statusName);

  return (
    <div className="asset-detail-page">
      <div className="asset-detail__header">
        <Link to={backToAssetPath} className="asset-detail__back">
          ← Quay lại chi tiết tài sản
        </Link>
        <div className="asset-detail__title-row">
          <div className="asset-detail__title-group">
            <h1 className="asset-detail__title">Cá thể: {instance.instanceCode}</h1>
            <span className="asset-detail__status">{statusLabel}</span>
          </div>
        </div>
      </div>

      <div className="asset-detail__card">
        <div className="asset-detail__section">
          <h2 className="asset-detail__section-title">Thông tin cá thể</h2>
          <div className="asset-detail__info-grid">
            <div className="asset-detail__info-col">
              <div className="asset-detail__info-row">
                <span className="label">Mã cá thể</span>
                <span className="value">{instance.instanceCode}</span>
              </div>
              <div className="asset-detail__info-row">
                <span className="label">Mã tài sản (danh mục)</span>
                <span className="value">{instance.assetCode?.trim() || '—'}</span>
              </div>
              <div className="asset-detail__info-row">
                <span className="label">Tên tài sản (danh mục)</span>
                <span className="value">{instance.assetName?.trim() || '—'}</span>
              </div>
              <div className="asset-detail__info-row">
                <span className="label">Số seri</span>
                <span className="value">{instance.serialNumber?.trim() || '—'}</span>
              </div>
              <div className="asset-detail__info-row">
                <span className="label">Kho</span>
                <span className="value">{instance.warehouseName?.trim() || '—'}</span>
              </div>
              <div className="asset-detail__info-row asset-detail__info-row--multiline">
                <span className="label">Vị trí tài sản</span>
                <span className="value">{formatCurrentLocationLabel(instance)}</span>
              </div>
            </div>
            <div className="asset-detail__info-col">
              <div className="asset-detail__info-row">
                <span className="label">Ngày mua</span>
                <span className="value">{formatDate(instance.purchaseDate)}</span>
              </div>
              <div className="asset-detail__info-row">
                <span className="label">Ngày đưa vào sử dụng</span>
                <span className="value">{formatDate(instance.inUseDate)}</span>
              </div>
              <div className="asset-detail__info-row">
                <span className="label">Giá gốc</span>
                <span className="value">{formatVnd(instance.originalPrice)}</span>
              </div>
              <div className="asset-detail__info-row">
                <span className="label">Giá trị hiện tại</span>
                <span className="value">{formatVnd(instance.currentValue)}</span>
              </div>
              <div className="asset-detail__info-row">
                <span className="label">Số hợp đồng</span>
                <span className="value">{instance.contractNo?.trim() || '—'}</span>
              </div>
              <div className="asset-detail__info-row">
                <span className="label">Người phụ trách</span>
                <span className="value">
                  {instance.currentResponsibleEmployeeName?.trim() || '—'}
                </span>
              </div>
              <div className="asset-detail__info-row asset-detail__info-row--multiline">
                <span className="label">Ghi chú cá thể</span>
                <span className="value">{instance.note?.trim() || '—'}</span>
              </div>
              <div className="asset-detail__info-row asset-detail__info-row--multiline">
                <span className="label">Tình trạng / Mô tả</span>
                <span className="value">{instance.condition?.trim() || '—'}</span>
              </div>
            </div>
          </div>
        </div>

        <div className="asset-detail__section">
          <h2 className="asset-detail__section-title">Khấu hao (theo bản ghi gần nhất)</h2>
          <div className="asset-detail__info-row">
            <span className="label">Chính sách</span>
            <span className="value">{instance.depreciationPolicyName ?? '—'}</span>
          </div>
          <div className="asset-detail__info-row">
            <span className="label">Thời gian hữu ích (tháng)</span>
            <span className="value">
              {instance.depreciationUsefulLifeMonths != null
                ? String(instance.depreciationUsefulLifeMonths)
                : '—'}
            </span>
          </div>
          <div className="asset-detail__info-row">
            <span className="label">Giá trị thu hồi ước tính</span>
            <span className="value">
              {instance.depreciationSalvageValue != null
                ? formatVnd(instance.depreciationSalvageValue)
                : '—'}
            </span>
          </div>
          <div className="asset-detail__info-row">
            <span className="label">Kỳ khấu hao gần nhất</span>
            <span className="value">
              {instance.depreciationPeriod ? formatDate(instance.depreciationPeriod) : '—'}
            </span>
          </div>
          <div className="asset-detail__info-row">
            <span className="label">Mức khấu hao kỳ gần nhất</span>
            <span className="value">
              {instance.depreciationAmount != null ? formatVnd(instance.depreciationAmount) : '—'}
            </span>
          </div>
          <div className="asset-detail__info-row">
            <span className="label">Lũy kế</span>
            <span className="value">
              {instance.accumulatedDepreciation != null
                ? formatVnd(instance.accumulatedDepreciation)
                : '—'}
            </span>
          </div>
          <div className="asset-detail__info-row">
            <span className="label">Giá trị còn lại (KH)</span>
            <span className="value">
              {instance.remainingValue != null ? formatVnd(instance.remainingValue) : '—'}
            </span>
          </div>
        </div>
      </div>
    </div>
  );
}

import { useEffect, useState } from 'react';
import { useParams, Link } from 'react-router-dom';
import {
  assetService,
  formatVnd,
  getStatusLabel,
  type AssetDetailResponse,
} from '../services/assetService';
import {
  getMaintenanceRecordStatusLabel,
  maintenanceRecordService,
  type MaintenanceRecordResponse,
} from '../services/maintenanceRecordService';
import {
  maintenanceScheduleService,
  type MaintenanceScheduleResponse,
} from '../services/maintenanceScheduleService';
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

function getFileNameFromUrl(url: string): string {
  try {
    const cleanUrl = url.split('?')[0];
    const parts = cleanUrl.split('/');
    const last = parts[parts.length - 1];
    return decodeURIComponent(last || url);
  } catch {
    return url;
  }
}

function parseEnumNumber(value: number | string | null | undefined): number {
  if (typeof value === 'number') return value;
  if (!value) return 0;
  const parsed = Number(value);
  if (Number.isFinite(parsed)) return parsed;
  const normalized = String(value).toLowerCase();
  if (normalized === 'onetime') return 1;
  if (normalized === 'periodic') return 2;
  if (normalized === 'day') return 1;
  if (normalized === 'week') return 2;
  if (normalized === 'month') return 3;
  if (normalized === 'year') return 4;
  return 0;
}

function getScheduleTypeLabel(value: number | string): string {
  return parseEnumNumber(value) === 1 ? 'Một lần' : 'Định kỳ';
}

function getRepeatUnitLabel(value?: number | string | null): string {
  const parsed = parseEnumNumber(value);
  if (parsed === 1) return 'Ngày';
  if (parsed === 2) return 'Tuần';
  if (parsed === 3) return 'Tháng';
  if (parsed === 4) return 'Năm';
  return '—';
}

export function AssetDetailPage() {
  const params = useParams<{ id: string }>();
  const id = params.id ? Number(params.id) : NaN;
  const [asset, setAsset] = useState<AssetDetailResponse | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [profile, setProfile] = useState<UserProfile | null>(null);
  const [maintenanceRecords, setMaintenanceRecords] = useState<MaintenanceRecordResponse[]>(
    []
  );
  const [maintenanceSchedules, setMaintenanceSchedules] = useState<MaintenanceScheduleResponse[]>(
    []
  );
  const [selectedMaintenanceRecord, setSelectedMaintenanceRecord] =
    useState<MaintenanceRecordResponse | null>(null);

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
        const [assetRes, profileRes, maintenanceRecordRes] = await Promise.all([
          assetService.getById(id),
          profileService.getProfile().catch(() => null),
          maintenanceRecordService.getByAssetId(id).catch(() => []),
        ]);
        const scheduleRes = await maintenanceScheduleService.findByAssetId(id).catch(() => []);
        if (cancelled) return;
        setAsset(assetRes);
        if (profileRes) setProfile(profileRes);
        setMaintenanceRecords(maintenanceRecordRes);
        setMaintenanceSchedules(scheduleRes);
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

  const primary = asset.instances?.[0];

  const statusLabel = getStatusLabel(asset.statusName);

  const depreciationPolicyLabel =
    primary?.depreciationPolicyName ?? 'Chưa cấu hình chính sách khấu hao';

  const depreciationUsefulLifeLabel =
    primary?.depreciationUsefulLifeMonths != null
      ? `${primary.depreciationUsefulLifeMonths} tháng`
      : '—';

  const depreciationSalvageValueLabel =
    primary?.depreciationSalvageValue != null
      ? formatVnd(primary.depreciationSalvageValue)
      : '—';

  const depreciationAmountLabel =
    primary?.depreciationAmount != null
      ? formatVnd(primary.depreciationAmount)
      : '—';

  const accumulatedDepreciationLabel =
    primary?.accumulatedDepreciation != null
      ? formatVnd(primary.accumulatedDepreciation)
      : '—';

  const remainingValueLabel =
    primary?.remainingValue != null ? formatVnd(primary.remainingValue) : '—';

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
                <span className="label">Giá trị hiện tại (phiên bản đầu)</span>
                <span className="value">
                  {primary != null ? formatVnd(primary.currentValue) : '—'}
                </span>
              </div>
            </div>
            <div className="asset-detail__info-col">
              <div className="asset-detail__info-row">
                <span className="label">Phòng ban sử dụng</span>
                <span className="value">{primary?.currentDepartmentName ?? '—'}</span>
              </div>
              <div className="asset-detail__info-row">
                <span className="label">Ngày mua</span>
                <span className="value">{formatDate(primary?.purchaseDate)}</span>
              </div>
              <div className="asset-detail__info-row">
                <span className="label">Giá gốc</span>
                <span className="value">
                  {primary != null ? formatVnd(primary.originalPrice) : '—'}
                </span>
              </div>
              <div className="asset-detail__info-row">
                <span className="label">Ngày đưa vào sử dụng</span>
                <span className="value">{formatDate(primary?.inUseDate ?? asset.inUseDate)}</span>
              </div>
            </div>
          </div>
        </div>

        <div className="asset-detail__section">
          <h2 className="asset-detail__section-title">Quy định bảo dưỡng</h2>
          <div className="asset-detail__table-wrapper">
            <table className="asset-detail__table">
              <thead>
                <tr>
                  <th>NỘI DUNG BẢO DƯỠNG</th>
                  <th>THỜI ĐIỂM ÁP DỤNG</th>
                  <th>LẶP LẠI THEO</th>
                  <th>BẢO DƯỠNG LẠI SAU</th>
                </tr>
              </thead>
              <tbody>
                {maintenanceSchedules.length > 0 ? (
                  maintenanceSchedules.map((schedule) => (
                    <tr key={schedule.scheduleId}>
                      <td>
                        {schedule.content?.trim() ||
                          (schedule.templateId ? `Mẫu quy định #${schedule.templateId}` : '—')}
                      </td>
                      <td>{formatDate(schedule.startDate)}</td>
                      <td>{getScheduleTypeLabel(schedule.scheduleType)}</td>
                      <td>
                        {schedule.intervalValue && schedule.intervalUnit
                          ? `${schedule.intervalValue} ${getRepeatUnitLabel(schedule.intervalUnit)}`
                          : '—'}
                      </td>
                    </tr>
                  ))
                ) : (
                  <tr>
                    <td colSpan={4} className="asset-detail__empty">
                      Chưa có quy định bảo dưỡng cho tài sản này.
                    </td>
                  </tr>
                )}
              </tbody>
            </table>
          </div>
        </div>

        <div className="asset-detail__section">
          <h2 className="asset-detail__section-title">Bảo hành</h2>
          <div className="asset-detail__info-row">
            <span className="label">Hạn bảo hành</span>
            <span className="value">—</span>
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
              {primary?.depreciationPeriod
                ? formatDate(primary.depreciationPeriod)
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
            <span className="value">
              {primary != null ? formatVnd(primary.originalPrice) : '—'}
            </span>
          </div>
          <div className="asset-detail__info-row">
            <span className="label">Giá trị hiện tại</span>
            <span className="value">
              {primary != null ? formatVnd(primary.currentValue) : '—'}
            </span>
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
                  <th>NGÀY THỰC HIỆN</th>
                  <th>NỘI DUNG CÔNG VIỆC</th>
                  <th>CHI PHÍ</th>
                  <th>TÌNH TRẠNG TRƯỚC</th>
                  <th>TÌNH TRẠNG SAU</th>
                  <th>GHI CHÚ KỸ THUẬT</th>
                  <th>TRẠNG THÁI</th>
                  <th>THAO TÁC</th>
                </tr>
              </thead>
              <tbody>
                {maintenanceRecords.length > 0 ? (
                  maintenanceRecords.map((record) => (
                    <tr key={record.recordId}>
                      <td>{formatDate(record.executionDate)}</td>
                      <td>{record.workPerformed || '—'}</td>
                      <td>
                        {record.totalCost != null ? formatVnd(record.totalCost) : '—'}
                      </td>
                      <td>{record.conditionBefore || '—'}</td>
                      <td>{record.conditionAfter || '—'}</td>
                      <td>{record.technicalNote || '—'}</td>
                      <td>{getMaintenanceRecordStatusLabel(record.status)}</td>
                      <td>
                        <button
                          type="button"
                          className="asset-detail__icon-btn"
                          title="Xem chi tiết đơn hoàn thành bảo dưỡng"
                          aria-label="Xem chi tiết đơn hoàn thành bảo dưỡng"
                          onClick={() => setSelectedMaintenanceRecord(record)}
                        >
                          👁️
                        </button>
                      </td>
                    </tr>
                  ))
                ) : (
                  <tr>
                    <td colSpan={8} className="asset-detail__empty">
                      Chưa có lịch sử bảo trì/bảo dưỡng cho tài sản này.
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
            {asset.documents && asset.documents.length > 0 ? (
              asset.documents.map((doc, idx) => (
                <div key={doc.documentId} className="asset-detail__file">
                  <span className="asset-detail__file-index">#{idx + 1}</span>
                  <a
                    className="asset-detail__file-name"
                    href={doc.fileUrl}
                    target="_blank"
                    rel="noreferrer"
                  >
                    {getFileNameFromUrl(doc.fileUrl)}
                  </a>
                </div>
              ))
            ) : (
              <p className="asset-detail__empty">Chưa có tài liệu đính kèm.</p>
            )}
          </div>
        </div>
      </div>

      {selectedMaintenanceRecord && (
        <div
          className="asset-detail__record-modal-overlay"
          onClick={() => setSelectedMaintenanceRecord(null)}
        >
          <div
            className="asset-detail__record-modal"
            role="dialog"
            aria-modal="true"
            aria-label="Chi tiết đơn hoàn thành bảo dưỡng"
            onClick={(e) => e.stopPropagation()}
          >
            <button
              type="button"
              className="asset-detail__record-modal-close-btn"
              onClick={() => setSelectedMaintenanceRecord(null)}
              aria-label="Đóng"
            >
              <span className="asset-detail__record-modal-close">×</span>
            </button>

            <div className="asset-detail__record-modal-header">
              <h3 className="asset-detail__record-modal-title">
                Chi tiết đơn hoàn thành bảo dưỡng
              </h3>
            </div>

            <div className="asset-detail__record-modal-body">
              <div className="asset-detail__record-modal-content">
                <div className="asset-detail__record-form-item">
                  <label htmlFor="maintenance-record-no">Số biên bản</label>
                  <input
                    id="maintenance-record-no"
                    type="text"
                    value={`MR-${selectedMaintenanceRecord.recordId}`}
                    disabled
                    className="asset-detail__record-input-disabled"
                  />
                </div>

                <div className="asset-detail__record-info-section">
                  <h4 className="asset-detail__record-section-title">Thông tin tài sản</h4>
                  <div className="asset-detail__record-info-grid">
                    <div className="asset-detail__record-info-row">
                      <div className="asset-detail__record-info-item">
                        <label>Mã tài sản</label>
                        <div className="asset-detail__record-info-value">{asset.code}</div>
                      </div>
                      <div className="asset-detail__record-info-item">
                        <label>Tên tài sản</label>
                        <div className="asset-detail__record-info-value">{asset.name}</div>
                      </div>
                    </div>
                    <div className="asset-detail__record-info-row">
                      <div className="asset-detail__record-info-item">
                        <label>Loại tài sản</label>
                        <div className="asset-detail__record-info-value">
                          {asset.assetTypeName ?? '—'}
                        </div>
                      </div>
                      <div className="asset-detail__record-info-item">
                        <label>Phòng ban sử dụng</label>
                        <div className="asset-detail__record-info-value">
                          {primary?.currentDepartmentName ?? '—'}
                        </div>
                      </div>
                    </div>
                  </div>
                </div>

                <div className="asset-detail__record-form-section">
                  <h4 className="asset-detail__record-section-title">
                    Thông tin báo cáo hoàn thành bảo dưỡng
                  </h4>
                  <div className="asset-detail__record-info-grid">
                    <div className="asset-detail__record-info-row">
                      <div className="asset-detail__record-info-item">
                        <label>Mã công việc</label>
                        <div className="asset-detail__record-info-value">
                          {selectedMaintenanceRecord.taskId}
                        </div>
                      </div>
                      <div className="asset-detail__record-info-item">
                        <label>Ngày thực hiện</label>
                        <div className="asset-detail__record-info-value">
                          {formatDate(selectedMaintenanceRecord.executionDate)}
                        </div>
                      </div>
                    </div>

                    <div className="asset-detail__record-info-row">
                      <div className="asset-detail__record-info-item">
                        <label>Chi phí thực tế</label>
                        <div className="asset-detail__record-info-value">
                          {formatVnd(selectedMaintenanceRecord.totalCost ?? 0)}
                        </div>
                      </div>
                      <div className="asset-detail__record-info-item">
                        <label>Trạng thái kết quả</label>
                        <div className="asset-detail__record-info-value">
                          {getMaintenanceRecordStatusLabel(selectedMaintenanceRecord.status)}
                        </div>
                      </div>
                    </div>

                    <div className="asset-detail__record-info-row asset-detail__record-info-row--single">
                      <div className="asset-detail__record-info-item">
                        <label>Nội dung công việc đã thực hiện</label>
                        <div className="asset-detail__record-info-value">
                          {selectedMaintenanceRecord.workPerformed || '—'}
                        </div>
                      </div>
                    </div>

                    <div className="asset-detail__record-info-row">
                      <div className="asset-detail__record-info-item">
                        <label>Tình trạng trước bảo dưỡng</label>
                        <div className="asset-detail__record-info-value">
                          {selectedMaintenanceRecord.conditionBefore || '—'}
                        </div>
                      </div>
                      <div className="asset-detail__record-info-item">
                        <label>Tình trạng sau bảo dưỡng</label>
                        <div className="asset-detail__record-info-value">
                          {selectedMaintenanceRecord.conditionAfter || '—'}
                        </div>
                      </div>
                    </div>

                    <div className="asset-detail__record-info-row asset-detail__record-info-row--single">
                      <div className="asset-detail__record-info-item">
                        <label>Ghi chú kỹ thuật</label>
                        <div className="asset-detail__record-info-value">
                          {selectedMaintenanceRecord.technicalNote || '—'}
                        </div>
                      </div>
                    </div>
                  </div>
                </div>
              </div>
            </div>

            <div className="asset-detail__record-modal-footer">
              <button
                type="button"
                onClick={() => setSelectedMaintenanceRecord(null)}
                className="asset-detail__record-btn-close"
              >
                Đóng
              </button>
            </div>
          </div>
        </div>
      )}
    </div>
  );
}

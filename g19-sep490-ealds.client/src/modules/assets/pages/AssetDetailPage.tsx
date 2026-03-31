import { useEffect, useState } from 'react';
import { useParams, Link } from 'react-router-dom';
import {
  assetService,
  formatVnd,
  getStatusLabel,
  type AssetDetailResponse,
  type AssetInstanceResponse,
} from '../services/assetService';
import {
  getMaintenanceRecordStatusLabel,
  maintenanceRecordService,
  type MaintenanceRecordResponse,
} from '../services/maintenanceRecordService';
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

/** Vị trí hiện tại từ bảng AssetLocation (phòng ban + ghi chú vị trí). */
function formatInstanceCurrentLocation(row: AssetInstanceResponse): string {
  const dept = row.currentDepartmentName?.trim();
  const note = row.currentLocationNote?.trim();
  if (dept && note) return `${dept} · ${note}`;
  if (dept) return dept;
  if (note) return note;
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

  const isAccountant =
    mapBackendRoleToAppRole(profile?.role ?? storedRole) === 'accountant';

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
        if (cancelled) return;
        setAsset(assetRes);
        if (profileRes) setProfile(profileRes);
        setMaintenanceRecords(maintenanceRecordRes);
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

  const instances = asset.instances ?? [];
  const instanceCount = instances.length;
  const primary = instances[0];
  const maintenanceSchedules = asset.maintenanceSchedules ?? [];

  const statusLabel = getStatusLabel(asset.statusName);

  const scheduleInstanceLabel = (row: (typeof maintenanceSchedules)[0]) =>
    row.instanceCode?.trim()
      ? row.instanceCode.trim()
      : '— (chung toàn bộ cá thể)';

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
                <span className="value">{instanceCount}</span>
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
              <label className="asset-detail__checkbox asset-detail__checkbox--readonly">
                <input type="checkbox" checked readOnly />
                <span>Là tài sản cố định</span>
              </label>
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
              <div className="asset-detail__info-row asset-detail__info-row--multiline">
                <span className="label">Quy cách tài sản</span>
                <span className="value">
                  {asset.specification?.trim() ? asset.specification.trim() : '—'}
                </span>
              </div>
              <div className="asset-detail__info-row asset-detail__info-row--multiline">
                <span className="label">Ghi chú</span>
                <span className="value">{asset.note?.trim() ? asset.note.trim() : '—'}</span>
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
                  <th>MÃ CÁ THỂ</th>
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
                      <td>{scheduleInstanceLabel(schedule)}</td>
                      <td>
                        {schedule.content?.trim() ||
                          (schedule.templateId
                            ? `Mẫu quy định #${schedule.templateId}`
                            : '—')}
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
                    <td colSpan={5} className="asset-detail__empty">
                      Chưa có quy định bảo dưỡng cho tài sản này.
                    </td>
                  </tr>
                )}
              </tbody>
            </table>
          </div>
        </div>

        <div className="asset-detail__section">
          <h2 className="asset-detail__section-title">Quá trình sử dụng</h2>
          <div className="asset-detail__table-wrapper">
            <table className="asset-detail__table">
              <thead>
                <tr>
                  <th>MÃ CÁ THỂ</th>
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
                  <td colSpan={7} className="asset-detail__empty">
                    Chưa có dữ liệu (cần API quá trình sử dụng theo từng cá thể).
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
                  <th>MÃ CÁ THỂ</th>
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
                      <td>{record.instanceCode ?? '—'}</td>
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
                    <td colSpan={9} className="asset-detail__empty">
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

        <div className="asset-detail__section">
          <h2 className="asset-detail__section-title">Danh sách cá thể</h2>
          <div className="asset-detail__table-wrapper">
            <table className="asset-detail__table">
              <thead>
                <tr>
                  <th>MÃ CÁ THỂ</th>
                  <th>SỐ SERI</th>
                  <th>TRẠNG THÁI</th>
                  <th>KHO</th>
                  <th>VỊ TRÍ TÀI SẢN</th>
                  <th>GIÁ TRỊ HIỆN TẠI</th>
                  <th>NGÀY MUA</th>
                  <th className="asset-detail__th-narrow">CHI TIẾT</th>
                </tr>
              </thead>
              <tbody>
                {instances.length > 0 ? (
                  instances.map((row) => (
                    <tr key={row.assetInstanceId}>
                      <td>{row.instanceCode}</td>
                      <td>{row.serialNumber?.trim() || '—'}</td>
                      <td>{getStatusLabel(row.statusName)}</td>
                      <td>{row.warehouseName?.trim() || '—'}</td>
                      <td>{formatInstanceCurrentLocation(row)}</td>
                      <td>{formatVnd(row.currentValue)}</td>
                      <td>{formatDate(row.purchaseDate)}</td>
                      <td>
                        <Link
                          className="asset-detail__icon-btn asset-detail__icon-btn--link"
                          to={`/asset-instances/${row.assetInstanceId}`}
                          state={{
                            backToPath: `/assets/${asset.assetId}`,
                            backLabel: '← Quay lại chi tiết tài sản',
                          }}
                          title="Xem chi tiết cá thể"
                          aria-label="Xem chi tiết cá thể"
                        >
                          👁️
                        </Link>
                      </td>
                    </tr>
                  ))
                ) : (
                  <tr>
                    <td colSpan={8} className="asset-detail__empty">
                      Chưa có cá thể nào cho tài sản này.
                    </td>
                  </tr>
                )}
              </tbody>
            </table>
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
                        <label>Mã cá thể</label>
                        <div className="asset-detail__record-info-value">
                          {selectedMaintenanceRecord.instanceCode ?? '—'}
                        </div>
                      </div>
                      <div className="asset-detail__record-info-item">
                        <label>Loại tài sản</label>
                        <div className="asset-detail__record-info-value">
                          {asset.assetTypeName ?? '—'}
                        </div>
                      </div>
                    </div>
                    <div className="asset-detail__record-info-row asset-detail__record-info-row--single">
                      <div className="asset-detail__record-info-item">
                        <label>Phòng ban sử dụng (tham chiếu)</label>
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

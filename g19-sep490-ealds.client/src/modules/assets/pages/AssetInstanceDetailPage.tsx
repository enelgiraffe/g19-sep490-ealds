import { useEffect, useState } from 'react';
import { Link, useLocation, useParams } from 'react-router-dom';
import {
  assetInstanceService,
  formatVnd,
  getStatusLabel,
  type AssetInstanceResponse,
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

function formatCurrentLocationLabel(row: AssetInstanceResponse): string {
  const dept = row.currentDepartmentName?.trim();
  const note = row.currentLocationNote?.trim();
  if (dept && note) return `${dept} · ${note}`;
  if (dept) return dept;
  if (note) return note;
  return '—';
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

function getWarrantyPeriodLabel(value?: number | null, unit?: string | null): string {
  if (value == null || !unit?.trim()) return '—';
  const normalized = unit.trim().toLowerCase();
  if (normalized === 'day' || normalized === 'days') return `${value} ngày`;
  if (normalized === 'week' || normalized === 'weeks') return `${value} tuần`;
  if (normalized === 'month' || normalized === 'months') return `${value} tháng`;
  if (normalized === 'year' || normalized === 'years') return `${value} năm`;
  return `${value} ${unit}`;
}

function getScheduleContentLabel(row: {
  content?: string | null;
  templateName?: string | null;
  templateId?: number | null;
}): string {
  if (row.content?.trim()) return row.content.trim();
  if (row.templateName?.trim()) return row.templateName.trim();
  if (row.templateId) return `Mẫu quy định #${row.templateId}`;
  return '—';
}

function getCleanWorkPerformedText(value?: string | null): string {
  if (!value?.trim()) return '—';
  const workLines = value
    .split('\n')
    .map((line) => line.trim())
    .filter(
      (line) =>
        line.length > 0 &&
        !line.toLowerCase().startsWith('reportnumber:') &&
        !line.toLowerCase().startsWith('returntousedate:')
    );
  return workLines.length > 0 ? workLines.join('\n') : '—';
}

export function AssetInstanceDetailPage() {
  const location = useLocation();
  const params = useParams<{ instanceId: string }>();
  const instanceId = params.instanceId ? Number(params.instanceId) : NaN;
  const [instance, setInstance] = useState<AssetInstanceResponse | null>(null);
  const [maintenanceSchedules, setMaintenanceSchedules] = useState<MaintenanceScheduleResponse[]>(
    []
  );
  const [maintenanceRecords, setMaintenanceRecords] = useState<MaintenanceRecordResponse[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [profile, setProfile] = useState<UserProfile | null>(null);
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
        const [instRes, profileRes, scheduleRes, recordRes] = await Promise.all([
          assetInstanceService.getById(instanceId),
          profileService.getProfile().catch(() => null),
          maintenanceScheduleService.findByInstanceId(instanceId).catch(() => []),
          maintenanceRecordService.getByInstanceId(instanceId).catch(() => []),
        ]);
        if (cancelled) return;
        setInstance(instRes);
        if (profileRes) setProfile(profileRes);
        setMaintenanceSchedules(scheduleRes);
        setMaintenanceRecords(recordRes);
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

  const state = (location.state ?? {}) as { backToPath?: string; backLabel?: string };
  const backToAssetPath = state.backToPath?.trim() || `/assets/${instance.assetId}`;
  const backLabel = state.backLabel?.trim() || '← Quay lại chi tiết tài sản';
  const statusLabel = getStatusLabel(instance.statusName);
  const latestGuarantee =
    instance.guarantees && instance.guarantees.length > 0
      ? [...instance.guarantees].sort((a, b) =>
          String(a.warrantyEndDate ?? '').localeCompare(String(b.warrantyEndDate ?? ''))
        )[instance.guarantees.length - 1]
      : null;

  const displayGuaranteeId = latestGuarantee?.guaranteeId ?? instance.guaranteeId;
  const displayWarrantyPeriodValue =
    latestGuarantee?.warrantyPeriodValue ?? instance.warrantyPeriodValue;
  const displayWarrantyPeriodUnit =
    latestGuarantee?.warrantyPeriodUnit ?? instance.warrantyPeriodUnit;
  const displayWarrantyConditions =
    latestGuarantee?.warrantyConditions ?? instance.warrantyConditions;
  const displayWarrantyStartDate = latestGuarantee?.startDate ?? instance.warrantyStartDate;
  const displayWarrantyEndDate = latestGuarantee?.warrantyEndDate ?? instance.warrantyEndDate;

  const scheduleScopeLabel = (row: MaintenanceScheduleResponse) =>
    row.assetInstanceId != null && row.assetInstanceId === instanceId
      ? 'Riêng cá thể này'
      : 'Chung (toàn bộ cá thể)';

  return (
    <div className="asset-detail-page">
      <div className="asset-detail__header">
        <Link to={backToAssetPath} className="asset-detail__back">
          {backLabel}
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
          <h2 className="asset-detail__section-title">Bảo hành và khấu hao</h2>
          <div className="asset-detail__info-grid">
            <div className="asset-detail__info-col">
              <h3 className="asset-detail__subsection-title">Thông tin bảo hành</h3>
              <div className="asset-detail__info-row">
                <span className="label">Mã bảo hành</span>
                <span className="value">
                  {displayGuaranteeId != null ? `BH-${displayGuaranteeId}` : '—'}
                </span>
              </div>
              <div className="asset-detail__info-row">
                <span className="label">Thời hạn bảo hành</span>
                <span className="value">
                  {getWarrantyPeriodLabel(displayWarrantyPeriodValue, displayWarrantyPeriodUnit)}
                </span>
              </div>
              <div className="asset-detail__info-row">
                <span className="label">Ngày bắt đầu</span>
                <span className="value">{formatDate(displayWarrantyStartDate)}</span>
              </div>
              <div className="asset-detail__info-row">
                <span className="label">Ngày kết thúc</span>
                <span className="value">{formatDate(displayWarrantyEndDate)}</span>
              </div>
              <div className="asset-detail__info-row asset-detail__info-row--multiline">
                <span className="label">Điều kiện bảo hành</span>
                <span className="value">{displayWarrantyConditions?.trim() || '—'}</span>
              </div>
            </div>

            <div className="asset-detail__info-col">
              <h3 className="asset-detail__subsection-title">Thông tin khấu hao</h3>
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

        <div className="asset-detail__section">
          <h2 className="asset-detail__section-title">Quy định bảo dưỡng</h2>
          <div className="asset-detail__table-wrapper">
            <table className="asset-detail__table">
              <thead>
                <tr>
                  <th>PHẠM VI</th>
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
                      <td>{scheduleScopeLabel(schedule)}</td>
                      <td>{getScheduleContentLabel(schedule)}</td>
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
                      Chưa có quy định bảo dưỡng cho cá thể này.
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
                    Chưa có dữ liệu (cần API quá trình sử dụng theo cá thể).
                  </td>
                </tr>
              </tbody>
            </table>
          </div>
        </div>

        <div className="asset-detail__section">
          <h2 className="asset-detail__section-title">Lịch sửa chữa, bảo dưỡng / bảo trì</h2>
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
                      <td>{getCleanWorkPerformedText(record.workPerformed)}</td>
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
                      Chưa có lịch sử bảo trì/bảo dưỡng cho cá thể này.
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
                  <label htmlFor="inst-maintenance-record-no">Số biên bản</label>
                  <input
                    id="inst-maintenance-record-no"
                    type="text"
                    value={`MR-${selectedMaintenanceRecord.recordId}`}
                    disabled
                    className="asset-detail__record-input-disabled"
                  />
                </div>

                <div className="asset-detail__record-info-section">
                  <h4 className="asset-detail__record-section-title">Thông tin tài sản / cá thể</h4>
                  <div className="asset-detail__record-info-grid">
                    <div className="asset-detail__record-info-row">
                      <div className="asset-detail__record-info-item">
                        <label>Mã tài sản</label>
                        <div className="asset-detail__record-info-value">
                          {instance.assetCode ?? '—'}
                        </div>
                      </div>
                      <div className="asset-detail__record-info-item">
                        <label>Tên tài sản</label>
                        <div className="asset-detail__record-info-value">
                          {instance.assetName ?? '—'}
                        </div>
                      </div>
                    </div>
                    <div className="asset-detail__record-info-row">
                      <div className="asset-detail__record-info-item">
                        <label>Mã cá thể</label>
                        <div className="asset-detail__record-info-value">
                          {selectedMaintenanceRecord.instanceCode ?? instance.instanceCode}
                        </div>
                      </div>
                      <div className="asset-detail__record-info-item">
                        <label>Phòng ban (vị trí hiện tại)</label>
                        <div className="asset-detail__record-info-value">
                          {instance.currentDepartmentName ?? '—'}
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
                          {getCleanWorkPerformedText(selectedMaintenanceRecord.workPerformed)}
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

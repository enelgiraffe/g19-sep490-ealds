import { FormEvent, useEffect, useState } from 'react';
import { Link, useNavigate, useParams } from 'react-router-dom';
import { assetService, type AssetResponse, type UpdateAssetPayload } from '../services/assetService';
import {
  maintenanceScheduleService,
  type MaintenanceScheduleResponse,
} from '../services/maintenanceScheduleService';
import './AssetCreatePage.css';

function getStoredUserId(): number | null {
  const raw = localStorage.getItem('user');
  if (!raw) return null;
  try {
    const parsed = JSON.parse(raw) as { id?: string | number | null };
    const idNum = typeof parsed.id === 'number' ? parsed.id : Number(parsed.id);
    return Number.isFinite(idNum) && idNum > 0 ? idNum : null;
  } catch {
    return null;
  }
}

function formatDate(iso?: string | null): string {
  if (!iso) return '—';
  try {
    const d = new Date(iso);
    return d.toLocaleDateString('vi-VN');
  } catch {
    return iso;
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

function getRepeatUnitLabel(value?: number | string | null): string {
  const parsed = parseEnumNumber(value);
  if (parsed === 1) return 'Ngày';
  if (parsed === 2) return 'Tuần';
  if (parsed === 3) return 'Tháng';
  if (parsed === 4) return 'Năm';
  return '—';
}

function toIsoWithOffset(baseDateIso: string | null | undefined, days: number): string {
  const fallback = new Date();
  const base = baseDateIso ? new Date(baseDateIso) : fallback;
  const safeBase = Number.isNaN(base.getTime()) ? fallback : base;
  safeBase.setDate(safeBase.getDate() + Math.max(0, days));
  return safeBase.toISOString();
}

export function AssetEditPage() {
  const { id } = useParams<{ id: string }>();
  const navigate = useNavigate();
  const assetId = Number(id);

  const [loading, setLoading] = useState(true);
  const [saving, setSaving] = useState(false);
  const [asset, setAsset] = useState<AssetResponse | null>(null);
  const [error, setError] = useState<string | null>(null);

  const [code, setCode] = useState('');
  const [name, setName] = useState('');
  const [assetTypeId, setAssetTypeId] = useState('');
  const [purchaseDate, setPurchaseDate] = useState('');
  const [originalPrice, setOriginalPrice] = useState('');
  const [currentValue, setCurrentValue] = useState('');
  const [unit, setUnit] = useState('');
  const [quantity, setQuantity] = useState('');
  const [warehouseId, setWarehouseId] = useState('');
  const [warrantyEndDate, setWarrantyEndDate] = useState('');

  const [supplier, setSupplier] = useState('');
  const [contractNumber, setContractNumber] = useState('');
  const [serialNumber, setSerialNumber] = useState('');
  const [specification, setSpecification] = useState('');
  const [note, setNote] = useState('');
  const [origin, setOrigin] = useState('');
  const [manager, setManager] = useState('');
  const [warrantyMonths, setWarrantyMonths] = useState('');
  const [warrantyCondition, setWarrantyCondition] = useState('');
  const [depreciationBaseValue, setDepreciationBaseValue] = useState('');
  const [depreciationStartDate, setDepreciationStartDate] = useState('');
  const [depreciationMonths, setDepreciationMonths] = useState('');
  const [depreciationRemainingMonths, setDepreciationRemainingMonths] = useState('');
  const [depreciationAccumulated, setDepreciationAccumulated] = useState('');
  const [depreciationRemainingValue, setDepreciationRemainingValue] = useState('');
  const [maintenanceSchedules, setMaintenanceSchedules] = useState<MaintenanceScheduleResponse[]>(
    []
  );
  const [scheduleModalOpen, setScheduleModalOpen] = useState(false);
  const [scheduleSubmitting, setScheduleSubmitting] = useState(false);
  const [scheduleError, setScheduleError] = useState<string | null>(null);
  const [maintenanceContent, setMaintenanceContent] = useState('');
  const [scheduleType, setScheduleType] = useState<1 | 2>(2);
  const [startPointMode, setStartPointMode] = useState<'inUse' | 'purchase'>('inUse');
  const [startOffsetDays, setStartOffsetDays] = useState<number>(0);
  const [repeatIntervalUnit, setRepeatIntervalUnit] = useState<1 | 2 | 3 | 4>(3);

  useEffect(() => {
    if (!assetId || Number.isNaN(assetId)) {
      setError('ID tài sản không hợp lệ.');
      setLoading(false);
      return;
    }

    let isMounted = true;
    setLoading(true);

    assetService
      .getById(assetId)
      .then(async (data) => {
        if (!isMounted) return;
        setAsset(data);
        setCode(data.code);
        setName(data.name);
        setAssetTypeId(String(data.assetTypeId));
        setPurchaseDate(data.purchaseDate);
        setOriginalPrice(String(data.originalPrice));
        setCurrentValue(String(data.currentValue));
        setUnit(data.unit);
        setQuantity(String(data.quantity));
        setWarehouseId(String(data.warehouseId));
        setWarrantyEndDate(data.warrantyEndDate ?? '');
        const schedules = await maintenanceScheduleService.findByAssetId(assetId).catch(() => []);
        if (!isMounted) return;
        setMaintenanceSchedules(schedules);
      })
      .catch(() => {
        if (!isMounted) return;
        setError('Không tải được thông tin tài sản.');
      })
      .finally(() => {
        if (isMounted) setLoading(false);
      });

    return () => {
      isMounted = false;
    };
  }, [assetId]);

  const openScheduleModal = () => {
    setScheduleError(null);
    setScheduleType(2);
    setStartPointMode('inUse');
    setStartOffsetDays(0);
    setRepeatIntervalUnit(3);
    setMaintenanceContent('');
    setScheduleModalOpen(true);
  };

  const handleCreateSchedule = async () => {
    if (!asset) return;
    const createBy = getStoredUserId();
    if (!createBy) {
      setScheduleError('Không xác định được người dùng tạo lịch bảo dưỡng.');
      return;
    }
    if (!maintenanceContent.trim()) {
      setScheduleError('Vui lòng nhập nội dung bảo dưỡng.');
      return;
    }

    setScheduleSubmitting(true);
    setScheduleError(null);
    try {
      const startDate =
        startPointMode === 'inUse'
          ? toIsoWithOffset(asset.inUseDate, startOffsetDays)
          : toIsoWithOffset(asset.purchaseDate, startOffsetDays);

      await maintenanceScheduleService.addSchedule({
        assetId: asset.assetId,
        templateId: null,
        content: maintenanceContent.trim(),
        scheduleType,
        intervalUnit: scheduleType === 2 ? repeatIntervalUnit : null,
        intervalValue: scheduleType === 2 ? 1 : null,
        startDate,
        endDate: null,
        isActive: true,
        createBy,
        createDate: new Date().toISOString(),
      });

      const refreshed = await maintenanceScheduleService
        .findByAssetId(asset.assetId)
        .catch(() => []);
      setMaintenanceSchedules(refreshed);
      setScheduleModalOpen(false);
    } catch (e: any) {
      const serverMessage = e?.response?.data;
      setScheduleError(
        typeof serverMessage === 'string' && serverMessage.trim()
          ? serverMessage
          : 'Thêm quy định bảo dưỡng thất bại. Vui lòng thử lại.'
      );
    } finally {
      setScheduleSubmitting(false);
    }
  };

  const handleSubmit = async (e: FormEvent) => {
    e.preventDefault();
    if (!assetId || Number.isNaN(assetId)) return;

    const payload: UpdateAssetPayload = {
      code: code.trim(),
      name: name.trim(),
      assetTypeId: assetTypeId ? Number(assetTypeId) : undefined,
      purchaseDate: purchaseDate || undefined,
      originalPrice: originalPrice ? Number(originalPrice) : undefined,
      currentValue: currentValue ? Number(currentValue) : undefined,
      unit: unit || undefined,
      quantity: quantity ? Number(quantity) : undefined,
      warehouseId: warehouseId ? Number(warehouseId) : undefined,
      warrantyEndDate: warrantyEndDate || null,
    };

    setSaving(true);
    setError(null);

    try {
      await assetService.update(assetId, payload);
      navigate('/accountant-assets');
    } catch {
      setError('Cập nhật tài sản thất bại. Vui lòng thử lại.');
    } finally {
      setSaving(false);
    }
  };

  if (loading) {
    return <div className="asset-create-page">Đang tải thông tin tài sản...</div>;
  }

  if (error && !asset) {
    return <div className="asset-create-page">{error}</div>;
  }

  return (
    <div className="asset-create-page">
      <div className="asset-create__header">
        <Link to="/accountant-assets" className="asset-create__back">
          ← Tất cả tài sản
        </Link>
        <div className="asset-create__title-row">
          <h1 className="asset-create__title">Sửa tài sản</h1>
          <span className="asset-create__status">{asset?.statusName ?? 'Đang sử dụng'}</span>
          <div className="asset-create__header-actions">
            <button
              type="button"
              className="asset-create__btn asset-create__btn--secondary"
              onClick={() => navigate(-1)}
            >
              Hủy
            </button>
            <button
              type="submit"
              form="asset-edit-form"
              className="asset-create__btn asset-create__btn--primary"
              disabled={saving}
            >
              {saving ? 'Đang lưu...' : 'Lưu'}
            </button>
          </div>
        </div>
      </div>

      <form id="asset-edit-form" onSubmit={handleSubmit} className="asset-create__card">
        {error && <div className="asset-create__error">{error}</div>}

        <section className="asset-create__section">
          <h2 className="asset-create__section-title">Thông tin chung</h2>
          <div className="asset-create__grid asset-create__grid--two">
            <div className="asset-create__column">
              <div className="asset-create__field">
                <label className="asset-create__label">
                  Mã tài sản<span className="asset-create__required">*</span>
                </label>
                <input
                  className="asset-create__input"
                  value={code}
                  onChange={(e) => setCode(e.target.value)}
                />
              </div>

              <div className="asset-create__field">
                <label className="asset-create__label">Người quản lý</label>
                <input
                  className="asset-create__input"
                  value={manager}
                  onChange={(e) => setManager(e.target.value)}
                />
              </div>

              <div className="asset-create__field">
                <label className="asset-create__label">
                  Loại tài sản<span className="asset-create__required">*</span>
                </label>
                <select
                  className="asset-create__select"
                  value={assetTypeId}
                  onChange={(e) => setAssetTypeId(e.target.value)}
                >
                  <option value="">Chọn loại tài sản</option>
                  <option value="1">Máy móc</option>
                  <option value="2">Thiết bị</option>
                  <option value="3">Khác</option>
                </select>
              </div>

              <div className="asset-create__field">
                <label className="asset-create__label">
                  Tên tài sản<span className="asset-create__required">*</span>
                </label>
                <input
                  className="asset-create__input"
                  value={name}
                  onChange={(e) => setName(e.target.value)}
                />
              </div>

              <div className="asset-create__field asset-create__field--inline">
                <div>
                  <label className="asset-create__label">Số lượng</label>
                  <input
                    type="number"
                    min={1}
                    className="asset-create__input"
                    value={quantity}
                    onChange={(e) => setQuantity(e.target.value)}
                  />
                </div>
                <div>
                  <label className="asset-create__label">Đơn vị tính</label>
                  <select
                    className="asset-create__select"
                    value={unit}
                    onChange={(e) => setUnit(e.target.value)}
                  >
                    <option value="">Chọn đơn vị tính</option>
                    <option value="Bộ">Bộ</option>
                    <option value="Cái">Cái</option>
                    <option value="Chiếc">Chiếc</option>
                    <option value="Máy">Máy</option>
                    <option value="Đôi">Đôi</option>
                    <option value="Bình">Bình</option>
                    <option value="Chai">Chai</option>
                    <option value="Cuốn">Cuốn</option>
                    <option value="Tập">Tập</option>
                    <option value="Mét">Mét</option>
                    <option value="Kiện">Kiện</option>
                    <option value="Thùng">Thùng</option>
                    <option value="Quyển">Quyển</option>
                    <option value="Hộp">Hộp</option>
                    <option value="Gói">Gói</option>
                  </select>
                </div>
              </div>

              <div className="asset-create__field">
                <label className="asset-create__label">Giá trị</label>
                <input
                  type="number"
                  min={0}
                  className="asset-create__input"
                  value={originalPrice}
                  onChange={(e) => setOriginalPrice(e.target.value)}
                />
              </div>

              <div className="asset-create__field">
                <label className="asset-create__label">Nguồn gốc</label>
                <input
                  className="asset-create__input"
                  value={origin}
                  onChange={(e) => setOrigin(e.target.value)}
                />
              </div>

              <label className="asset-create__checkbox-row">
                <input type="checkbox" checked readOnly />
                <span>Là tài sản cố định</span>
              </label>
            </div>

            <div className="asset-create__column">
              <div className="asset-create__field">
                <label className="asset-create__label">
                  Vị trí tài sản<span className="asset-create__required">*</span>
                </label>
                <select
                  className="asset-create__select"
                  value={warehouseId}
                  onChange={(e) => setWarehouseId(e.target.value)}
                >
                  <option value="">Chọn vị trí tài sản</option>
                  <option value="1">Kho Hà Nội</option>
                  <option value="2">Kho Thạch Thất</option>
                </select>
              </div>

              <div className="asset-create__field">
                <label className="asset-create__label">
                  Ngày mua<span className="asset-create__required">*</span>
                </label>
                <input
                  type="date"
                  className="asset-create__input"
                  value={purchaseDate}
                  onChange={(e) => setPurchaseDate(e.target.value)}
                />
              </div>

              <div className="asset-create__field">
                <label className="asset-create__label">Nhà cung cấp</label>
                <input
                  className="asset-create__input"
                  value={supplier}
                  onChange={(e) => setSupplier(e.target.value)}
                />
              </div>

              <div className="asset-create__field">
                <label className="asset-create__label">Số hợp đồng</label>
                <input
                  className="asset-create__input"
                  value={contractNumber}
                  onChange={(e) => setContractNumber(e.target.value)}
                />
              </div>

              <div className="asset-create__field">
                <label className="asset-create__label">Số serial</label>
                <input
                  className="asset-create__input"
                  value={serialNumber}
                  onChange={(e) => setSerialNumber(e.target.value)}
                />
              </div>

              <div className="asset-create__field">
                <label className="asset-create__label">Quy cách tài sản</label>
                <input
                  className="asset-create__input"
                  value={specification}
                  onChange={(e) => setSpecification(e.target.value)}
                />
              </div>

              <div className="asset-create__field">
                <label className="asset-create__label">Ghi chú</label>
                <textarea
                  className="asset-create__textarea"
                  rows={2}
                  value={note}
                  onChange={(e) => setNote(e.target.value)}
                />
              </div>
            </div>
          </div>
        </section>

        <section className="asset-create__section">
          <div className="asset-create__section-header">
            <h2 className="asset-create__section-title">Quy định bảo dưỡng</h2>
            <button
              type="button"
              className="asset-create__btn asset-create__btn--danger"
              onClick={openScheduleModal}
            >
              + Thêm nội dung bảo dưỡng
            </button>
          </div>
          {scheduleError && <div className="asset-create__error">{scheduleError}</div>}
          <table className="asset-create__maintenance-table">
            <thead>
              <tr>
                <th>Nội dung bảo dưỡng</th>
                <th>Thời điểm</th>
                <th>Lặp lại theo</th>
                <th>Bảo dưỡng lại sau</th>
              </tr>
            </thead>
            <tbody>
              {maintenanceSchedules.length > 0 ? (
                maintenanceSchedules.map((schedule) => (
                  <tr key={schedule.scheduleId}>
                    <td>
                      {schedule.content?.trim() ||
                        (schedule.templateId ? `Mẫu #${schedule.templateId}` : '—')}
                    </td>
                    <td>{formatDate(schedule.startDate)}</td>
                    <td>{parseEnumNumber(schedule.scheduleType) === 1 ? 'Một lần' : 'Định kỳ'}</td>
                    <td>
                      {schedule.intervalValue && schedule.intervalUnit
                        ? `${schedule.intervalValue} ${getRepeatUnitLabel(schedule.intervalUnit)}`
                        : '—'}
                    </td>
                  </tr>
                ))
              ) : (
                <tr>
                  <td colSpan={4}>Chưa có quy định bảo dưỡng.</td>
                </tr>
              )}
            </tbody>
          </table>
        </section>

        <section className="asset-create__section">
          <h2 className="asset-create__section-title">Bảo hành</h2>
          <div className="asset-create__grid asset-create__grid--three">
            <div className="asset-create__field">
              <label className="asset-create__label">Thời gian bảo hành</label>
              <div className="asset-create__field--inline">
                <input
                  type="number"
                  min={0}
                  className="asset-create__input"
                  value={warrantyMonths}
                  onChange={(e) => setWarrantyMonths(e.target.value)}
                />
                <span className="asset-create__suffix">Tháng</span>
              </div>
            </div>
            <div className="asset-create__field">
              <label className="asset-create__label">Điều kiện bảo hành</label>
              <input
                className="asset-create__input"
                value={warrantyCondition}
                onChange={(e) => setWarrantyCondition(e.target.value)}
              />
            </div>
            <div className="asset-create__field">
              <label className="asset-create__label">Hạn bảo hành</label>
              <input
                type="date"
                className="asset-create__input"
                value={warrantyEndDate}
                onChange={(e) => setWarrantyEndDate(e.target.value)}
              />
            </div>
          </div>
        </section>

        <section className="asset-create__section">
          <h2 className="asset-create__section-title">Thông tin khấu hao</h2>
          <div className="asset-create__grid asset-create__grid--three">
            <div className="asset-create__field">
              <label className="asset-create__label">Giá trị tính khấu hao</label>
              <input
                type="number"
                min={0}
                className="asset-create__input"
                value={depreciationBaseValue}
                onChange={(e) => setDepreciationBaseValue(e.target.value)}
              />
            </div>
            <div className="asset-create__field">
              <label className="asset-create__label">Thời gian còn lại</label>
              <div className="asset-create__field--inline">
                <input
                  type="number"
                  min={0}
                  className="asset-create__input"
                  value={depreciationRemainingMonths}
                  onChange={(e) => setDepreciationRemainingMonths(e.target.value)}
                />
                <span className="asset-create__suffix">Tháng</span>
              </div>
            </div>
            <div className="asset-create__field">
              <label className="asset-create__label">Giá trị còn lại</label>
              <input
                type="number"
                min={0}
                className="asset-create__input"
                value={depreciationRemainingValue}
                onChange={(e) => setDepreciationRemainingValue(e.target.value)}
              />
            </div>
            <div className="asset-create__field">
              <label className="asset-create__label">Ngày bắt đầu khấu hao</label>
              <input
                type="date"
                className="asset-create__input"
                value={depreciationStartDate}
                onChange={(e) => setDepreciationStartDate(e.target.value)}
              />
            </div>
            <div className="asset-create__field">
              <label className="asset-create__label">Khấu hao lũy kế</label>
              <input
                type="number"
                min={0}
                className="asset-create__input"
                value={depreciationAccumulated}
                onChange={(e) => setDepreciationAccumulated(e.target.value)}
              />
            </div>
            <div className="asset-create__field">
              <label className="asset-create__label">Thời gian khấu hao</label>
              <div className="asset-create__field--inline">
                <input
                  type="number"
                  min={0}
                  className="asset-create__input"
                  value={depreciationMonths}
                  onChange={(e) => setDepreciationMonths(e.target.value)}
                />
                <span className="asset-create__suffix">Tháng</span>
              </div>
            </div>
          </div>
          <div className="asset-create__field asset-create__field--current-value">
            <label className="asset-create__label">Giá trị hiện tại (để lưu dữ liệu)</label>
            <input
              type="number"
              min={0}
              className="asset-create__input"
              value={currentValue}
              onChange={(e) => setCurrentValue(e.target.value)}
            />
          </div>
        </section>

        <section className="asset-create__section">
          <h2 className="asset-create__section-title">Tài liệu</h2>
          <div className="asset-create__files">
            <div className="asset-create__file">
              <span className="asset-create__file-index">#1</span>
              <span className="asset-create__file-name">Tài liệu đính kèm</span>
            </div>
            <div className="asset-create__file">
              <span className="asset-create__file-index">#2</span>
              <span className="asset-create__file-name">Tài liệu đính kèm</span>
            </div>
          </div>
          <div className="asset-create__file-actions">
            <button type="button" className="asset-create__btn asset-create__btn--danger">
              Tải toàn bộ
            </button>
            <button type="button" className="asset-create__btn asset-create__btn--danger">
              Thêm tài liệu
            </button>
          </div>
        </section>
      </form>

      {scheduleModalOpen && asset && (
        <div className="schedule-modal-overlay" role="dialog" aria-modal="true">
          <div className="schedule-modal">
            <button
              type="button"
              className="schedule-modal__close-btn"
              onClick={() => setScheduleModalOpen(false)}
              aria-label="Đóng"
            >
              <span className="schedule-modal__close">×</span>
            </button>

            <div className="schedule-modal__header">
              <h2 className="schedule-modal__title">Quy định bảo dưỡng</h2>
            </div>

            <div className="schedule-modal__body">
              <div className="schedule-modal__content">
                <div className="schedule-form__item">
                  <label>
                    Nội dung bảo dưỡng<span className="asset-create__required">*</span>
                  </label>
                  <textarea
                    className="schedule-input schedule-input--textarea"
                    rows={4}
                    placeholder="-"
                    value={maintenanceContent}
                    onChange={(e) => setMaintenanceContent(e.target.value)}
                  />
                </div>

                <div className="schedule-form__item">
                  <label>Tần suất bảo dưỡng</label>
                  <div className="schedule-radio-row">
                    <label>
                      <input
                        type="radio"
                        checked={scheduleType === 1}
                        onChange={() => setScheduleType(1)}
                      />{' '}
                      Một lần
                    </label>
                    <label>
                      <input
                        type="radio"
                        checked={scheduleType === 2}
                        onChange={() => setScheduleType(2)}
                      />{' '}
                      Định kỳ
                    </label>
                  </div>
                </div>

                <div className="schedule-form__item">
                  <label>Thời điểm bảo dưỡng</label>
                  <div className="schedule-timepoint-row">
                    <select
                      className="schedule-input"
                      value={startPointMode}
                      onChange={(e) =>
                        setStartPointMode(e.target.value === 'purchase' ? 'purchase' : 'inUse')
                      }
                    >
                      <option value="inUse">Sau ngày bắt đầu sử dụng</option>
                      <option value="purchase">Sau ngày mua</option>
                    </select>
                    <input
                      type="number"
                      min={0}
                      className="schedule-input schedule-input--days"
                      value={startOffsetDays}
                      onChange={(e) => setStartOffsetDays(Math.max(0, Number(e.target.value || 0)))}
                    />
                    <span className="schedule-timepoint-unit">Ngày</span>
                  </div>
                </div>

                <div className="schedule-form__item">
                  <label>Bảo dưỡng lặp lại theo</label>
                  <select
                    className="schedule-input"
                    value={repeatIntervalUnit}
                    onChange={(e) => setRepeatIntervalUnit(Number(e.target.value) as 1 | 2 | 3 | 4)}
                    disabled={scheduleType === 1}
                  >
                    <option value={1}>Ngày</option>
                    <option value={2}>Tuần</option>
                    <option value={3}>Tháng</option>
                    <option value={4}>Năm</option>
                  </select>
                </div>

                {scheduleError && <div className="mark-damaged-error-text">{scheduleError}</div>}
              </div>
            </div>

            <div className="schedule-modal__footer">
              <button
                type="button"
                className="schedule-btn-submit"
                onClick={handleCreateSchedule}
                disabled={scheduleSubmitting}
              >
                {scheduleSubmitting ? 'Đang lưu...' : 'Xác nhận'}
              </button>
              <button
                type="button"
                className="schedule-btn-close"
                onClick={() => setScheduleModalOpen(false)}
                disabled={scheduleSubmitting}
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


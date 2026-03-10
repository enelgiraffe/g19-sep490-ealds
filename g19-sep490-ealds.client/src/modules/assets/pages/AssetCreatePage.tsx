import { FormEvent, useState } from 'react';
import { Link, useNavigate } from 'react-router-dom';
import { assetService, type CreateAssetPayload } from '../services/assetService';
import './AssetCreatePage.css';

interface GeneralInfoForm {
  code: string;
  name: string;
  assetTypeId: string;
  location: string;
  manager: string;
  purchaseDate: string;
  supplier: string;
  contractNumber: string;
  serialNumber: string;
  specification: string;
  quantity: string;
  unit: string;
  value: string;
  origin: string;
  note: string;
  warehouseId: string;
  isFixedAsset: boolean;
}

interface WarrantyForm {
  durationMonths: string;
  conditions: string;
  expiryDate: string;
}

interface DepreciationForm {
  baseValue: string;
  startDate: string;
  lifeTimeMonths: string;
  remainingMonths: string;
  accumulatedDepreciation: string;
  remainingValue: string;
  depreciationPolicyId: string;
}

interface AllocationForm {
  allocateNow: boolean;
  usageTarget: string;
}

interface ExtraInfoForm {
  customFieldKey: string;
  contactInfo: string;
}

export function AssetCreatePage() {
  const navigate = useNavigate();

  const [general, setGeneral] = useState<GeneralInfoForm>({
    code: '',
    name: '',
    assetTypeId: '',
    location: '',
    manager: '',
    purchaseDate: '',
    supplier: '',
    contractNumber: '',
    serialNumber: '',
    specification: '',
    quantity: '1',
    unit: '',
    value: '',
    origin: '',
    note: '',
    warehouseId: '',
    isFixedAsset: true,
  });

  const [warranty, setWarranty] = useState<WarrantyForm>({
    durationMonths: '',
    conditions: '',
    expiryDate: '',
  });

  const [depreciation, setDepreciation] = useState<DepreciationForm>({
    baseValue: '',
    startDate: '',
    lifeTimeMonths: '',
    remainingMonths: '',
    accumulatedDepreciation: '',
    remainingValue: '',
    depreciationPolicyId: '',
  });

  const [allocation, setAllocation] = useState<AllocationForm>({
    allocateNow: false,
    usageTarget: '',
  });

  const [extra, setExtra] = useState<ExtraInfoForm>({
    customFieldKey: '',
    contactInfo: '',
  });

  const [isSubmitting, setIsSubmitting] = useState(false);
  const [submitError, setSubmitError] = useState<string | null>(null);

  const handleSubmit = async (e: FormEvent) => {
    e.preventDefault();
    if (!general.code || !general.name || !general.assetTypeId || !general.purchaseDate) {
      alert('Vui lòng nhập đầy đủ các trường bắt buộc được đánh dấu *.');
      return;
    }
    if (!general.warehouseId || Number(general.warehouseId) <= 0) {
      alert('Vui lòng chọn kho.');
      return;
    }

    setSubmitError(null);
    setIsSubmitting(true);

    const payload: CreateAssetPayload = {
      code: general.code.trim(),
      name: general.name.trim(),
      assetTypeId: Number(general.assetTypeId),
      purchaseDate: general.purchaseDate,
      originalPrice: Number(general.value || 0),
      currentValue: Number(general.value || 0),
      warrantyEndDate: warranty.expiryDate || null,
      inUseDate: general.purchaseDate || null,
      unit: general.unit || 'Cái',
      quantity: Number(general.quantity || 1),
      warehouseId: Number(general.warehouseId),
      createdBy: 0,
      depreciationPolicyId: depreciation.depreciationPolicyId
        ? Number(depreciation.depreciationPolicyId)
        : null,
    };

    try {
      await assetService.create(payload);
      navigate('/assets');
    } catch (err: unknown) {
      const msg =
        err && typeof err === 'object' && 'response' in err
          ? (err as { response?: { data?: { message?: string } } }).response?.data?.message
          : null;
      setSubmitError(msg || 'Tạo tài sản thất bại. Kiểm tra kết nối backend hoặc dữ liệu.');
    } finally {
      setIsSubmitting(false);
    }
  };

  return (
    <div className="asset-create-page">
      <div className="asset-create__header">
        <Link to="/assets" className="asset-create__back">
          ← Tất cả tài sản
        </Link>
        <div className="asset-create__title-row">
          <h1 className="asset-create__title">Thêm tài sản</h1>
          <span className="asset-create__status">Đang sử dụng</span>
          <div className="asset-create__header-actions">
            <button
              type="button"
              className="asset-create__btn asset-create__btn--secondary"
              onClick={() => navigate('/assets')}
            >
              Hủy
            </button>
            <button
              type="submit"
              form="asset-create-form"
              className="asset-create__btn asset-create__btn--primary"
              disabled={isSubmitting}
            >
              {isSubmitting ? 'Đang lưu...' : 'Lưu'}
            </button>
          </div>
        </div>
      </div>

      <form id="asset-create-form" onSubmit={handleSubmit} className="asset-create__card">
        {submitError && (
          <div className="asset-create__error" role="alert">
            {submitError}
          </div>
        )}
        {/* Thông tin chung */}
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
                  value={general.code}
                  onChange={(e) => setGeneral({ ...general, code: e.target.value })}
                />
              </div>

              <div className="asset-create__field">
                <label className="asset-create__label">Người quản lý</label>
                <input
                  className="asset-create__input"
                  value={general.manager}
                  onChange={(e) => setGeneral({ ...general, manager: e.target.value })}
                />
              </div>

              <div className="asset-create__field">
                <label className="asset-create__label">
                  Loại tài sản<span className="asset-create__required">*</span>
                </label>
                <select
                  className="asset-create__select"
                  value={general.assetTypeId}
                  onChange={(e) => setGeneral({ ...general, assetTypeId: e.target.value })}
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
                  value={general.name}
                  onChange={(e) => setGeneral({ ...general, name: e.target.value })}
                />
              </div>

              <div className="asset-create__field asset-create__field--inline">
                <div>
                  <label className="asset-create__label">Số lượng</label>
                  <input
                    type="number"
                    min={1}
                    className="asset-create__input"
                    value={general.quantity}
                    onChange={(e) => setGeneral({ ...general, quantity: e.target.value })}
                  />
                </div>
                <div>
                  <label className="asset-create__label">Đơn vị tính</label>
                  <input
                    className="asset-create__input"
                    value={general.unit}
                    onChange={(e) => setGeneral({ ...general, unit: e.target.value })}
                  />
                </div>
              </div>

              <div className="asset-create__field">
                <label className="asset-create__label">Giá trị</label>
                <input
                  type="number"
                  min={0}
                  className="asset-create__input"
                  value={general.value}
                  onChange={(e) => setGeneral({ ...general, value: e.target.value })}
                />
              </div>

              <div className="asset-create__field">
                <label className="asset-create__label">Nguồn gốc</label>
                <input
                  className="asset-create__input"
                  value={general.origin}
                  onChange={(e) => setGeneral({ ...general, origin: e.target.value })}
                />
              </div>

              <label className="asset-create__checkbox-row">
                <input
                  type="checkbox"
                  checked={general.isFixedAsset}
                  onChange={(e) => setGeneral({ ...general, isFixedAsset: e.target.checked })}
                />
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
                  value={general.warehouseId}
                  onChange={(e) =>
                    setGeneral({ ...general, warehouseId: e.target.value, location: e.target.value })
                  }
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
                  value={general.purchaseDate}
                  onChange={(e) => setGeneral({ ...general, purchaseDate: e.target.value })}
                />
              </div>

              <div className="asset-create__field">
                <label className="asset-create__label">Nhà cung cấp</label>
                <input
                  className="asset-create__input"
                  value={general.supplier}
                  onChange={(e) => setGeneral({ ...general, supplier: e.target.value })}
                />
              </div>

              <div className="asset-create__field">
                <label className="asset-create__label">Số hợp đồng</label>
                <input
                  className="asset-create__input"
                  value={general.contractNumber}
                  onChange={(e) => setGeneral({ ...general, contractNumber: e.target.value })}
                />
              </div>

              <div className="asset-create__field">
                <label className="asset-create__label">Số serial</label>
                <input
                  className="asset-create__input"
                  value={general.serialNumber}
                  onChange={(e) => setGeneral({ ...general, serialNumber: e.target.value })}
                />
              </div>

              <div className="asset-create__field">
                <label className="asset-create__label">Quy cách tài sản</label>
                <input
                  className="asset-create__input"
                  value={general.specification}
                  onChange={(e) => setGeneral({ ...general, specification: e.target.value })}
                />
              </div>

              <div className="asset-create__field">
                <label className="asset-create__label">Ghi chú</label>
                <textarea
                  className="asset-create__textarea"
                  rows={2}
                  value={general.note}
                  onChange={(e) => setGeneral({ ...general, note: e.target.value })}
                />
              </div>
            </div>
          </div>
        </section>

        {/* Bảo hành */}
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
                  value={warranty.durationMonths}
                  onChange={(e) =>
                    setWarranty({ ...warranty, durationMonths: e.target.value })
                  }
                />
                <span className="asset-create__suffix">Tháng</span>
              </div>
            </div>
            <div className="asset-create__field">
              <label className="asset-create__label">Điều kiện bảo hành</label>
              <input
                className="asset-create__input"
                value={warranty.conditions}
                onChange={(e) => setWarranty({ ...warranty, conditions: e.target.value })}
              />
            </div>
            <div className="asset-create__field">
              <label className="asset-create__label">Hạn bảo hành</label>
              <input
                type="date"
                className="asset-create__input"
                value={warranty.expiryDate}
                onChange={(e) => setWarranty({ ...warranty, expiryDate: e.target.value })}
              />
            </div>
          </div>
        </section>

        {/* Thông tin khấu hao */}
        <section className="asset-create__section">
          <h2 className="asset-create__section-title">Thông tin khấu hao</h2>
          <div className="asset-create__grid asset-create__grid--three">
            <div className="asset-create__field">
              <label className="asset-create__label">Giá trị tính khấu hao</label>
              <input
                type="number"
                min={0}
                className="asset-create__input"
                value={depreciation.baseValue}
                onChange={(e) => setDepreciation({ ...depreciation, baseValue: e.target.value })}
              />
            </div>

            <div className="asset-create__field">
              <label className="asset-create__label">Thời gian còn lại</label>
              <div className="asset-create__field--inline">
                <input
                  type="number"
                  min={0}
                  className="asset-create__input"
                  value={depreciation.remainingMonths}
                  onChange={(e) =>
                    setDepreciation({ ...depreciation, remainingMonths: e.target.value })
                  }
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
                value={depreciation.remainingValue}
                onChange={(e) =>
                  setDepreciation({ ...depreciation, remainingValue: e.target.value })
                }
              />
            </div>

            <div className="asset-create__field">
              <label className="asset-create__label">Ngày bắt đầu khấu hao</label>
              <input
                type="date"
                className="asset-create__input"
                value={depreciation.startDate}
                onChange={(e) =>
                  setDepreciation({ ...depreciation, startDate: e.target.value })
                }
              />
            </div>

            <div className="asset-create__field">
              <label className="asset-create__label">Khấu hao lũy kế</label>
              <input
                type="number"
                min={0}
                className="asset-create__input"
                value={depreciation.accumulatedDepreciation}
                onChange={(e) =>
                  setDepreciation({
                    ...depreciation,
                    accumulatedDepreciation: e.target.value,
                  })
                }
              />
            </div>

            <div className="asset-create__field">
              <label className="asset-create__label">Thời gian khấu hao</label>
              <div className="asset-create__field--inline">
                <input
                  type="number"
                  min={0}
                  className="asset-create__input"
                  value={depreciation.lifeTimeMonths}
                  onChange={(e) =>
                    setDepreciation({ ...depreciation, lifeTimeMonths: e.target.value })
                  }
                />
                <span className="asset-create__suffix">Tháng</span>
              </div>
            </div>
          </div>
        </section>

        {/* Đã cấp phát */}
        <section className="asset-create__section">
          <h2 className="asset-create__section-title">Đã cấp phát</h2>
          <div className="asset-create__grid asset-create__grid--two">
            <label className="asset-create__checkbox-row">
              <input
                type="checkbox"
                checked={allocation.allocateNow}
                onChange={(e) =>
                  setAllocation({ ...allocation, allocateNow: e.target.checked })
                }
              />
              <span>Có cấp phát ngay</span>
            </label>

            <div className="asset-create__field">
              <label className="asset-create__label">Đối tượng sử dụng</label>
              <input
                className="asset-create__input"
                value={allocation.usageTarget}
                onChange={(e) =>
                  setAllocation({ ...allocation, usageTarget: e.target.value })
                }
              />
            </div>
          </div>
        </section>

        {/* Tài liệu */}
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
            <button
              type="button"
              className="asset-create__btn asset-create__btn--danger"
            >
              Tải toàn bộ
            </button>
            <button
              type="button"
              className="asset-create__btn asset-create__btn--danger"
            >
              Thêm tài liệu
            </button>
          </div>
        </section>

      </form>
    </div>
  );
}


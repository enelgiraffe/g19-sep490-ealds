import { FormEvent, useEffect, useState } from 'react';
import { Link, useNavigate, useParams } from 'react-router-dom';
import { assetService, type AssetResponse, type UpdateAssetPayload } from '../services/assetService';
import './AssetCreatePage.css';

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
      .then((data) => {
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
                  <input
                    className="asset-create__input"
                    value={unit}
                    onChange={(e) => setUnit(e.target.value)}
                  />
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
            <button type="button" className="asset-create__btn asset-create__btn--danger">
              + Thêm nội dung bảo dưỡng
            </button>
          </div>
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
              <tr>
                <td>Thay dầu</td>
                <td>12/08/2024</td>
                <td>Tháng</td>
                <td>-</td>
              </tr>
              <tr>
                <td>Kiểm tra an toàn</td>
                <td>12/08/2024</td>
                <td>Quý</td>
                <td>-</td>
              </tr>
              <tr>
                <td>Đo sai số</td>
                <td>12/08/2024</td>
                <td>Năm</td>
                <td>-</td>
              </tr>
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
    </div>
  );
}


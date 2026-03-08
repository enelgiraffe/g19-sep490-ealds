import { useEffect, useState } from 'react';
import { useParams, Link } from 'react-router-dom';
import {
  assetService,
  formatVnd,
  getStatusLabel,
  type AssetResponse,
} from '../services/assetService';
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

  useEffect(() => {
    if (!id || Number.isNaN(id)) {
      setError('ID tài sản không hợp lệ.');
      setLoading(false);
      return;
    }
    let cancelled = false;
    setLoading(true);
    setError(null);
    assetService
      .getById(id)
      .then((data) => {
        if (!cancelled) setAsset(data);
      })
      .catch(() => {
        if (!cancelled) setError('Không tải được thông tin tài sản.');
      })
      .finally(() => {
        if (!cancelled) setLoading(false);
      });
    return () => {
      cancelled = true;
    };
  }, [id]);

  if (loading) {
    return (
      <div className="asset-detail-page">
        <div className="asset-detail__header">
          <Link to="/assets" className="asset-detail__back">
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
          <Link to="/assets" className="asset-detail__back">
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

  return (
    <div className="asset-detail-page">
      <div className="asset-detail__header">
        <Link to="/assets" className="asset-detail__back">
          ← Tất cả tài sản
        </Link>
        <div className="asset-detail__title-row">
          <h1 className="asset-detail__title">{asset.name}</h1>
          <span className="asset-detail__status">{statusLabel}</span>
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
                <span className="label">Vị trí tài sản</span>
                <span className="value">{asset.warehouseName ?? '—'}</span>
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
          <h2 className="asset-detail__section-title">Lịch sử sửa chữa, bảo dưỡng</h2>
          <div className="asset-detail__table-wrapper">
            <table className="asset-detail__table">
              <thead>
                <tr>
                  <th>NGÀY BẮT ĐẦU</th>
                  <th>NGHIỆP VỤ</th>
                  <th>NGÀY HOÀN THÀNH</th>
                  <th>NỘI DUNG</th>
                  <th>CHI PHÍ THỰC TẾ</th>
                </tr>
              </thead>
              <tbody>
                <tr>
                  <td colSpan={5} className="asset-detail__empty">
                    Chưa có dữ liệu (cần API sửa chữa/bảo dưỡng).
                  </td>
                </tr>
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

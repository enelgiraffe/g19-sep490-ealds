import { useMemo } from 'react';
import { useParams, Link } from 'react-router-dom';
import './AssetDetailPage.css';

interface AssetItem {
  id: number;
  code: string;
  name: string;
  type: string;
  manager: string;
  location: string;
  price: string;
  status: string;
}

const MOCK_ASSETS: AssetItem[] = [
  {
    id: 1,
    code: 'MCS',
    name: 'Máy cắt sắt',
    type: 'Cơ khí',
    manager: 'Nguyễn Văn A',
    location: 'Kho Hà Nội',
    price: '910,000,000 đ',
    status: 'Đang sử dụng',
  },
  {
    id: 2,
    code: 'MUV',
    name: 'Máy uốn vòm',
    type: 'Cơ khí',
    manager: 'Bùi Huy Hoàng',
    location: 'Kho Thạch Thất',
    price: '500,000,000 đ',
    status: 'Đang sử dụng',
  },
  {
    id: 3,
    code: 'FSF90',
    name: 'Ôtô Ferrari SF90',
    type: 'Máy móc',
    manager: 'Trần Thị B',
    location: 'Garage nội bộ',
    price: '34,000,500,000 đ',
    status: 'Đang sử dụng',
  },
  {
    id: 4,
    code: 'MEG',
    name: 'Máy ép góc',
    type: 'Cơ khí',
    manager: 'Bùi Huy Hoàng',
    location: 'Kho Thạch Thất',
    price: '500,000,000 đ',
    status: 'Chưa sử dụng',
  },
];

export function AssetDetailPage() {
  const params = useParams<{ id: string }>();

  const asset = useMemo(
    () => MOCK_ASSETS.find((a) => String(a.id) === params.id),
    [params.id],
  );

  if (!asset) {
    return (
      <div className="asset-detail-page">
        <div className="asset-detail__header">
          <Link to="/assets" className="asset-detail__back">
            ← Quay lại danh sách tài sản
          </Link>
        </div>
        <div className="asset-detail__card">
          <p>Không tìm thấy tài sản.</p>
        </div>
      </div>
    );
  }

  return (
    <div className="asset-detail-page">
      <div className="asset-detail__header">
        <Link to="/assets" className="asset-detail__back">
          ← Tất cả tài sản
        </Link>
        <div className="asset-detail__title-row">
          <h1 className="asset-detail__title">{asset.name}</h1>
          <span className="asset-detail__status">Đang sử dụng</span>
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
                <span className="label">Người quản lý</span>
                <span className="value">{asset.manager}</span>
              </div>
              <div className="asset-detail__info-row">
                <span className="label">Loại tài sản</span>
                <span className="value">{asset.type}</span>
              </div>
              <div className="asset-detail__info-row">
                <span className="label">Tên tài sản</span>
                <span className="value">{asset.name}</span>
              </div>
              <div className="asset-detail__info-row">
                <span className="label">Số lượng</span>
                <span className="value">2</span>
              </div>
              <div className="asset-detail__info-row">
                <span className="label">Đơn vị tính</span>
                <span className="value">Cái</span>
              </div>
            </div>
            <div className="asset-detail__info-col">
              <div className="asset-detail__info-row">
                <span className="label">Vị trí tài sản</span>
                <span className="value">{asset.location}</span>
              </div>
              <div className="asset-detail__info-row">
                <span className="label">Ngày mua</span>
                <span className="value">26/1/2026</span>
              </div>
              <div className="asset-detail__info-row">
                <span className="label">Nhà cung cấp</span>
                <span className="value">Nikon</span>
              </div>
              <div className="asset-detail__info-row">
                <span className="label">Số hợp đồng</span>
                <span className="value">HD-101</span>
              </div>
              <div className="asset-detail__info-row">
                <span className="label">Số serial</span>
                <span className="value">87B52-FGHJ-K</span>
              </div>
              <div className="asset-detail__info-row">
                <span className="label">Quy cách tài sản</span>
                <span className="value">Giá trị</span>
              </div>
            </div>
          </div>
          <div className="asset-detail__info-row">
            <span className="label">Giá trị</span>
            <span className="value">{asset.price}</span>
          </div>
          <div className="asset-detail__info-row">
            <span className="label">Nguồn gốc</span>
            <span className="value">Nhật Bản</span>
          </div>
          <label className="asset-detail__checkbox">
            <input type="checkbox" checked readOnly />
            <span>Là tài sản cố định</span>
          </label>
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
                  <th>NGƯỜI QUẢN LÝ</th>
                  <th>PHÒNG BAN BỘ PHẬN DÙNG</th>
                  <th>BIÊN BẢN CẤP PHÁT/THU HỒI</th>
                </tr>
              </thead>
              <tbody>
                <tr>
                  <td>12/8/2024</td>
                  <td>BB01</td>
                  <td>Ghi tăng</td>
                  <td>Đã sử dụng</td>
                  <td>Kho Thạch Thất</td>
                  <td>450,000,000đ</td>
                  <td>Bùi Huy Hoàng</td>
                  <td>Phòng sản xuất</td>
                  <td>BB-11</td>
                </tr>
                <tr>
                  <td>12/8/2024</td>
                  <td>BB01</td>
                  <td>Đang sửa chữa</td>
                  <td>Đã sử dụng</td>
                  <td>Kho Thạch Thất</td>
                  <td>450,000,000đ</td>
                  <td>Bùi Huy Hoàng</td>
                  <td>Phòng sản xuất</td>
                  <td>_</td>
                </tr>
                <tr>
                  <td>12/8/2024</td>
                  <td>BB01</td>
                  <td>Hoàn thành sửa chữa</td>
                  <td>Đã sử dụng</td>
                  <td>Kho Thạch Thất</td>
                  <td>450,000,000đ</td>
                  <td>Bùi Huy Hoàng</td>
                  <td>Phòng sản xuất</td>
                  <td>_</td>
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
                  <th>NỘI DUNG SỬA CHỮA/BẢO TRÌ</th>
                  <th>MÔ TẢ CHI TIẾT</th>
                  <th>CHI PHÍ THỰC TẾ</th>
                  <th>ĐƠN VỊ SỬA CHỮA/BẢO DƯỠNG</th>
                  <th>BẢO HÀNH</th>
                </tr>
              </thead>
              <tbody>
                <tr>
                  <td>12/8/2024</td>
                  <td>Bảo dưỡng định kỳ</td>
                  <td>16/09/2020</td>
                  <td>Vệ sinh, kiểm tra hoạt độ...</td>
                  <td>_</td>
                  <td>4,500,000đ</td>
                  <td>Công ty ABC</td>
                  <td>3 Tháng</td>
                </tr>
                <tr>
                  <td>12/8/2024</td>
                  <td>Sửa chữa nhỏ</td>
                  <td>12/03/2026</td>
                  <td>Thay quạt tản nhiệt</td>
                  <td>_</td>
                  <td>1,200,000đ</td>
                  <td>Công ty ABC</td>
                  <td>6 Tháng</td>
                </tr>
              </tbody>
            </table>
          </div>
        </div>

        <div className="asset-detail__section">
          <h2 className="asset-detail__section-title">Bảo hành</h2>
          <div className="asset-detail__info-row">
            <span className="label">Thời gian bảo hành</span>
            <span className="value">08/09/2024</span>
          </div>
          <div className="asset-detail__info-row">
            <span className="label">Điều kiện bảo hành</span>
            <span className="value">08/09/2024</span>
          </div>
          <div className="asset-detail__info-row">
            <span className="label">Hạn bảo hành</span>
            <span className="value">08/09/2024</span>
          </div>
        </div>

        <div className="asset-detail__section">
          <h2 className="asset-detail__section-title">Thông tin khấu hao</h2>
          <div className="asset-detail__info-row">
            <span className="label">Giá trị bắt đầu khấu hao</span>
            <span className="value">08/09/2024</span>
          </div>
          <div className="asset-detail__info-row">
            <span className="label">Ngày bắt đầu khấu hao</span>
            <span className="value">08/09/2024</span>
          </div>
          <div className="asset-detail__info-row">
            <span className="label">Tuổi gọi hết khấu hao</span>
            <span className="value">08/09/2024</span>
          </div>
        </div>

        <div className="asset-detail__section">
          <h2 className="asset-detail__section-title">Tài liệu</h2>
          <div className="asset-detail__files">
            <div className="asset-detail__file">
              <span className="asset-detail__file-icon">📄</span>
              <span className="asset-detail__file-name">Tài liệu đính kèm</span>
              <button className="asset-detail__file-download">⬇</button>
            </div>
            <div className="asset-detail__file">
              <span className="asset-detail__file-icon">📄</span>
              <span className="asset-detail__file-name">Tài liệu đính kèm</span>
              <button className="asset-detail__file-download">⬇</button>
            </div>
          </div>
          <div className="asset-detail__actions">
            <button className="asset-detail__btn asset-detail__btn--danger">
              Tải toàn bộ
            </button>
            <button className="asset-detail__btn asset-detail__btn--danger">
              Thêm tài liệu
            </button>
          </div>
        </div>

        <div className="asset-detail__section">
          <h2 className="asset-detail__section-title">Thông tin khác</h2>
          <div className="asset-detail__info-row">
            <span className="label">Chọn trường thông tin</span>
            <select className="asset-detail__select">
              <option>Chọn trường thông tin tự tạo sẵn mục</option>
            </select>
          </div>
          <div className="asset-detail__info-row">
            <span className="label">Thông tin liên hệ</span>
            <input
              type="text"
              className="asset-detail__input"
              placeholder="Nhập thông tin liên hệ"
            />
          </div>
        </div>
      </div>
    </div>
  );
}

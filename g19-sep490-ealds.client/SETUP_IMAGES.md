# Setup Images - Hướng dẫn thêm ảnh

## ⚠️ Cần thêm ảnh background

Hiện tại, dự án đang thiếu ảnh background cho màn hình đăng nhập.

### Bước 1: Chuẩn bị ảnh

**Ảnh background cần:**
- Tên file: `background-login.jpg`
- Kích thước đề xuất: **1920x1080 px** hoặc cao hơn
- Nội dung: Ảnh kiến trúc thành phố, tòa nhà hiện đại (giống ảnh bạn đã cung cấp)
- Định dạng: JPG hoặc PNG

### Bước 2: Đặt ảnh vào đúng thư mục

```
ealds/
  public/
    images/
      ✅ logo.svg (đã có)
      ❌ background-login.jpg (cần thêm) ← ĐẶT ẢNH VÀO ĐÂY
```

### Bước 3: Đặt tên file chính xác

**Quan trọng:** Ảnh phải có tên chính xác là: `background-login.jpg`

### Cách thêm ảnh:

#### Cách 1: Sử dụng ảnh từ Unsplash
```bash
# Tải ảnh từ link này (ảnh giống thiết kế):
https://images.unsplash.com/photo-1514565131-fce0801e5785?w=1920&q=80

# Lưu thành: ealds/public/images/background-login.jpg
```

#### Cách 2: Sử dụng ảnh của công ty
1. Lấy ảnh từ thư mục branding/marketing của công ty
2. Resize về kích thước 1920x1080 nếu cần
3. Đổi tên thành `background-login.jpg`
4. Copy vào `ealds/public/images/`

#### Cách 3: Download từ stock photos
- [Unsplash](https://unsplash.com/s/photos/city-architecture)
- [Pexels](https://www.pexels.com/search/city%20building/)
- Tìm kiếm: "modern city architecture", "business building walkway"

### Bước 4: Kiểm tra

Sau khi đặt ảnh xong:

```bash
cd ealds
npm run dev
```

Truy cập `http://localhost:5173/login` để xem kết quả.

## 📁 Cấu trúc hiện tại

```
public/
  images/
    ✅ logo.svg          - Logo Sakura Hà Minh (đã có)
    ❌ background-login.jpg - Ảnh nền đăng nhập (CẦN THÊM)
    ✅ README.md         - Hướng dẫn
```

## 🎨 Thiết kế

Login form sẽ:
- Hiển thị ảnh background với blur effect
- Form đăng nhập position absolute ở giữa màn hình
- Logo SVG màu đỏ gradient
- Responsive trên mobile

## ✅ Hoàn thành

- [x] Tạo folder `public/images/`
- [x] Tạo logo.svg
- [x] Setup code để sử dụng ảnh từ folder
- [ ] **BẠN CẦN: Thêm file background-login.jpg**

---

**Lưu ý:** Nếu bạn muốn dùng ảnh khác hoặc đổi tên file, hãy cập nhật import trong file:
- `src/modules/auth/pages/LoginPage.tsx` (dòng 2)

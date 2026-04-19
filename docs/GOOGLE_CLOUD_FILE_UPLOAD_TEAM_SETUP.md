# Hướng dẫn: Upload file với Google Cloud Storage (cho thành viên nhóm)

Tài liệu này giúp **mỗi developer** chạy được chức năng upload file trên backend (lưu lên **Google Cloud Storage**) trong dự án EALDS.

## Cách hoạt động (ngắn gọn)

- Client (React) gửi file tới **`POST /api/files/upload`** (đã đăng nhập, có JWT).
- **ASP.NET Core** nhận file và đẩy lên bucket GCS (hoặc lưu cục bộ nếu chưa cấu hình bucket).
- API trả về **`url`** công khai (thường dạng `https://storage.googleapis.com/...`) để lưu vào DB hoặc hiển thị link tải.

Bạn **không** cần cấu hình CORS trên bucket cho luồng này, vì trình duyệt chỉ gọi API của team, không upload thẳng lên GCS.

---

## Phần A — Team lead / người đã tạo GCP (đã xong thì bỏ qua)

Đảm bảo trên Google Cloud đã có:

1. **Project** và **Cloud Storage API** bật.
2. **Bucket** (ví dụ `document-bucket-ealds`) — ghi nhớ **tên bucket**.
3. **Service account** có quyền ghi object vào bucket (ví dụ vai trò *Storage Object Admin* trên bucket đó, hoặc tương đương).
4. **Key JSON** (tải file `.json` một lần) — **chỉ chia sẻ trong kênh nội bộ an toàn**, không đưa lên Git.

Nếu link tải file cần mở được trên trình duyệt, bucket hoặc object cần được cấu hình **đọc công khai** (hoặc dùng URL ký — hiện dự án đang hướng tới URL công khai kiểu `storage.googleapis.com`).

---

## Phần B — Mỗi thành viên cần gì?

| Thứ cần | Ghi chú |
|--------|---------|
| File JSON service account | Do lead cấp, hoặc tài khoản GCP được gán quyền tạo key (hiếm khi trong lớp) |
| Biết **tên bucket** | Trùng với `GoogleCloudStorage:BucketName` |
| JWT / đăng nhập app | Upload API có `[Authorize]` |

---

## Phần C — Cấu hình trên máy dev (Windows)

### Cách 1: Biến môi trường `GOOGLE_APPLICATION_CREDENTIALS` (khuyến nghị)

1. Lưu file JSON vào một thư mục **không** nằm trong repo (ví dụ `D:\Secrets\ealds-gcs.json`).
2. Đặt biến môi trường **User** hoặc **session**:
   - **Tên biến:** `GOOGLE_APPLICATION_CREDENTIALS`
   - **Giá trị:** đường dẫn đầy đủ tới file JSON, ví dụ `D:\Secrets\ealds-gcs.json`
3. **Đóng hết** Visual Studio / terminal cũ, mở lại, rồi chạy API (`dotnet run` hoặc F5).

Kiểm tra nhanh trong PowerShell (session hiện tại):

```powershell
$env:GOOGLE_APPLICATION_CREDENTIALS = "D:\Secrets\ealds-gcs.json"
Test-Path $env:GOOGLE_APPLICATION_CREDENTIALS   # phải ra True
```

Trong `appsettings.json` / `appsettings.Development.json` có thể để **`CredentialsPath` rỗng** — runtime sẽ đọc biến môi trường trên.

### Cách 2: Đường dẫn tương đối trong cấu hình

1. Copy file JSON vào thư mục **server** (cùng cấp với `appsettings.json`), ví dụ `g19-sep490-ealds.Server\gcs-key.json`.
2. Thêm vào User Secrets hoặc `appsettings.Development.json` (file dev **không commit** nếu chứa tên file nhạy cảm; tốt nhất dùng **User Secrets**):

```json
"GoogleCloudStorage": {
  "BucketName": "document-bucket-ealds",
  "ObjectPrefix": "uploads",
  "CredentialsPath": "gcs-key.json",
  "PublicUrlBase": ""
}
```

Đường dẫn tương đối được giải thích theo **thư mục gốc ứng dụng** (Content Root của project server).

### Cách 3: User Secrets (dotnet) — tránh lộ key trong Git

Trong thư mục `g19-sep490-ealds.Server`:

```bash
dotnet user-secrets init
dotnet user-secrets set "GoogleCloudStorage:BucketName" "document-bucket-ealds"
dotnet user-secrets set "GoogleCloudStorage:CredentialsPath" "D:\\Secrets\\ealds-gcs.json"
```

(Có thể dùng đường dẫn tuyệt đối tới JSON.)

---

## Phần D — `appsettings` tối thiểu

Ví dụ (bucket thật, credentials qua env hoặc secrets):

```json
"GoogleCloudStorage": {
  "BucketName": "document-bucket-ealds",
  "ObjectPrefix": "uploads",
  "CredentialsPath": "",
  "PublicUrlBase": ""
}
```

- **`BucketName` rỗng:** API dùng lưu file cục bộ `wwwroot/uploads` (không cần GCS) — tiện cho ai chưa có quyền GCP.
- **`PublicUrlBase`:** chỉ cần khi dùng CDN / domain tùy chỉnh; để trống thì dùng URL mặc định GCS.

---

## Phần E — Chức năng trong app dùng upload

- **Đơn mua / PO:** upload qua service gọi `/api/files/upload`.
- **Tài sản — tạo / sửa:** tài liệu đính kèm upload lên cùng endpoint, sau đó lưu URL vào bảng `Document`.

Mọi request upload đều cần **token đăng nhập** (cùng cơ chế các API khác).

---

## Phần F — Lỗi thường gặp

| Triệu chứng | Hướng xử lý |
|-------------|-------------|
| `Your default credentials were not found` / ADC | Chưa có JSON hoặc chưa set `GOOGLE_APPLICATION_CREDENTIALS` / `CredentialsPath` đúng. Xem Phần C. |
| `credentials file not found` | Sai đường dẫn file; kiểm tra file tồn tại, dùng đường dẫn tuyệt đối thử lại. |
| 403 khi upload lên GCS | Service account thiếu quyền trên bucket; nhờ lead chỉnh IAM. |
| Upload OK nhưng mở link 403 / AccessDenied | Object/bucket chưa public đọc (hoặc URL sai); chỉnh quyền đọc object hoặc dùng signed URL (sẽ cần đổi code sau này). |
| API vẫn lưu file local | `BucketName` đang rỗng → đang dùng `LocalFileStorageService`. |

---

## Phần G — Bảo mật (bắt buộc đọc)

- **Không** commit file JSON service account vào Git.
- **Không** chụp màn hình chứa toàn bộ private key.
- Key bị lộ → **xóa key cũ** trên GCP và **tạo key mới**, phát lại cho team qua kênh an toàn.
- `.gitignore` của repo đã có thể ignore một số pattern key; vẫn nên để JSON **ngoài** thư mục repo.

---

## Phần H — Production (tham khảo)

- Ưu tiên **Workload Identity** / service account gắn với Cloud Run, GKE, VM — **không** nhúng file JSON trên server.
- Cấu hình bucket + IAM + (tuỳ chọn) CDN giống môi trường dev nhưng dùng secret manager / biến môi trường của nền tảng host.

---

*Nếu chỉnh sửa flow upload hoặc tên section config, cập nhật lại tài liệu này cho đồng bộ.*

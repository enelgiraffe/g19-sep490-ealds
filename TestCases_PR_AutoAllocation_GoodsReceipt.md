# Test case luong duyet PR va cap phat

Muc tieu
- Sau khi giam doc duyet xong PR, he thong tao san 1 yeu cau cap phat
- Yeu cau nay chua cho ke toan duyet ngay, phai cho nhan hang xong
- Khi nhan hang du, yeu cau chuyen sang cho ke toan duyet

## Nhom 1 - Tao yeu cau cap phat sau duyet PR

TC01 - Tao yeu cau cap phat tu dong
- Dieu kien: PR dang cho giam doc duyet, co danh sach tai san hop le
- Buoc:
  1) Truong phong tao PR - purchase request - yêu cầu mua
  2) Giam doc duyet buoc cuoi
- Mong doi:
  - He thong tao 1 yeu cau cap phat
  - Danh sach tai san trong yeu cau cap phat giong voi PR
  - Trang thai ban dau la cho nhan hang

TC02 - PR khong co tai san hop le
- Dieu kien: PR khong co dong tai san hop le
- Buoc: giam doc duyet PR
- Mong doi: he thong khong tao yeu cau cap phat

TC03 - Khong tao trung
- Dieu kien: PR da co yeu cau cap phat
- Buoc: kiem tra lai sau khi duyet
- Mong doi: moi PR chi co 1 yeu cau cap phat

## Nhom 2 - Bien nhan hang va mo buoc ke toan

TC04 - Nhan hang mot phan
- Dieu kien: da co yeu cau cap phat o trang thai cho nhan hang
- Buoc: ke toan tao bien nhan mot phan
- Mong doi:
  - Yeu cau cap phat van o trang thai cho nhan hang
  - Chua mo cho ke toan duyet cap phat

TC05 - Nhan hang day du
- Dieu kien: da co yeu cau cap phat cho PR
- Buoc: ke toan tao bien nhan den khi du so luong
- Mong doi: yeu cau cap phat chuyen sang trang thai cho ke toan duyet

TC06 - Don mua doc lap
- Dieu kien: don mua khong di tu PR
- Buoc: nhan hang day du cho don mua doc lap
- Mong doi: khong tac dong den yeu cau cap phat cua luong PR

## Nhom 3 - Duyet cap phat

TC07 - Chua nhan du hang thi khong duyet
- Dieu kien: yeu cau dang cho nhan hang
- Buoc: ke toan thu duyet cap phat
- Mong doi:
  - He thong khong cho duyet
  - Bao loi chua den buoc duyet

TC08 - Da nhan du hang thi duyet duoc
- Dieu kien: yeu cau da sang buoc cho ke toan duyet
- Buoc: ke toan duyet yeu cau cap phat
- Mong doi:
  - Duyet thanh cong
  - Tao don cap phat

TC09 - So luong khong du de cap phat
- Dieu kien: so luong can cap phat lon hon so luong thuc te
- Buoc: ke toan duyet yeu cau
- Mong doi:
  - He thong chan, khong cho duyet
  - Bao loi thieu so luong

## Nhom 4 - Kiem tra giao dien

TC10 - Hien thi dung trang thai
- Buoc: mo danh sach yeu cau cap phat ngay sau khi PR duyet
- Mong doi: hien thi trang thai cho nhan hang

TC11 - Trang thai cho nhan hang thi khong co nut duyet
- Buoc: ke toan mo chi tiet yeu cau dang cho nhan hang
- Mong doi: khong hien nut Duyet va Tu choi

TC12 - Nhan du hang xong thi co nut duyet
- Buoc:
  1) Hoan tat nhan hang
  2) Mo lai chi tiet yeu cau
- Mong doi: hien nut Duyet va Tu choi cho ke toan
# Test cases — Luồng PR duyệt → YC cấp phát tự sinh → Biên nhận → Kế toán

Tài liệu mô tả các kịch bản kiểm thử cho luồng: **giám đốc duyệt xong PR** → hệ thống tạo **một yêu cầu cấp phát** (`Status = 5` chờ nhận hàng) → **biên nhận đủ** (PO liên kết PR) → chuyển **`5 → 0`** (chờ kế toán) → kế toán duyệt (kiểm tra tồn kho, tạo đơn cấp phát).

Tham chiếu code: `DirectorApproveController`, `PurchaseLinkedAllocationRequestService`, `GoodsReceiptsController`, `AllocationOrderWorkflow`, `AccountantApproveController`.

---

## Nhóm A — Tạo yêu cầu cấp phát sau duyệt PR

| ID | Tiêu đề | Điều kiện đầu | Bước thực hiện | Kết quả mong đợi |
|----|----------|----------------|----------------|-------------------|
| **A1** | PR có dòng đã gắn catalog `AssetId`, giám đốc duyệt bước cuối | PR `status = 1` (chờ giám đốc), có ≥ 1 `AssetRequestPurchaseLine` với `AssetId` hợp lệ; `CreatedBy` gắn nhân viên có `DepartmentId`; chưa có `AssetRequest` cấp phát với `SourcePurchaseRequestId` = id PR | Gọi API duyệt giám đốc (`POST .../director/{id}/approve`) | PR `status = 2`; tồn tại đúng **một** `AssetRequest` loại cấp phát với `SourcePurchaseRequestId = PR.Id`, `Status = 5`, `ProposedData` có `departmentId` = phòng ban người tạo PR, các dòng `assetId` / `quantity` khớp gom theo catalog từ các dòng PR có `AssetId` |
| **A2** | PR không có dòng nào có `AssetId` | PR ở trạng thái chờ giám đốc; mọi dòng mua `AssetId` null | Duyệt giám đốc | **Không** tạo `AssetRequest` cấp phát có `SourcePurchaseRequestId` = PR (hoặc không có bản ghi thỏa điều kiện liên kết) |
| **A3** | Idempotent — không tạo trùng | Đã có sẵn 1 yêu cầu cấp phát `SourcePurchaseRequestId` = PR | Gọi lại logic tạo từ PR (nếu có API/script) hoặc đảm bảo không có đường duyệt PR lần 2 cùng id | Không thêm bản ghi cấp phát thứ 2 cho cùng PR |
| **A4** | Tạo YC không phụ thuộc tồn kho tại thời điểm duyệt PR | Ngay sau duyệt PR, kho **chưa** có đủ instance theo số lượng PR | Thực hiện A1 | YC cấp phát **vẫn được tạo** (`Status = 5`), không báo lỗi “vượt quá tồn kho” ở bước tạo |

---

## Nhóm B — Biên nhận hàng và chuyển trạng thái 5 → 0

| ID | Tiêu đề | Điều kiện đầu | Bước thực hiện | Kết quả mong đợi |
|----|----------|----------------|----------------|-------------------|
| **B1** | Nhận một phần — chưa mở kế toán | Đã có YC cấp phát `Status = 5`, `SourcePurchaseRequestId` = PR; `Procurement` gắn `AssetRequestId` = PR; còn dòng PO chưa nhận đủ | Tạo biên nhận **một phần** (`POST /api/goods-receipts`) | `Procurement.Status` ≠ 3 (ví dụ 0 hoặc 1); YC cấp phát **vẫn `Status = 5`** |
| **B2** | Nhận đủ — mở kế toán | Sau B1 hoặc setup PO một lần nhận đủ | Biên nhận làm mọi dòng PO `ReceivedQuantity >= Quantity` | `Procurement.Status = 3`; YC cấp phát chuyển **`Status = 0`**; có thông báo bước duyệt (notify kế toán) nếu cấu hình notify hoạt động |
| **B3** | PO không gắn PR | `Procurement.AssetRequestId` null (đơn mua độc lập) | Nhận đủ hàng | Không có YC cấp phát PR nào chuyển `5 → 0` do luồng này |
| **B4** | Nhiều lần biên nhận tới khi đủ | PO nhiều dòng, nhận qua nhiều biên nhận | Các lần nhận trung gian + lần cuối đủ số | Chỉ sau **lần biên nhận làm PO đạt đủ** thì YC chuyển `5 → 0` |

---

## Nhóm C — Kế toán và tồn kho

| ID | Tiêu đề | Điều kiện đầu | Bước thực hiện | Kết quả mong đợi |
|----|----------|----------------|----------------|-------------------|
| **C1** | Không duyệt khi còn chờ nhận hàng | YC cấp phát `Status = 5` | Kế toán `POST .../accountant/{id}/approve` | **400** / thông báo không ở trạng thái chờ kế toán (chỉ `Status = 0` mới duyệt) |
| **C2** | Duyệt sau khi đủ hàng và đủ tồn | Sau B2; số instance trong kho (chưa gán phòng ban) ≥ số lượng trong `ProposedData` | Kế toán approve | Thành công; có `AssetAllocationOrder`; YC theo luồng hiện tại (ví dụ `Status = 2` sau duyệt kế toán) |
| **C3** | Thiếu tồn kho so với snapshot YC | `ProposedData` yêu cầu số lượng lớn hơn tồn kho thực tế sau nhận hàng | Kế toán approve | **Thất bại**, message kiểu vượt quá tồn kho (validation tồn kho bật khi duyệt kế toán) |

---

## Nhóm D — UI (kiểm thử thủ công / E2E)

| ID | Tiêu đề | Bước | Kết quả mong đợi |
|----|----------|------|------------------|
| **D1** | Ban xem danh sách cấp phát | Sau A1, mở màn yêu cầu cấp phát theo phòng ban | Hiển thị trạng thái **«Chờ nhận hàng (PR)»** (map `Status = 5`) |
| **D2** | Kế toán không thao tác duyệt khi đang 5 | YC `Status = 5`, mở modal kế toán | **Không** hiện nút Duyệt / Từ chối (chỉ khi `Status = 0`) |
| **D3** | Sau khi nhận đủ | Sau B2, kế toán mở lại danh sách / modal | Có nút Duyệt / Từ chối; nhãn trạng thái phù hợp “Chờ duyệt” / chờ kế toán |

---

## Gợi ý tự động hóa (tùy chọn)

- **Integration**: WebApplicationFactory + DB test; seed PR + lines + workflow; gọi director approve → assert DB; gọi goods receipt đến PO completed → assert `Status` YC; gọi accountant approve → assert `AssetAllocationOrder`.
- **Unit**: `PurchaseLinkedAllocationRequestService.TryPromoteAwaitingGoodsReceiptForProcurementAsync` với `Procurement.Status` và danh sách `AssetRequest` giả lập.

---

## Hằng số tham chiếu (backend)

| Ý nghĩa | Giá trị |
|---------|---------|
| YC cấp phát chờ kế toán | `AllocationOrderWorkflow.RequestStatusPendingAccountant` = **0** |
| YC cấp phát chờ nhận hàng (PR) | `AllocationOrderWorkflow.RequestStatusAwaitingGoodsReceipt` = **5** |
| PO đã nhận đủ | `PurchaseOrdersController.StatusCompleted` = **3** |

# Đề xuất cải tiến Module Hóa đơn nhà cung cấp

## Tổng quan các thay đổi đã thực hiện

### 1. Cải thiện giao diện form tạo hóa đơn
- ✅ Sử dụng UI/UX hiện đại từ MarkDamagedAssetModal
- ✅ Bố cục rõ ràng với các section được phân tách
- ✅ Màu sắc nhất quán với hệ thống (#FE3720 cho primary actions)
- ✅ Validation form với thông báo lỗi rõ ràng
- ✅ Responsive design với scrollbar tùy chỉnh

### 2. Cải thiện bảng danh sách hóa đơn
- ✅ Sử dụng bố cục table từ RequestsPage
- ✅ Pagination tùy chỉnh với footer hiển thị thông tin rõ ràng
- ✅ Filters inline với search, supplier, date range
- ✅ Status pills với màu sắc phân biệt rõ ràng

### 3. Loại bỏ các cột liên quan đến Hợp đồng
- ✅ Đã loại bỏ cột "Hợp đồng" khỏi bảng danh sách
- ✅ Tập trung vào thông tin hóa đơn cốt lõi

### 4. Thêm chức năng chọn nhà cung cấp
- ✅ Dropdown chọn nhà cung cấp trong form
- ✅ Auto-fill nhà cung cấp khi chọn đơn mua/biên nhận
- ✅ Validation bắt buộc nhập nhà cung cấp

## Đề xuất bổ sung dựa trên quy trình SAP

### 1. Validation và Data Quality (Ưu tiên cao)

#### 1.1 Three-Way Matching
**Mô tả**: Kiểm tra khớp dữ liệu giữa 3 chứng từ: Purchase Order, Goods Receipt, và Supplier Invoice

**Triển khai đề xuất**:
```typescript
interface ThreeWayMatchResult {
  matched: boolean;
  discrepancies: {
    field: string;
    poValue: number;
    grValue: number;
    invoiceValue: number;
    tolerance: number;
  }[];
}

// Kiểm tra tolerance cho số lượng và giá
const QUANTITY_TOLERANCE = 0.05; // 5%
const PRICE_TOLERANCE = 0.02; // 2%
```

**Lợi ích**:
- Phát hiện sai sót về số lượng, giá cả
- Giảm rủi ro thanh toán sai
- Tăng độ tin cậy dữ liệu

#### 1.2 Duplicate Invoice Detection
**Mô tả**: Kiểm tra hóa đơn trùng lặp dựa trên số hóa đơn và nhà cung cấp

**Triển khai đề xuất**:
- Kiểm tra trước khi lưu: `invoiceNumber + supplierId`
- Cảnh báo nếu phát hiện trùng trong 90 ngày gần nhất
- Cho phép override với lý do (ví dụ: hóa đơn điều chỉnh)

### 2. Workflow và Approval (Ưu tiên trung bình)

#### 2.1 Invoice Approval Workflow
**Mô tả**: Quy trình phê duyệt hóa đơn trước khi thanh toán

**Các bước đề xuất**:
1. **Kế toán tạo hóa đơn** → Status: "Chờ xác nhận"
2. **Trưởng phòng kế toán xác nhận** → Status: "Đã xác nhận"
3. **Giám đốc phê duyệt** (nếu giá trị > ngưỡng) → Status: "Đã phê duyệt"
4. **Sẵn sàng thanh toán** → Status: "Chờ thanh toán"

**Ngưỡng phê duyệt đề xuất**:
- < 10 triệu: Trưởng phòng kế toán
- 10-50 triệu: Giám đốc
- > 50 triệu: Giám đốc + Hội đồng quản trị

#### 2.2 Exception Handling
**Mô tả**: Xử lý các trường hợp ngoại lệ

**Các trường hợp**:
- Hóa đơn không khớp với đơn mua (quantity/price mismatch)
- Hóa đơn không có biên nhận hàng
- Hóa đơn quá hạn thanh toán
- Hóa đơn từ nhà cung cấp bị blacklist

### 3. Payment Terms và Due Date Management (Ưu tiên trung bình)

#### 3.1 Payment Terms
**Mô tả**: Quản lý điều khoản thanh toán

**Thêm fields**:
```typescript
interface SupplierInvoice {
  // ... existing fields
  paymentTerms: string; // "NET30", "NET60", "COD", etc.
  dueDate: string; // Tự động tính dựa trên invoiceDate + paymentTerms
  discountTerms?: string; // "2/10 NET30" (2% discount if paid within 10 days)
  earlyPaymentDiscountDate?: string;
  earlyPaymentDiscountAmount?: number;
}
```

**Lợi ích**:
- Tối ưu hóa cash flow
- Tận dụng early payment discount
- Tránh phạt trả chậm

#### 3.2 Payment Status Tracking
**Thêm trạng thái thanh toán**:
- "Chưa thanh toán"
- "Đã lên lịch thanh toán"
- "Đang xử lý"
- "Đã thanh toán"
- "Thanh toán một phần"
- "Quá hạn"

### 4. Tax và Compliance (Ưu tiên cao)

#### 4.1 VAT Handling
**Mô tả**: Quản lý thuế VAT chi tiết

**Thêm fields**:
```typescript
interface SupplierInvoiceLine {
  // ... existing fields
  taxCode: string; // "VAT10", "VAT8", "VAT0", "EXEMPT"
  taxRate: number; // 0.10, 0.08, 0.00
  taxAmount: number;
  netAmount: number; // Trước thuế
  grossAmount: number; // Sau thuế
}

interface SupplierInvoice {
  // ... existing fields
  totalNetAmount: number;
  totalTaxAmount: number;
  totalGrossAmount: number;
  taxInvoiceNumber?: string; // Số hóa đơn VAT
  taxInvoiceDate?: string;
}
```

#### 4.2 E-Invoice Integration
**Mô tả**: Tích hợp với hệ thống hóa đơn điện tử

**Chức năng đề xuất**:
- Import hóa đơn điện tử từ file XML
- Validate với cơ quan thuế
- Lưu trữ chứng từ điện tử
- Báo cáo thuế tự động

### 5. Reporting và Analytics (Ưu tiên thấp)

#### 5.1 Invoice Aging Report
**Mô tả**: Báo cáo công nợ theo thời gian

**Các cột**:
- Nhà cung cấp
- Số hóa đơn
- Ngày hóa đơn
- Ngày đến hạn
- Số ngày quá hạn
- Số tiền
- Trạng thái

#### 5.2 Supplier Performance Metrics
**Mô tả**: Đánh giá hiệu suất nhà cung cấp

**Metrics**:
- On-time delivery rate
- Invoice accuracy rate (% hóa đơn không có lỗi)
- Average payment cycle
- Discount utilization rate

#### 5.3 Cash Flow Forecasting
**Mô tả**: Dự báo dòng tiền

**Chức năng**:
- Hiển thị hóa đơn cần thanh toán theo tuần/tháng
- Tổng số tiền cần thanh toán
- Cảnh báo thiếu hụt tiền mặt

### 6. Document Management (Ưu tiên trung bình)

#### 6.1 Attachment Support
**Mô tả**: Đính kèm file hóa đơn gốc

**Chức năng**:
- Upload PDF/image của hóa đơn
- Upload chứng từ liên quan (delivery note, packing list)
- Preview file trong modal
- Download file

#### 6.2 Audit Trail
**Mô tả**: Lịch sử thay đổi hóa đơn

**Thông tin lưu**:
- Người thực hiện
- Thời gian
- Hành động (tạo, sửa, hủy, phê duyệt)
- Giá trị trước/sau thay đổi

### 7. Integration Points (Ưu tiên thấp)

#### 7.1 Accounting System Integration
**Mô tả**: Tích hợp với hệ thống kế toán

**Chức năng**:
- Tự động tạo bút toán kế toán
- Đồng bộ với sổ cái
- Export dữ liệu sang phần mềm kế toán (MISA, Fast, etc.)

#### 7.2 Payment Gateway Integration
**Mô tả**: Tích hợp với cổng thanh toán

**Chức năng**:
- Thanh toán trực tuyến cho nhà cung cấp
- Tracking payment status
- Reconciliation tự động

## Roadmap triển khai đề xuất

### Phase 1: Foundation (1-2 tuần)
- ✅ UI/UX improvements (Đã hoàn thành)
- Three-way matching validation
- Duplicate invoice detection
- Tax handling (VAT)

### Phase 2: Workflow (2-3 tuần)
- Approval workflow
- Payment terms management
- Payment status tracking
- Exception handling

### Phase 3: Compliance (2-3 tuần)
- E-invoice integration
- Tax reporting
- Audit trail
- Document management

### Phase 4: Analytics (1-2 tuần)
- Invoice aging report
- Supplier performance metrics
- Cash flow forecasting

### Phase 5: Integration (2-4 tuần)
- Accounting system integration
- Payment gateway integration

## Tài liệu tham khảo

1. **SAP Best Practices for Invoice Management**
   - Validate extracted data against master records
   - Use certified integrations
   - Automate validation and exception handling
   - Prioritize straight-through processing

2. **Global E-Invoicing Standards (2026)**
   - Electronic invoicing mandates in multiple jurisdictions
   - Localization support for 41 countries
   - Integration with government portals

3. **Standard Invoice Components**
   - Unique invoice number and dates
   - Complete seller/buyer details
   - Detailed line items with quantities and prices
   - Tax amounts and totals
   - Supporting references (PO, delivery confirmations)

## Kết luận

Module Hóa đơn nhà cung cấp đã được cải thiện đáng kể về mặt UI/UX và chức năng cơ bản. Các đề xuất bổ sung trên dựa trên best practices từ SAP và các hệ thống ERP hàng đầu, giúp:

1. **Tăng độ chính xác**: Three-way matching, duplicate detection
2. **Cải thiện kiểm soát**: Approval workflow, audit trail
3. **Tối ưu tài chính**: Payment terms, early payment discount
4. **Đảm bảo tuân thủ**: VAT handling, e-invoice integration
5. **Hỗ trợ quyết định**: Reporting và analytics

Triển khai theo roadmap từng phase sẽ giúp đảm bảo chất lượng và giảm thiểu rủi ro.

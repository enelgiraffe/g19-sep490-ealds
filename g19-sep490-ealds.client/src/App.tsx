import { BrowserRouter, Routes, Route } from 'react-router-dom';
import { AppLayout } from './shared/layouts/AppLayout';
import { HomePlaceholder } from './shared/layouts/HomePlaceholder';
import { ModulePlaceholder } from './shared/layouts/ModulePlaceholder';
import { DirectorDashboardPage } from './modules/dashboard/pages/DirectorDashboardPage';
import { ApprovalDetailPage } from './modules/dashboard/pages/ApprovalDetailPage';
import { NotificationsPage } from './modules/notifications/pages/NotificationsPage';
import { LoginPage } from './modules/auth/pages/LoginPage';
import { ForgotPasswordPage } from './modules/auth/pages/ForgotPasswordPage';
import { VerifyOTPPage } from './modules/auth/pages/VerifyOTPPage';
import { AssetListPage } from './modules/assets/pages/AssetListPage';
import { AssetDetailPage } from './modules/assets/pages/AssetDetailPage';
import { ProfilePage } from './modules/profile/pages/ProfilePage';
import { PurchaseOrdersPage } from './modules/purchase-orders/pages/PurchaseOrdersPage';
import { AccountantAssetListPage } from './modules/accountant/pages/AccountantAssetListPage';
import './App.css';

function App() {
  return (
    <BrowserRouter>
      <Routes>
        {/* Asset detail – full page, không dùng AppLayout */}
        <Route path="/assets/:id" element={<AssetDetailPage />} />

        <Route path="/login" element={<LoginPage />} />
        <Route path="/forgot-password" element={<ForgotPasswordPage />} />
        <Route path="/verify-otp" element={<VerifyOTPPage />} />
        <Route path="/" element={<AppLayout />}>
          <Route index element={<HomePlaceholder />} />
          {/* Common */}
          <Route path="notifications" element={<NotificationsPage />} />
          <Route path="profile" element={<ProfilePage />} />
          {/* Department head, Accountant, Director */}
          <Route path="assets" element={<AssetListPage />} />
          <Route path="accountant-assets" element={<AccountantAssetListPage />} />
          <Route path="purchase-orders" element={<PurchaseOrdersPage />} />
          <Route path="transfers" element={<ModulePlaceholder title="Điều chuyển" />} />
          <Route path="repairs" element={<ModulePlaceholder title="Sửa chữa" />} />
          <Route path="maintenance" element={<ModulePlaceholder title="Bảo trì" />} />
          <Route path="liquidation" element={<ModulePlaceholder title="Thanh lý" />} />
          <Route path="allocations" element={<ModulePlaceholder title="Cấp phát-Thu hồi" />} />
          <Route path="cost-recording" element={<ModulePlaceholder title="Ghi nhận chi phí" />} />
          <Route path="requests" element={<ModulePlaceholder title="Yêu cầu" />} />
          <Route path="dashboard" element={<DirectorDashboardPage />} />
          <Route path="approval-detail/:id" element={<ApprovalDetailPage />} />
          <Route path="reports" element={<ModulePlaceholder title="Báo cáo" />} />
          {/* Admin */}
          <Route path="users" element={<ModulePlaceholder title="Người dùng" />} />
          <Route path="roles" element={<ModulePlaceholder title="Vai trò" />} />
          <Route path="departments" element={<ModulePlaceholder title="Phòng ban" />} />
          <Route path="categories" element={<ModulePlaceholder title="Danh mục" />} />
          <Route path="approval-workflows" element={<ModulePlaceholder title="Quy trình phê duyệt" />} />
          <Route path="extended-fields" element={<ModulePlaceholder title="Trường mở rộng" />} />
          <Route path="system-settings" element={<ModulePlaceholder title="Cấu hình hệ thống" />} />
        </Route>
      </Routes>
    </BrowserRouter>
  );
}

export default App;

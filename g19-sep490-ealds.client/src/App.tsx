import { BrowserRouter, Routes, Route } from 'react-router-dom';
import { AppLayout } from './shared/layouts/AppLayout';
import { HomePlaceholder } from './shared/layouts/HomePlaceholder';
import { ModulePlaceholder } from './shared/layouts/ModulePlaceholder';
import { DirectorDashboardPage } from './modules/dashboard/pages/DirectorDashboardPage';
import { ApprovalDetailPage } from './modules/dashboard/pages/ApprovalDetailPage';
import { NotificationsPage } from './modules/notifications/pages/NotificationsPage';
import { LoginPage } from './modules/auth/pages/LoginPage';
import { ForgotPasswordPage } from './modules/auth/pages/ForgotPasswordPage';
import { ResetPasswordPage } from './modules/auth/pages/ResetPasswordPage';
import { VerifyOTPPage } from './modules/auth/pages/VerifyOTPPage';
import { ProtectedRoute } from './modules/auth/components/ProtectedRoute';
import { AssetListPage } from './modules/assets/pages/AssetListPage';
import { AssetDetailPage } from './modules/assets/pages/AssetDetailPage';
import { AssetInstanceDetailPage } from './modules/assets/pages/AssetInstanceDetailPage';
import { AssetInstanceEditPage } from './modules/assets/pages/AssetInstanceEditPage';
import { AssetCreatePage } from './modules/assets/pages/AssetCreatePage';
import { AssetEditPage } from './modules/assets/pages/AssetEditPage';
import { ProfilePage } from './modules/profile/pages/ProfilePage';
import { PurchaseOrdersPage } from './modules/purchase-orders/pages/PurchaseOrdersPage';
import { TransfersPage } from './modules/transfers/pages/TransfersPage';
import { AccountantAssetListPage } from './modules/accountant/pages/AccountantAssetListPage';
import { RepairsPage } from './modules/repairs/pages/RepairsPage';
import { MaintenancePage } from './modules/maintenance/pages/MaintenancePage';
import { InventoryPage } from './modules/inventory/pages/InventoryPage';
import { PeriodicInventoryExecutionPage } from './modules/inventory/pages/PeriodicInventoryExecutionPage';
import { DirectorInventoryPage } from './modules/inventory/pages/DirectorInventoryPage';
import { InventoryReviewPage } from './modules/inventory/pages/InventoryReviewPage';
import { LiquidationPage } from './modules/liquidation/pages/LiquidationPage';
import { RequestsPage } from './modules/requests/pages/RequestsPage';
import { CategoriesPage } from './modules/admin/pages/CategoriesPage';
import { DepartmentsPage } from './modules/admin/pages/DepartmentsPage';
import { UsersPage } from './modules/admin/pages/UsersPage';
import { UserDetailPage } from './modules/admin/pages/UserDetailPage';
import { CostRecordingPage } from './modules/cost-recording/pages/CostRecordingPage';
import { AccountantAllocationsPage } from './modules/allocations/pages/AccountantAllocationsPage';
import { DepartmentAllocationRequestsPage } from './modules/allocations/pages/DepartmentAllocationRequestsPage';
import { AllocationOrderDetailPage } from './modules/allocations/pages/AllocationOrderDetailPage';
import './App.css';

function App() {
  return (
    <BrowserRouter>
      <Routes>
        {/* Asset detail – full page, không dùng AppLayout */}
        <Route path="/assets/:id" element={<AssetDetailPage />} />
        <Route path="/asset-instances/:instanceId" element={<AssetInstanceDetailPage />} />
        <Route
          path="/asset-instances/:instanceId/edit"
          element={
            <ProtectedRoute>
              <AssetInstanceEditPage />
            </ProtectedRoute>
          }
        />
        {/* Asset create – full page, không dùng AppLayout */}
        <Route
          path="/assets/new"
          element={
            <ProtectedRoute>
              <AssetCreatePage />
            </ProtectedRoute>
          }
        />
        {/* Asset edit – full page, không dùng AppLayout */}
        <Route
          path="/assets/:id/edit"
          element={
            <ProtectedRoute>
              <AssetEditPage />
            </ProtectedRoute>
          }
        />

        <Route path="/login" element={<LoginPage />} />
        <Route path="/forgot-password" element={<ForgotPasswordPage />} />
        <Route path="/reset-password" element={<ResetPasswordPage />} />
        <Route path="/verify-otp" element={<VerifyOTPPage />} />
        <Route path="/" element={<ProtectedRoute><AppLayout /></ProtectedRoute>}>
          <Route index element={<HomePlaceholder />} />
          {/* Common */}
          <Route path="notifications" element={<NotificationsPage />} />
          <Route path="profile" element={<ProfilePage />} />
          {/* Department head, Accountant, Director */}
          <Route path="assets" element={<AssetListPage />} />
          <Route path="accountant-assets" element={<AccountantAssetListPage />} />
          <Route path="purchase-orders" element={<PurchaseOrdersPage />} />
          <Route path="transfers" element={<TransfersPage />} />
          <Route path="repairs" element={<RepairsPage />} />
          <Route path="maintenance" element={<MaintenancePage />} />
          <Route path="inventory" element={<InventoryPage />} />
          <Route path="inventory/:sessionId" element={<PeriodicInventoryExecutionPage />} />
          <Route path="inventory-review/:sessionId" element={<InventoryReviewPage />} />
          <Route path="reports" element={<DirectorInventoryPage />} />
          <Route path="liquidation" element={<LiquidationPage />} />
          <Route path="allocations" element={<AccountantAllocationsPage />} />
          <Route path="allocations/order/:orderId" element={<AllocationOrderDetailPage />} />
          <Route path="allocations/handover-order/:orderId" element={<AllocationOrderDetailPage />} />
          <Route path="allocation-requests" element={<DepartmentAllocationRequestsPage />} />
          <Route path="cost-recording" element={<CostRecordingPage />} />
          <Route path="requests" element={<RequestsPage />} />
          <Route path="dashboard" element={<DirectorDashboardPage />} />
          <Route path="approval-detail/:id" element={<ApprovalDetailPage />} />
          {/* Admin */}
          <Route path="users" element={<UsersPage />} />
          <Route path="users/:id" element={<UserDetailPage />} />
          <Route path="roles" element={<ModulePlaceholder title="Vai trò" />} />
          <Route path="departments" element={<DepartmentsPage />} />
          <Route path="categories" element={<CategoriesPage />} />
          <Route path="approval-workflows" element={<ModulePlaceholder title="Quy trình phê duyệt" />} />
          <Route path="extended-fields" element={<ModulePlaceholder title="Trường mở rộng" />} />
          <Route path="system-settings" element={<ModulePlaceholder title="Cấu hình hệ thống" />} />
        </Route>
      </Routes>
    </BrowserRouter>
  );
}

export default App;

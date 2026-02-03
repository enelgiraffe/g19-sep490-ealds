import { KPICards } from '../components/KPICards';
import { PendingApprovalsTable } from '../components/PendingApprovalsTable';
import { AssetStatusChart } from '../components/AssetStatusChart';
import { MOCK_KPI, MOCK_PENDING_APPROVALS, MOCK_ASSET_STATUS } from '../data/dashboardMockData';

export function DirectorDashboardPage() {
  return (
    <div>
      <h1 style={{ marginBottom: 24, fontSize: 20 }}>Dashboard – Giám đốc</h1>
      <KPICards data={MOCK_KPI} />
      <PendingApprovalsTable dataSource={MOCK_PENDING_APPROVALS} />
      <AssetStatusChart data={MOCK_ASSET_STATUS} />
    </div>
  );
}

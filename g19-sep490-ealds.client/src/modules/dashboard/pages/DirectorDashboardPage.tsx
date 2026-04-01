import { useEffect, useState } from 'react';
import { Alert, Spin } from 'antd';
import { KPICards } from '../components/KPICards';
import { PendingApprovalsTable } from '../components/PendingApprovalsTable';
import { AssetStatusChart } from '../components/AssetStatusChart';
import { directorDashboardService } from '../services/directorDashboardService';
import type { AssetStatusItem, KPISummary, PendingApprovalRow } from '../types/dashboard.types';

const emptyKpi: KPISummary = {
  totalAssets: 0,
  totalAssetValue: 0,
  pendingApprovals: 0,
  assetsDueMaintenance: 0,
};

export function DirectorDashboardPage() {
  const [kpi, setKpi] = useState<KPISummary>(emptyKpi);
  const [pendingRows, setPendingRows] = useState<PendingApprovalRow[]>([]);
  const [assetStatus, setAssetStatus] = useState<AssetStatusItem[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    let cancelled = false;
    (async () => {
      setLoading(true);
      setError(null);
      try {
        const data = await directorDashboardService.getSummary();
        if (cancelled) return;
        setKpi(data.kpi);
        setPendingRows(data.pendingApprovals);
        setAssetStatus(data.assetStatus);
      } catch {
        if (!cancelled) {
          setError('Không thể tải dữ liệu dashboard. Vui lòng thử lại sau.');
          setKpi(emptyKpi);
          setPendingRows([]);
          setAssetStatus([]);
        }
      } finally {
        if (!cancelled) setLoading(false);
      }
    })();
    return () => {
      cancelled = true;
    };
  }, []);

  return (
    <div>
      <h1 style={{ marginBottom: 24, fontSize: 20 }}>Dashboard – Giám đốc</h1>
      {error && (
        <Alert type="error" message={error} showIcon style={{ marginBottom: 16 }} />
      )}
      <Spin spinning={loading}>
        <KPICards data={kpi} />
        <PendingApprovalsTable dataSource={pendingRows} maxRows={6} />
        <AssetStatusChart data={assetStatus} />
      </Spin>
    </div>
  );
}

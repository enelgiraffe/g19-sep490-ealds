import { Card } from 'antd';
import type { KPISummary } from '../types/dashboard.types';
import './KPICards.css';

interface KPICardsProps {
  data: KPISummary;
}

const cards = [
  { key: 'totalAssets', title: 'Tổng số tài sản', valueKey: 'totalAssets' as const, suffix: '' },
  { key: 'totalAssetValue', title: 'Tổng giá trị tài sản (ước tính)', valueKey: 'totalAssetValue' as const, suffix: ' tỷ' },
  { key: 'pendingApprovals', title: 'Yêu cầu chờ phê duyệt', valueKey: 'pendingApprovals' as const, suffix: '' },
  { key: 'assetsDueMaintenance', title: 'Tài sản sắp bảo trì / kiểm kê', valueKey: 'assetsDueMaintenance' as const, suffix: '' },
];

function formatCardValue(valueKey: keyof KPISummary, raw: number): string {
  if (valueKey === 'totalAssetValue') {
    return raw.toLocaleString('vi-VN', { minimumFractionDigits: 0, maximumFractionDigits: 2 });
  }
  return Math.round(raw).toLocaleString('vi-VN');
}

export function KPICards({ data }: KPICardsProps) {
  return (
    <div className="kpi-cards">
      {cards.map(({ key, title, valueKey, suffix }) => (
        <Card key={key} className="kpi-cards__card" size="small">
          <div className="kpi-cards__title">{title}</div>
          <div className="kpi-cards__value">
            {formatCardValue(valueKey, data[valueKey])}
            {suffix}
          </div>
        </Card>
      ))}
    </div>
  );
}

import { Empty } from 'antd';
import { PieChart, Pie, Cell, ResponsiveContainer, Legend, Tooltip } from 'recharts';
import type { AssetStatusItem } from '../types/dashboard.types';
import './AssetStatusChart.css';

interface AssetStatusChartProps {
  data: AssetStatusItem[];
}

export function AssetStatusChart({ data }: AssetStatusChartProps) {
  const total = data.reduce((s, x) => s + x.value, 0);

  return (
    <div className="asset-status-chart">
      <h3 className="asset-status-chart__title">Trạng thái tài sản (Asset Overview)</h3>
      <div className="asset-status-chart__wrapper">
        {total === 0 ? (
          <Empty description="Chưa có dữ liệu tài sản" style={{ padding: '48px 0' }} />
        ) : (
        <ResponsiveContainer width="100%" height={280}>
          <PieChart>
            <Pie
              data={data}
              dataKey="value"
              nameKey="name"
              cx="50%"
              cy="50%"
              outerRadius={100}
              label={({ name, percent }) => `${name} ${(((percent ?? 0) as number) * 100).toFixed(0)}%`}
            >
              {data.map((entry, index) => (
                <Cell key={`cell-${index}`} fill={entry.color ?? '#1677ff'} />
              ))}
            </Pie>
            <Tooltip formatter={(value) => [Number(value ?? 0), 'Số lượng']} />
            <Legend />
          </PieChart>
        </ResponsiveContainer>
        )}
      </div>
    </div>
  );
}

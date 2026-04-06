import { Tabs } from 'antd';
import { AllocationOrdersPanel } from './AllocationOrdersPanel';
import '../../requests/pages/RequestsPage.css';

export function AccountantAllocationsPage() {
  return (
    <div className="requests-page">
      <div className="requests-header">
        <h1 className="requests-title">Đơn cấp phát &amp; hoàn trả</h1>
      </div>

      <div className="requests-card">
        <Tabs
          defaultActiveKey="allocation"
          className="requests-tabs"
          items={[
            {
              key: 'allocation',
              label: 'Đơn cấp phát',
              children: <AllocationOrdersPanel kind="allocation" />,
            },
            {
              key: 'handover',
              label: 'Đơn hoàn trả',
              children: <AllocationOrdersPanel kind="handover" />,
            },
          ]}
        />
      </div>
    </div>
  );
}

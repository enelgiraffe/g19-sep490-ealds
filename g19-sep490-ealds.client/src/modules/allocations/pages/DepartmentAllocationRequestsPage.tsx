import { Tabs } from 'antd';
import { DepartmentRequestsByModePanel } from './DepartmentRequestsByModePanel';
import '../../requests/pages/RequestsPage.css';

export function DepartmentAllocationRequestsPage() {
  return (
    <div className="requests-page">
      <div className="requests-header">
        <h1 className="requests-title">Cấp phát &amp; hoàn trả tài sản</h1>
      </div>

      <div className="requests-card">
        <Tabs
          defaultActiveKey="allocation"
          className="requests-tabs"
          items={[
            {
              key: 'allocation',
              label: 'Cấp phát',
              children: <DepartmentRequestsByModePanel mode="allocation" />,
            },
            {
              key: 'handover',
              label: 'Hoàn trả',
              children: <DepartmentRequestsByModePanel mode="handover" />,
            },
          ]}
        />
      </div>
    </div>
  );
}

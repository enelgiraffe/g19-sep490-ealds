import { Tabs } from 'antd';
import { DepartmentRequestsByModePanel } from './DepartmentRequestsByModePanel';
import '../../requests/pages/RequestsPage.css';

export function DepartmentAllocationRequestsPage() {
  return (
    <div className="requests-page">
      <div className="requests-header">
        <h1 className="requests-title">Cấp phát &amp; thu hồi tài sản</h1>
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
              label: 'Thu hồi',
              children: <DepartmentRequestsByModePanel mode="handover" />,
            },
          ]}
        />
      </div>
    </div>
  );
}

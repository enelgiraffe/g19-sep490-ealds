import { Table, Button } from 'antd';
import type { ColumnsType } from 'antd/es/table';
import { useNavigate } from 'react-router-dom';
import type { PendingApprovalRow } from '../types/dashboard.types';

interface PendingApprovalsTableProps {
  dataSource: PendingApprovalRow[];
}

export function PendingApprovalsTable({ dataSource }: PendingApprovalsTableProps) {
  const navigate = useNavigate();

  const columns: ColumnsType<PendingApprovalRow> = [
    { title: 'Loại yêu cầu', dataIndex: 'requestType', key: 'requestType', width: 140 },
    { title: 'Phòng ban', dataIndex: 'department', key: 'department', width: 120 },
    { title: 'Ngày', dataIndex: 'date', key: 'date', width: 100 },
    { title: 'Trạng thái', dataIndex: 'status', key: 'status', width: 100 },
    {
      title: 'Action',
      key: 'action',
      width: 100,
      render: (_, record) => (
        <Button type="link" size="small" onClick={() => navigate(`/approval-detail/${record.id}`)}>
          View
        </Button>
      ),
    },
  ];

  return (
    <div style={{ marginBottom: 24 }}>
      <h3 style={{ marginBottom: 12, fontSize: 16 }}>Yêu cầu chờ phê duyệt (Pending Approvals)</h3>
      <Table
        dataSource={dataSource}
        columns={columns}
        rowKey="id"
        pagination={false}
        size="small"
      />
    </div>
  );
}

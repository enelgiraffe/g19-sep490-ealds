import { useParams, useNavigate } from 'react-router-dom';
import { Button } from 'antd';

export function ApprovalDetailPage() {
  const { id } = useParams<{ id: string }>();
  const navigate = useNavigate();

  return (
    <div>
      <h1>Chi tiết phê duyệt (Approval Detail)</h1>
      <p>ID yêu cầu: {id}</p>
      <p>Nội dung chi tiết sẽ được triển khai khi ghép API.</p>
      <Button type="primary" onClick={() => navigate('/dashboard')}>
        Quay lại Dashboard
      </Button>
    </div>
  );
}

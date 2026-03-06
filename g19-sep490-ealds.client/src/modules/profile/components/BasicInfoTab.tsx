import { Form, Input, DatePicker, Button } from 'antd';
import dayjs from 'dayjs';
import './BasicInfoTab.css';

export function BasicInfoTab() {
  const [form] = Form.useForm();

  const handleUpdate = (values: any) => {
    console.log('Update profile:', values);
  };

  return (
    <div className="basic-info-tab">
      <div className="basic-info-tab__header">
        <Button type="primary" icon={<span>✏️</span>} className="basic-info-tab__edit-btn">
          Chỉnh sửa
        </Button>
      </div>

      <Form
        form={form}
        layout="vertical"
        onFinish={handleUpdate}
        className="basic-info-form"
        initialValues={{
          email: 'hoangdzai@gmail.com',
          fullName: 'Bùi Huy Hoàng',
          birthday: dayjs('2004-01-02'),
          position: 'Trưởng phòng công nghệ',
          employeeCode: 'HE181928',
          department: 'Phòng Công nghệ thông tin',
        }}
      >
        <div className="basic-info-form__row">
          <Form.Item label="Email" name="email" className="basic-info-form__item">
            <Input placeholder="Nhập email" disabled />
          </Form.Item>
          <Form.Item label="Vị trí công việc" name="position" className="basic-info-form__item">
            <Input placeholder="Nhập vị trí công việc" disabled />
          </Form.Item>
        </div>

        <div className="basic-info-form__row">
          <Form.Item label="Tên đầy đủ" name="fullName" className="basic-info-form__item">
            <Input placeholder="Nhập tên đầy đủ" disabled />
          </Form.Item>
          <Form.Item label="Mã nhân viên" name="employeeCode" className="basic-info-form__item">
            <Input placeholder="Nhập mã nhân viên" disabled />
          </Form.Item>
        </div>

        <div className="basic-info-form__row">
          <Form.Item label="Ngày tháng năm sinh" name="birthday" className="basic-info-form__item">
            <DatePicker 
              style={{ width: '100%' }} 
              format="DD/MM/YYYY" 
              placeholder="Chọn ngày sinh"
              disabled
            />
          </Form.Item>
          <Form.Item label="Phòng ban" name="department" className="basic-info-form__item">
            <Input placeholder="Nhập phòng ban" disabled />
          </Form.Item>
        </div>
      </Form>
    </div>
  );
}

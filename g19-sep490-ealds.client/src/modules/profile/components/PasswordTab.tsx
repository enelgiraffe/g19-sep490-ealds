import { Form, Input, Button } from 'antd';
import { useState } from 'react';
import './PasswordTab.css';

export function PasswordTab() {
  const [form] = Form.useForm();
  const [showCurrentPassword, setShowCurrentPassword] = useState(false);
  const [showNewPassword, setShowNewPassword] = useState(false);
  const [showConfirmPassword, setShowConfirmPassword] = useState(false);

  const handleUpdatePassword = (values: any) => {
    console.log('Update password:', values);
  };

  return (
    <div className="password-tab">
      <Form
        form={form}
        layout="vertical"
        onFinish={handleUpdatePassword}
        className="password-form"
      >
        <Form.Item
          label="Mật khẩu hiện tại"
          name="currentPassword"
          rules={[
            { required: true, message: 'Vui lòng nhập mật khẩu hiện tại' }
          ]}
        >
          <Input.Password
            placeholder="Nhập mật khẩu hiện tại"
            visibilityToggle={{
              visible: showCurrentPassword,
              onVisibleChange: setShowCurrentPassword
            }}
          />
        </Form.Item>

        <Form.Item
          label="Nhập mật khẩu mới"
          name="newPassword"
          rules={[
            { required: true, message: 'Vui lòng nhập mật khẩu mới' },
            { min: 6, message: 'Mật khẩu yếu, vui lòng cập nhật lại. Mật khẩu cần có ít nhất 6 ký tự, đặc biệt, từ viết hoa' }
          ]}
          validateStatus="error"
          help="Mật khẩu yếu, vui lòng cập nhật lại. Mật khẩu cần có ít nhất 6 ký tự, đặc biệt, từ viết hoa"
        >
          <Input.Password
            placeholder="Nhập mật khẩu mới"
            visibilityToggle={{
              visible: showNewPassword,
              onVisibleChange: setShowNewPassword
            }}
          />
        </Form.Item>

        <Form.Item
          label="Nhập lại mật khẩu mới"
          name="confirmPassword"
          dependencies={['newPassword']}
          validateStatus="error"
          help="Mật khẩu mới đang không trùng nhau"
          rules={[
            { required: true, message: 'Vui lòng nhập lại mật khẩu mới' },
            ({ getFieldValue }) => ({
              validator(_, value) {
                if (!value || getFieldValue('newPassword') === value) {
                  return Promise.resolve();
                }
                return Promise.reject(new Error('Mật khẩu mới đang không trùng nhau'));
              },
            }),
          ]}
        >
          <Input.Password
            placeholder="Nhập lại mật khẩu mới"
            visibilityToggle={{
              visible: showConfirmPassword,
              onVisibleChange: setShowConfirmPassword
            }}
          />
        </Form.Item>

        <Form.Item>
          <Button type="primary" htmlType="submit" className="password-form__submit-btn">
            Cập nhật
          </Button>
        </Form.Item>
      </Form>
    </div>
  );
}

import { Form, Input, Button, message } from 'antd';
import { useState } from 'react';
import { profileService } from '../services/profileService';
import './PasswordTab.css';

export function PasswordTab() {
  const [form] = Form.useForm();
  const [showCurrentPassword, setShowCurrentPassword] = useState(false);
  const [showNewPassword, setShowNewPassword] = useState(false);
  const [showConfirmPassword, setShowConfirmPassword] = useState(false);
  const [submitting, setSubmitting] = useState(false);

  const handleUpdatePassword = async (values: any) => {
    try {
      setSubmitting(true);
      await profileService.changePassword({
        currentPassword: values.currentPassword,
        newPassword: values.newPassword,
        confirmNewPassword: values.confirmPassword,
      });

      message.success('Đổi mật khẩu thành công.');
      form.resetFields();
    } catch (error: any) {
      const errorMessage =
        error?.response?.data?.message ||
        'Đổi mật khẩu thất bại. Vui lòng thử lại.';
      message.error(errorMessage);
      throw error;
    } finally {
      setSubmitting(false);
    }
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
          dependencies={['currentPassword']}
          rules={[
            { required: true, message: 'Vui lòng nhập mật khẩu mới' },
            { min: 8, message: 'Mật khẩu phải có ít nhất 8 ký tự.' },
            {
              pattern: /^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[^a-zA-Z\d]).{8,}$/,
              message: 'Mật khẩu phải bao gồm chữ hoa, chữ thường, số và ký tự đặc biệt.',
            },
            ({ getFieldValue }) => ({
              validator(_, value) {
                const current = getFieldValue('currentPassword') as string | undefined;
                if (!value || !current || value !== current) {
                  return Promise.resolve();
                }
                return Promise.reject(
                  new Error('Mật khẩu mới không được trùng với mật khẩu hiện tại.'),
                );
              },
            }),
          ]}
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
          <Button
            type="primary"
            htmlType="submit"
            className="password-form__submit-btn"
            loading={submitting}
          >
            Cập nhật
          </Button>
        </Form.Item>
      </Form>
    </div>
  );
}

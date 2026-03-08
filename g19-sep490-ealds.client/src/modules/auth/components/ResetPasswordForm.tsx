import { Form, Input, Button } from 'antd';
import { Link, useSearchParams } from 'react-router-dom';
import { LeftOutlined, LockOutlined, EyeInvisibleOutlined, EyeTwoTone } from '@ant-design/icons';
import { useResetPassword } from '../hooks/useResetPassword';
import logoImg from '/images/logoCompany.png';
import './ResetPasswordForm.css';

export const ResetPasswordForm = () => {
  const [searchParams] = useSearchParams();
  const token = searchParams.get('token') ?? '';
  const { resetPassword, loading } = useResetPassword();

  const onFinish = async (values: { newPassword: string; confirmNewPassword: string }) => {
    await resetPassword(token, values.newPassword, values.confirmNewPassword);
  };

  if (!token) {
    return (
      <div className="reset-password-form-container">
        <div className="reset-password-logo">
          <img src={logoImg} alt="Logo" className="logo-icon" />
          <div className="logo-text">SAKURA HÀ MINH</div>
        </div>
        <h1 className="reset-password-title">Liên kết không hợp lệ</h1>
        <p className="reset-password-description">
          Thiếu token đặt lại mật khẩu. Vui lòng sử dụng link trong email hoặc yêu cầu gửi lại.
        </p>
        <div className="back-to-login-item">
          <Link to="/login" className="back-to-login-link">
            <LeftOutlined /> Quay lại đăng nhập
          </Link>
        </div>
      </div>
    );
  }

  return (
    <div className="reset-password-form-container">
      <div className="reset-password-logo">
        <img src={logoImg} alt="Sakura Hà Minh Logo" className="logo-icon" />
        <div className="logo-text">SAKURA HÀ MINH</div>
      </div>

      <h1 className="reset-password-title">Đặt lại mật khẩu</h1>
      <p className="reset-password-description">
        Nhập mật khẩu mới (ít nhất 6 ký tự) và xác nhận.
      </p>

      <Form
        name="reset-password"
        onFinish={onFinish}
        autoComplete="off"
        layout="vertical"
        className="reset-password-form"
      >
        <Form.Item
          name="newPassword"
          label="Mật khẩu mới"
          rules={[
            { required: true, message: 'Vui lòng nhập mật khẩu mới.' },
            { min: 6, message: 'Mật khẩu phải có ít nhất 6 ký tự.' },
          ]}
        >
          <Input.Password
            size="large"
            placeholder="Nhập mật khẩu mới"
            className="reset-password-input"
            prefix={<LockOutlined />}
            iconRender={(visible) =>
              visible ? <EyeTwoTone /> : <EyeInvisibleOutlined />
            }
          />
        </Form.Item>

        <Form.Item
          name="confirmNewPassword"
          label="Xác nhận mật khẩu"
          dependencies={['newPassword']}
          rules={[
            { required: true, message: 'Vui lòng xác nhận mật khẩu.' },
            ({ getFieldValue }) => ({
              validator(_, value) {
                if (!value || getFieldValue('newPassword') === value) {
                  return Promise.resolve();
                }
                return Promise.reject(new Error('Hai mật khẩu không khớp.'));
              },
            }),
          ]}
        >
          <Input.Password
            size="large"
            placeholder="Nhập lại mật khẩu mới"
            className="reset-password-input"
            prefix={<LockOutlined />}
            iconRender={(visible) =>
              visible ? <EyeTwoTone /> : <EyeInvisibleOutlined />
            }
          />
        </Form.Item>

        <Form.Item>
          <Button
            type="primary"
            htmlType="submit"
            size="large"
            loading={loading}
            className="reset-password-button"
            block
          >
            Đặt lại mật khẩu
          </Button>
        </Form.Item>

        <Form.Item className="back-to-login-item">
          <Link to="/login" className="back-to-login-link">
            <LeftOutlined /> Quay lại đăng nhập
          </Link>
        </Form.Item>
      </Form>
    </div>
  );
};

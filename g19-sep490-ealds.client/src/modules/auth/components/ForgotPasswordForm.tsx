import { Form, Input, Button, Alert } from 'antd';
import { Link } from 'react-router-dom';
import { LeftOutlined } from '@ant-design/icons';
import { useForgotPassword } from '../hooks/useForgotPassword';
import logoImg from '/images/logoCompany.png';
import './ForgotPasswordForm.css';

export const ForgotPasswordForm = () => {
  const { sendRequest, loading, error, success, successMessage } = useForgotPassword();

  const onFinish = async (values: { email: string }) => {
    await sendRequest((values.email ?? '').trim());
  };

  return (
    <div className="forgot-password-form-container">
      <div className="forgot-password-logo">
        <img src={logoImg} alt="Sakura Hà Minh Logo" className="logo-icon" />
        <div className="logo-text">SAKURA HÀ MINH</div>
      </div>

      <h1 className="forgot-password-title">Quên mật khẩu?</h1>
      
      <p className="forgot-password-description">
        Vui lòng nhập Email tài khoản
      </p>

      {error && (
        <Alert
          message={error}
          type="error"
          showIcon
          closable
          className="forgot-password-alert"
        />
      )}

      {success && (
        <Alert
          message={successMessage}
          type="success"
          showIcon
          className="forgot-password-alert"
        />
      )}

      <Form
        name="forgot-password"
        onFinish={onFinish}
        autoComplete="off"
        layout="vertical"
        className="forgot-password-form"
      >
        <Form.Item
          name="email"
          normalize={(v) => (typeof v === 'string' ? v.trim() : v)}
          rules={[
            { required: true, message: 'Vui lòng nhập email!' },
            { type: 'email', message: 'Địa chỉ email không hợp lệ. Vui lòng kiểm tra lại.' },
          ]}
        >
          <Input
            size="large"
            placeholder="Nhập email của bạn"
            className="forgot-password-input"
            autoComplete="email"
          />
        </Form.Item>

        <Form.Item>
          <Button
            type="primary"
            htmlType="submit"
            size="large"
            loading={loading}
            className="forgot-password-button"
            block
          >
            Gửi yêu cầu
          </Button>
        </Form.Item>

        <Form.Item className="back-to-login-item l">
          <Link to="/login" className="back-to-login-link">
            <LeftOutlined /> Quay lại
          </Link>
        </Form.Item>
      </Form>
    </div>
  );
};

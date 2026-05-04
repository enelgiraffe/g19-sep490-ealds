import { useEffect } from 'react';
import { Form, Input, Button } from 'antd';
import { LockOutlined, EyeInvisibleOutlined, EyeTwoTone } from '@ant-design/icons';
import { Link, useNavigate } from 'react-router-dom';
import { useLogin } from '../hooks/useLogin';
import type { LoginFormData } from '../types/auth.types';
import { getDefaultLandingPath, mapBackendRoleToAppRole } from '../types/auth.types';
import logoImg from '/images/logoCompany.png';
import './LoginForm.css';

export const LoginForm = () => {
  const { login, loading } = useLogin();
  const navigate = useNavigate();

  useEffect(() => {
    const accessToken = localStorage.getItem('accessToken');
    if (!accessToken) return;

    const storedUser = localStorage.getItem('user');

    if (storedUser) {
      try {
        const user = JSON.parse(storedUser);
        const appRole = mapBackendRoleToAppRole(user.role);
        navigate(getDefaultLandingPath(appRole), { replace: true });
        return;
      } catch {
        // ignore parse error and fallback to default redirect below
      }
    }

    navigate('/dashboard', { replace: true });
  }, [navigate]);

  const onFinish = async (values: LoginFormData) => {
    await login(values);// ← Gọi function login từ useLogin hook
    
  };

  return (
    <div className="login-form-container">
      <div className="login-logo">
        <img src={logoImg} alt="Sakura Hà Minh Logo" className="logo-icon" />
      </div>

      <h1 className="login-title">Đăng nhập</h1>

      <Form
        name="login"
        onFinish={onFinish}
        autoComplete="off"
        layout="vertical"
        className="login-form"
        initialValues={{
          email: '_',
        }}
      >
        <Form.Item
          label="Email"
          name="email"
          rules={[
            { required: true, message: 'Vui lòng nhập email!' },
            { type: 'email', message: 'Email không hợp lệ!' },
          ]}
        >
          <Input
            size="large"
            placeholder="Nhập email của bạn"
            className="login-input"
          />
        </Form.Item>

        <Form.Item
          label="Mật khẩu"
          name="password"
          rules={[{ required: true, message: 'Vui lòng nhập mật khẩu!' }]}
        >
          <Input.Password
            size="large"
            placeholder="Nhập mật khẩu"
            className="login-input"
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
            className="login-button"
            block
          >
            Đăng nhập
          </Button>
        </Form.Item>

        <Form.Item className="forgot-password-item">
          <Link to="/forgot-password" className="forgot-password-link">
            <LockOutlined /> Quên mật khẩu ?
          </Link>
        </Form.Item>
      </Form>
    </div>
  );
};

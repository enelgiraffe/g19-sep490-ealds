import { useState } from 'react';
import { Form, Button, Input } from 'antd';
import { Link, useLocation } from 'react-router-dom';
import { LeftOutlined } from '@ant-design/icons';
import { useVerifyOTP } from '../hooks/useVerifyOTP';
import logoImg from '/images/logoCompany.png';
import './VerifyOTPForm.css';

export const VerifyOTPForm = () => {
  const location = useLocation();
  const email = location.state?.email || '';
  const { verifyOTP, loading } = useVerifyOTP();
  const [otp, setOtp] = useState(['', '', '', '', '', '']);

  const handleOtpChange = (index: number, value: string) => {
    if (value.length > 1) {
      value = value.slice(-1);
    }

    const newOtp = [...otp];
    newOtp[index] = value;
    setOtp(newOtp);

    // Auto focus next input
    if (value && index < 5) {
      const nextInput = document.getElementById(`otp-input-${index + 1}`);
      nextInput?.focus();
    }
  };

  const handleKeyDown = (index: number, e: React.KeyboardEvent<HTMLInputElement>) => {
    if (e.key === 'Backspace' && !otp[index] && index > 0) {
      const prevInput = document.getElementById(`otp-input-${index - 1}`);
      prevInput?.focus();
    }
  };

  const onFinish = async () => {
    const otpCode = otp.join('');
    await verifyOTP(email, otpCode);
  };

  return (
    <div className="verify-otp-form-container">
      <div className="verify-otp-logo">
        <img src={logoImg} alt="Sakura Hà Minh Logo" className="logo-icon" />
        <div className="logo-text">SAKURA HÀ MINH</div>
      </div>

      <h1 className="verify-otp-title">Nhận OTP</h1>
      
      <p className="verify-otp-description">
        Mã OTP được gửi tới mail <strong>{email}</strong>
      </p>

      <Form
        name="verify-otp"
        onFinish={onFinish}
        className="verify-otp-form"
      >
        <Form.Item label="Nhập mã OTP" className="otp-form-item">
          <div className="otp-input-group">
            {otp.map((digit, index) => (
              <Input
                key={index}
                id={`otp-input-${index}`}
                type="text"
                maxLength={1}
                value={digit}
                onChange={(e) => handleOtpChange(index, e.target.value)}
                onKeyDown={(e) => handleKeyDown(index, e)}
                className="otp-input"
                size="large"
              />
            ))}
          </div>
        </Form.Item>

        <Form.Item>
          <Button
            type="primary"
            htmlType="submit"
            size="large"
            loading={loading}
            className="verify-otp-button"
            block
            disabled={otp.some(digit => !digit)}
          >
            Xác nhận
          </Button>
        </Form.Item>

        <Form.Item className="resend-otp-item">
          <span className="resend-otp-text">Bạn chưa nhận được mã OTP? </span>
          <Link to="/forgot-password" className="resend-otp-link">
            Gửi lại
          </Link>
        </Form.Item>

        <Form.Item className="back-to-login-item">
          <Link to="/login" className="back-to-login-link">
            <LeftOutlined /> Quay lại
          </Link>
        </Form.Item>
      </Form>
    </div>
  );
};

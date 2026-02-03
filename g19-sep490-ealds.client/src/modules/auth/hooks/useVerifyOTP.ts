import { useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { message } from 'antd';
import { authService } from '../services/authService';

export const useVerifyOTP = () => {
  const [loading, setLoading] = useState(false);
  const navigate = useNavigate();

  const verifyOTP = async (email: string, otp: string) => {
    try {
      setLoading(true);

      await authService.verifyOTP(email, otp);
      
      message.success('Xác thực OTP thành công!');
      
      // Navigate to reset password page
      setTimeout(() => {
        navigate('/reset-password', { state: { email, otp } });
      }, 1000);
    } catch (err: any) {
      const errorMessage = err.response?.data?.message || 'Mã OTP không hợp lệ. Vui lòng thử lại.';
      message.error(errorMessage);
    } finally {
      setLoading(false);
    }
  };

  return { verifyOTP, loading };
};

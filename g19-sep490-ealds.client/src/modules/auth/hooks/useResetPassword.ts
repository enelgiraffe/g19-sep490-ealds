import { useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { message } from 'antd';
import { authService } from '../services/authService';

export const useResetPassword = () => {
  const [loading, setLoading] = useState(false);
  const navigate = useNavigate();

  const resetPassword = async (
    token: string,
    newPassword: string,
    confirmNewPassword: string
  ) => {
    try {
      setLoading(true);
      const data = await authService.resetPassword(token, newPassword, confirmNewPassword);
      message.success(data.message ?? 'Mật khẩu đã được đặt lại thành công.');
      navigate('/login', { replace: true });
    } catch (err: any) {
      const errorMessage =
        err.response?.data?.message || 'Token không hợp lệ hoặc đã hết hạn. Vui lòng thử lại.';
      message.error(errorMessage);
      throw err;
    } finally {
      setLoading(false);
    }
  };

  return { resetPassword, loading };
};

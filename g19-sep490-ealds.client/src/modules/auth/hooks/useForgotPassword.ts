import { useState } from 'react';
import { authService } from '../services/authService';

export const useForgotPassword = () => {
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [success, setSuccess] = useState(false);
  const [successMessage, setSuccessMessage] = useState<string>('');

  const sendRequest = async (email: string) => {
    try {
      setLoading(true);
      setError(null);
      setSuccess(false);
      setSuccessMessage('');

      const data = await authService.forgotPassword(email);
      setSuccess(true);
      setSuccessMessage(data.message ?? 'Nếu email tồn tại trong hệ thống, bạn sẽ nhận được hướng dẫn đặt lại mật khẩu.');
    } catch (err: any) {
      const data = err.response?.data;
      const errorMessage =
        data?.message ||
        (data?.errors?.Email && Array.isArray(data.errors.Email) ? data.errors.Email[0] : null) ||
        (err.response?.status === 500 ? 'Gửi email thất bại. Vui lòng thử lại sau.' : null) ||
        (err.response?.status ? 'Yêu cầu thất bại. Vui lòng thử lại.' : 'Lỗi kết nối. Vui lòng kiểm tra mạng và thử lại.');
      setError(errorMessage);
      setSuccess(false);
    } finally {
      setLoading(false);
    }
  };

  return { sendRequest, loading, error, success, successMessage };
};

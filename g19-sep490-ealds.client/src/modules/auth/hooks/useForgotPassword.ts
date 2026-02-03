import { useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { authService } from '../services/authService';

export const useForgotPassword = () => {
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [success, setSuccess] = useState(false);
  const navigate = useNavigate();

  const sendOTP = async (email: string) => {
    try {
      setLoading(true);
      setError(null);
      setSuccess(false);

      await authService.forgotPassword(email);
      
      setSuccess(true);
      
      // Navigate to OTP verification page after 1.5 seconds
      setTimeout(() => {
        navigate('/verify-otp', { state: { email } });
      }, 1500);
    } catch (err: any) {
      const errorMessage = err.response?.data?.message || 'Địa chỉ email không hợp lệ. Vui lòng kiểm tra lại.';
      setError(errorMessage);
      setSuccess(false);
    } finally {
      setLoading(false);
    }
  };

  return { sendOTP, loading, error, success };
};

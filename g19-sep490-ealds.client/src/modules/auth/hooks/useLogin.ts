import { useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { message } from 'antd';
import { authService } from '../services/authService';
import type { LoginFormData } from '../types/auth.types';

export const useLogin = () => {
  const [loading, setLoading] = useState(false);
  const navigate = useNavigate();

  const login = async (values: LoginFormData) => {
    try {
      setLoading(true);
      const response = await authService.login(values);
      
      // Store tokens (you can use localStorage or a state management solution)
      localStorage.setItem('accessToken', response.accessToken);
      if (response.refreshToken) {
        localStorage.setItem('refreshToken', response.refreshToken);
      }
      localStorage.setItem('user', JSON.stringify(response.user));

      message.success('Đăng nhập thành công!');
      
      // Navigate to dashboard or home page
      navigate('/dashboard');
    } catch (error: any) {
      const errorMessage = error.response?.data?.message || 'Đăng nhập thất bại. Vui lòng thử lại.';
      message.error(errorMessage);
      throw error;
    } finally {
      setLoading(false);
    }
  };

  return { login, loading };
};

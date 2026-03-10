import { useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { message } from 'antd';
import { useAppStore } from '../../../stores/appStore';
import { authService } from '../services/authService';
import type { LoginFormData } from '../types/auth.types';
import { mapBackendRoleToAppRole } from '../types/auth.types';

export const useLogin = () => {
  const [loading, setLoading] = useState(false);
  const navigate = useNavigate();
  const setCurrentRole = useAppStore((s) => s.setCurrentRole);

  const login = async (values: LoginFormData) => {
    try {
      setLoading(true);
      const response = await authService.login(values);

      localStorage.setItem('accessToken', response.accessToken);
      if (response.refreshToken) {
        localStorage.setItem('refreshToken', response.refreshToken);
      }
      localStorage.setItem('user', JSON.stringify(response.user));

      const appRole = mapBackendRoleToAppRole(response.user.role);
      setCurrentRole(appRole);

      message.success('Đăng nhập thành công!');

      if (appRole === 'department_head') {
        navigate('/assets');
      } else if (appRole === 'accountant') {
        navigate('/accountant-assets');
      } else {
        navigate('/dashboard');
      }
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

import { useEffect } from 'react';
import { Outlet } from 'react-router-dom';
import { useAppStore } from '../../stores/appStore';
import { mapBackendRoleToAppRole } from '../../modules/auth/types/auth.types';
import { Sidebar } from './Sidebar';
import './AppLayout.css';

export function AppLayout() {
  const setCurrentRole = useAppStore((s) => s.setCurrentRole);

  useEffect(() => {
    const raw = localStorage.getItem('user');
    if (raw) {
      try {
        const user = JSON.parse(raw);
        setCurrentRole(mapBackendRoleToAppRole(user.role));
      } catch {
        // ignore
      }
    }
  }, [setCurrentRole]);

  return (
    <div className="app-layout">
      <div className="app-layout__sidebar">
        <Sidebar />
      </div>
      <main className="app-layout__main">
        <div className="app-layout__content">
          <Outlet />
        </div>
      </main>
    </div>
  );
}

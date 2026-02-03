import { Outlet } from 'react-router-dom';
import { Sidebar } from './Sidebar';
import './AppLayout.css';

export function AppLayout() {
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

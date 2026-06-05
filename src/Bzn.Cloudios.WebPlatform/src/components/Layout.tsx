import type { ReactNode } from 'react';
import { Link, useLocation } from 'react-router-dom';
import { useAuth } from '../contexts/AuthContext';
import { Breadcrumb } from './Breadcrumb';

interface LayoutProps {
  children: ReactNode;
}

export function Layout({ children }: LayoutProps) {
  const { user, logout } = useAuth();
  const location = useLocation();

  const menuItems = [
    { path: '/dashboard', label: 'Global Dashboard' },
    { path: '/realms', label: 'Realms' },
    { path: '/services', label: 'All Services' },
    { path: '/settings', label: 'Settings' },
  ];

  return (
    <div className="layout-container">
      <aside className="sidebar">
        <div className="sidebar-header">
          <h1>Cloudios Admin</h1>
          <p>Platform Administration</p>
        </div>

        <nav className="sidebar-nav">
          <ul>
            {menuItems.map((item) => (
              <li key={item.path}>
                <Link
                  to={item.path}
                  className={location.pathname === item.path ? 'active' : ''}
                >
                  {item.label}
                </Link>
              </li>
            ))}
          </ul>
        </nav>

        <div className="sidebar-footer">
          <div className="sidebar-footer-content">
            <div className="sidebar-footer-info">
              <p>{user?.email}</p>
              <p>{user?.role}</p>
            </div>
            <button onClick={logout}>Logout</button>
          </div>
        </div>
      </aside>

      <main className="main-content">
        <Breadcrumb />
        {children}
      </main>
    </div>
  );
}

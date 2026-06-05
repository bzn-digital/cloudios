import type { ReactNode } from 'react';
import { useState } from 'react';
import { Link, useLocation } from 'react-router-dom';
import { useAuth } from '../contexts/AuthContext';
import { Breadcrumb } from './Breadcrumb';

interface LayoutProps {
  children: ReactNode;
}

interface MenuItem {
  path: string;
  label: string;
  submenu?: { path: string; label: string }[];
}

export function Layout({ children }: LayoutProps) {
  const { user, logout } = useAuth();
  const location = useLocation();
  const [expandedMenus, setExpandedMenus] = useState<Set<string>>(new Set(['computing']));

  const menuItems: MenuItem[] = [
    { path: '/dashboard', label: 'Global Dashboard' },
    { 
      path: '/computing', 
      label: 'Computing',
      submenu: [
        { path: '/computing/services-templates', label: 'Services Templates' },
        { path: '/computing/containers', label: 'Containers' },
        { path: '/computing/servers', label: 'Servers' },
      ]
    },
    { path: '/realms', label: 'Realms' },
    { path: '/services', label: 'All Services' },
    { path: '/settings', label: 'Settings' },
  ];

  const toggleMenu = (path: string) => {
    const newExpanded = new Set(expandedMenus);
    if (newExpanded.has(path)) {
      newExpanded.delete(path);
    } else {
      newExpanded.add(path);
    }
    setExpandedMenus(newExpanded);
  };

  const isMenuActive = (item: MenuItem): boolean => {
    if (item.submenu) {
      return item.submenu.some(sub => location.pathname === sub.path);
    }
    return location.pathname === item.path;
  };

  const isSubmenuActive = (path: string): boolean => {
    return location.pathname === path;
  };

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
                {item.submenu ? (
                  <>
                    <button
                      onClick={() => toggleMenu(item.path)}
                      className={`sidebar-menu-button ${isMenuActive(item) ? 'active' : ''}`}
                    >
                      <span>{item.label}</span>
                      <span className={`sidebar-arrow ${expandedMenus.has(item.path) ? 'expanded' : ''}`}>
                        ▼
                      </span>
                    </button>
                    {expandedMenus.has(item.path) && (
                      <ul className="sidebar-submenu">
                        {item.submenu.map((sub) => (
                          <li key={sub.path}>
                            <Link
                              to={sub.path}
                              className={isSubmenuActive(sub.path) ? 'active' : ''}
                            >
                              {sub.label}
                            </Link>
                          </li>
                        ))}
                      </ul>
                    )}
                  </>
                ) : (
                  <Link
                    to={item.path}
                    className={location.pathname === item.path ? 'active' : ''}
                  >
                    {item.label}
                  </Link>
                )}
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
